using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SignalR;

namespace Chat {
    public class TracedSignalBus : ISignalBus {
        private ISignalBus _bus;
        private ConcurrentDictionary<Tuple<string, EventHandler<SignaledEventArgs>>, EventHandler<SignaledEventArgs>> _cache = new ConcurrentDictionary<Tuple<string, EventHandler<SignaledEventArgs>>, EventHandler<SignaledEventArgs>>();

        public TracedSignalBus(ISignalBus bus) {
            _bus = bus;
        }

        public void AddHandler(string eventKey, EventHandler<SignaledEventArgs> handler) {
            EventHandler<SignaledEventArgs> traceHandler = (sender, e) => {
                TraceHelper.WriteTrace("Bus", e.EventKey, "Signal Received");
                handler(sender, e);
            };

            if (_cache.TryAdd(Tuple.Create(eventKey, handler), traceHandler)) {
                _bus.AddHandler(eventKey, traceHandler);
            }
        }

        public void RemoveHandler(string eventKey, EventHandler<SignaledEventArgs> handler) {
            EventHandler<SignaledEventArgs> tracedHandler;
            if (_cache.TryRemove(Tuple.Create(eventKey, handler), out tracedHandler)) {
                _bus.RemoveHandler(eventKey, tracedHandler);
            }
        }

        public Task Signal(string eventKey) {
            TraceHelper.WriteTrace("Bus", eventKey, "Signal Sent", eventKey);
            return _bus.Signal(eventKey);
        }
    }
}