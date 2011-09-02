using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR;
using System.Web.Script.Serialization;

namespace Chat {
    public class TracedMessageStore : IMessageStore {
        private IMessageStore _store;

        public TracedMessageStore(IMessageStore store) {
            _store = store;
        }

        public Task<IEnumerable<Message>> GetAll(string key) {
            return _store.GetAll(key);
        }

        public Task<IEnumerable<Message>> GetAllSince(string key, long id) {
            var d = TraceHelper.BeginTrace("Store", key, "GetAllSince({0})", id);
            return _store.GetAllSince(key, id).ContinueWith(t => {
                if (d != null) {
                    d.Dispose();
                }
                return t;
            }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
        }

        public Task<long?> GetLastId() {
            var d = TraceHelper.BeginTrace("Store", null, "GetLastId()");
            return _store.GetLastId().ContinueWith(t => {
                if (d != null) {
                    d.Dispose();
                }
                return t;
            }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
        }

        public Task Save(string key, object value) {
            var d = TraceHelper.BeginTrace("Store", key, "Save({0})", new JavaScriptSerializer().Serialize(value));
            return _store.Save(key, value).ContinueWith(t => {
                if (d != null) {
                    d.Dispose();
                }
                return t;
            }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
        }
    }
}
