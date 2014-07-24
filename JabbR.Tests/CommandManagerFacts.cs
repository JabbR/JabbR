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
        public static CommandManager CreateCommandManager()
        {
            IJabbrRepository repository;

            return CreateCommandManager(out repository);
        }

        public static CommandManager CreateCommandManager(out IJabbrRepository repository)
        {
            return CreateCommandManager("roomName", out repository);
        }

        public static CommandManager CreateCommandManager(string roomName, out IJabbrRepository repository)
        {
            Mock<INotificationService> notificationService;

            return CreateCommandManager(roomName, out repository, out notificationService);
        }

        public static CommandManager CreateCommandManager(out IJabbrRepository repository, out Mock<INotificationService> notificationService)
        {
            return CreateCommandManager("roomName", out repository, out notificationService);
        }

        public static CommandManager CreateCommandManager(string roomName, out IJabbrRepository repository, out Mock<INotificationService> notificationService)
        {
            repository = new InMemoryRepository();
            var cache = new Mock<ICache>().Object;
            var recentMessageCache = new Mock<IRecentMessageCache>().Object;
            notificationService = new Mock<INotificationService>();

            var chatService = new ChatService(cache, recentMessageCache, repository);
            var membershipService = new MembershipService(repository, new CryptoService(new SettingsKeyProvider(ApplicationSettings.GetDefaultSettings())));
            return new CommandManager("clientId", "userAgent", "userId", roomName, chatService, repository, cache, notificationService.Object, membershipService);
        }

        public class ParseCommand
        {
            [Fact]
            public void ReturnsTheCommandName()
            {
                var commandManager = CreateCommandManager();

                const string commandName = "thecommand";
                string[] args;
                var parsedName = commandManager.ParseCommand(String.Format("/{0}", commandName), out args);

                Assert.Equal(commandName, parsedName);
            }

            [Fact]
            public void ReturnsNullForEmptyCommandName()
            {
                var commandManager = CreateCommandManager();

                string[] args;
                var parsedName = commandManager.ParseCommand("/", out args);

                Assert.Null(parsedName);
            }

            [Fact]
            public void ParsesTheArguments()
            {
                var commandManager = CreateCommandManager();
            
                var parts = new[] {"/cmd", "arg0", "arg1", "arg2"};
                var command = String.Join(" ", parts);
                
                string[] parsedArgs;
                commandManager.ParseCommand(command, out parsedArgs);

                Assert.Equal(parts.Skip(1), parsedArgs);
            }

            [Fact]
            public void IgnoresMultipleWhitespaceBetweenArguments()
            {
                var commandManager = CreateCommandManager();
                
                var parts = new[] { "/cmd", "arg0", "arg1", "arg2" };
                var command = String.Join("    ", parts);

                string[] parsedArgs;
                commandManager.ParseCommand(command, out parsedArgs);

                Assert.Equal(parts.Skip(1), parsedArgs);
            }

            [Fact]
            public void ProducesEmptyArrayIfNoArguments()
            {
                var commandManager = CreateCommandManager();

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
                var commandManager = CreateCommandManager();

                bool result = commandManager.TryHandleCommand("foo");

                Assert.False(result);
            }

            [Fact]
            public void ReturnsFalseIfCommandStartsWithSlash()
            {
                var commandManager = CreateCommandManager();

                bool result = commandManager.TryHandleCommand("/foo", new string[] { });

                Assert.False(result);
            }

            [Fact]
            public void ThrowsIfCommandDoesntExist()
            {
                IJabbrRepository repository;
                var commandManager = CreateCommandManager(out repository);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    HashedPassword = "password".ToSha256(null)
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };

                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/message"));
                Assert.Equal("message is not a valid command.", ex.Message);
            }

            [Fact]
            public void ThrowsIfCommandIsAmbiguous()
            {
                IJabbrRepository repository;
                var commandManager = CreateCommandManager(out repository);

                var user = new ChatUser
                {
                    Name = "tilde",
                    Id = "userId",
                    HashedPassword = "password".ToSha256(null)
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                };

                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/a"));
                Assert.True(ex.Message.StartsWith("a is ambiguous: "));
            }

            [Fact]
            public void DoesNotThrowIfCommandIsPrefixButExactMatch()
            {
                var commandManager = CreateCommandManager();

                ICommand command;
                commandManager.MatchCommand("invite", out command);

                Assert.IsType<JabbR.Commands.InviteCommand>(command);
            }

            [Fact]
            public void DoesNotThrowIfCommandIsPrefixButCaseInsensitiveMatch()
            {
                var commandManager = CreateCommandManager();

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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);
                
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);

                bool result = commandManager.TryHandleCommand("/logout");

                Assert.True(result);
                notificationService.Verify(m => m.LogOut(user, "clientId"), Times.Once());
            }
        }

        public class InviteCodeCommand
        {
            [Fact]
            public void InviteCodeShowsErrorForPublicRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("Only private rooms can have invite codes.", ex.Message);
            }

            [Fact]
            public void InviteCodeSetsCodeIfNoCodeAndCurrentUserOwner()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/invitecode roomName");

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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("This command cannot be invoked from the Lobby.", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);
                
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                room.AllowedUsers.Add(user);
                repository.Add(room);
                
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                room.AllowedUsers.Add(user);
                repository.Add(room);
                
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Private = true,
                    Closed = true,
                    InviteCode = "123457"
                };
                room2.Users.Add(user);
                room2.AllowedUsers.Add(user);
                repository.Add(room2);

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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Owners.Add(user);
                room.Users.Add(user);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/resetinvitecode");

                Assert.True(result);
                Assert.NotEqual("123456", room.InviteCode);
            }

            [Fact]
            public void InviteCodeResetCodeWhenResetInviteCodeCalledOnClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                room2.Owners.Add(user);
                room2.Users.Add(user);
                repository.Add(room2);

                bool result = commandManager.TryHandleCommand("/resetinvitecode room");

                Assert.True(result);
                Assert.NotEqual("123456", room.InviteCode);
            }

            [Fact]
            public void ThrowsIfNonUserRequestsInviteCodeWhenNoneSet()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true
                };
                room.Users.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("You do not have access to roomName.", ex.Message);
            }

            [Fact]
            public void ThrowsIfNonOwnerRequestsInviteCodeWhenNoneSet()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true
                };
                room.Users.Add(user);
                room.AllowedUsers.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invitecode"));
                Assert.Equal("You are not an owner of roomName.", ex.Message);
            }

            [Fact]
            public void ThrowsIfNonUserRequestsResetInviteCode()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/resetinvitecode"));
                Assert.Equal("You do not have access to roomName.", ex.Message);
            }

            [Fact]
            public void ThrowsIfNonOwnerRequestsResetInviteCode()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                room.Users.Add(user);
                room.AllowedUsers.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/resetinvitecode"));
                Assert.Equal("You are not an owner of roomName.", ex.Message);
            }
        }

        public class JoinCommand
        {
            [Fact]
            public void DoesNotThrowIfUserAlreadyInRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/join roomName");

                Assert.True(result);
                notificationService.Verify(m => m.JoinRoom(user, room), Times.Once());
            }

            [Fact]
            public void CanJoinRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/join roomName");

                Assert.True(result);
                notificationService.Verify(m => m.JoinRoom(user, room), Times.Once());
            }

            [Fact]
            public void ThrowIfRoomIsEmpty()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join"));
                Assert.Equal("Which room do you want to join?", ex.Message);
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndNoInviteCodeProvided()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true
                };
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join roomName"));
                Assert.Equal("Unable to join roomName. This room is locked and you don't have permission to enter. If you have an invite code, enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndInviteCodeIncorrect()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join roomName 789012"));
                Assert.Equal("Unable to join roomName. This room is locked and you don't have permission to enter. If you have an invite code, enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void ThrowIfUserNotAllowedAndNoInviteCodeSet()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true
                };
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join roomName 789012"));
                Assert.Equal("Unable to join roomName. This room is locked and you don't have permission to enter. If you have an invite code, enter it in the /join command",
                             ex.Message);
            }

            [Fact]
            public void JoinIfInviteCodeIsCorrect()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    InviteCode = "123456"
                };
                repository.Add(room);
                
                commandManager.TryHandleCommand("/join roomName 123456");
                notificationService.Verify(ns => ns.JoinRoom(user, room));
            }

            [Fact]
            public void JoinIfUserIsAllowed()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true
                };
                room.AllowedUsers.Add(user);
                repository.Add(room);

                commandManager.TryHandleCommand("/join roomName");
                notificationService.Verify(ns => ns.JoinRoom(user, room));
            }

            [Fact]
            public void ThrowIfUserNotAllowedToJoinPrivateRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "anurse",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = true
                };
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/join roomName"));
                Assert.Equal("Unable to join roomName. This room is locked and you don't have permission to enter. If you have an invite code, enter it in the /join command",
                             ex.Message);
            }
        }

        public class AddOwnerCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/addowner "));
                Assert.Equal("Who do you want to make an owner?", ex.Message);
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/addowner dfowler"));
                Assert.Equal("Which room do you want to add ownership to?", ex.Message);
            }

            [Fact]
            public void CanAddOwnerToRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomOwnerUser = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                
                var result = commandManager.TryHandleCommand("/addowner dfowler2 test");

                Assert.True(result);
                notificationService.Verify(m => m.AddOwner(targetUser, room), Times.Once());
                Assert.True(room.Owners.Contains(targetUser));
                Assert.True(targetUser.OwnedRooms.Contains(room));
            }

            [Fact]
            public void ThrowsIfRoomIsClosed()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwnerUser = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Closed = true
                };
                room.Owners.Add(roomOwnerUser);
                roomOwnerUser.Rooms.Add(room);
                targetUser.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/addowner dfowler2"));
                Assert.Equal("roomName is closed.", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwnerUser = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                room.Owners.Add(roomOwnerUser);
                roomOwnerUser.Rooms.Add(room);
                targetUser.Rooms.Add(room);
                repository.Add(room);
                
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/removeowner "));
                Assert.Equal("Which owner do you want to remove?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameThrowsFromLobby()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/removeowner dfowler"));
                Assert.Equal("Which room do you want to remove the owner from?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomCreator = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Creator = roomCreator
                };
                room.Owners.Add(roomCreator);
                roomCreator.Rooms.Add(room);
                // make target user an owner
                room.Owners.Add(targetUser);
                targetUser.Rooms.Add(room);
                repository.Add(room);

                var result = commandManager.TryHandleCommand("/removeowner dfowler2");

                Assert.True(result);
                notificationService.Verify(m => m.RemoveOwner(targetUser, room), Times.Once());
                Assert.False(room.Owners.Contains(targetUser));
                Assert.False(targetUser.OwnedRooms.Contains(room));
            }

            [Fact]
            public void CreatorCanRemoveOwnerFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomCreator = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Creator = roomCreator
                };
                room.Owners.Add(roomCreator);
                roomCreator.Rooms.Add(room);
                // make target user an owner
                room.Owners.Add(targetUser);
                targetUser.Rooms.Add(room);
                repository.Add(room);

                var result = commandManager.TryHandleCommand("/removeowner dfowler2 roomName");

                Assert.True(result);
                notificationService.Verify(m => m.RemoveOwner(targetUser, room), Times.Once());
                Assert.False(room.Owners.Contains(targetUser));
                Assert.False(targetUser.OwnedRooms.Contains(room));
            }

            [Fact]
            public void CannotRemoveOwnerFromClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomCreator = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/removeowner dfowler2 test"));
                Assert.Equal("test is closed.", ex.Message);
            }

            [Fact]
            public void NonCreatorsCannotRemoveOwnerFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/removeowner dfowler2 test"));
                Assert.Equal("You are not the creator of test.", ex.Message);
            }
        }

        public class CreateCommand
        {
            [Fact]
            public void MissingRoomNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/create "));
                Assert.Equal("No room specified.", ex.Message);
            }

            [Fact]
            public void ThrowsIfRoomNameIsEmpty()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("create", new [] { "", "" }));
                Assert.Equal("Room names cannot contain spaces.", ex.Message);
            }

            [Fact]
            public void CreateRoomFailsIfRoomAlreadyExists()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "Test"
                };
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/create Test"));
                Assert.Equal("Test already exists.", ex.Message);
            }

            [Fact]
            public void CreateRoomFailsIfRoomNameContainsSpaces()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/create Test Room"));
                Assert.Equal("Room names cannot contain spaces.", ex.Message);
            }

            [Fact]
            public void CreateRoomFailsIfRoomAlreadyExistsButItsClosed()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Closed = true
                };
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/create roomName"));
                Assert.Equal("roomName already exists, but it's closed.", ex.Message);
            }

            [Fact]
            public void CanCreateRoomAndJoinsAutomatically()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
               
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/gravatar "));
                Assert.Equal("Which email address do you want to use for the Gravatar image?", ex.Message);
            }

            [Fact]
            public void CanSetGravatar()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "UserId"
                };
                repository.Add(user);
                
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                const string note = "this is a test note. Pew^Pew";
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                bool result = commandManager.TryHandleCommand("/note " + note);

                Assert.True(result);
                Assert.Equal(note, user.Note);
                notificationService.Verify(x => x.ChangeNote(user), Times.Once());
            }

            [Fact]
            public void CanSetNoteWithNoTextClearsTheNoteProperty()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                bool result = commandManager.TryHandleCommand("/note ");

                Assert.True(result);
                Assert.Null(user.Note);
                notificationService.Verify(x => x.ChangeNote(user), Times.Once());
            }

            [Fact]
            public void ThrowsIfNoteTextDoesNotValidate()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                String note = new String('A', 141);
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/note " + note));
                Assert.Equal("Sorry, but your note is too long. Please keep it under 140 characters.", ex.Message);
            }
        }

        public class AfkCommand
        {
            [Fact]
            public void CanSetAfkWithTextSetsTheNoteProperty()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                const string note = "I'll be back later!";
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "userId"
                };
                repository.Add(user);
                
                bool result = commandManager.TryHandleCommand("/afk " + note);

                Assert.True(result);
                Assert.Equal(note, user.AfkNote);
                notificationService.Verify(x => x.ChangeAfk(user), Times.Once());
            }

            [Fact]
            public void CanSetAfkWithNoTextSetTheNoteProperty()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                bool result = commandManager.TryHandleCommand("/afk");

                Assert.True(result);
                Assert.Null(user.AfkNote);
                notificationService.Verify(x => x.ChangeAfk(user), Times.Once());
            }

            [Fact]
            public void ThrowsIfAfkTextIsNotValid()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                String note = new String('A', 141);
                var user = new ChatUser
                {
                    Name = "asshat",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/afk " + note));
                Assert.Equal("Sorry, but your note is too long. Please keep it under 140 characters.", ex.Message);
            }
        }

        public class HelpCommand
        {
            [Fact]
            public void CanShowHelp()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick"));
                Assert.Equal("Who do you want to to kick?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameThrowsFromLobby()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick cjm1"));
                Assert.Equal("Which room do you want to kick them from?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Users.Add(user3);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                user3.Rooms.Add(room);
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/kick dfowler3");

                Assert.True(result);
                Assert.False(room.Users.Contains(user3));
                Assert.False(user3.Rooms.Contains(room));
                notificationService.Verify(x => x.KickUser(user3, room, user, null), Times.Once());
            }

            [Fact]
            public void CannotKickUserIfNotExists()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick fowler3"));
                Assert.Equal("Unable to find fowler3.", ex.Message);
            }

            [Fact]
            public void CannotKickUserIfUserIsNotInRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user3 = new ChatUser
                {
                    Name = "dfowler3",
                    Id = "3"
                };
                repository.Add(user3);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler3"));
                Assert.Equal("dfowler3 isn't in roomName.", ex.Message);
            }

            [Fact]
            public void CanKickUser()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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

                bool result = commandManager.TryHandleCommand("/kick dfowler3 room");

                Assert.True(result);
                Assert.False(room.Users.Contains(user3));
                Assert.False(user3.Rooms.Contains(room));
                notificationService.Verify(x => x.KickUser(user3, room, user, null), Times.Once());
            }

            [Fact]
            public void CannotKickUserInClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler3 room"));
                Assert.Equal("room is closed.", ex.Message);
            }

            [Fact]
            public void CannotKickUrSelf()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler"));
                Assert.Equal("Why would you want to kick yourself?", ex.Message);
            }

            [Fact]
            public void CannotKickIfUserIsNotOwner()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user2 = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user2);
                var user = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler"));
                Assert.Equal("You are not an owner of roomName.", ex.Message);
            }

            [Fact]
            public void IfNotRoomCreatorCannotKickOwners()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Creator = user2
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                room.Owners.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/kick dfowler2"));
                Assert.Equal("Owners cannot kick other owners. Only the room creator can kick an owner.", ex.Message);
            }

            [Fact]
            public void RoomCreatorCanKickOwners()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Creator = user
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                room.Owners.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                var result = commandManager.TryHandleCommand("/kick dfowler2");

                Assert.True(result);
                Assert.False(room.Users.Contains(user2));
                Assert.False(user2.Rooms.Contains(room));
                notificationService.Verify(x => x.KickUser(user2, room, user, null), Times.Once());
            }
        }

        public class BanCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    IsAdmin = true
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban"));
                Assert.Equal("Who do you want to ban?", ex.Message);
            }

            [Fact]
            public void BannerIsNotAnAdminThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                var ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban dfowler2"));

                Assert.True(ex.Message == "You are not an admin.");
            }

            [Fact]
            public void CannotBanUserIfNotExists()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban fowler3"));
                Assert.Equal("Unable to find fowler3.", ex.Message);
            }

            [Fact]
            public void AdminCannotBanAdmin()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban dfowler2"));
                Assert.Equal("You cannot ban another admin.", ex.Message);
            }

            [Fact]
            public void CannotBanUrSelf()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    IsAdmin = true
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/ban dfowler"));
                Assert.Equal("You cannot ban yourself!", ex.Message);
            }
        }

        public class UnbanCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    IsAdmin = true
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unban"));
                Assert.Equal("Who do you want to unban?", ex.Message);
            }

            [Fact]
            public void UnbannerIsNotAnAdminThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                var ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unban dfowler2"));

                Assert.True(ex.Message == "You are not an admin.");
            }

            [Fact]
            public void CannotUnbanUserIfNotExists()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unban fowler3"));
                Assert.Equal("Unable to find fowler3.", ex.Message);
            }

            [Fact]
            public void AdminCannotUnbanAdmin()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unban dfowler2"));
                Assert.Equal("You cannot unban another admin.", ex.Message);
            }

            [Fact]
            public void CannotUnbanUrSelf()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    IsAdmin = true
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unban dfowler"));
                Assert.Equal("You cannot unban another admin.", ex.Message);
            }

            [Fact]
            public void AdminUnbanBannedUser()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    IsAdmin = true
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2",
                    IsAdmin = false,
                    IsBanned = true
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                var ex = commandManager.TryHandleCommand("/unban dfowler2");

                Assert.False(user2.IsBanned);
            }

            [Fact]
            public void AdminUnbanUnbannedUser()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    IsAdmin = true
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2",
                    IsAdmin = false,
                    IsBanned = false
                };
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                var ex = commandManager.TryHandleCommand("/unban dfowler2");

                Assert.False(user2.IsBanned);
            }
        }

        public class CheckBannedCommand
        {
            [Fact]
            public void CheckBannerIsNotAnAdminThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "1"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                var ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/checkbanned dfowler2"));

                Assert.True(ex.Message == "You are not an admin.");
            }

            [Fact]
            public void CannotCheckBannedUserIfNotExists()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/checkbanned fowler3"));
                Assert.Equal("Unable to find fowler3.", ex.Message);
            }

        }

        public class LeaveCommand
        {
            [Fact]
            public void CanLeaveRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/leave");

                Assert.True(result);
                Assert.False(room.Users.Contains(user));
                Assert.False(user.Rooms.Contains(room));
                notificationService.Verify(x => x.LeaveRoom(user, room), Times.Once());
            }

            [Fact]
            public void CanLeaveClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Closed = true
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/leave");

                Assert.True(result);
                Assert.False(room.Users.Contains(user));
                Assert.False(user.Rooms.Contains(room));
                notificationService.Verify(x => x.LeaveRoom(user, room), Times.Once());
            }

            [Fact]
            public void CanLeaveRoomByGivingRoomName()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/leave roomName");

                Assert.True(result);
                Assert.False(room.Users.Contains(user));
                Assert.False(user.Rooms.Contains(room));
                notificationService.Verify(x => x.LeaveRoom(user, room), Times.Once());
            }

            [Fact]
            public void CannotLeaveRoomIfNotInRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/leave"));
                Assert.Equal("Which room do you want to leave?", ex.Message);
            }
        }

        public class ListCommand
        {
            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/list"));
                Assert.Equal("Which room do you want to list the current users of?", ex.Message);
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/list test"));
                Assert.Equal("Unable to find test.", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                
                var userList = new List<string>();
                notificationService.Setup(m => m.ListUsers(It.IsAny<ChatRoom>(), It.IsAny<IEnumerable<string>>()))
                                   .Callback<ChatRoom, IEnumerable<string>>((_, names) =>
                                   {
                                       userList.AddRange(names);
                                   });

                bool result = commandManager.TryHandleCommand("/list");

                Assert.True(result);
                Assert.Equal(2, userList.Count);
                Assert.True(userList.Contains("dfowler2"));
                Assert.True(userList.Contains("dfowler"));
            }

            [Fact]
            public void CanShowUserList()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);

                var userList = new List<string>();
                notificationService.Setup(m => m.ListUsers(It.IsAny<ChatRoom>(), It.IsAny<IEnumerable<string>>()))
                                   .Callback<ChatRoom, IEnumerable<string>>((_, names) =>
                                   {
                                       userList.AddRange(names);
                                   });

                bool result = commandManager.TryHandleCommand("/list roomName");

                Assert.True(result);
                Assert.Equal(2, userList.Count);
                Assert.True(userList.Contains("dfowler2"));
                Assert.True(userList.Contains("dfowler"));
            }

            [Fact]
            public void ThrowsOnClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Closed = true
                };
                room.Users.Add(user);
                room.Users.Add(user2);
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                repository.Add(room);
                
                var userList = new List<string>();
                notificationService.Setup(m => m.ListUsers(It.IsAny<ChatRoom>(), It.IsAny<IEnumerable<string>>()))
                                   .Callback<ChatRoom, IEnumerable<string>>((_, names) =>
                                   {
                                       userList.AddRange(names);
                                   });

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/list roomName"));
                Assert.Equal("roomName is closed.", ex.Message);
            }
        }

        public class MeCommand
        {
            [Fact]
            public void MissingContentThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/me"));
                Assert.Equal("You what?", ex.Message);
            }

            [Fact]
            public void MissingRoomThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/me is testing"));
                Assert.Equal("Use '/join room' to join a room.", ex.Message);
            }

            [Fact]
            public void CanUseMeCommand()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/me is testing");

                Assert.True(result);
                notificationService.Verify(x => x.OnSelfMessage(room, user, "is testing"), Times.Once());
            }

            [Fact]
            public void ClosedRoomThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Closed = true
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/me is testing"));
                Assert.Equal("roomName is closed.", ex.Message);
            }
        }

        public class MsgCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/msg"));
                Assert.Equal("Who do you want to send a private message to?", ex.Message);
            }

            [Fact]
            public void ThrowsIfUserDoesntExist()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/msg dfowler3"));
                Assert.Equal("Unable to find dfowler3.", ex.Message);
            }

            [Fact]
            public void CannotMessageOwnUser()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/msg dfowler"));
                Assert.Equal("You can't private message yourself!", ex.Message);
            }

            [Fact]
            public void MissingMessageTextThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/msg dfowler2 "));
                Assert.Equal("What do you want to say to dfowler2?", ex.Message);
            }

            [Fact]
            public void CanSendMessage()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite"));
                Assert.Equal("Who do you want to invite?", ex.Message);
            }

            [Fact]
            public void NotExistingUserNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite dfowler2"));
                Assert.Equal("Unable to find dfowler2.", ex.Message);
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite dfowler2"));
                Assert.Equal("Which room do you want to invite them to?", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/invite dfowler2");

                Assert.True(result);
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite dfowler2 asfasfdsad"));
                Assert.Equal("Unable to find asfasfdsad.", ex.Message);
            }

            [Fact]
            public void ThrowsIfThereIsOnlyOneUser()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "room"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite void"));
                Assert.Equal("Unable to find void.", ex.Message);
            }

            [Fact]
            public void CannotInviteOwnUser()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/invite dfowler"));
                Assert.Equal("You can't invite yourself!", ex.Message);
            }
        }

        public class NudgeCommand
        {
            [Fact]
            public void ThrowsIfUserDoesntExists()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge void"));
                Assert.Equal("Unable to find void.", ex.Message);
            }

            [Fact]
            public void CannotNudgeOwnUser()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge dfowler"));
                Assert.Equal("You can't nudge yourself!", ex.Message);
            }

            [Fact]
            public void NudgingTwiceWithin60SecondsThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                
                bool result = commandManager.TryHandleCommand("/nudge dfowler2");

                Assert.True(result);
                notificationService.Verify(x => x.NudgeUser(user, user2), Times.Once());
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge dfowler2"));
                Assert.Equal("User can only be nudged once every 60 seconds.", ex.Message);
            }

            [Fact]
            public void CanNudge()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user2);
                
                bool result = commandManager.TryHandleCommand("/nudge dfowler2");

                Assert.True(result);
                notificationService.Verify(x => x.NudgeUser(user, user2), Times.Once());
                Assert.NotNull(user2.LastNudged);
            }

            [Fact]
            public void CanNudgeRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                user2.Rooms.Add(room);
                room.Users.Add(user2);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/nudge");

                Assert.True(result);
                notificationService.Verify(x => x.NudgeRoom(room, user), Times.Once());
                Assert.NotNull(room.LastNudged);
            }

            [Fact]
            public void CannotNudgeRoomTwiceWithin60Seconds()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                user2.Rooms.Add(room);
                room.Users.Add(user2);
                repository.Add(room);

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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Closed = true
                };
                user.Rooms.Add(room);
                room.Users.Add(user);
                user2.Rooms.Add(room);
                room.Users.Add(user2);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/nudge"));
                Assert.Equal("roomName is closed.", ex.Message);
            }
        }

        public class WhoCommand
        {
            [Fact]
            public void CanGetUserList()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                bool result = commandManager.TryHandleCommand("/who");

                Assert.True(result);
                notificationService.Verify(x => x.ListUsers(), Times.Once());
            }

            [Fact]
            public void CanGetUserInfo()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                bool result = commandManager.TryHandleCommand("/who dfowler");

                Assert.True(result);
                notificationService.Verify(x => x.ShowUserInfo(user), Times.Once());
            }

            [Fact]
            public void CannotGetInfoForInvalidUser()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/who sethwebster"));
                Assert.Equal("Unable to find sethwebster.", ex.Message);
            }
        }

        public class WhereCommand
        {
            [Fact]
            public void MissingUserNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/where"));
                Assert.Equal("Who do you want to locate?", ex.Message);
            }

            [Fact]
            public void CannotShowUserRoomsWhenEnteringPartOfName()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/where dfow"));
                Assert.Equal("Unable to find dfow.", ex.Message);
            }

            [Fact]
            public void CanShowUserRooms()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow"));
                Assert.Equal("Who do you want to grant access permissions to?", ex.Message);
            }

            [Fact]
            public void NotExistingUserNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow dfowler2"));
                Assert.Equal("Unable to find dfowler2.", ex.Message);
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow dfowler2"));
                Assert.Equal("Which room do you want to allow access to?", ex.Message);
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow dfowler2 asfasfdsad"));
                Assert.Equal("Unable to find asfasfdsad.", ex.Message);
            }

            [Fact]
            public void CannotAllowUserToRoomIfNotPrivate()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName"
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allow dfowler2 roomName"));
                Assert.Equal("roomName is not a private room.", ex.Message);
            }

            [Fact]
            public void CanAllowUserToRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Private = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/allow dfowler2 roomName");

                Assert.True(result);
                notificationService.Verify(x => x.AllowUser(user2, room), Times.Once());
                Assert.True(room.AllowedUsers.Contains(user2));
            }

            [Fact]
            public void CanAllowUserToClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Private = true,
                    Closed = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/allow dfowler2 roomName");

                Assert.True(result);
                notificationService.Verify(x => x.AllowUser(user2, room), Times.Once());
                Assert.True(room.AllowedUsers.Contains(user2));
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Private = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                repository.Add(room);

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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName", 
                    Private = true, 
                    AllowedUsers = new Collection<ChatUser>() { user },
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/allowed");

                Assert.True(result);
                notificationService.Verify(x => x.ListAllowedUsers(room), Times.Once());
            }

            [Fact]
            public void CanGetAllowedUserListPublicRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = false,
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/allowed roomName");

                Assert.True(result);
                notificationService.Verify(x => x.ListAllowedUsers(room), Times.Once());
            }

            [Fact]
            public void CanGetAllowedUserListSpecified()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "otherRoom",
                    Private = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                };
                repository.Add(room);
                var room2 = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room2);

                bool result = commandManager.TryHandleCommand("/allowed otherRoom");

                Assert.True(result);
                notificationService.Verify(x => x.ListAllowedUsers(room), Times.Once());
            }

            [Fact]
            public void CanGetAllowedUserListClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "otherRoom",
                    Private = true,
                    Closed = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                };
                repository.Add(room);
                var room2 = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room2);
                
                bool result = commandManager.TryHandleCommand("/allowed otherRoom");

                Assert.True(result);
                notificationService.Verify(x => x.ListAllowedUsers(room), Times.Once());
            }
            
            [Fact]
            public void CannotGetInfoForInvalidRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allowed room3"));
                Assert.Equal("Unable to find room3.", ex.Message);
            }

            [Fact]
            public void CannotGetInfoForInaccessiblePrivateRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "otherRoom",
                    Private = true,
                    AllowedUsers = new Collection<ChatUser>() { },
                    Users = new Collection<ChatUser>() { }
                };
                repository.Add(room);
                var room2 = new ChatRoom
                {
                    Name = "roomName",
                    Private = true,
                    AllowedUsers = new Collection<ChatUser>() { user },
                    Users = new Collection<ChatUser>() { user }
                };
                repository.Add(room2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/allowed otherRoom"));
                Assert.Equal("You do not have access to otherRoom.", ex.Message);
            }
        }

        public class LockCommand
        {
            [Fact]
            public void MissingRoomNameThrowsFromLobby()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/lock"));
                Assert.Equal("Which room do you want to lock?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Private = false
                };
                room.Creator = user;
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/lock");

                Assert.True(result);
                notificationService.Verify(x => x.LockRoom(user, room), Times.Once());
                Assert.True(room.Private);
            }

            [Fact]
            public void NotExistingRoomNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/lock room"));
                Assert.Equal("Unable to find room.", ex.Message);
            }

            [Fact]
            public void CanLockRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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

                bool result = commandManager.TryHandleCommand("/lock room");

                Assert.True(result);
                notificationService.Verify(x => x.LockRoom(user, room), Times.Once());
                Assert.True(room.Private);
            }

            [Fact]
            public void CanLockClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                // Act & Assert.
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/close"));
                Assert.Equal("Which room do you want to close?", ex.Message);
            }

            [Fact]
            public void MissingRoomNameSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Owners.Add(user);

                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/close");

                Assert.True(result);
                notificationService.Verify(x => x.CloseRoom(room.Users, room), Times.Once());
                Assert.True(room.Closed);
            }

            [Fact]
            public void NotExistingRoomNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/close ruroh"));
                Assert.Equal("Unable to find ruroh.", ex.Message);
            }

            [Fact]
            public void CannotCloseARoomIfTheUserIsNotAnOwner()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                repository.Add(room);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/close roomName"));
                Assert.Equal("You are not an owner of roomName.", ex.Message);
            }

            [Fact]
            public void CanCloseRoomWithNoPeople()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Owners.Add(user);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/close roomName");

                Assert.True(result);
                notificationService.Verify(x => x.CloseRoom(room.Users, room), Times.Once());
                Assert.True(room.Closed);
            }

            [Fact]
            public void CanCloseRoomWithPeopleAndOwnerNotInTheRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var randomUser = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(roomOwner);
                repository.Add(randomUser);

                var room = new ChatRoom
                {
                    Name = "roomName"
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

                bool result = commandManager.TryHandleCommand("/close roomName");

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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow"));
                Assert.Equal("Who you want to revoke access permissions from?", ex.Message);
            }

            [Fact]
            public void NotExistingUserNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);
                
                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow dfowler2"));
                Assert.Equal("Unable to find dfowler2.", ex.Message);
            }

            [Fact]
            public void MissingRoomThrowsFromLobby()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow dfowler2"));
                Assert.Equal("Which room do you want to revoke access from?", ex.Message);
            }

            [Fact]
            public void MissingRoomSucceedsFromRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Private = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                user2.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user2);
                repository.Add(room);

                bool result = commandManager.TryHandleCommand("/unallow dfowler2");

                Assert.True(result);
                notificationService.Verify(x => x.UnallowUser(user2, room, user), Times.Once());
                Assert.False(room.AllowedUsers.Contains(user2));
            }

            [Fact]
            public void NotExistingRoomThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                var user2 = new ChatUser
                {
                    Name = "dfowler2",
                    Id = "2"
                };
                repository.Add(user);
                repository.Add(user2);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow dfowler2 asfasfdsad"));
                Assert.Equal("Unable to find asfasfdsad.", ex.Message);
            }

            [Fact]
            public void CannotUnAllowUserToRoomIfNotPrivate()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/unallow dfowler2 room"));
                Assert.Equal("room is not a private room.", ex.Message);
            }

            [Fact]
            public void CanUnAllowUserToRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                
                bool result = commandManager.TryHandleCommand("/unallow dfowler2 room");

                Assert.True(result);
                notificationService.Verify(x => x.UnallowUser(user2, room, user), Times.Once());
                Assert.False(room.AllowedUsers.Contains(user2));
            }

            [Fact]
            public void CanUnAllowUserToClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
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
                    Name = "roomName",
                    Private = true,
                    Closed = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                user2.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user2);
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/unallow dfowler2 roomName");

                Assert.True(result);
                notificationService.Verify(x => x.UnallowUser(user2, room, user), Times.Once());
                Assert.False(room.AllowedUsers.Contains(user2));
            }
        }

        public class FlagCommand
        {
            [Fact]
            public void CanSetFlag()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                bool result = commandManager.TryHandleCommand("/flag au");

                Assert.True(result);
                Assert.Equal("au", user.Flag);
                notificationService.Verify(x => x.ChangeFlag(user), Times.Once());
            }

            [Fact]
            public void CanSetFlagWithUppercaseIso()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                bool result = commandManager.TryHandleCommand("/flag AU");

                Assert.True(result);
                Assert.Equal("au", user.Flag);
                notificationService.Verify(x => x.ChangeFlag(user), Times.Once());
            }

            [Fact]
            public void NoIsoCodeClearsFlag()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                bool result = commandManager.TryHandleCommand("/flag");

                Assert.True(result);
                Assert.Null(user.Flag);
                notificationService.Verify(x => x.ChangeFlag(user), Times.Once());
            }

            [Fact]
            public void IncorrectIsoCodeThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/flag XX"));
                Assert.Equal("Sorry, but the country ISO code you requested doesn't exist. Please refer to http://en.wikipedia.org/wiki/ISO_3166-1_alpha-2 for a proper list of country ISO codes.", ex.Message);
            }

            [Fact]
            public void TooLongIsoCodeThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/flag xxxxx"));
                Assert.Equal("Sorry, but the country ISO code you requested doesn't exist. Please refer to http://en.wikipedia.org/wiki/ISO_3166-1_alpha-2 for a proper list of country ISO codes.", ex.Message);
            }
        }

        public class MemeCommand
        {
            [Fact]
            public void CanGenerateMeme()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/meme aa top-line bottom-line");

                Assert.True(result);
                notificationService.Verify(x => x.GenerateMeme(user, room, "https://upboat.me/aa/top-line/bottom-line.jpg"), Times.Once());
            }

            [Fact]
            public void EncodesSpecialCharacters()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                repository.Add(room);
                
                bool result = commandManager.TryHandleCommand("/meme ramsay cold-hands? type-faster.../.");

                Assert.True(result);
                notificationService.Verify(x => x.GenerateMeme(user, room, "https://upboat.me/ramsay/cold-hands%3F/type-faster...%2F..jpg"), Times.Once());
            }

            [Fact]
            public void InLobbyThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/meme aa top-line bottom-line"));
                Assert.Equal("This command cannot be invoked from the Lobby.", ex.Message);
            }

            [Fact]
            public void MissingAllArgumentsThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                repository.Add(room);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/meme"));
                Assert.Equal("What type of meme do you want to generate, and with what message? You need to provide 3 seperate arguments delimeted by spaces. The list of available memes is at: https://upboat.me/List .", ex.Message);
            }

            [Fact]
            public void MissingSomeArgumentsThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                repository.Add(room);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/meme cd top-line"));
                Assert.Equal("Incorrect number of meme arguments. You need to provide 3 seperate arguments delimeted by spaces. Use a dash (eg: your-message) to display a space in your message.", ex.Message);
            }
        }

        public class OpenCommand
        {
            [Fact]
            public void NotLoggedInThrows()
            {
                var repository = new InMemoryRepository();
                var cache = new Mock<ICache>().Object;
                var service = new ChatService(cache, new Mock<IRecentMessageCache>().Object, repository);
                var notificationService = new Mock<INotificationService>();
                var commandManager = new CommandManager("clientid",
                                                        "userAgent",
                                                        null,
                                                        null,
                                                        service,
                                                        repository,
                                                        cache,
                                                        notificationService.Object,
                                                        null);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open"));
                Assert.Equal("You're not logged in.", ex.Message);
            }

            [Fact]
            public void MissingRoomNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open"));
                Assert.Equal("Which room do you want to open?", ex.Message);

            }

            [Fact]
            public void NotExistingRoomNameThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open ruroh"));
                Assert.Equal("Unable to find ruroh.", ex.Message);
            }

            [Fact]
            public void CannotOpenAnAlreadyOpenRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);

                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Owners.Add(roomOwner);
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open roomName"));
                Assert.Equal("roomName is already open.", ex.Message);
            }

            [Fact]
            public void CannotOpenARoomIfTheUserIsNotAnOwner()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);

                var room = new ChatRoom
                {
                    Name = "roomName",
                    Closed = true
                };
                repository.Add(room);

                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/open roomName"));
                Assert.Equal("You are not an owner of roomName.", ex.Message);
            }

            [Fact]
            public void RoomOpensAndOwnerJoinedAutomaticallyIfUserIsOwner()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);

                var room = new ChatRoom
                {
                    Name = "roomName",
                    Closed = true
                };
                room.Owners.Add(roomOwner);
                repository.Add(room);

                var result = commandManager.TryHandleCommand("/open roomName");

                Assert.True(result);
                Assert.False(room.Closed);
                Assert.True(roomOwner.Rooms.Any(x => x.Name.Equals("roomName", StringComparison.OrdinalIgnoreCase)));
            }
        }

        public class TopicCommand
        {
            [Fact]
            public void UserMustBeOwner()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Users.Add(roomOwner);
                repository.Add(room);
                
                string topicLine = "This is the room's topic";
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/topic " + topicLine));

                Assert.Equal("You are not an owner of roomName.", exception.Message);    
            }

            [Fact]
            public void CommandSucceeds()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                
                string topicLine = "This is the room's topic";
                bool result = commandManager.TryHandleCommand("/topic " + topicLine);

                Assert.True(result);
                Assert.Equal(topicLine, room.Topic);
                notificationService.Verify(x => x.ChangeTopic(roomOwner, room), Times.Once());     
            }

            [Fact]
            public void ThrowsIfRoomClosed()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Closed = true
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                
                string topicLine = "This is the room's topic";
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/topic " + topicLine));
                Assert.Equal("roomName is closed.", ex.Message);
            }

            [Fact]
            public void ThrowsIfTopicExceedsMaxLength()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                
                string topicLine = new String('A', 81);
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/topic " + topicLine));

                Assert.Equal("Sorry, but your topic is too long. Please keep it under 80 characters.", exception.Message);    
            }

            [Fact]
            public void CommandClearsTopicIfNoTextProvided()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "roomName"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/broadcast something"));
                Assert.Equal("You are not an admin.", ex.Message);
            }

            [Fact]
            public void MissingMessageTextThrows()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    IsAdmin = true
                };
                repository.Add(user);
                
                HubException ex = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/broadcast"));
                Assert.Equal("What message do you want to broadcast?", ex.Message);
            }

            [Fact]
            public void CanBroadcastMessage()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(null, out repository, out notificationService);

                var user = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId",
                    IsAdmin = true
                };
                repository.Add(user);
                
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
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom {
                    Name = "roomName"
                };
                room.Users.Add(roomOwner);
                repository.Add(room);
                
                string welcomeMessage = "This is the room's welcome message";
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/welcome " + welcomeMessage));

                Assert.Equal("You are not an owner of roomName.", exception.Message);
            }

            [Fact]
            public void ThrowsOnClosedRoom()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom
                {
                    Name = "roomName",
                    Closed = true
                };
                room.Users.Add(roomOwner);
                repository.Add(room);
                
                string welcomeMessage = "This is the room's welcome message";
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/welcome " + welcomeMessage));

                Assert.Equal("roomName is closed.", exception.Message);
            }

            [Fact]
            public void CommandSucceeds()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom {
                    Name = "roomName"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                
                string welcomeMessage = "This is the room's welcome message";
                bool result = commandManager.TryHandleCommand("/welcome " + welcomeMessage);

                Assert.True(result);
                Assert.Equal(welcomeMessage, room.Welcome);
                notificationService.Verify(x => x.ChangeWelcome(roomOwner, room), Times.Once());
            }

            [Fact]
            public void ThrowsIfWelcomeMessageExceedsMaxLength()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom {
                    Name = "roomName"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                
                string welcomeMessage = new String('A', 201);
                var exception = Assert.Throws<HubException>(() => commandManager.TryHandleCommand("/welcome " + welcomeMessage));

                Assert.Equal("Sorry, but your welcome is too long. Please keep it under 200 characters.", exception.Message);
            }

            [Fact]
            public void CommandClearsWelcomeIfNoTextProvided()
            {
                IJabbrRepository repository;
                Mock<INotificationService> notificationService;
                var commandManager = CreateCommandManager(out repository, out notificationService);

                var roomOwner = new ChatUser
                {
                    Name = "dfowler",
                    Id = "userId"
                };
                repository.Add(roomOwner);
                var room = new ChatRoom {
                    Name = "roomName",
                    Welcome = "foo"
                };
                room.Owners.Add(roomOwner);
                room.Users.Add(roomOwner);
                repository.Add(room);
                
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
            var membershipService = new MembershipService(repository, new CryptoService(new SettingsKeyProvider(ApplicationSettings.GetDefaultSettings())));
            var commandManager = new CommandManager("clientid",
                                                    "userAgent",
                                                    null,
                                                    null,
                                                    service,
                                                    repository,
                                                    cache,
                                                    notificationService.Object,
                                                    membershipService);

            return Assert.Throws<T>(() => commandManager.TryHandleCommand(command));
        }
    }
}
