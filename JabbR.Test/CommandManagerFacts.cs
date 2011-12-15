using System;
using System.Collections.Generic;
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, notificationService.Object);

                bool result = commandManager.TryHandleCommand("foo");

                Assert.False(result);
            }

            [Fact]
            public void ReturnsFalseIfCommandStartsWithSlash()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, notificationService.Object);

                bool result = commandManager.TryHandleCommand("/foo", new string[] { });

                Assert.False(result);
            }

            [Fact]
            public void ThrowsIfCommandDoesntExists()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    HashedPassword = "password".ToSha256(null)
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };

                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);
                var commandManager = new CommandManager("1", "1", "room", service, repository, notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/foo"));
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
            public void CreateNewUserFailsIfNoPassword()
            {
                VerifyThrows<InvalidOperationException>("/nick dfowler");
            }

            [Fact]
            public void ThrowsIfNickIsEmpty()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        null,
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("nick", new string[] { "", "" }));
            }

            [Fact]
            public void CreatesNewUserIfPasswordSpecified()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                Assert.Equal("password".ToSha256(null), user.HashedPassword);
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
                    HashedPassword = "password".ToSha256(null)
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                Assert.Equal("password".ToSha256(user.Salt), user.HashedPassword);
                notificationService.Verify(m => m.OnUserCreated(user), Times.Once());
            }

            [Fact]
            public void SetPasswordForExistingUser()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Salt = "salt",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                Assert.Equal("password".ToSha256("salt"), user.HashedPassword);
                notificationService.Verify(m => m.SetPassword(), Times.Once());
            }

            [Fact]
            public void CanChangePasswordIfNewPasswordIsEmpty()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Salt = "salt",
                    Id = "1",
                    HashedPassword = ""
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("nick", new string[] { "/nick", "dfowler", "password", "" }));
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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                Assert.Equal("newpassword".ToSha256("salt"), user.HashedPassword);
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
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
            [Fact]
            public void ThrowsIfNoUser()
            {
                VerifyThrows<InvalidOperationException>("/logout");
            }

            [Fact]
            public void LogOut()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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

        public class InviteCodeCommand
        {
            [Fact]
            public void InviteCodeShowsErrorForPublicRoom()
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
                room.Owners.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("Only private rooms can have invite codes", ex.Message);
            }

            [Fact]
            public void InviteCodeSetsCodeIfNoCodeAndCurrentUserOwner()
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
                    Name = "room",
                    Private = true
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/invitecode");

                Assert.True(result);
                Assert.NotNull(room.InviteCode);
                Assert.Equal(6, room.InviteCode.Length);
                Assert.True(room.InviteCode.All(c => Char.IsDigit(c)));
                notificationService.Verify(n => n.PostNotification(room, user, String.Format("Invite Code for this room: {0}", room.InviteCode)));
            }

            [Fact]
            public void InviteCodeDisplaysCodeIfCodeSet()
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
                    Name = "room",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/invitecode");

                Assert.True(result);
                Assert.NotNull(room.InviteCode);
                Assert.Equal(6, room.InviteCode.Length);
                Assert.True(room.InviteCode.All(c => Char.IsDigit(c)));
                notificationService.Verify(n => n.PostNotification(room, user, String.Format("Invite Code for this room: {0}", room.InviteCode)));
            }

            [Fact]
            public void InviteCodeResetCodeWhenResetInviteCodeCalled()
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
                    Name = "room",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Owners.Add(user);
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/resetinvitecode");

                Assert.True(result);
                Assert.NotEqual("123456", room.InviteCode);
            }

            [Fact]
            public void ThrowsIfNonOwnerRequestsInviteCodeWhenNoneSet()
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
                    Name = "room",
                    Private = true
                };
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("You are not an owner of room", ex.Message);
            }

            [Fact]
            public void ThrowsIfNonOwnerRequestsResetInviteCode()
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
                    Name = "room",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/resetinvitecode"));
                Assert.Equal("You are not an owner of room", ex.Message);
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
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);


                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/join"));
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndNoInviteCodeProvided()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = true
                };
                repository.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/join room"));
                Assert.Equal("Unable to join room. This room is locked and you don't have permission to enter. If you have an invite code, make sure to enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndInviteCodeIncorrect()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = true,
                    InviteCode = "123456"
                };
                repository.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/join room 789012"));
                Assert.Equal("Unable to join room. This room is locked and you don't have permission to enter. If you have an invite code, make sure to enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndNoInviteCodeSet()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = true
                };
                repository.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/join room 789012"));
                Assert.Equal("Unable to join room. This room is locked and you don't have permission to enter. If you have an invite code, make sure to enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void JoinIfInviteCodeIsCorrect()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = true,
                    InviteCode = "123456"
                };
                repository.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                commandManager.TryHandleCommand("/join room 123456");
                notificationService.Verify(ns => ns.JoinRoom(user, room));
            }

            [Fact]
            public void JoinIfUserIsAllowed()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = true
                };
                room.AllowedUsers.Add(user);
                repository.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                commandManager.TryHandleCommand("/join room");
                notificationService.Verify(ns => ns.JoinRoom(user, room));
            }

            [Fact]
            public void ThrowIfUserNotAllowedToJoinPrivateRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = true
                };
                repository.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/join room"));
                Assert.Equal("Unable to join room. This room is locked and you don't have permission to enter. If you have an invite code, make sure to enter it in the /join command",
                             ex.Message);
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/addowner dfowler2 test");

                Assert.True(result);
                notificationService.Verify(m => m.AddOwner(targetUser, room), Times.Once());
                Assert.True(room.Owners.Contains(targetUser));
                Assert.True(targetUser.OwnedRooms.Contains(room));
            }

        }

        public class RemoveOwnerCommand
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/removeowner "));
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/removeowner dfowler"));
            }

            [Fact]
            public void CreatorCanRemoveOwnerFromRoom()
            {
                var repository = new InMemoryRepository();
                var roomCreator = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var targetUser = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(roomCreator);
                repository.Add(targetUser);
                var room = new ChatRoom()
                {
                    Name = "test",
                    Creator = roomCreator
                };
                room.Owners.Add(roomCreator);
                roomCreator.Rooms.Add(room);
                // make target user an owner
                room.Owners.Add(targetUser);
                targetUser.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/removeowner dfowler2 test");

                Assert.True(result);
                notificationService.Verify(m => m.RemoveOwner(targetUser, room), Times.Once());
                Assert.False(room.Owners.Contains(targetUser));
                Assert.False(targetUser.OwnedRooms.Contains(room));
            }

            [Fact]
            public void NonCreatorsCannotRemoveOwnerFromRoom()
            {
                var repository = new InMemoryRepository();
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var targetUser = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(roomOwner);
                repository.Add(targetUser);
                var room = new ChatRoom()
                {
                    Name = "test"
                };
                room.Owners.Add(roomOwner);
                roomOwner.Rooms.Add(room);
                // make target user an owner
                room.Owners.Add(targetUser);
                targetUser.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/removeowner dfowler2 test"));
            }
        }

        public class CreateCommand
        {
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/create "));
            }

            [Fact]
            public void ThrowsIfRoomNameIsEmpty()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("create", new string[] { "", "" }));
            }

            [Fact]
            public void CreateRoomFailsIfRoomAlreadyExists()
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
                    Name = "Test"
                };
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/create Test"));
            }

            [Fact]
            public void CreateRoomFailsIfRoomNameContainsSpaces()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/create Test Room"));
            }

            [Fact]
            public void CanCreateRoomAndJoinsAutomaticly()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/create Test");

                Assert.True(result);
                Assert.True(repository.Rooms.Any(x => x.Name.Equals("test", StringComparison.OrdinalIgnoreCase)));
                Assert.True(user.Rooms.Any(x => x.Name.Equals("test", StringComparison.OrdinalIgnoreCase)));
            }

        }

        public class GravatarCommand
        {
            [Fact]
            public void MissingEmailThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/gravatar "));
            }

            [Fact]
            public void CanSetGravatar()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/gravatar test@jabbR.net");

                Assert.True(result);
                Assert.Equal("test@jabbR.net".ToLowerInvariant().ToMD5(), user.Hash);
                notificationService.Verify(x => x.ChangeGravatar(user), Times.Once());
            }
        }

        public class HelpCommand
        {
            [Fact]
            public void CanShowHelp()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/help");

                Assert.True(result);
                notificationService.Verify(x => x.ShowHelp(), Times.Once());
            }
        }

        public class KickCommand
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
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/kick"));
            }

            [Fact]
            public void CannotKickUserIfUserIsOnlyOneInRoom()
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
                room.Owners.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/kick fowler2"));
            }

            [Fact]
            public void CannotKickUserIfNotExists()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/kick fowler3"));
            }

            [Fact]
            public void CannotKickUserIfUserIsNotInRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var user3 = new ChatUser
                {
                    Name = "dfowler3",
                    Id = "3"
                };
                repository.Add(user3);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/kick dfowler3"));
            }

            [Fact]
            public void CanKickUser()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var user3 = new ChatUser
                {
                    Name = "dfowler3",
                    Id = "3"
                };
                repository.Add(user3);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Users.Add(user3);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                user3.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/kick dfowler3");

                Assert.True(result);
                Assert.False(room.Users.Contains(user3));
                Assert.False(user3.Rooms.Contains(room));
                notificationService.Verify(x => x.KickUser(user3, room), Times.Once());
            }

            [Fact]
            public void CannotKickUrSelf()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/kick dfowler"));
            }

            [Fact]
            public void CannotKickIfUserIsNotOwner()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "2",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/kick dfowler"));
            }

            [Fact]
            public void IfNotRoomCreatorCannotKickOwners()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room",
                    Creator = user
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                room.Owners.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "2",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/kick dfowler"));
            }

            [Fact]
            public void RoomCreatorCanKickOwners()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room",
                    Creator = user
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                room.Owners.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/kick dfowler2");

                Assert.True(result);
                Assert.False(room.Users.Contains(user2));
                Assert.False(user2.Rooms.Contains(room));
                notificationService.Verify(x => x.KickUser(user2, room), Times.Once());
            }
        }

        public class LeaveCommand
        {
            [Fact]
            public void CanLeaveRoom()
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/leave");

                Assert.True(result);
                Assert.False(room.Users.Contains(user));
                Assert.False(user.Rooms.Contains(room));
                notificationService.Verify(x => x.LeaveRoom(user, room), Times.Once());
            }

            [Fact]
            public void CanLeaveRoomByGivingRoomName()
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/leave room");

                Assert.True(result);
                Assert.False(room.Users.Contains(user));
                Assert.False(user.Rooms.Contains(room));
                notificationService.Verify(x => x.LeaveRoom(user, room), Times.Once());
            }

            [Fact]
            public void CannotLeaveRoomIfNotImRoom()
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/leave"));
            }
        }

        public class ListCommand
        {
            [Fact]
            public void MissingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/list"));
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/list test"));
            }

            [Fact]
            public void CanShowUserList()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/list room");

                var userList = new List<String>();
                userList.Add(user.Name);
                userList.Add(user2.Name);

                Assert.True(result);
                notificationService.Verify(x => x.ListUsers(room, userList), Times.Once());
            }
        }

        public class MeCommand
        {
            [Fact]
            public void MissingContentThrows()
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
                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/me"));
            }

            [Fact]
            public void MissingRoomThrows()
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
                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/me is testing"));
            }

            [Fact]
            public void CanUseMeCommand()
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
                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/me is testing");

                Assert.True(result);
                notificationService.Verify(x => x.OnSelfMessage(room, user, "is testing"), Times.Once());
            }
        }

        public class MsgCommand
        {
            [Fact]
            public void ThrowsIfThereIsOnlyOneUser()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/msg"));
            }

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
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/msg"));
            }

            [Fact]
            public void ThrowsIfUserDoesntExists()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/msg dfowler3"));
            }

            [Fact]
            public void CannotMessageOwnUser()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/msg dfowler"));
            }

            [Fact]
            public void MissingMessageTextThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/msg dfowler2 "));
            }

            [Fact]
            public void CanSendMessage()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/msg dfowler2 what is up?");

                Assert.True(result);
                notificationService.Verify(x => x.SendPrivateMessage(user, user2, "what is up?"), Times.Once());
            }
        }

        public class NudgeCommand
        {
            [Fact]
            public void ThrowsIfThereIsOnlyOneUser()
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/nudge void"));
            }

            [Fact]
            public void ThrowsIfUserDoesntExists()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/nudge void"));
            }

            [Fact]
            public void CannotNudgeOwnUser()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/nudge dfowler"));
            }

            [Fact]
            public void NudgingTwiceWithin60SecondsThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nudge dfowler2");

                Assert.True(result);
                notificationService.Verify(x => x.NugeUser(user, user2), Times.Once());
                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/nudge dfowler2"));
            }

            [Fact]
            public void CanNudge()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nudge dfowler2");

                Assert.True(result);
                notificationService.Verify(x => x.NugeUser(user, user2), Times.Once());
                Assert.NotNull(user2.LastNudged);
            }

            [Fact]
            public void CanNudgeRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                user2.Rooms.Add(room);
                room.Users.Add(user2);
                repository.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nudge");

                Assert.True(result);
                notificationService.Verify(x => x.NudgeRoom(room, user), Times.Once());
                Assert.NotNull(room.LastNudged);
            }

            [Fact]
            public void CannotNudgeRoomTwiceWithin60Seconds()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                user2.Rooms.Add(room);
                room.Users.Add(user2);
                repository.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nudge");

                Assert.True(result);
                notificationService.Verify(x => x.NudgeRoom(room, user), Times.Once());
                Assert.NotNull(room.LastNudged);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/nudge"));
            }

        }

        public class RoomsCommand
        {
            [Fact]
            public void CanShowRooms()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/rooms");

                Assert.True(result);
                notificationService.Verify(x => x.ShowRooms(), Times.Once());
            }
        }

        public class WhoCommand
        {
            [Fact]
            public void CanGetUserList()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/who");

                Assert.True(result);
                notificationService.Verify(x => x.ListUsers(), Times.Once());
            }

            [Fact]
            public void CanGetUserInfo()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/who dfowler");

                Assert.True(result);
                notificationService.Verify(x => x.ShowUserInfo(user), Times.Once());
            }

            [Fact]
            public void CannotGetInfoForInvalidUser()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/who sethwebster"));
            }
        }

        public class WhereCommand
        {

            [Fact]
            public void CannotShowUserRoomsWhenEnteringPartOfName()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/where dfow"));
            }

            [Fact]
            public void CanShowUserRooms()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/where dfowler");

                Assert.True(result);
                notificationService.Verify(x => x.ListRooms(user), Times.Once());
            }

        }

        public class AllowCommand
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/allow"));
            }

            [Fact]
            public void NotExistingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/allow dfowler2"));
            }

            [Fact]
            public void MissingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/allow dfowler2"));
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/allow dfowler2 asfasfdsad"));
            }

            [Fact]
            public void CannotAllowUserToRoomIfNotPrivate()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/allow dfowler2 room"));
            }

            [Fact]
            public void CanAllowUserToRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/allow dfowler2 room");

                Assert.True(result);
                notificationService.Verify(x => x.AllowUser(user2, room), Times.Once());
                Assert.True(room.AllowedUsers.Contains(user2));
            }
        }

        public class LockCommand
        {
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/lock"));
            }

            [Fact]
            public void NotExistingRoomNameThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/lock room"));
            }

            [Fact]
            public void CanLockRoom()
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
                    Name = "room",
                    Private = false
                };
                room.Creator = user;
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/lock room");

                Assert.True(result);
                notificationService.Verify(x => x.LockRoom(user, room), Times.Once());
                Assert.True(room.Private);
            }

        }

        public class UnAllowCommand
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/unallow"));
            }

            [Fact]
            public void NotExistingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/unallow dfowler2"));
            }

            [Fact]
            public void MissingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/unallow dfowler2"));
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/unallow dfowler2 asfasfdsad"));
            }
            [Fact]
            public void CannotUnAllowUserToRoomIfNotPrivate()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                user2.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user2);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                Assert.Throws<InvalidOperationException>(() => commandManager.TryHandleCommand("/unallow dfowler2 room"));
            }

            [Fact]
            public void CanUnAllowUserToRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                user2.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user2);
                repository.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/unallow dfowler2 room");

                Assert.True(result);
                notificationService.Verify(x => x.UnallowUser(user2, room), Times.Once());
                Assert.False(room.AllowedUsers.Contains(user2));
            }
        }

        public static void VerifyThrows<T>(string command) where T : Exception
        {
            var repository = new InMemoryRepository();
            var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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
