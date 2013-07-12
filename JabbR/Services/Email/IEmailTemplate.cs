using System.Collections.Generic;

namespace JabbR.Services
{
    public interface IEmailTemplate
    {
        string From { get; set; }

        string Sender { get; set; }

        ICollection<string> To { get; }

        ICollection<string> ReplyTo { get; }

        ICollection<string> CC { get; }

        ICollection<string> Bcc { get; }

        IDictionary<string, string> Headers { get; }

        string Subject { get; set; }

        string Body { get; }

        void SetModel(dynamic model);

        void Execute();
    }
}