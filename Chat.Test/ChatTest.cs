using System.Security.Principal;
using System.Web;
using Chat.Models;
using Moq;
using SignalR;
using SignalR.Hubs;
using Xunit;
using ChatHub = SignalR.Samples.Hubs.Chat.Chat;

namespace Chat.Test
{
    public class ChatTest
    {
        [Fact]
        public void JoinReturnsFalseIfNoCookies()
        {
            var repository = new InMemoryRepository();
            var chat = new ChatHub(repository);
            var connection = new Mock<IConnection>();
            var prinicipal = new Mock<IPrincipal>();
            var clientState = new TrackingDictionary();
            string clientId = "test";
            chat.Agent = new ClientAgent(connection.Object, "Chat");
            chat.Caller = new SignalAgent(connection.Object, clientId, "Chat", clientState);
            chat.Context = new HubContext(clientId, new HttpCookieCollection(), prinicipal.Object);

            bool result = chat.Join();
            string versionString = typeof(ChatHub).Assembly.GetName().Version.ToString();

            Assert.Equal(versionString, clientState["version"]);
            Assert.False(result);
        }

        [Fact]
        public void JoinCallsAddUserIfValidUserIdInCookieAndUserList()
        {
            var repository = new InMemoryRepository();
            var user = new ChatUser
            {
                Id = "1234",
                Name = "John",
                Hash = "Hash"
            };
            repository.Add(user);

            var chat = new ChatHub(repository);
            var connection = new Mock<IConnection>();
            var prinicipal = new Mock<IPrincipal>();
            var cookies = new HttpCookieCollection();
            cookies.Add(new HttpCookie("userid", "1234"));
            var clientState = new TrackingDictionary();
            string clientId = "20";
            chat.Agent = new ClientAgent(connection.Object, "Chat");
            chat.Caller = new SignalAgent(connection.Object, clientId, "Chat", clientState);
            chat.Context = new HubContext(clientId, cookies, prinicipal.Object);

            chat.Join();

            Assert.Equal("1234", clientState["id"]);
            Assert.Equal("John", clientState["name"]);
            Assert.Equal("Hash", clientState["hash"]);
            Assert.Equal("20", user.ClientId);
            // Need a better way to verify method name and arguments
            connection.Verify(m => m.Broadcast("Chat.20", It.IsAny<object>()), Times.Once());
        }
    }
}
