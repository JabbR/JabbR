using System;
using System.Linq;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Moq;
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AddUser("taken", clientId: null, password: null));
            }

            [Fact]
            public void ThrowsIfNameIsNullOrEmpty()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AddUser(null, clientId: null, password: null));
                Assert.Throws<InvalidOperationException>(() => service.AddUser(String.Empty, clientId: null, password: null));
            }

            [Fact]
            public void ThrowsIfPasswordIsTooShort()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AddUser("SomeUser", clientId: null, password: "short"));
            }

            [Fact]
            public void AddsUserToRepository()
            {
                var crypto = new Mock<ICryptoService>();
                crypto.Setup(c => c.CreateSalt()).Returns("salted");
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, crypto.Object);

                service.AddUser("SomeUser", clientId: null, password: "password");

                var user = repository.GetUserByName("SomeUser");
                Assert.NotNull(user);
                Assert.Equal("SomeUser", user.Name);
                Assert.Equal("salted", user.Salt);
                Assert.Equal("8f5793009fe15c2227e3528d0507413a83dff10635d3a6acf1ba3229a03380d8", user.HashedPassword);
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.ChangeUserPassword(user, "passwor", "foo"));
            }

            [Fact]
            public void ThrowsIfNewPasswordIsNull()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test",
                    Salt = "salt",
                    HashedPassword = "password".ToSha256("salt")
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.ChangeUserPassword(user, "password", null));
            }

            [Fact]
            public void UpatesUserPassword()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test",
                    Salt = "pepper",
                    HashedPassword = "password".ToSha256("pepper")
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.ChangeUserPassword(user, "password", "password2");

                Assert.Equal("password2".ToSha256("pepper"), user.HashedPassword);
            }

            [Fact]
            public void EnsuresSaltedPassword()
            {
                var crypto = new Mock<ICryptoService>();
                crypto.Setup(c => c.CreateSalt()).Returns("salt");
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "Test",
                    Salt = null,
                    HashedPassword = "password".ToSha256(null)
                };
                repository.Add(user);
                var service = new ChatService(repository, crypto.Object);

                service.ChangeUserPassword(user, "password", "password");

                Assert.Equal("salt", user.Salt);
                Assert.Equal("password".ToSha256("salt"), user.HashedPassword);
            }
        }

        public class AuthenticateUser
        {
            [Fact]
            public void ThrowsIfUserDoesNotExist()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AuthenticateUser("SomeUser", "foo"));
            }

            [Fact]
            public void ThrowsIfUserPasswordDoesNotMatch()
            {
                var repository = new InMemoryRepository();
                repository.Add(new ChatUser
                {
                    Name = "foo",
                    Salt = "salt",
                    HashedPassword = "passwords".ToSha256("salt")
                });
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AuthenticateUser("SomeUser", null));
            }

            [Fact]
            public void DoesNotThrowIfPasswordsMatch()
            {
                var repository = new InMemoryRepository();
                repository.Add(new ChatUser
                {
                    Name = "foo",
                    HashedPassword = "3049a1f8327e0215ea924b9e4e04cd4b0ff1800c74a536d9b81d3d8ced9994d3"
                });
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                service.AuthenticateUser("foo", "passwords");
            }

            [Fact]
            public void DoesNotThrowIfSaltedPasswordsMatch()
            {
                var repository = new InMemoryRepository();
                repository.Add(new ChatUser
                {
                    Name = "foo",
                    Salt = "salted",
                    HashedPassword = "8f5793009fe15c2227e3528d0507413a83dff10635d3a6acf1ba3229a03380d8"
                });
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                service.AuthenticateUser("foo", "password");
            }

            [Fact]
            public void EnsuresStoredPasswordIsSalted()
            {
                var crypto = new Mock<ICryptoService>();
                crypto.Setup(c => c.CreateSalt()).Returns("salted");
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo",
                    HashedPassword = "3049a1f8327e0215ea924b9e4e04cd4b0ff1800c74a536d9b81d3d8ced9994d3"
                };
                repository.Add(user);
                var service = new ChatService(repository, crypto.Object);

                service.AuthenticateUser("foo", "passwords");

                Assert.Equal("salted", user.Salt);
                Assert.Equal("9ce70d2ab42c9a9012ed6f80f85ab400ef1483f70e227a42b6d77faea204db26", user.HashedPassword);
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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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
                repository.Add(room);
                room.Users.Add(user);
                user.Rooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

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

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.KickUser(user, user2, room);

                Assert.False(user2.Rooms.Contains(room));
                Assert.False(room.Users.Contains(user2));
            }
        }

        public class DisconnectClient
        {
            [Fact]
            public void RemovesClientFromUserClientList()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo",
                    Status = (int)UserStatus.Inactive
                };
                user.ConnectedClients.Add(new ChatClient
                {
                    Id = "foo",
                    User = user
                });

                user.ConnectedClients.Add(new ChatClient
                {
                    Id = "bar",
                    User = user
                });

                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.DisconnectClient("foo");

                Assert.Equal(1, user.ConnectedClients.Count);
                Assert.Equal("bar", user.ConnectedClients.First().Id);
            }

            [Fact]
            public void MarksUserAsOfflineIfNoMoreClients()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo",
                    Status = (int)UserStatus.Inactive
                };
                user.ConnectedClients.Add(new ChatClient
                {
                    Id = "foo",
                    User = user
                });

                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.DisconnectClient("foo");

                Assert.Equal(0, user.ConnectedClients.Count);
                Assert.Equal((int)UserStatus.Offline, user.Status);
            }

            [Fact]
            public void ReturnsNullIfNoUserForClientId()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                ChatUser user = service.DisconnectClient("foo");

                Assert.Null(user);
            }
        }
    }
}
