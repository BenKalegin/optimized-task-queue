using System;

namespace OptimizedTaskQueue
{
    public interface ILog
    {
        /// <summary>
        /// Some event happened that can be helpful in investigations and should be stored for some days 
        /// </summary>
        Action<string> Info { get; }

        /// <summary>
        /// Some exception occur, but was handled and workaround taken
        /// </summary>
        Action<string> Warning { get; }

        /// <summary>
        /// Unexpected exception or return code encountered
        /// </summary>
        Action<Exception, string> Error { get;  }

        Action<string> Debug { get; }
        Action<string> Perf { get; }
    }
}