using System;
using System.Collections.Concurrent;

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
        public void Flush()
        {
            while (_queue.TryDequeue(out var job))
            {
                try
                {
                    job.Execute();
                }
                catch (Exception e)
                {
                    Log.Error(this, "JobQueue Error: {Exception}", e);
                }
            }
        }
    }
}