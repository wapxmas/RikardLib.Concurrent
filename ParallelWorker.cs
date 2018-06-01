using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RikardLib.Concurrent
{
    public class ParallelWorker
    {
        private static int nextThreadId = 0;
        private List<WorkerThread> threads;
        private Queue<Action> works = new Queue<Action>();
        private object workLock = new object();

        public ManualResetEventSlim workWait = new ManualResetEventSlim(false);
        private ManualResetEventSlim workDone = new ManualResetEventSlim(true);

        private bool breakSignalled = false, worksFromEvent = false;
        private Action<Exception> log;

        public event Action OnLastWorkEvent;

        public event Func<Action> OnThreadWorkFinish;

        public ParallelWorker(int threadsNum, Action<Exception> log = null)
        {
            threads = new List<WorkerThread>();

            if (threadsNum > 0)
            {
                Enumerable.Range(0, threadsNum).ToList().ForEach(_ => threads.Add(
                    new WorkerThread(Interlocked.Increment(ref nextThreadId), this, log)));
            }

            this.log = log;
        }

        public int ThreadsCount
        {
            get
            {
                return threads.Where(t => !t.IsKilled).Count();
            }
        }

        public void Threads(int num)
        {
            int threadsCount = ThreadsCount;

            if (threadsCount < num)
            {
                Enumerable.Range(0, num - threadsCount).ToList().ForEach(_ =>
                    threads.Add(new WorkerThread(Interlocked.Increment(ref nextThreadId), this, log)));
            }
            else if (threadsCount > num)
            {
                List<WorkerThread> l = threads.Where(t => !t.IsKilled).ToList();

                Enumerable.Range(0, threadsCount - num).ToList().ForEach(i => l[i].Kill());
            }
        }

        public void StartGetWorksFromEvent()
        {
            lock (workLock)
            {
                worksFromEvent = true;

                workWait.Set();
            }
        }

        public void StopGetWorksFromEvent()
        {
            lock (workLock)
            {
                if (!worksFromEvent) return;

                worksFromEvent = false;

                workWait.Reset();
            }
        }

        public void AddWork(Action work)
        {
            lock (workLock)
            {
                works.Enqueue(work);

                if (!breakSignalled)
                {
                    workDone.Reset();

                    workWait.Set();
                }
            }
        }

        public void AddWorksList(List<Action> newWorks)
        {
            lock (workLock)
            {
                newWorks.ForEach((w) => works.Enqueue(w));

                if (!breakSignalled)
                {
                    workDone.Reset();

                    workWait.Set();
                }
            }
        }

        public int WorksCount()
        {
            lock (workLock)
            {
                return works.Count;
            }
        }

        public void AddWorksNoBlock(List<Action> newWorks)
        {
            newWorks.ForEach((w) => works.Enqueue(w));
        }

        public void Reset()
        {
            lock (workLock)
            {
                if (breakSignalled)
                {
                    breakSignalled = false;

                    if (works.Count > 0)
                    {
                        works.Clear();

                        workDone.Reset();

                        workWait.Set();
                    }
                }
            }
        }

        public void Continue()
        {
            lock (workLock)
            {
                if (breakSignalled)
                {
                    breakSignalled = false;

                    if (works.Count > 0)
                    {
                        workDone.Reset();

                        workWait.Set();
                    }
                }
            }
        }

        public void Break()
        {
            lock (workLock)
            {
                breakSignalled = true;
            }
        }

        public Action GetWork()
        {
            Action work = null;

            lock (workLock)
            {
                try
                {
                    if (works.Count > 0 && !breakSignalled)
                    {
                        work = works.Dequeue();

                        if (works.Count == 0 && OnLastWorkEvent != null)
                        {
                            OnLastWorkEvent();
                        }
                    }
                }
                catch (Exception e)
                {
                    log?.Invoke(e);
                }
                finally
                {
                    if (work == null)
                    {
                        workDone.Set();

                        if (!worksFromEvent)
                        {
                            workWait.Reset();
                        }
                    }
                }
            }

            return work;
        }

        public void WaitWorkDone()
        {
            if (ThreadsCount > 0)
            {
                workDone.Wait();
            }

            threads.ForEach((t) => t.WaitWorkDone());
        }

        private class WorkerThread
        {
            private int threadId;
            private Thread thread;
            private ParallelWorker main;
            private volatile bool kill = false;

            private ManualResetEventSlim workDone = new ManualResetEventSlim(true);
            private Action<Exception> log;

            public WorkerThread(int threadId, ParallelWorker main, Action<Exception> log)
            {
                this.threadId = threadId;
                this.main = main;
                this.log = log;
                
                this.thread = new Thread(Work);
                this.thread.IsBackground = true;
                this.thread.Start();
            }

            public void WaitWorkDone()
            {
                workDone.Wait();
            }

            public bool IsKilled
            {
                get
                {
                    return kill;
                }
            }

            public void Kill()
            {
                kill = true;
            }

            private void Work()
            {
                try
                {
                    while (!kill)
                    {
                        main.workWait.Wait();

                        if (kill)
                        {
                            break;
                        }

                        try
                        {
                            workDone.Reset();

                            Action work = main.GetWork();

                            if (work != null)
                            {
                                work();
                            }

                            if (main.OnThreadWorkFinish != null)
                            {
                                while ((work = main.OnThreadWorkFinish()) != null)
                                {
                                    work();
                                }

                                main.StopGetWorksFromEvent();
                            }
                        }
                        catch (Exception e)
                        {
                            log?.Invoke(e);
                        }
                        finally
                        {
                            workDone.Set();
                        }
                    }
                }
                catch (Exception e)
                {
                    log?.Invoke(e);
                }
            }
        }
    }
}
