using System;
using System.Linq;
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
                Assert.True(user.ConnectedClients.Any(c => c.Id == "clientid"));
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
                Assert.True(user.ConnectedClients.Any(c => c.Id == "clientid"));
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
            public void ThrowsIfTryingToClaimExistingUserName()
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
                                                        "2",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/nick dfowler"));
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
                Assert.True(user.ConnectedClients.Any(c => c.Id == "clientid"));
                notificationService.Verify(m => m.LogOn(user, "clientid"), Times.Once());
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
                notificationService.Verify(m => m.LogOut(user, "clientid"), Times.Once());
            }
        }

        public class JoinCommand
        {
            [Fact]
            public void DoesNotThrowIfUserAlreadyInRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/join room");

                Assert.True(result);
                notificationService.Verify(m => m.JoinRoom(user, room), Times.Once());
            }

            [Fact]
            public void CanJoinRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                repository.Add(room);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/join room");

                Assert.True(result);
                notificationService.Verify(m => m.JoinRoom(user, room), Times.Once());
            }

            [Fact]
            public void ThrowIfRoomIsEmpty()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);


                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/join"));
            }
        }

        public class AddOwnerCommand
        {
            [Fact]
            public void MissingUserNameThrows()
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

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/addowner "));
            }

            [Fact]
            public void MissingRoomNameThrows()
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

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/addowner dfowler"));
            }

            [Fact]
            public void CanAddOwnerToRoom()
            {
                var repository = new InMemoryRepository();
                var roomOwnerUser = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var targetUser = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(roomOwnerUser);
                repository.Add(targetUser);
                var room = new ChatRoom()
                {
                    Name = "test"
                };
                room.Owners.Add(roomOwnerUser);
                roomOwnerUser.Rooms.Add(room);
                targetUser.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/addowner dfowler2 test");

                Assert.True(result);
                notificationService.Verify(m => m.OnOwnerAdded(targetUser, room), Times.Once());
                Assert.True(room.Owners.Contains(targetUser));
                Assert.True(targetUser.OwnedRooms.Contains(room));
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
