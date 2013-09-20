using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JabbR.Commands;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Microsoft.AspNet.SignalR;
using Moq;
using Xunit;

namespace JabbR.Test
{
    public class CommandManagerFacts
    {
        public class ParseCommand
        {
            [Fact]
            public void ReturnsTheCommandName()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, cache, notificationService.Object);

                const string commandName = "thecommand";
                string[] args;
                var parsedName = commandManager.ParseCommand(String.Format("/{0}", commandName), out args);

                Assert.Equal(commandName, parsedName);
            }

            [Fact]
            public void ReturnsNullForEmptyCommandName()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, cache, notificationService.Object);

                string[] args;
                var parsedName = commandManager.ParseCommand("/", out args);

                Assert.Null(parsedName);
            }

            [Fact]
            public void ParsesTheArguments()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, cache, notificationService.Object);
            
                var parts = new[] {"/cmd", "arg0", "arg1", "arg2"};
                var command = String.Join(" ", parts);
                
                string[] parsedArgs;
                commandManager.ParseCommand(command, out parsedArgs);

                Assert.Equal(parts.Skip(1), parsedArgs);
            }

            [Fact]
            public void IgnoresMultipleWhitespaceBetweenArguments()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, cache, notificationService.Object);
                
                var parts = new[] { "/cmd", "arg0", "arg1", "arg2" };
                var command = String.Join("    ", parts);

                string[] parsedArgs;
                commandManager.ParseCommand(command, out parsedArgs);

                Assert.Equal(parts.Skip(1), parsedArgs);
            }

            [Fact]
            public void ProducesEmptyArrayIfNoArguments()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, cache, notificationService.Object);

                string[] parsedArgs;
                commandManager.ParseCommand("/cmd", out parsedArgs);

                Assert.Equal(0, parsedArgs.Length);
            }
        }

        public class TryHandleCommand
        {
            [Fact]
            public void ReturnsFalseIfCommandDoesntStartWithSlash()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, cache, notificationService.Object);

                bool result = commandManager.TryHandleCommand("foo");

                Assert.False(result);
            }

            [Fact]
            public void ReturnsFalseIfCommandStartsWithSlash()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("id", "id", "name", service, repository, cache, notificationService.Object);

                bool result = commandManager.TryHandleCommand("/foo", new string[] { });

                Assert.False(result);
            }

            [Fact]
            public void ThrowsIfCommandDoesntExist ()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
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
                var commandManager = new CommandManager("1", "1", "room", service, repository, cache, notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/message"));
                Assert.Equal("message is not a valid command.", ex.Message);
            }

            [Fact]
            public void ThrowsIfCommandIsAmbiguous()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var user = new ChatUser
                {
                    Name = "tilde",
                    Id = "1",
                    HashedPassword = "password".ToSha256(null)
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                };

                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);
                var commandManager = new CommandManager("1", "1", "room", service, repository, cache, notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/a"));
                Assert.True(ex.Message.StartsWith("a is ambiguous: "));
            }

            [Fact]
            public void DoesNotThrowIfCommandIsPrefixButExactMatch()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();

                var commandManager = new CommandManager("1", "1", "room", service, repository, cache, notificationService.Object);

                ICommand command;
                commandManager.MatchCommand("invite", out command);

                Assert.IsType<JabbR.Commands.InviteCommand>(command);
            }

            [Fact]
            public void DoesNotThrowIfCommandIsPrefixButCaseInsensitiveMatch()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();

                var commandManager = new CommandManager("1", "1", "room", service, repository, cache, notificationService.Object);

                ICommand command;
                commandManager.MatchCommand("CrEaT", out command);

                Assert.IsType<JabbR.Commands.CreateCommand>(command);
            }
        }
        
        public class LogOutCommand
        {
            [Fact]
            public void ThrowsIfNoUser()
            {
                VerifyThrows<HubException>("/logout");
            }

            [Fact]
            public void LogOut()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
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
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("Only private rooms can have invite codes.", ex.Message);
            }

            [Fact]
            public void InviteCodeSetsCodeIfNoCodeAndCurrentUserOwner()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/invitecode room");

                Assert.True(result);
                Assert.NotNull(room.InviteCode);
                Assert.Equal(6, room.InviteCode.Length);
                Assert.True(room.InviteCode.All(c => Char.IsDigit(c)));
                // expect the notification in the lobby (null room)
                notificationService.Verify(n => n.PostNotification(room, user, String.Format("Invite Code for {0}: {1}", room.Name, room.InviteCode)));
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("This command cannot be invoked from the Lobby.", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                room.AllowedUsers.Add(user);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/invitecode");

                Assert.True(result);
                Assert.NotNull(room.InviteCode);
                Assert.Equal(6, room.InviteCode.Length);
                Assert.True(room.InviteCode.All(c => Char.IsDigit(c)));
                notificationService.Verify(n => n.PostNotification(room, user, String.Format("Invite Code for {0}: {1}", room.Name, room.InviteCode)));
            }

            [Fact]
            public void InviteCodeDisplaysCodeIfCodeSet()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                room.AllowedUsers.Add(user);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/invitecode");

                Assert.True(result);
                Assert.NotNull(room.InviteCode);
                Assert.Equal(6, room.InviteCode.Length);
                Assert.True(room.InviteCode.All(c => Char.IsDigit(c)));
                notificationService.Verify(n => n.PostNotification(room, user, String.Format("Invite Code for {0}: {1}", room.Name, room.InviteCode)));
            }

            [Fact]
            public void InviteCodeDisplaysCodeIfCodeSetOnClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Closed = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                room.AllowedUsers.Add(user);
                repository.Add(room);

                var room2 = new ChatRoom
                {
                    Name = "room2",
                    Private = true,
                    Closed = true,
                    InviteCode = "123456"
                };
                room2.Users.Add(user);
                room2.AllowedUsers.Add(user);
                repository.Add(room2);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room2",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/invitecode room");

                Assert.True(result);
                Assert.NotNull(room.InviteCode);
                Assert.Equal(6, room.InviteCode.Length);
                Assert.True(room.InviteCode.All(c => Char.IsDigit(c)));
                notificationService.Verify(n => n.PostNotification(room2, user, String.Format("Invite Code for {0}: {1}", room.Name, room.InviteCode)));
            }

            [Fact]
            public void InviteCodeResetCodeWhenResetInviteCodeCalled()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/resetinvitecode");

                Assert.True(result);
                Assert.NotEqual("123456", room.InviteCode);
            }

            [Fact]
            public void InviteCodeResetCodeWhenResetInviteCodeCalledOnClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Closed = true,
                    InviteCode = "123456"
                };
                room.Owners.Add(user);
                room.Users.Add(user);
                repository.Add(room);

                var room2 = new ChatRoom
                {
                    Name = "room2",
                    Private = true,
                    InviteCode = "123456"
                };
                room2.Owners.Add(user);
                room2.Users.Add(user);
                repository.Add(room2);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room2.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/resetinvitecode room");

                Assert.True(result);
                Assert.NotEqual("123456", room.InviteCode);
            }

            [Fact]
            public void ThrowsIfNonUserRequestsInviteCodeWhenNoneSet()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName,
                    Private = true
                };
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("You do not have access to " + roomName + ".", ex.Message);
            }

            [Fact]
            public void ThrowsIfNonOwnerRequestsInviteCodeWhenNoneSet()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName,
                    Private = true
                };
                room.Users.Add(user);
                room.AllowedUsers.Add(user);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("You are not an owner of " + roomName + ".", ex.Message);
            }

            [Fact]
            public void ThrowsIfNonUserRequestsResetInviteCode()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName,
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/resetinvitecode"));
                Assert.Equal("You do not have access to " + roomName + ".", ex.Message);
            }

            [Fact]
            public void ThrowsIfNonOwnerRequestsResetInviteCode()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName,
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                room.AllowedUsers.Add(user);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        room.Name,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/resetinvitecode"));
                Assert.Equal("You are not an owner of " + roomName + ".", ex.Message);
            }
        }

        public class JoinCommand
        {
            [Fact]
            public void DoesNotThrowIfUserAlreadyInRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/join room");

                Assert.True(result);
                notificationService.Verify(m => m.JoinRoom(user, room), Times.Once());
            }

            [Fact]
            public void CanJoinRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/join room");

                Assert.True(result);
                notificationService.Verify(m => m.JoinRoom(user, room), Times.Once());
            }

            [Fact]
            public void ThrowIfRoomIsEmpty()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);


                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join"));
                Assert.Equal("Which room do you want to join?", ex.Message);
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndNoInviteCodeProvided()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join room"));
                Assert.Equal("Unable to join room. This room is locked and you don't have permission to enter. If you have an invite code, enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndInviteCodeIncorrect()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join room 789012"));
                Assert.Equal("Unable to join room. This room is locked and you don't have permission to enter. If you have an invite code, enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndNoInviteCodeSet()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join room 789012"));
                Assert.Equal("Unable to join room. This room is locked and you don't have permission to enter. If you have an invite code, enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void JoinIfInviteCodeIsCorrect()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                commandManager.TryHandleCommand("/join room 123456");
                notificationService.Verify(ns => ns.JoinRoom(user, room));
            }

            [Fact]
            public void JoinIfUserIsAllowed()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                commandManager.TryHandleCommand("/join room");
                notificationService.Verify(ns => ns.JoinRoom(user, room));
            }

            [Fact]
            public void ThrowIfUserNotAllowedToJoinPrivateRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join room"));
                Assert.Equal("Unable to join room. This room is locked and you don't have permission to enter. If you have an invite code, enter it in the /join command",
                             ex.Message);
            }
        }

        public class AddOwnerCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/addowner "));
                Assert.Equal("Who do you want to make an owner?", ex.Message);
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/addowner dfowler"));
                Assert.Equal("Which room do you want to add ownership to?", ex.Message);
            }

            [Fact]
            public void CanAddOwnerToRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/addowner dfowler2 test");

                Assert.True(result);
                notificationService.Verify(m => m.AddOwner(targetUser, room), Times.Once());
                Assert.True(room.Owners.Contains(targetUser));
                Assert.True(targetUser.OwnedRooms.Contains(room));
            }

            [Fact]
            public void ThrowsIfRoomIsClosed()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Name = "test",
                    Closed = true
                };
                room.Owners.Add(roomOwnerUser);
                roomOwnerUser.Rooms.Add(room);
                targetUser.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "test",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/addowner dfowler2"));
                Assert.Equal("test is closed.", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "test",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/addowner dfowler2");

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
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/removeowner "));
                Assert.Equal("Which owner do you want to remove?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/removeowner dfowler"));
                Assert.Equal("Which room do you want to remove the owner from?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "test",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/removeowner dfowler2");

                Assert.True(result);
                notificationService.Verify(m => m.RemoveOwner(targetUser, room), Times.Once());
                Assert.False(room.Owners.Contains(targetUser));
                Assert.False(targetUser.OwnedRooms.Contains(room));
            }

            [Fact]
            public void CreatorCanRemoveOwnerFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/removeowner dfowler2 test");

                Assert.True(result);
                notificationService.Verify(m => m.RemoveOwner(targetUser, room), Times.Once());
                Assert.False(room.Owners.Contains(targetUser));
                Assert.False(targetUser.OwnedRooms.Contains(room));
            }

            [Fact]
            public void CannorRemoveOwnerFromClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Creator = roomCreator,
                    Closed = true
                };
                room.Owners.Add(roomCreator);
                roomCreator.Rooms.Add(room);
                // make target user an owner
                room.Owners.Add(targetUser);
                targetUser.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/removeowner dfowler2 test"));
                Assert.Equal("test is closed.", ex.Message);
            }

            [Fact]
            public void NonCreatorsCannotRemoveOwnerFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/removeowner dfowler2 test"));
                Assert.Equal("You are not the creator of test.", ex.Message);
            }
        }

        public class CreateCommand
        {
            [Fact]
            public void MissingRoomNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/create "));
                Assert.Equal("No room specified.", ex.Message);
            }

            [Fact]
            public void ThrowsIfRoomNameIsEmpty()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("create", new [] { "", "" }));
                Assert.Equal("Room names cannot contain spaces.", ex.Message);
            }

            [Fact]
            public void CreateRoomFailsIfRoomAlreadyExists()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/create Test"));
                Assert.Equal("Test already exists.", ex.Message);
            }

            [Fact]
            public void CreateRoomFailsIfRoomNameContainsSpaces()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/create Test Room"));
                Assert.Equal("Room names cannot contain spaces.", ex.Message);
            }

            [Fact]
            public void CreateRoomFailsIfRoomAlreadyExistsButItsClosed()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName,
                    Closed = true
                };
                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                // Act & Assert.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/create " + roomName));
                Assert.Equal(roomName + " already exists, but it's closed.", ex.Message);
            }

            [Fact]
            public void CanCreateRoomAndJoinsAutomaticly()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
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
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/gravatar "));
                Assert.Equal("Which email address do you want to use for the Gravatar image?", ex.Message);
            }

            [Fact]
            public void CanSetGravatar()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/gravatar test@jabbR.net");

                Assert.True(result);
                Assert.Equal("test@jabbR.net".ToLowerInvariant().ToMD5(), user.Hash);
                notificationService.Verify(x => x.ChangeGravatar(user), Times.Once());
            }
        }

        public class NoteCommand
        {
            [Fact]
            public void CanSetNoteWithTextSetsTheNoteProperty()
            {
                // Arrange.
                const string note = "this is a test note. Pew^Pew";
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                bool result = commandManager.TryHandleCommand("/note " + note);

                // Assert.
                Assert.True(result);
                Assert.Equal(note, user.Note);
                notificationService.Verify(x => x.ChangeNote(user), Times.Once());
            }

            [Fact]
            public void CanSetNoteWithNoTextClearsTheNoteProperty()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                bool result = commandManager.TryHandleCommand("/note ");

                // Assert.
                Assert.True(result);
                Assert.Null(user.Note);
                notificationService.Verify(x => x.ChangeNote(user), Times.Once());
            }

            [Fact]
            public void ThrowsIfNoteTextDoesNotValidate()
            {
                // Arrange.
                String note = new String('A', 141);

                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act & Assert.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/note " + note));
                Assert.Equal("Sorry, but your note is too long. Please keep it under 140 characters.", ex.Message);
            }
        }

        public class AfkCommand
        {
            [Fact]
            public void CanSetAfkWithTextSetsTheNoteProperty()
            {
                // Arrange.
                const string note = "I'll be back later!";
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                bool result = commandManager.TryHandleCommand("/afk " + note);

                Assert.True(result);
                Assert.Equal(note, user.AfkNote);
                notificationService.Verify(x => x.ChangeAfk(user), Times.Once());
            }

            [Fact]
            public void CanSetAfkWithNoTextSetTheNoteProperty()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                bool result = commandManager.TryHandleCommand("/afk");

                Assert.True(result);
                Assert.Null(user.AfkNote);
                notificationService.Verify(x => x.ChangeAfk(user), Times.Once());
            }

            [Fact]
            public void ThrowsIfAfkTextIsNotValid()
            {
                // Arrange.
                String note = new String('A', 141);

                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act & Assert.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/afk " + note));
                Assert.Equal("Sorry, but your note is too long. Please keep it under 140 characters.", ex.Message);
            }
        }

        public class HelpCommand
        {
            [Fact]
            public void CanShowHelp()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/?");

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
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick"));
                Assert.Equal("Who do you want to to kick?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "cjm1",
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
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick cjm1"));
                Assert.Equal("Which room do you want to kick them from?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/kick dfowler3");

                Assert.True(result);
                Assert.False(room.Users.Contains(user3));
                Assert.False(user3.Rooms.Contains(room));
                notificationService.Verify(x => x.KickUser(user3, room), Times.Once());
            }

            [Fact]
            public void CannotKickUserIfNotExists()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick fowler3"));
                Assert.Equal("Unable to find fowler3.", ex.Message);
            }

            [Fact]
            public void CannotKickUserIfUserIsNotInRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler3"));
                Assert.Equal("dfowler3 isn't in room.", ex.Message);
            }

            [Fact]
            public void CanKickUser()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/kick dfowler3 room");

                Assert.True(result);
                Assert.False(room.Users.Contains(user3));
                Assert.False(user3.Rooms.Contains(room));
                notificationService.Verify(x => x.KickUser(user3, room), Times.Once());
            }

            [Fact]
            public void CannotKickUserInClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Name = "room",
                    Closed = true
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Users.Add(user3);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                user3.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler3 room"));
                Assert.Equal("room is closed.", ex.Message);
            }

            [Fact]
            public void CannotKickUrSelf()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler"));
                Assert.Equal("Why would you want to kick yourself?", ex.Message);
            }

            [Fact]
            public void CannotKickIfUserIsNotOwner()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "2",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler"));
                Assert.Equal("You are not an owner of room.", ex.Message);
            }

            [Fact]
            public void IfNotRoomCreatorCannotKickOwners()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "2",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler"));
                Assert.Equal("Owners cannot kick other owners. Only the room creator can kick an owner.", ex.Message);
            }

            [Fact]
            public void RoomCreatorCanKickOwners()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                var result = commandManager.TryHandleCommand("/kick dfowler2");

                Assert.True(result);
                Assert.False(room.Users.Contains(user2));
                Assert.False(user2.Rooms.Contains(room));
                notificationService.Verify(x => x.KickUser(user2, room), Times.Once());
            }
        }


        public class BanCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    IsAdmin = true
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban"));
                Assert.Equal("Who do you want to ban?", ex.Message);
            }

            [Fact]
            public void BannerIsNotAnAdminThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                var ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban dfowler2"));

                Assert.True(ex.Message == "You are not an admin.");
            }

            [Fact]
            public void CannotBanUserIfNotExists()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    IsAdmin = true
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban fowler3"));
                Assert.Equal("Unable to find fowler3.", ex.Message);
            }

            [Fact]
            public void AdminCannotBanAdmin()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    IsAdmin = true
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2",
                    IsAdmin = true
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban dfowler2"));
                Assert.Equal("You cannot ban another admin.", ex.Message);
            }

            [Fact]
            public void CannotBanUrSelf()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    IsAdmin = true
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban dfowler"));
                Assert.Equal("You cannot ban another admin.", ex.Message);
            }
        }

        public class LeaveCommand
        {
            [Fact]
            public void CanLeaveRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/leave");

                Assert.True(result);
                Assert.False(room.Users.Contains(user));
                Assert.False(user.Rooms.Contains(room));
                notificationService.Verify(x => x.LeaveRoom(user, room), Times.Once());
            }

            [Fact]
            public void CanLeaveClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Closed = true
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
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
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/leave room");

                Assert.True(result);
                Assert.False(room.Users.Contains(user));
                Assert.False(user.Rooms.Contains(room));
                notificationService.Verify(x => x.LeaveRoom(user, room), Times.Once());
            }

            [Fact]
            public void CannotLeaveRoomIfNotInRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/leave"));
                Assert.Equal("Which room do you want to leave?", ex.Message);
            }
        }

        public class ListCommand
        {
            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/list"));
                Assert.Equal("Which room do you want to list the current users of?", ex.Message);
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/list test"));
                Assert.Equal("Unable to find test.", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var userList = new List<string>();
                notificationService.Setup(m => m.ListUsers(It.IsAny<ChatRoom>(), It.IsAny<IEnumerable<string>>()))
                                   .Callback<ChatRoom, IEnumerable<string>>((_, names) =>
                                   {
                                       userList.AddRange(names);
                                   });

                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/list");

                Assert.True(result);
                Assert.Equal(2, userList.Count);
                Assert.True(userList.Contains("dfowler2"));
                Assert.True(userList.Contains("dfowler"));
            }

            [Fact]
            public void CanShowUserList()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var userList = new List<string>();
                notificationService.Setup(m => m.ListUsers(It.IsAny<ChatRoom>(), It.IsAny<IEnumerable<string>>()))
                                   .Callback<ChatRoom, IEnumerable<string>>((_, names) =>
                                   {
                                       userList.AddRange(names);
                                   });

                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/list room");

                Assert.True(result);
                Assert.Equal(2, userList.Count);
                Assert.True(userList.Contains("dfowler2"));
                Assert.True(userList.Contains("dfowler"));
            }

            [Fact]
            public void ThrowsOnClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Closed = true
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var userList = new List<string>();
                notificationService.Setup(m => m.ListUsers(It.IsAny<ChatRoom>(), It.IsAny<IEnumerable<string>>()))
                                   .Callback<ChatRoom, IEnumerable<string>>((_, names) =>
                                   {
                                       userList.AddRange(names);
                                   });

                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/list room"));
                Assert.Equal("room is closed.", ex.Message);
            }
        }

        public class MeCommand
        {
            [Fact]
            public void MissingContentThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/me"));
                Assert.Equal("You what?", ex.Message);
            }

            [Fact]
            public void MissingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/me is testing"));
                Assert.Equal("Use '/join room' to join a room.", ex.Message);
            }

            [Fact]
            public void CanUseMeCommand()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/me is testing");

                Assert.True(result);
                notificationService.Verify(x => x.OnSelfMessage(room, user, "is testing"), Times.Once());
            }

            [Fact]
            public void ClosedRoomThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Closed = true
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/me is testing"));
                Assert.Equal("room is closed.", ex.Message);
            }
        }

        public class MsgCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;

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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/msg"));
                Assert.Equal("Who do you want to send a private message to?", ex.Message);
            }

            [Fact]
            public void ThrowsIfUserDoesntExist()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/msg dfowler3"));
                Assert.Equal("Unable to find dfowler3.", ex.Message);
            }

            [Fact]
            public void CannotMessageOwnUser()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/msg dfowler"));
                Assert.Equal("You can't private message yourself!", ex.Message);
            }

            [Fact]
            public void MissingMessageTextThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/msg dfowler2 "));
                Assert.Equal("What do you want to say to dfowler2?", ex.Message);
            }

            [Fact]
            public void CanSendMessage()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/msg dfowler2 what is up?");

                Assert.True(result);
                notificationService.Verify(x => x.SendPrivateMessage(user, user2, "what is up?"), Times.Once());
            }
        }

        public class InviteCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite"));
                Assert.Equal("Who do you want to invite?", ex.Message);
            }

            [Fact]
            public void NotExistingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite dfowler2"));
                Assert.Equal("Unable to find dfowler2.", ex.Message);
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite dfowler2"));
                Assert.Equal("Which room do you want to invite them to?", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var room = new ChatRoom { Name = "test", };
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "test",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/invite dfowler2");

                Assert.True(result);
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite dfowler2 asfasfdsad"));
                Assert.Equal("Unable to find asfasfdsad.", ex.Message);
            }

            [Fact]
            public void ThrowsIfThereIsOnlyOneUser()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite void"));
                Assert.Equal("Unable to find void.", ex.Message);
            }

            [Fact]
            public void CannotInviteOwnUser()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite dfowler"));
                Assert.Equal("You can't invite yourself!", ex.Message);
            }
        }

        public class NudgeCommand
        {
            [Fact]
            public void ThrowsIfUserDoesntExists()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge void"));
                Assert.Equal("Unable to find void.", ex.Message);
            }

            [Fact]
            public void CannotNudgeOwnUser()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge dfowler"));
                Assert.Equal("You can't nudge yourself!", ex.Message);
            }

            [Fact]
            public void NudgingTwiceWithin60SecondsThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nudge dfowler2");

                Assert.True(result);
                notificationService.Verify(x => x.NugeUser(user, user2), Times.Once());
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge dfowler2"));
                Assert.Equal("User can only be nudged once every 60 seconds.", ex.Message);
            }

            [Fact]
            public void CanNudge()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
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
                var cache = new Mock<ICache>().Object;
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
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
                var cache = new Mock<ICache>().Object;
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

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/nudge");

                Assert.True(result);
                notificationService.Verify(x => x.NudgeRoom(room, user), Times.Once());
                Assert.NotNull(room.LastNudged);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge"));
                Assert.Equal("Room can only be nudged once every 60 seconds.", ex.Message);
            }

            [Fact]
            public void CannotNudgeClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Closed = true
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                user2.Rooms.Add(room);
                room.Users.Add(user2);
                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge"));
                Assert.Equal("room is closed.", ex.Message);
            }
        }

        public class WhoCommand
        {
            [Fact]
            public void CanGetUserList()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/who");

                Assert.True(result);
                notificationService.Verify(x => x.ListUsers(), Times.Once());
            }

            [Fact]
            public void CanGetUserInfo()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/who dfowler");

                Assert.True(result);
                notificationService.Verify(x => x.ShowUserInfo(user), Times.Once());
            }

            [Fact]
            public void CannotGetInfoForInvalidUser()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/who sethwebster"));
                Assert.Equal("Unable to find sethwebster.", ex.Message);
            }
        }

        public class WhereCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/where"));
                Assert.Equal("Who do you want to locate?", ex.Message);
            }

            [Fact]
            public void CannotShowUserRoomsWhenEnteringPartOfName()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/where dfow"));
                Assert.Equal("Unable to find dfow.", ex.Message);
            }

            [Fact]
            public void CanShowUserRooms()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
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
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow"));
                Assert.Equal("Who do you want to grant access permissions to?", ex.Message);
            }

            [Fact]
            public void NotExistingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow dfowler2"));
                Assert.Equal("Unable to find dfowler2.", ex.Message);
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow dfowler2"));
                Assert.Equal("Which room do you want to allow access to?", ex.Message);
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow dfowler2 asfasfdsad"));
                Assert.Equal("Unable to find asfasfdsad.", ex.Message);
            }

            [Fact]
            public void CannotAllowUserToRoomIfNotPrivate()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow dfowler2 room"));
                Assert.Equal("room is not a private room.", ex.Message);
            }

            [Fact]
            public void CanAllowUserToRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/allow dfowler2 room");

                Assert.True(result);
                notificationService.Verify(x => x.AllowUser(user2, room), Times.Once());
                Assert.True(room.AllowedUsers.Contains(user2));
            }

            [Fact]
            public void CanAllowUserToClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Private = true,
                    Closed = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/allow dfowler2 room");

                Assert.True(result);
                notificationService.Verify(x => x.AllowUser(user2, room), Times.Once());
                Assert.True(room.AllowedUsers.Contains(user2));
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/allow dfowler2");

                Assert.True(result);
                notificationService.Verify(x => x.AllowUser(user2, room), Times.Once());
                Assert.True(room.AllowedUsers.Contains(user2));
            }
        }

        public class AllowedCommand
        {
            [Fact]
            public void CanGetAllowedUserListDefault()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    AllowedUsers = new Collection<ChatUser>() { user },
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/allowed");

                Assert.True(result);
                notificationService.Verify(x => x.ListAllowedUsers(room), Times.Once());
            }

            [Fact]
            public void CanGetAllowedUserListPublicRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = false,
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/allowed room");

                Assert.True(result);
                notificationService.Verify(x => x.ListAllowedUsers(room), Times.Once());
            }

            [Fact]
            public void CanGetAllowedUserListSpecified()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    AllowedUsers = new Collection<ChatUser>() { user },
                };
                repository.Add(room);
                var room2 = new ChatRoom
                {
                    Name = "room2",
                    Private = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room2);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room2",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/allowed room");

                Assert.True(result);
                notificationService.Verify(x => x.ListAllowedUsers(room), Times.Once());
            }

            [Fact]
            public void CanGetAllowedUserListClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Closed = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                };
                repository.Add(room);
                var room2 = new ChatRoom
                {
                    Name = "room2",
                    Private = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room2);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room2",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/allowed room");

                Assert.True(result);
                notificationService.Verify(x => x.ListAllowedUsers(room), Times.Once());
            }
            
            [Fact]
            public void CannotGetInfoForInvalidRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allowed room3"));
                Assert.Equal("Unable to find room3.", ex.Message);
            }

            [Fact]
            public void CannotGetInfoForInaccessiblePrivateRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    AllowedUsers = new Collection<ChatUser>() { },
                    Users = new Collection<ChatUser>() { }
                };
                repository.Add(room);
                var room2 = new ChatRoom
                {
                    Name = "room2",
                    Private = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room2);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room2",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allowed room"));
                Assert.Equal("You do not have access to room.", ex.Message);
            }
        }

        public class LockCommand
        {
            [Fact]
            public void MissingRoomNameThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/lock"));
                Assert.Equal("Which room do you want to lock?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/lock");

                Assert.True(result);
                notificationService.Verify(x => x.LockRoom(user, room), Times.Once());
                Assert.True(room.Private);
            }

            [Fact]
            public void NotExistingRoomNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/lock room"));
                Assert.Equal("Unable to find room.", ex.Message);
            }

            [Fact]
            public void CanLockRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/lock room");

                Assert.True(result);
                notificationService.Verify(x => x.LockRoom(user, room), Times.Once());
                Assert.True(room.Private);
            }

            [Fact]
            public void CanLockClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room",
                    Private = false,
                    Closed = true
                };
                room.Creator = user;
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/lock room");

                Assert.True(result);
                notificationService.Verify(x => x.LockRoom(user, room), Times.Once());
                Assert.True(room.Private);
            }
        }

        public class CloseCommand
        {
            [Fact]
            public void MissingRoomNameThrowsFromLobby()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                // Act & Assert.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/close"));
                Assert.Equal("Which room do you want to close?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameSucceedsFromRoom()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName
                };
                // Add a room owner.
                room.Owners.Add(user);

                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        roomName,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/close");

                Assert.True(result);
                notificationService.Verify(x => x.CloseRoom(room.Users, room), Times.Once());
                Assert.True(room.Closed);
            }

            [Fact]
            public void NotExistingRoomNameThrows()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                // Act & Assert.
                const string roomName = "ruroh";
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/close " + roomName));
                Assert.Equal("Unable to find " + roomName + ".", ex.Message);
            }

            [Fact]
            public void CannotCloseARoomIfTheUserIsNotAnOwner()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName
                };
                // Add a room owner.
                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/close " + roomName));
                Assert.Equal("You are not an owner of " + roomName + ".", ex.Message);
            }

            [Fact]
            public void CanCloseRoomWithNoPeople()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName
                };
                // Add a room owner.
                room.Owners.Add(user);

                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/close " + roomName);

                Assert.True(result);
                notificationService.Verify(x => x.CloseRoom(room.Users, room), Times.Once());
                Assert.True(room.Closed);
            }

            [Fact]
            public void CanCloseRoomWithPeopleAndOwnerNotInTheRoom()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                var randomUser = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(roomOwner);
                repository.Add(randomUser);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName
                };
                // Add a room owner (but the owner is not INSIDE the room).
                room.Owners.Add(roomOwner);

                // Add a random user.
                room.Users.Add(randomUser);
                randomUser.Rooms.Add(room);

                repository.Add(room);

                // Make a copy of all the users which should be removed from the room, so we can 
                // verify that these users we passed into the closeRoom method.
                var users = room.Users.ToList();

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/close " + roomName);

                Assert.True(result);
                notificationService.Verify(x => x.CloseRoom(users, room), Times.Once());
                Assert.True(room.Closed);
            }
        }

        public class UnAllowCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow"));
                Assert.Equal("Who you want to revoke access permissions from?", ex.Message);
            }

            [Fact]
            public void NotExistingUserNameThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow dfowler2"));
                Assert.Equal("Unable to find dfowler2.", ex.Message);
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow dfowler2"));
                Assert.Equal("Which room do you want to revoke access from?", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/unallow dfowler2");

                Assert.True(result);
                notificationService.Verify(x => x.UnallowUser(user2, room), Times.Once());
                Assert.False(room.AllowedUsers.Contains(user2));
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow dfowler2 asfasfdsad"));
                Assert.Equal("Unable to find asfasfdsad.", ex.Message);
            }

            [Fact]
            public void CannotUnAllowUserToRoomIfNotPrivate()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow dfowler2 room"));
                Assert.Equal("room is not a private room.", ex.Message);
            }

            [Fact]
            public void CanUnAllowUserToRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/unallow dfowler2 room");

                Assert.True(result);
                notificationService.Verify(x => x.UnallowUser(user2, room), Times.Once());
                Assert.False(room.AllowedUsers.Contains(user2));
            }

            [Fact]
            public void CanUnAllowUserToClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
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
                    Private = true,
                    Closed = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                user2.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user2);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/unallow dfowler2 room");

                Assert.True(result);
                notificationService.Verify(x => x.UnallowUser(user2, room), Times.Once());
                Assert.False(room.AllowedUsers.Contains(user2));
            }
        }

        public class FlagCommand
        {
            [Fact]
            public void CanSetFlag()
            {
                // Arrange.
                const string isoCode = "au";
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                bool result = commandManager.TryHandleCommand("/flag " + isoCode);

                Assert.True(result);
                Assert.Equal(isoCode, user.Flag);
                notificationService.Verify(x => x.ChangeFlag(user), Times.Once());
            }

            [Fact]
            public void CanSetFlagWithUppercaseIso()
            {
                // Arrange.
                const string isoCode = "AU";
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                bool result = commandManager.TryHandleCommand("/flag " + isoCode);

                Assert.True(result);
                Assert.Equal(isoCode.ToLowerInvariant(), user.Flag);
                notificationService.Verify(x => x.ChangeFlag(user), Times.Once());
            }

            [Fact]
            public void NoIsoCodeClearsFlag()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                bool result = commandManager.TryHandleCommand("/flag");

                Assert.True(result);
                Assert.Null(user.Flag);
                notificationService.Verify(x => x.ChangeFlag(user), Times.Once());
            }

            [Fact]
            public void IncorrectIsoCodeThrows()
            {
                // Arrange.
                const string isoCode = "xx";
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act and Assert
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/flag " + isoCode));
                Assert.Equal("Sorry, but the country ISO code you requested doesn't exist. Please refer to http://en.wikipedia.org/wiki/ISO_3166-1_alpha-2 for a proper list of country ISO codes.", ex.Message);
            }

            [Fact]
            public void TooLongIsoCodeThrows()
            {
                // Arrange.
                const string isoCode = "xxxxx";
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act and Assert
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/flag " + isoCode));
                Assert.Equal("Sorry, but the country ISO code you requested doesn't exist. Please refer to http://en.wikipedia.org/wiki/ISO_3166-1_alpha-2 for a proper list of country ISO codes.", ex.Message);
            }
        }

        public class MemeCommand
        {
            [Fact]
            public void CanGenerateMeme()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                bool result = commandManager.TryHandleCommand("/meme aa top-line bottom-line");

                // Assert.
                Assert.True(result);
                notificationService.Verify(x => x.GenerateMeme(user, room, "https://upboat.me/aa/top-line/bottom-line.jpg"), Times.Once());
            }

            [Fact]
            public void InLobbyThrows()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "", // In the lobby.
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/meme aa top-line bottom-line"));
                Assert.Equal("This command cannot be invoked from the Lobby.", ex.Message);
            }

            [Fact]
            public void MissingAllArgumentsThrows()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/meme"));
                Assert.Equal("What type of meme do you want to generate and with what message? You need to provide 3 seperate arguments delimeted by spaces. Help: here's the list of memes: http://upboat.me/List .", ex.Message);
            }

            [Fact]
            public void MissingSomeArgumentsThrows()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                // Act.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/meme cd top-line"));
                Assert.Equal("Incorrect number of meme arguments. You need to provide 3 seperate arguments delimeted by spaces. (TIP: use a dash (eg: -) to display a space in your message.", ex.Message);
            }
        }

        public class OpenCommand
        {
            [Fact]
            public void NotLoggedInThrows()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        null,
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                // Act & Assert.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open"));
                Assert.Equal("You're not logged in.", ex.Message);
            }

            [Fact]
            public void MissingRoomNameThrows()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                // Act & Assert.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open"));
                Assert.Equal("Which room do you want to open?", ex.Message);

            }

            [Fact]
            public void NotExistingRoomNameThrows()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                // Act & Assert.
                const string roomName = "ruroh";
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open " + roomName));
                Assert.Equal("Unable to find " + roomName + ".", ex.Message);
            }

            [Fact]
            public void CannotOpenAnAlreadyOpenRoom()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName
                };
                room.Owners.Add(roomOwner);
                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open " + roomName));
                Assert.Equal(roomName + " is already open.", ex.Message);
            }

            [Fact]
            public void CannotOpenARoomIfTheUserIsNotAnOwner()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName,
                    Closed = true
                };
                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open " + roomName));
                Assert.Equal("You are not an owner of " + roomName + ".", ex.Message);
            }

            [Fact]
            public void RoomOpensAndOwnerJoinedAutomaticallyIfUserIsOwner()
            {
                // Arrange.
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);

                const string roomName = "test";
                var room = new ChatRoom
                {
                    Name = roomName,
                    Closed = true
                };
                // Add a room owner
                room.Owners.Add(roomOwner);
                repository.Add(room);

                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                var result = commandManager.TryHandleCommand("/open " + roomName);

                Assert.True(result);
                Assert.False(room.Closed);
                Assert.True(roomOwner.Rooms.Any(x => x.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase)));
            }
        }

        public class TopicCommand
        {
            [Fact]
            public void UserMustBeOwner()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                string topicLine = "This is the room's topic";
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/topic " + topicLine));

                Assert.Equal("You are not an owner of room.", exception.Message);    
            }

            [Fact]
            public void CommandSucceeds()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                string topicLine = "This is the room's topic";
                bool result = commandManager.TryHandleCommand("/topic " + topicLine);

                Assert.True(result);
                Assert.Equal(topicLine, room.Topic);
                notificationService.Verify(x => x.ChangeTopic(roomOwner, room), Times.Once());     
            }

            [Fact]
            public void ThrowsIfRoomClosed()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "room",
                    Closed = true
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                string topicLine = "This is the room's topic";
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/topic " + topicLine));
                Assert.Equal("room is closed.", ex.Message);
            }

            [Fact]
            public void ThrowsIfTopicExceedsMaxLength()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                string topicLine = new String('A', 81);
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/topic " + topicLine));

                Assert.Equal("Sorry, but your topic is too long. Please keep it under 80 characters.", exception.Message);    
            }

            [Fact]
            public void CommandClearsTopicIfNoTextProvided()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/topic");

                Assert.True(result);
                Assert.Equal(null, room.Topic);
                notificationService.Verify(x => x.ChangeTopic(roomOwner, room), Times.Once());
            }
        }

        public class BroadcastCommand
        {
            [Fact]
            public void ThrowsIfUserIsNotAdmin()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1"
                };
                repository.Add(user);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/broadcast something"));
                Assert.Equal("You are not an admin.", ex.Message);
            }

            [Fact]
            public void MissingMessageTextThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    IsAdmin = true
                };
                repository.Add(user);
                
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/broadcast"));
                Assert.Equal("What message do you want to broadcast?", ex.Message);
            }

            [Fact]
            public void CanBroadcastMessage()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "1",
                    IsAdmin = true
                };
                repository.Add(user);
                
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/broadcast what is up?");

                Assert.True(result);
                notificationService.Verify(x => x.BroadcastMessage(user, "what is up?"), Times.Once());
            }
        }

        public class WelcomeCommand
        {
            [Fact]
            public void UserMustBeOwner()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser {
                    Name = "thomasjo",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom {
                    Name = "room"
                };
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                string welcomeMessage = "This is the room's welcome message";
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/welcome " + welcomeMessage));

                Assert.Equal("You are not an owner of room.", exception.Message);
            }

            [Fact]
            public void ThrowsOnClosedRoom()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser
                {
                    Name = "thomasjo",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "room",
                    Closed = true
                };
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                string welcomeMessage = "This is the room's welcome message";
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/welcome " + welcomeMessage));

                Assert.Equal("room is closed.", exception.Message);
            }

            [Fact]
            public void CommandSucceeds()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser {
                    Name = "thomasjo",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom {
                    Name = "room"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                string welcomeMessage = "This is the room's welcome message";
                bool result = commandManager.TryHandleCommand("/welcome " + welcomeMessage);

                Assert.True(result);
                Assert.Equal(welcomeMessage, room.Welcome);
                notificationService.Verify(x => x.ChangeWelcome(roomOwner, room), Times.Once());
            }

            [Fact]
            public void ThrowsIfWelcomeMessageExceedsMaxLength()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser {
                    Name = "thomasjo",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom {
                    Name = "room"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);
                string welcomeMessage = new String('A', 201);
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/welcome " + welcomeMessage));

                Assert.Equal("Sorry, but your welcome is too long. Please keep it under 200 characters.", exception.Message);
            }

            [Fact]
            public void CommandClearsWelcomeIfNoTextProvided()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var roomOwner = new ChatUser {
                    Name = "thomasjo",
                    Id = "1"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom {
                    Name = "room",
                    Welcome = "foo"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "1",
                                                        "room",
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object);

                bool result = commandManager.TryHandleCommand("/welcome");

                Assert.True(result);
                Assert.Equal(null, room.Welcome);
                notificationService.Verify(x => x.ChangeWelcome(roomOwner, room), Times.Once());
            }
        }

        public static T VerifyThrows<T>(string command) where T : Exception
        {
            var repository = new InMemoryRepository();
            var cache = new Mock<ICache>().Object;
            var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
            var notificationService = new Mock<INotificationService>();
            var commandManager = new CommandManager("clientid",
                                                    null,
                                                    null,
                                                    service,
                                                    repository,
                                                    cache,
                                                    notificationService.Object);

            return Assert.Throws<T>(() => commandManager.TryHandleCommand(command));
        }
    }
}
