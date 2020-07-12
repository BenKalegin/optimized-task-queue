using System;
using System.Linq;
using System.Threading;
using OptimizedTaskQueue;

namespace TestHeavyWait
{
    public class ConsoleLog : ILog
    {
        Action<string> ILog.Info => Console.WriteLine;
        Action<string> ILog.Warning => Console.WriteLine;
        Action<Exception, string> ILog.Error => (e, s) =>
        {
            if (s != null)
                Console.WriteLine(s);
            Console.WriteLine(DumpException(e));
        };


        private int skipCounter = 0;

        Action<string> ILog.Debug => s => { if (DebugAllowed) DebugInternal(s); };
        public Action<string> Perf => DebugInternal;


        public bool DebugAllowed = false;

        private void DebugInternal(string s)
        {
            if (Interlocked.Increment(ref skipCounter) % 100 == 0)
                Console.WriteLine(s);
        }

        private string DumpException(Exception e)
        {
            if (e is AggregateException agg && agg.InnerExceptions.Count == 1)
                return DumpException(agg.InnerExceptions.First());
            return e.ToString();
        }
    }
}