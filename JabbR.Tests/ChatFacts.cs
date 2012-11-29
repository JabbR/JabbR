using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using JabbR.ContentProviders.Core;
using JabbR.Models;
using JabbR.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace JabbR.Test
{
    public class ChatFacts
    {
        public class Join
        {
            [Fact]
            public void CannotJoinChat()
            {
                var clientState = new StateChangeTracker();
                string clientId = "1";
                var user = new ChatUser
                {
                    Id = "1234",
                    Name = "John"
                };

                TestableChat chat = GetTestableChat(clientId, clientState, user);
                chat.Clients.Caller.id = "1234";

                bool result = chat.Join();

                Assert.False(result);
            }

            [Fact]
            public void CanJoinChatIfIdentitySet()
            {
                var clientState = new StateChangeTracker();
                string clientId = "1";
                var user = new ChatUser
                {
                    Id = "1234",
                    Name = "John",
                    Identity = "foo"
                };

                TestableChat chat = GetTestableChat(clientId, clientState, user);
                chat.Clients.Caller.id = "1234";

                bool result = chat.Join();

                Assert.Equal("1234", clientState["id"]);
                Assert.Equal("John", clientState["name"]);
                Assert.True(result);
                // TODO: find out why these don't work
                //Assert.Equal(1, user.ConnectedClients.Count);
                //Assert.Equal("1", user.ConnectedClients.First().Id);
            }

            [Fact]
            public void MissingUsernameReturnsFalse()
            {
                var clientState = new StateChangeTracker();
                string clientId = "1";
                var user = new ChatUser();

                TestableChat chat = GetTestableChat(clientId, clientState, user);

                bool result = chat.Join();

                Assert.False(result);
            }

            [Fact]
            public void CanDeserializeClientState()
            {
                var clientState = new StateChangeTracker();
                string clientId = "1";
                var user = new ChatUser
                {
                    Id = "1234",
                    Name = "John",
                    Identity = "foo"
                };

                var cookies = new Dictionary<string, Cookie>();
                var cookie = new Cookie("jabbr.state", JsonConvert.SerializeObject(new ClientState { UserId = user.Id }));
                cookies[cookie.Name] = cookie;


                TestableChat chat = GetTestableChat(clientId, clientState, user, cookies);

                bool result = chat.Join();

                Assert.Equal("1234", clientState["id"]);
                Assert.Equal("John", clientState["name"]);
                Assert.True(result);
            }
        }

        public static TestableChat GetTestableChat(string clientId, StateChangeTracker clientState, ChatUser user)
        {
            return GetTestableChat(clientId, clientState, user, new Dictionary<string, Cookie>());
        }

        public static TestableChat GetTestableChat(string connectionId, StateChangeTracker clientState, ChatUser user, IDictionary<string, Cookie> cookies)
        {
            // setup things needed for chat
            var repository = new InMemoryRepository();
            var resourceProcessor = new Mock<IResourceProcessor>();
            var chatService = new Mock<IChatService>();
            var connection = new Mock<IConnection>();
            var settings = new Mock<IApplicationSettings>();
            var mockPipeline = new Mock<IHubPipelineInvoker>();

            settings.Setup(m => m.AuthApiKey).Returns("key");

            // add user to repository
            repository.Add(user);

            // create testable chat
            var chat = new TestableChat(settings, resourceProcessor, chatService, repository, connection);
            var mockedConnectionObject = chat.MockedConnection.Object;

            chat.Clients = new HubConnectionContext(mockPipeline.Object, mockedConnectionObject, "Chat", connectionId, clientState);

            var prinicipal = new Mock<IPrincipal>();

            var request = new Mock<IRequest>();
            request.Setup(m => m.Cookies).Returns(cookies);
            request.Setup(m => m.User).Returns(prinicipal.Object);

            // setup context
            chat.Context = new HubCallerContext(request.Object, connectionId);

            return chat;
        }

        public class TestableChat : Chat
        {
            public Mock<IResourceProcessor> MockedResourceProcessor { get; private set; }
            public Mock<IChatService> MockedChatService { get; private set; }
            public IJabbrRepository Repository { get; private set; }
            public Mock<IConnection> MockedConnection { get; private set; }
            public Mock<IApplicationSettings> MockSettings { get; set; }

            public TestableChat(Mock<IApplicationSettings> mockSettings, Mock<IResourceProcessor> mockedResourceProcessor, Mock<IChatService> mockedChatService, IJabbrRepository repository, Mock<IConnection> connection)
                : base(mockSettings.Object, mockedResourceProcessor.Object, mockedChatService.Object, repository, new Mock<ICache>().Object)
            {
                MockedResourceProcessor = mockedResourceProcessor;
                MockedChatService = mockedChatService;
                MockSettings = mockSettings;
                Repository = repository;
                MockedConnection = connection;
            }
        }
    }
}
