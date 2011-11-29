using System;
using System.Linq;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Xunit;

namespace JabbR.Test
{
    public class ChatServiceFacts
    {
        public class AddUser
        {
            [Fact]
            public void ThrowsIfNameIsInValid()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AddUser("some in valid name", clientId: null, password: null));
            }

            [Fact]
            public void ThrowsIfNameInUse()
            {
                var repository = new InMemoryRepository();
                repository.Add(new ChatUser()
                {
                    Name = "taken"
                });
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AddUser("taken", clientId: null, password: null));
            }

            [Fact]
            public void ThrowsIfNameIsNullOrEmpty()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AddUser(null, clientId: null, password: null));
                Assert.Throws<InvalidOperationException>(() => service.AddUser(String.Empty, clientId: null, password: null));
            }

            [Fact]
            public void ThrowsIfPasswordIsTooShort()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AddUser("SomeUser", clientId: null, password: "short"));
            }

            [Fact]
            public void AddsUserToRepository()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository);

                service.AddUser("SomeUser", clientId: null, password: "password");

                var user = repository.GetUserByName("SomeUser");
                Assert.NotNull(user);
                Assert.Equal("SomeUser", user.Name);
                Assert.Equal("password".ToSha256(), user.HashedPassword);
            }
        }

        public class ChangeUserName
        {
            [Fact]
            public void ThrowsIfNameIsInvalid()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test"
                };
                repository.Add(user);
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.ChangeUserName(user, "name with spaces"));
            }

            [Fact]
            public void ThrowsIfNameIsTaken()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test"
                };
                repository.Add(user);
                repository.Add(new ChatUser()
                {
                    Name = "taken"
                });
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.ChangeUserName(user, "taken"));
            }

            [Fact]
            public void ThrowsIfUserNameIsSame()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test"
                };
                repository.Add(user);
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.ChangeUserName(user, "Test"));
            }

            [Fact]
            public void UpdatesUserName()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test"
                };
                repository.Add(user);
                var service = new ChatService(repository);

                service.ChangeUserName(user, "Test2");

                Assert.Equal("Test2", user.Name);
            }
        }

        public class ChangeUserPassword
        {
            [Fact]
            public void ThrowsUserPasswordDoesNotMatchOldPassword()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.ChangeUserPassword(user, "passwor", "foo"));
            }

            [Fact]
            public void ThrowsIfNewPasswordIsNull()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.ChangeUserPassword(user, "password", null));
            }

            [Fact]
            public void UpatesUserPassword()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test",
                    HashedPassword = "password".ToSha256()
                };
                repository.Add(user);
                var service = new ChatService(repository);

                service.ChangeUserPassword(user, "password", "password2");

                Assert.Equal("password2".ToSha256(), user.HashedPassword);
            }
        }

        public class AuthenticateUser
        {
            [Fact]
            public void ThrowsIfUserDoesNotExist()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AuthenticateUser("SomeUser", "foo"));
            }

            [Fact]
            public void ThrowsIfUserPasswordDoesNotMatch()
            {
                var repository = new InMemoryRepository();
                repository.Add(new ChatUser
                {
                    Name = "foo",
                    HashedPassword = "passwords".ToSha256()
                });
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AuthenticateUser("SomeUser", "foo"));
            }

            [Fact]
            public void ThrowsIfUserPasswordNotSet()
            {
                var repository = new InMemoryRepository();
                repository.Add(new ChatUser
                {
                    Name = "foo"
                });
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AuthenticateUser("SomeUser", null));
            }

            [Fact]
            public void DoesNotThrowIfPasswordsMatch()
            {
                var repository = new InMemoryRepository();
                repository.Add(new ChatUser
                {
                    Name = "foo",
                    HashedPassword = "passwords".ToSha256()
                });
                var service = new ChatService(repository);

                service.AuthenticateUser("foo", "passwords");
            }
        }

        public class AddRoom
        {
            public void ThrowsIfRoomNameIsLobby()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AddRoom(user, "Lobby"));
                Assert.Throws<InvalidOperationException>(() => service.AddRoom(user, "LObbY"));
            }

            public void ThrowsIfRoomNameInvalid()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AddRoom(user, "Invalid name"));
            }

            public void AddsUserAsCreatorAndOwner()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var service = new ChatService(repository);

                ChatRoom room = service.AddRoom(user, "NewRoom");

                Assert.NotNull(room);
                Assert.Equal("NewRoom", room.Name);
                Assert.Same(room, repository.GetRoomByName("NewRoom"));
                Assert.True(room.Owners.Contains(user));
                Assert.Same(room.Creator, user);
                Assert.True(user.OwnedRooms.Contains(room));
            }
        }

        public class JoinRoom
        {
            [Fact]
            public void AddsUserToRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "Room"
                };
                var service = new ChatService(repository);

                service.JoinRoom(user, room);

                Assert.True(user.Rooms.Contains(room));
                Assert.True(room.Users.Contains(user));
            }
        }

        public class UpdateActivity
        {
            public void UpdatesStatus()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo",
                    Status = (int)UserStatus.Inactive
                };
                repository.Add(user);
                var service = new ChatService(repository);

                service.UpdateActivity(user);

                Assert.Equal((int)UserStatus.Active, user.Status);
            }
        }

        public class LeaveRoom
        {
            [Fact]
            public void RemovesUserFromRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "Room"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);

                var service = new ChatService(repository);

                service.LeaveRoom(user, room);

                Assert.False(user.Rooms.Contains(room));
                Assert.False(room.Users.Contains(user));
            }
        }

        public class AddMessage
        {
            [Fact]
            public void AddsNewMessageToRepository()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "Room"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);

                var service = new ChatService(repository);
                ChatMessage message = service.AddMessage(user, room, "Content");

                Assert.NotNull(message);
                Assert.Same(message, room.Messages.First());
                Assert.Equal("Content", message.Content);
            }
        }

        public class AddOwner
        {
            public void ThrowsIfUserIsNotOwner()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "Room"
                };
                room.Users.Add(user);
                user.Rooms.Add(room);

                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AddOwner(user, user, room));
            }

            public void ThrowsIfUserIsAlreadyAnOwner()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "Room"
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);

                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.AddOwner(user, user, room));
            }

            public void MakesUserOwner()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                var user2 = new ChatUser
                {
                    Name = "foo2"
                };
                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Creator = user
                };
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);
                room.Users.Add(user);

                var service = new ChatService(repository);

                service.AddOwner(user, user2, room);

                Assert.True(room.Owners.Contains(user2));
                Assert.True(user2.OwnedRooms.Contains(room));
            }
        }

        public class KickUser
        {
            public void ThrowsIfKickSelf()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Creator = user
                };
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);
                room.Users.Add(user);

                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.KickUser(user, user, room));
            }

            public void ThrowsIfUserIsNotOwner()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };

                var user2 = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                };

                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                room.Users.Add(user);
                room.Users.Add(user2);

                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.KickUser(user, user2, room));
            }

            public void ThrowsIfTargetUserNotInRoom()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };

                var user2 = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Creator = user
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                user.Rooms.Add(room);
                room.Users.Add(user);

                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.KickUser(user, user2, room));
            }

            public void ThrowsIfOwnerTriesToRemoveOwner()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };

                var user2 = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);

                user2.OwnedRooms.Add(room);
                room.Owners.Add(user2);

                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                room.Users.Add(user);
                room.Users.Add(user2);

                var service = new ChatService(repository);

                Assert.Throws<InvalidOperationException>(() => service.KickUser(user, user2, room));
            }

            public void DoesNotThrowIfCreatorKicksOwner()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };

                var user2 = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Creator = user
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);

                user2.OwnedRooms.Add(room);
                room.Owners.Add(user2);

                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                room.Users.Add(user);
                room.Users.Add(user2);

                var service = new ChatService(repository);

                service.KickUser(user, user2, room);

                Assert.False(user2.Rooms.Contains(room));
                Assert.False(room.Users.Contains(user2));
            }
        }
    }
}
