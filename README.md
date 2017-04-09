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
