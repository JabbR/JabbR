using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JabbR.Services
{
    public abstract class EmailTemplate : IEmailTemplate
    {
        private readonly StringBuilder _buffer;

        [DebuggerStepThrough]
        protected EmailTemplate()
        {
            To = new List<string>();
            ReplyTo = new List<string>();
            CC = new List<string>();
            Bcc = new List<string>();
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            _buffer = new StringBuilder();
        }

        public string From { get; set; }

        public string Sender { get; set; }

        public ICollection<string> To { get; private set; }

        public ICollection<string> ReplyTo { get; private set; }

        public ICollection<string> CC { get; private set; }

        public ICollection<string> Bcc { get; private set; }

        public IDictionary<string, string> Headers { get; private set; }

        public string Subject { get; set; }

        public string Body
        {
            get { return _buffer.ToString(); }
        }

        protected dynamic Model { get; private set; }

        public void SetModel(dynamic model)
        {
            Model = model;
        }

        public abstract void Execute();

        public virtual void Write(object value)
        {
            WriteLiteral(value);
        }

        public virtual void WriteLiteral(object value)
        {
            _buffer.Append(value);
        }
    }
}