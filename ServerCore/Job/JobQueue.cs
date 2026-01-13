using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ServerCore.Job
{
    public class JobQueue
    {
        private readonly ConcurrentQueue<IJob> _queue = new();
        public bool IsEmpty => _queue.IsEmpty;
        public void Push(IJob job)
        {
            _queue.Enqueue(job);
        }
        public async ValueTask FlushAsync()
        {
            while (_queue.TryDequeue(out var job))
            {
                try
                {
                    await job.ExecuteAsync();
                }
                catch (Exception e)
                {
                    Log.Error(this, "JobQueue Error: {Exception}", e);
                }
            }
        }
    }
}