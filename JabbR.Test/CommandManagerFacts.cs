using System;
using JabbR.Commands;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Moq;
using Xunit;

namespace JabbR.Test
{
    public class CommandManagerFacts
    {
        public class TryHandleCommand
        {
            [Fact]
            public void ReturnsFalseIfCommandDoesntStartWithSlash()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, notificationService.Object);

                bool result = commandManager.TryHandleCommand("foo");

                Assert.False(result);
            }
        }

        public class NickCommand
        {
            [Fact]
            public void MissingNameThrows()
            {
                VerifyThrows<InvalidOperationException>("/nick");
                VerifyThrows<InvalidOperationException>("/nick     ");
            }

            [Fact]
            public void CreateNewUser()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        null,
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nick dfowler");

                Assert.True(result);
                var user = repository.GetUserByName("dfowler");
                Assert.NotNull(user);
                Assert.Equal("dfowler", user.Name);
                Assert.Equal("clientid", user.ClientId);
                notificationService.Verify(m => m.OnUserCreated(user), Times.Once());
            }

            [Fact]
            public void ChangeNick()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nick dfowler2");

                Assert.True(result);
                Assert.NotNull(user);
                Assert.Equal("dfowler2", user.Name);
                notificationService.Verify(m => m.OnUserNameChanged(user, "dfowler", "dfowler2"), Times.Once());
            }

            [Fact]
            public void CreatesNewUserWithPassword()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        null,
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nick dfowler password");

                Assert.True(result);
                var user = repository.GetUserByName("dfowler");
                Assert.NotNull(user);
                Assert.Equal("dfowler", user.Name);
                Assert.Equal("clientid", user.ClientId);
                Assert.Equal("password".ToSha256(), user.HashedPassword);
                notificationService.Verify(m => m.OnUserCreated(user), Times.Once());
            }

            [Fact]
            public void SetPasswordForExistingUser()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nick dfowler password");

                Assert.True(result);
                Assert.NotNull(user);
                Assert.Equal("dfowler", user.Name);
                Assert.Equal("password".ToSha256(), user.HashedPassword);
                notificationService.Verify(m => m.SetPassword(), Times.Once());
            }

            [Fact]
            public void ClaimUserName()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        null,
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nick dfowler password");

                Assert.True(result);
                Assert.NotNull(user);
                Assert.Equal("dfowler", user.Name);
                Assert.Equal("clientid", user.ClientId);
                notificationService.Verify(m => m.Initialize(user), Times.Once());
            }

            [Fact]
            public void ChangePassword()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nick dfowler password newpassword");

                Assert.True(result);
                Assert.NotNull(user);
                Assert.Equal("dfowler", user.Name);
                Assert.Equal("newpassword".ToSha256(), user.HashedPassword);
                notificationService.Verify(m => m.ChangePassword(), Times.Once());
            }

            [Fact]
            public void ChangePasswordOfExistingUser()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        null,
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/nick dfowler password newpassword"));
            }
        }

        public class LogOutCommand
        {
            public void ThrowsIfNoUser()
            {                
                VerifyThrows<InvalidOperationException>("/logout");
            }

            public void LogOut()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/logout");

                Assert.True(result);
                notificationService.Verify(m => m.LogOut(user), Times.Once());
            }
        }

        public static void VerifyThrows<T>(string command) where T : Exception
        {
            var repository = new InMemoryRepository();
            var service = new ChatService(repository);
            var notificationService = new Mock<INotificationService>();
            var commandManager = new CommandManager("clientid",
                                                    null,
                                                    null,
                                                    service,
                                                    repository,
                                                    notificationService.Object);

            Assert.Throws<T>(() => commandManager.TryHandleCommand(command));
        }
    }
}
