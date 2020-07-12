using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptimizedTaskQueue;

namespace TestHeavyWait
{
    class Program
    {
        private static readonly ILog log = new ConsoleLog();
        private static readonly Random random = new Random();
        private static IList<Task> receiveTasks; 
        private static readonly DateTime starTime = DateTime.Now;
        private static ISomeApiService someApiService;

        public static async Task Main(string[] args)
        {
            try
            {
                log.Info("Starting server...");
                var uri = new HttpListener().CreateSocketServer();
                log.Info("Server Started at " + uri);
                someApiService = new SomeApiService(uri, log);
                var abortSignal = new CancellationTokenSource();
                log.Info("Starting listeners...");
                await Start(abortSignal.Token);
            }
            catch (Exception e)
            {
                log.Error(e, "cant initialize");
            }
        }

        private static async Task Start(CancellationToken abortSignal)
        {
            receiveTasks = Enumerable.Range(0, 3).Select(index => DequeueTask(abortSignal)).ToArray();
            var apiTasks = new OptimizedTaskQueue<bool>(log);

            while (!abortSignal.IsCancellationRequested)
            {
                    // don't inflate the list if it is already large 
                try
                {
                    await WaitForAndHandleEvent(abortSignal, apiTasks);
                }
                catch (Exception e)
                {
                    log.Error(e, "Error waiting for next event");
                }
            }
        }

        private static async Task WaitForAndHandleEvent(CancellationToken abortSignal, OptimizedTaskQueue<bool> apiTasks)
        {
            var watchDogCancelToken = new CancellationTokenSource();
            var watchdogCombinedWithAbort = CancellationTokenSource.CreateLinkedTokenSource(abortSignal, watchDogCancelToken.Token);
            using (watchdogCombinedWithAbort)
            using (watchDogCancelToken)
            {
                var waitAndListenForCancelTask = Task.Delay(20000, watchdogCombinedWithAbort.Token);
                var allTasks = new List<Task>();
                var firstApiTask = apiTasks.Peek;
                log.Debug($"awaitable is {firstApiTask?.Id}");
                if (firstApiTask != null)
                    allTasks.Add(firstApiTask);
                var receiveStart = allTasks.Count;
                allTasks.AddRange(receiveTasks);
                var receiveEnd = allTasks.Count;
                allTasks.Add(waitAndListenForCancelTask);

                log.Debug($"Waiting for tasks {string.Join(",", allTasks.Select(t => t.Id))}");

                // TODO make some fault tolerance on exceptions. Best 2-level, first attempt to recover in the code and second with container recycle 
                var task = await Task.WhenAny(allTasks);
                log.Debug($"when any returned task #{task.Id}");

                var index = allTasks.IndexOf(task);
                if (index < 0)
                    throw new Exception("bad index");
                if (index < receiveEnd && index >= receiveStart)
                {
                    // receiver fired
                    receiveTasks[receiveStart - index] = DequeueTask(abortSignal);
                    watchDogCancelToken.Cancel();
                    apiTasks.Enqueue(AddSomeApiCallTasks(abortSignal));
                }
                else if (index == 0)
                {
                    // api fired
                    watchDogCancelToken.Cancel();
                    apiTasks.Dequeue(firstApiTask);
                }
                else
                {
                    // watchdog timer fired - nothing happened last 20 seconds. 
                    return;
                }
            }
        }

        private static IEnumerable<Task<bool>> AddSomeApiCallTasks(CancellationToken abortSignal)
        {
            //if (random.Next(10) > 0)
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
            yield return CallApiAndHandleResult(abortSignal);
        }

        private static async Task<bool> CallApiAndHandleResult(CancellationToken abortSignal)
        {
            await StartNewApiCall(abortSignal);
            return await HandleApiCallEvent(abortSignal);
        }

        private static int QueueTaskNumber;
        private static async Task DequeueTask(CancellationToken abortSignal)
        {
            log.Debug($"Dequeue #{Interlocked.Increment(ref QueueTaskNumber)}");
            await Task.Delay(random.Next(1), abortSignal);
        }

        private static async Task<bool> HandleApiCallEvent(CancellationToken abortSignal)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), abortSignal);
            return true;
        }

        private static int apiCallNumber;

        private static async Task StartNewApiCall(CancellationToken abortSignal)
        {
            await someApiService.Invoke("{}", abortSignal);
            var rate = apiCallNumber / Math.Max((DateTime.Now - starTime).TotalSeconds, 1);
            log.Perf($"invoke  #{Interlocked.Increment(ref apiCallNumber)} rate: {rate}/s");

        }

    }
}
