using System.Security.Principal;
using System.Web;
using JabbR.Models;
using Moq;
using SignalR;
using SignalR.Hubs;
using Xunit;

namespace JabbR.Test
{
    public class ChatTest
    {
        [Fact]
        public void JoinReturnsFalseIfNoCookies()
        {
            var repository = new InMemoryRepository();
            var chat = new Chat(repository);
            var connection = new Mock<IConnection>();
            var prinicipal = new Mock<IPrincipal>();
            var clientState = new TrackingDictionary();
            string clientId = "test";
            chat.Agent = new ClientAgent(connection.Object, "Chat");
            chat.Caller = new SignalAgent(connection.Object, clientId, "Chat", clientState);
            chat.Context = new HubContext(clientId, new HttpCookieCollection(), prinicipal.Object);

            bool result = chat.Join();
            string versionString = typeof(Chat).Assembly.GetName().Version.ToString();

            Assert.Equal(versionString, clientState["version"]);
            Assert.False(result);
        }
    }
}
