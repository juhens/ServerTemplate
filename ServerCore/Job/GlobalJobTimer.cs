using System;
using System.Threading;

namespace ServerCore.Job
{
    public static class GlobalJobTimer
    {
        private struct TimerTask
        {
            public JobSerializer Target;
            public IJob Job;
        }

        private static readonly Util.PriorityQueue<TimerTask, long> Pq = new();
        private static readonly object Lock = new();

        public static void Start()
        {
            var thread = new Thread(ThreadMain) { Name = "GlobalTimerThread" };
            thread.Start();
        }

        private static void ThreadMain()
        {
            while (true)
            {
                var now = Environment.TickCount64;
                TimerTask task;
                var hasTask = false;

                lock (Lock)
                {
                    if (Pq.TryPeek(out task, out var executionTick))
                    {
                        if (executionTick <= now)
                        {
                            Pq.Dequeue();
                            hasTask = true;
                        }
                    }
                }

                if (hasTask)
                {
                    task.Target.Push(task.Job);
                }
                else
                {
                    // 얘도 채널로 빼던지 고정으로 둘지 고민해보자
                    Thread.Sleep(1);
                }
            }
        }

        public static void PushAfter(int tickAfter, JobSerializer target, Action action)
        {
            var executionTick = Environment.TickCount64 + tickAfter;
            var job = Job.Create(action, JobPriority.Normal);

            lock (Lock)
            {
                Pq.Enqueue(new TimerTask { Target = target, Job = job }, executionTick);
            }
        }

        public static void PushAfter<T1>(int tickAfter, JobSerializer target, Action<T1> action, T1 t1)
        {
            var executionTick = Environment.TickCount64 + tickAfter;
            var job = Job<T1>.Create(action, t1, JobPriority.Normal);

            lock (Lock)
            {
                Pq.Enqueue(new TimerTask { Target = target, Job = job }, executionTick);
            }
        }

        public static void PushAfter<T1, T2>(int tickAfter, JobSerializer target, Action<T1, T2> action, T1 t1, T2 t2)
        {
            var executionTick = Environment.TickCount64 + tickAfter;
            var job = Job<T1, T2>.Create(action, t1, t2, JobPriority.Normal);

            lock (Lock)
            {
                Pq.Enqueue(new TimerTask { Target = target, Job = job }, executionTick);
            }
        }

        public static void PushAfter<T1, T2, T3>(int tickAfter, JobSerializer target, Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3)
        {
            var executionTick = Environment.TickCount64 + tickAfter;
            var job = Job<T1, T2, T3>.Create(action, t1, t2, t3, JobPriority.Normal);

            lock (Lock)
            {
                Pq.Enqueue(new TimerTask { Target = target, Job = job }, executionTick);
            }
        }

        public static void PushAfter<T1, T2, T3, T4>(int tickAfter, JobSerializer target, Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            var executionTick = Environment.TickCount64 + tickAfter;
            var job = Job<T1, T2, T3, T4>.Create(action, t1, t2, t3, t4, JobPriority.Normal);

            lock (Lock)
            {
                Pq.Enqueue(new TimerTask { Target = target, Job = job }, executionTick);
            }
        }
    }
}
