using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR;
using System.Linq;
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
            return _store.GetAllSince(key, id).ContinueWith(t => {
                TraceHelper.WriteTrace("Store", key, "GetAllSince({0}) => {1}", id, Serialize(t.Result.Select(m => new { m.Id, m.Value })));
                return t;
            }).Unwrap();
        }

        public Task<long?> GetLastId() {
            TraceHelper.WriteTrace("Store", null, "GetLastId()");
            return _store.GetLastId();
        }

        public Task Save(string key, object value) {
            TraceHelper.WriteTrace("Store", key, "Save({0})", Serialize(value));
            return _store.Save(key, value);
        }

        private string Serialize(object value) {
            return new JavaScriptSerializer().Serialize(value);
        }
    }
}
