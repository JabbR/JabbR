using System.Collections.Generic;
using System.Security.Principal;
using JabbR.ContentProviders.Core;
using JabbR.Models;
using JabbR.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Moq;

namespace JabbR.Test
{
    public class ChatFacts
    {
        public static TestableChat GetTestableChat(string clientId, StateChangeTracker clientState, ChatUser user)
        {
            return GetTestableChat(clientId, clientState, user, new Dictionary<string, Cookie>());
        }

        public static TestableChat GetTestableChat(string connectionId, StateChangeTracker clientState, ChatUser user, IDictionary<string, Cookie> cookies)
        {
            // setup things needed for chat
            var repository = new InMemoryRepository();
            var resourceProcessor = new Mock<ContentProviderProcessor>();
            var chatService = new Mock<IChatService>();
            var connection = new Mock<IConnection>();
            var settings = new Mock<IApplicationSettings>();
            var mockPipeline = new Mock<IHubPipelineInvoker>();

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
            public Mock<ContentProviderProcessor> MockedResourceProcessor { get; private set; }
            public Mock<IChatService> MockedChatService { get; private set; }
            public IJabbrRepository Repository { get; private set; }
            public Mock<IConnection> MockedConnection { get; private set; }

            public TestableChat(Mock<IApplicationSettings> mockSettings, Mock<ContentProviderProcessor> mockedResourceProcessor, Mock<IChatService> mockedChatService, IJabbrRepository repository, Mock<IConnection> connection)
                : base(mockedResourceProcessor.Object, mockedChatService.Object, repository, new Mock<ICache>().Object)
            {
                MockedResourceProcessor = mockedResourceProcessor;
                MockedChatService = mockedChatService;
                Repository = repository;
                MockedConnection = connection;
            }
        }
    }
}
