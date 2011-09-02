using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Web;
using System.Threading.Tasks;

namespace Chat {
    public class LogInfo {
        public string Category { get; set; }
        public string Signal { get; set; }
        public int? TaskId { get; set; }
        public int ThreadId { get; set; }
        public string Message { get; set; }
        public DateTime When { get; set; }
        public string Path { get; set; }
        public string ClientId { get; set; }
    }

    public static class TraceHelper {
        public static readonly ConcurrentBag<LogInfo> Logs = new ConcurrentBag<LogInfo>();

        public static IDisposable BeginTrace(string cateogry, string signal, string message, params object[] args) {
            var context = HttpContext.Current;
            return new DisposableAction(() => {
                WriteMessage(cateogry, signal, String.Format(message, args), context);
            });
        }

        private class DisposableAction : IDisposable {
            private readonly Action _action;
            public DisposableAction(Action action) {
                _action = action;
            }

            public void Dispose() {
                _action();
            }
        }

        private static void WriteMessage(string cateogry, string eventName, string message, HttpContext context) {
            context = context ?? HttpContext.Current;

            Logs.Add(new LogInfo {
                Signal = eventName,
                Category = cateogry,
                Message = message,
                TaskId = Task.CurrentId,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                When = DateTime.Now,
                Path = context == null ? "No idea" : context.Request.Path,
                ClientId = context == null ? "No idea" : context.Request.Form["clientId"]
            });
        }
    }
}