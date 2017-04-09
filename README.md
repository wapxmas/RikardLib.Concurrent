# RikardLib.Concurrent
Some concurrent helper classes

# Usage of the ParallelCircleQueue&lt;T&gt;
```cs
    class Program
    {
        static void Main(string[] args)
        {
            var pcq = new ParallelCircleQueue<string>(new string[] { "s1", "s2", "s3" }.ToList());

            var tasks = new[]
            {
                Task.Factory.StartNew(() => ExampleTask(pcq)),
                Task.Factory.StartNew(() => ExampleTask(pcq)),
                Task.Factory.StartNew(() => ExampleTask(pcq))
            };

            Task.WaitAll(tasks);
        }

        static void ExampleTask<T>(ParallelCircleQueue<T> pcq)
        {
            foreach(var _ in Enumerable.Range(1, 100))
            {
                using (var p = pcq.GetItem())
                {
                    Console.WriteLine(p.Item);
                }
            }
        }
    }
```

# Usage of the ParallelGatherSingle&lt;T&gt;
```cs
        static void Main(string[] args)
        {
            var pgs = new ParallelGatherSingle<string>(ExampleGatherItems);

            var tasks = new[]
            {
                Task.Factory.StartNew(() => ExampleTask(pgs)),
                Task.Factory.StartNew(() => ExampleTask(pgs)),
                Task.Factory.StartNew(() => ExampleTask(pgs))
            };

            Task.WaitAll(tasks);

            Console.WriteLine("ExampleGatherItems proceedes in the background. Press enter to wait gathering is done.");
            Console.ReadLine();

            pgs.WaitGatherDone();
        }

        private static void ExampleGatherItems(string item)
        {
            Console.WriteLine(item);
            Thread.Sleep(500);
        }

        static void ExampleTask(ParallelGatherSingle<string> pgs)
        {
            var rnd = new Random();

            foreach(var n in Enumerable.Range(1, 10))
            {
                pgs.AddData($"{Thread.CurrentThread.ManagedThreadId} : {n}");
                Thread.Sleep(rnd.Next(100, 300));
            }
        }
    }
```

# Usage of the ParallelWorker
```cs
    class Program
    {
        static void Main(string[] args)
        {
            var pw = new ParallelWorker(threadsNum: 5);

            var tasks = new[]
            {
                Task.Factory.StartNew(() => ExampleTask(pw)),
                Task.Factory.StartNew(() => ExampleTask(pw)),
                Task.Factory.StartNew(() => ExampleTask(pw))
            };

            Task.WaitAll(tasks);

            Console.WriteLine("ParallelWorker proceedes in the background. Press enter to wait all worker threads are done.");
            Console.ReadLine();

            pw.WaitWorkDone();
        }

        static void ExampleTask(ParallelWorker pw)
        {
            var rnd = new Random();

            foreach(var n in Enumerable.Range(1, 100))
            {
                pw.AddWork(() => { Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} : {n}"); Thread.Sleep(1000); });
            }

            Thread.Sleep(3000);
        }
    }
```
