using System.Collections.Generic;

namespace JabbR.Infrastructure
{
    public class AlertMessageStore
    {
        internal const string AlertMessageKey = "__Alerts__";

        public AlertMessageStore()
        {
            Messages = new List<KeyValuePair<string, string>>();
        }

        public ICollection<KeyValuePair<string, string>> Messages { get; private set; }

        public void AddMessage(string messageType, string alertMessage)
        {
            Messages.Add(new KeyValuePair<string, string>(messageType, alertMessage));
        }
    }
}