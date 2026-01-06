using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ServerCore.Job
{
    public interface IJobScheduler
    {
        void Push(JobSerializer jobSerializer);
    }

    public class JobScheduler : IJobScheduler
    {
        private readonly Channel<JobSerializer> _channel = Channel.CreateUnbounded<JobSerializer>();
        public void Push(JobSerializer jobSerializer)
        {
#if DEBUG
            if (JobSerializer.IsExecutorPush == false)
            {
                var currentThread = Thread.CurrentThread.Name ?? "Unknown";
                var msg = $"[Critical] 'JobScheduler.Push' must be called via JobSerializer only! (Thread: {currentThread})";
                Environment.FailFast(msg);
            }
#endif
            _channel.Writer.TryWrite(jobSerializer);
        }
        public void Start(int threadCount)
        {
            for (var i = 0; i < threadCount; i++)
            {
                Task.Factory.StartNew(
                    async () => await ThreadMain(),
                    TaskCreationOptions.LongRunning
                );
            }
        }
        private async Task ThreadMain()
        {
            var reader = _channel.Reader;
            await foreach (var jobSerializer in reader.ReadAllAsync())
            {
                jobSerializer.Execute();
            }
        }
    }
}