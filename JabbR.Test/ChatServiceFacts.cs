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

                Assert.Throws<InvalidOperationException>(() => service.AddUser("some in valid name", clientId: null, userAgent: null, password: null));
            }

            [Fact]
            public void UnicodeNameIsValid()
            {
                // Fix issue #370
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);
                var user = service.AddUser("ТарасБуга", clientId: null, userAgent: null, password: "password");

                Assert.Equal("ТарасБуга", user.Name);
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

                Assert.Throws<InvalidOperationException>(() => service.AddUser("taken", clientId: null, userAgent: null, password: null));
            }

            [Fact]
            public void ThrowsIfNameIsNullOrEmpty()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AddUser(null, clientId: null, userAgent: null, password: null));
                Assert.Throws<InvalidOperationException>(() => service.AddUser(String.Empty, clientId: null, userAgent: null, password: null));
            }

            [Fact]
            public void ThrowsIfPasswordIsTooShort()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AddUser("SomeUser", clientId: null, userAgent: null, password: "short"));
            }

            [Fact]
            public void AddsUserToRepository()
            {
                var crypto = new Mock<ICryptoService>();
                crypto.Setup(c => c.CreateSalt()).Returns("salted");
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, crypto.Object);

                service.AddUser("SomeUser", clientId: null, userAgent: null, password: "password");

                var user = repository.GetUserByName("SomeUser");
                Assert.NotNull(user);
                Assert.Equal("SomeUser", user.Name);
                Assert.Equal("salted", user.Salt);
                Assert.Equal("8f5793009fe15c2227e3528d0507413a83dff10635d3a6acf1ba3229a03380d8", user.HashedPassword);
            }

            [Fact]
            public void AddsAuthUserToRepository()
            {
                var repository = new InMemoryRepository();
                var service = new ChatService(repository, null);

                service.AddUser("SomeUser", "identity", "email");

                var user = repository.GetUserByIdentity("identity");
                Assert.NotNull(user);
                Assert.Equal("SomeUser", user.Name);
                Assert.Equal("identity", user.Identity);
                Assert.Equal("email", user.Email);
                Assert.Equal("0c83f57c786a0b4a39efab23731c7ebc", user.Hash);
            }

            [Fact]
            public void AddsNumberToUserNameIfTaken()
            {
                var repository = new InMemoryRepository();
                repository.Add(new ChatUser
                {
                    Name = "david",
                    Id = "1"
                });

                var service = new ChatService(repository, null);

                service.AddUser("david", "idenity", null);

                var user = repository.GetUserByIdentity("idenity");
                Assert.NotNull(user);
                Assert.Equal("david1", user.Name);
                Assert.Equal("idenity", user.Identity);
                Assert.Null(user.Email);
                Assert.Null(user.Hash);
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
                    Name = "SomeUser",
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
                    Name = "SomeUser"
                });
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AuthenticateUser("SomeUser", "password"));
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
            [Fact]
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

            [Fact]
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

            [Fact]
            public void ThrowsIfRoomNameContainsPeriod()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AddRoom(user, "Invalid.name"));
            }

            [Fact]
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

                service.JoinRoom(user, room, null);

                Assert.True(user.Rooms.Contains(room));
                Assert.True(room.Users.Contains(user));
            }

            [Fact]
            public void AddsUserToRoomIfAllowedAndRoomIsPrivate()
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
                    Private = true
                };
                room.AllowedUsers.Add(user);
                user.AllowedRooms.Add(room);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.JoinRoom(user, room, null);

                Assert.True(user.Rooms.Contains(room));
                Assert.True(room.Users.Contains(user));
            }

            [Fact]
            public void ThrowsIfRoomIsPrivateAndNotAllowed()
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
                    Private = true
                };

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.JoinRoom(user, room, null));
            }

            [Fact]
            public void AddsUserToRoomIfUserIsAdminAndRoomIsPrivate()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                repository.Add(user);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Private = true
                };

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.JoinRoom(user, room, null);

                Assert.True(user.Rooms.Contains(room));
                Assert.True(room.Users.Contains(user));
            }
        }

        public class UpdateActivity
        {
            [Fact]
            public void CanUpdateActivity()
            {
                var repository = new InMemoryRepository();
                var user = new ChatUser
                {
                    Name = "foo",
                    Status = (int)UserStatus.Inactive,
                    IsAfk = true,
                    AfkNote = "note!?"
                };
                repository.Add(user);
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.UpdateActivity(user, "client1", userAgent: null);
                var clients = user.ConnectedClients.ToList();

                Assert.Equal((int)UserStatus.Active, user.Status);
                Assert.Equal(1, clients.Count);
                Assert.Equal("client1", clients[0].Id);
                Assert.Same(user, clients[0].User);
                Assert.Null(user.AfkNote);
                Assert.False(user.IsAfk);
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
                ChatMessage message = service.AddMessage(user, room, Guid.NewGuid().ToString(), "Content");

                Assert.NotNull(message);
                Assert.Same(message, room.Messages.First());
                Assert.Equal("Content", message.Content);
            }
        }

        public class AddOwner
        {
            [Fact]
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

            [Fact]
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

            [Fact]
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

            [Fact]
            public void MakesUserOwnerIfUserAlreadyAllowed()
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
                    Private = true,
                    Creator = user
                };
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);
                room.Users.Add(user);

                user2.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user2);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.AddOwner(user, user2, room);

                Assert.True(room.Owners.Contains(user2));
                Assert.True(user2.OwnedRooms.Contains(room));
            }

            [Fact]
            public void MakesOwnerAllowedIfRoomLocked()
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
                    Creator = user,
                    Private = true
                };
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);
                room.Users.Add(user);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.AddOwner(user, user2, room);

                Assert.True(user2.AllowedRooms.Contains(room));
                Assert.True(room.AllowedUsers.Contains(user2));
                Assert.True(room.Owners.Contains(user2));
                Assert.True(user2.OwnedRooms.Contains(room));
            }

            [Fact]
            public void NonOwnerAdminCanAddUserAsOwner()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                var user2 = new ChatUser
                {
                    Name = "foo2"
                };
                repository.Add(admin);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Creator = admin
                };

                admin.Rooms.Add(room);
                room.Users.Add(admin);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.AddOwner(admin, user2, room);

                Assert.True(room.Owners.Contains(user2));
                Assert.True(user2.OwnedRooms.Contains(room));
            }

            // TODO: admin can add self as owner
        }

        public class RemoveOwner
        {
            [Fact]
            public void ThrowsIfTargettedUserIsNotOwner()
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

                room.Creator = user;
                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                room.Users.Add(user);
                room.Users.Add(user2);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.RemoveOwner(user, user2, room));
            }

            [Fact]
            public void ThrowsIfActingUserIsNotCreatorOrAdmin() {
                var repository = new InMemoryRepository();
                var user = new ChatUser {
                    Name = "foo"
                };

                var user2 = new ChatUser {
                    Name = "foo2"
                };

                repository.Add(user);
                repository.Add(user2);
                var room = new ChatRoom {
                    Name = "Room",
                };

                user.Rooms.Add(room);
                user2.Rooms.Add(room);

                room.Users.Add(user);
                room.Users.Add(user2);

                room.Owners.Add(user);
                user.OwnedRooms.Add(room);

                room.Owners.Add(user2);
                user2.OwnedRooms.Add(room);
                
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.RemoveOwner(user, user2, room));
            }

            [Fact]
            public void RemovesOwnerIfActingUserIsAdmin()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };

                var user2 = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(admin);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                };

                admin.Rooms.Add(room);
                user2.Rooms.Add(room);

                room.Users.Add(admin);
                room.Users.Add(user2);

                room.Owners.Add(user2);
                user2.OwnedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.RemoveOwner(admin, user2, room);

                Assert.False(room.Owners.Contains(user2));
                Assert.False(user2.OwnedRooms.Contains(room));
            }
        }

        public class KickUser
        {
            [Fact]
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

            [Fact]
            public void ThrowsIfUserIsNotOwnerAndNotAdmin()
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

            [Fact]
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

            [Fact]
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

            [Fact]
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

            [Fact]
            public void AdminCanKickUser()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };

                var user2 = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(admin);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                };

                admin.Rooms.Add(room);
                user2.Rooms.Add(room);
                room.Users.Add(admin);
                room.Users.Add(user2);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.KickUser(admin, user2, room);

                Assert.False(user2.Rooms.Contains(room));
                Assert.False(room.Users.Contains(user2));
            }

            [Fact]
            public void DoesNotThrowIfAdminKicksOwner()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };

                var user2 = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(admin);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room"
                };

                user2.OwnedRooms.Add(room);
                room.Owners.Add(user2);

                admin.Rooms.Add(room);
                user2.Rooms.Add(room);
                room.Users.Add(admin);
                room.Users.Add(user2);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.KickUser(admin, user2, room);

                Assert.False(user2.Rooms.Contains(room));
                Assert.False(room.Users.Contains(user2));
            }

            [Fact]
            public void AdminCanKickCreator()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };

                var creator = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(admin);
                repository.Add(creator);

                var room = new ChatRoom
                {
                    Name = "Room",
                    Creator = creator
                };

                creator.OwnedRooms.Add(room);
                room.Owners.Add(creator);

                admin.Rooms.Add(room);
                creator.Rooms.Add(room);
                room.Users.Add(admin);
                room.Users.Add(creator);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.KickUser(admin, creator, room);

                Assert.False(creator.Rooms.Contains(room));
                Assert.False(room.Users.Contains(creator));
            }

            [Fact]
            public void ThrowsIfOwnerTriesToRemoveAdmin()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };

                var owner = new ChatUser
                {
                    Name = "foo2"
                };

                repository.Add(admin);
                repository.Add(owner);
                var room = new ChatRoom
                {
                    Name = "Room",
                };

                owner.OwnedRooms.Add(room);
                room.Owners.Add(owner);

                admin.Rooms.Add(room);
                owner.Rooms.Add(room);
                room.Users.Add(admin);
                room.Users.Add(owner);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.KickUser(owner, admin, room));
            }

            [Fact]
            public void AdminCanKickAdmin()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };

                var otherAdmin = new ChatUser
                {
                    Name = "foo2",
                    IsAdmin = true
                };

                repository.Add(admin);
                repository.Add(otherAdmin);

                var room = new ChatRoom
                {
                    Name = "Room"
                };

                admin.Rooms.Add(room);
                otherAdmin.Rooms.Add(room);
                room.Users.Add(admin);
                room.Users.Add(otherAdmin);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.KickUser(admin, otherAdmin, room);

                Assert.False(otherAdmin.Rooms.Contains(room));
                Assert.False(room.Users.Contains(otherAdmin));
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

        public class LockRoom
        {
            [Fact]
            public void LocksRoomIfOwner()
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

                service.LockRoom(user, room);

                Assert.True(room.Private);
                Assert.True(user.AllowedRooms.Contains(room));
                Assert.True(room.AllowedUsers.Contains(user));
            }

            [Fact]
            public void ThrowsIfRoomAlreadyLocked()
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
                    Creator = user,
                    Private = true
                };
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                room.AllowedUsers.Add(user);
                user.AllowedRooms.Add(room);
                user.Rooms.Add(room);
                room.Users.Add(user);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.LockRoom(user, room));
            }

            [Fact]
            public void LocksRoom()
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

                service.LockRoom(user, room);

                Assert.True(room.Private);
                Assert.True(user.AllowedRooms.Contains(room));
                Assert.True(room.AllowedUsers.Contains(user));
            }

            [Fact]
            public void MakesAllUsersAllowed()
            {
                var repository = new InMemoryRepository();
                var creator = new ChatUser
                {
                    Name = "foo"
                };
                var users = Enumerable.Range(0, 5).Select(i => new ChatUser
                {
                    Name = "user_" + i
                }).ToList();

                var offlineUsers = Enumerable.Range(6, 10).Select(i => new ChatUser
                {
                    Name = "user_" + i,
                    Status = (int)UserStatus.Offline
                }).ToList();

                var room = new ChatRoom
                {
                    Name = "room",
                    Creator = creator
                };
                room.Owners.Add(creator);
                creator.OwnedRooms.Add(room);
                repository.Add(room);
                foreach (var u in users)
                {
                    room.Users.Add(u);
                    u.Rooms.Add(room);
                    repository.Add(u);
                }
                foreach (var u in offlineUsers)
                {
                    room.Users.Add(u);
                    u.Rooms.Add(room);
                    repository.Add(u);
                }
                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.LockRoom(creator, room);

                foreach (var u in users)
                {
                    Assert.True(u.AllowedRooms.Contains(room));
                    Assert.True(room.AllowedUsers.Contains(u));
                }

                foreach (var u in offlineUsers)
                {
                    Assert.False(u.AllowedRooms.Contains(room));
                    Assert.False(room.AllowedUsers.Contains(u));
                }
            }

            [Fact]
            public void LocksRoomIfAdmin()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                repository.Add(admin);
                var room = new ChatRoom
                {
                    Name = "Room"
                };
                room.Users.Add(admin);
                admin.Rooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.LockRoom(admin, room);

                Assert.True(room.Private);
                Assert.True(admin.AllowedRooms.Contains(room));
                Assert.True(room.AllowedUsers.Contains(admin));
            }
        }

        public class AllowUser
        {
            [Fact]
            public void ThrowsIfRoomNotPrivate()
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
                room.Users.Add(user);
                user.Rooms.Add(room);
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AllowUser(user, user2, room));
            }

            [Fact]
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
                    Name = "Room",
                    Private = true
                };
                room.Users.Add(user);
                user.Rooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AllowUser(user, user, room));
            }

            [Fact]
            public void ThrowsIfUserIsAlreadyAllowed()
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
                    Private = true
                };
                room.Users.Add(user);
                room.AllowedUsers.Add(user2);
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);
                user2.AllowedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AllowUser(user, user2, room));
            }

            [Fact]
            public void AllowsUserIntoRoom()
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
                    Private = true
                };
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);
                room.Users.Add(user);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.AllowUser(user, user2, room);

                Assert.True(room.AllowedUsers.Contains(user2));
                Assert.True(user2.AllowedRooms.Contains(room));
            }

            [Fact]
            public void AdminCanAllowUserIntoRoom()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                repository.Add(admin);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Private = true
                };
                room.Users.Add(admin);
                admin.Rooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.AllowUser(admin, admin, room);

                Assert.True(room.AllowedUsers.Contains(admin));
                Assert.True(admin.AllowedRooms.Contains(room));
            }
        }

        public class UnallowUser
        {
            [Fact]
            public void ThrowsIfRoomNotPrivate()
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
                room.Users.Add(user);
                user.Rooms.Add(room);
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.UnallowUser(user, user2, room));
            }

            [Fact]
            public void ThrowsIfTargetUserIsCreator()
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
                    Private = true,
                    Creator = user
                };
                room.Users.Add(user);
                user.Rooms.Add(room);
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                room.AllowedUsers.Add(user);
                user.AllowedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.UnallowUser(user, user, room));
            }

            [Fact]
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
                    Private = true
                };
                room.Users.Add(user);
                user.Rooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.UnallowUser(user, user2, room));
            }

            [Fact]
            public void DoesNotThrowIfUserIsAdminButIsNotOwner()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                var user2 = new ChatUser
                {
                    Name = "foo2"
                };
                repository.Add(admin);
                repository.Add(user2);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Private = true
                };
                room.AllowedUsers.Add(user2);
                room.Users.Add(admin);
                admin.Rooms.Add(room);
                user2.AllowedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.UnallowUser(admin, user2, room);

                Assert.False(room.Users.Contains(user2));
                Assert.False(user2.Rooms.Contains(room));
                Assert.False(room.AllowedUsers.Contains(user2));
                Assert.False(user2.AllowedRooms.Contains(room));
            }

            [Fact]
            public void ThrowsIfUserIsNotAllowed()
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
                    Private = true
                };
                room.Users.Add(user);
                room.Owners.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.UnallowUser(user, user2, room));
            }

            [Fact]
            public void ThrowIfOwnerTriesToUnallowOwner()
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
                    Private = true
                };
                user.OwnedRooms.Add(room);
                room.Owners.Add(user);
                user.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user);

                user2.OwnedRooms.Add(room);
                room.Owners.Add(user2);
                user2.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user2);

                user.Rooms.Add(room);
                user2.Rooms.Add(room);
                room.Users.Add(user);
                room.Users.Add(user2);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.UnallowUser(user, user2, room));
            }

            [Fact]
            public void UnallowsAndRemovesUserFromRoom()
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
                    Private = true
                };
                room.AllowedUsers.Add(user2);
                room.Owners.Add(user);
                room.Users.Add(user);
                user.OwnedRooms.Add(room);
                user.Rooms.Add(room);
                user2.AllowedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.UnallowUser(user, user2, room);

                Assert.False(room.Users.Contains(user2));
                Assert.False(user2.Rooms.Contains(room));
                Assert.False(room.AllowedUsers.Contains(user2));
                Assert.False(user2.AllowedRooms.Contains(room));
            }

            [Fact]
            public void AdminCanUnallowUser()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                var user = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(admin);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Private = true
                };
                room.Users.Add(admin);
                admin.Rooms.Add(room);

                room.AllowedUsers.Add(admin);
                admin.AllowedRooms.Add(room);
                room.AllowedUsers.Add(user);
                user.AllowedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.UnallowUser(admin, user, room);

                Assert.False(room.Users.Contains(user));
                Assert.False(user.Rooms.Contains(room));
                Assert.False(room.AllowedUsers.Contains(user));
                Assert.False(user.AllowedRooms.Contains(room));
            }

            [Fact]
            public void AdminCanUnallowOwner()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                var owner = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(admin);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Private = true
                };
                room.Users.Add(admin);
                admin.Rooms.Add(room);

                room.Users.Add(owner);
                owner.Rooms.Add(room);
                room.Owners.Add(owner);
                owner.OwnedRooms.Add(room);

                room.AllowedUsers.Add(admin);
                admin.AllowedRooms.Add(room);
                room.AllowedUsers.Add(owner);
                owner.AllowedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.UnallowUser(admin, owner, room);

                Assert.False(room.Users.Contains(owner));
                Assert.False(owner.Rooms.Contains(room));
                Assert.False(room.AllowedUsers.Contains(owner));
                Assert.False(owner.AllowedRooms.Contains(room));
            }

            [Fact]
            public void AdminCanUnallowCreator()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                var creator = new ChatUser
                {
                    Name = "foo"
                };
                repository.Add(admin);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Private = true,
                    Creator = creator
                };
                room.Users.Add(admin);
                admin.Rooms.Add(room);

                room.Owners.Add(admin);
                admin.OwnedRooms.Add(room);
                room.Owners.Add(creator);
                creator.OwnedRooms.Add(room);

                room.AllowedUsers.Add(admin);
                admin.AllowedRooms.Add(room);
                room.AllowedUsers.Add(creator);
                creator.AllowedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.UnallowUser(admin, creator, room);

                Assert.False(room.Users.Contains(creator));
                Assert.False(creator.Rooms.Contains(room));
                Assert.False(room.AllowedUsers.Contains(creator));
                Assert.False(creator.AllowedRooms.Contains(room));
            }

            [Fact]
            public void ThrowIfOwnerTriesToUnallowAdmin()
            {
                var repository = new InMemoryRepository();
                var owner = new ChatUser
                {
                    Name = "foo"
                };

                var admin = new ChatUser
                {
                    Name = "foo2",
                    IsAdmin = true
                };

                repository.Add(owner);
                repository.Add(admin);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Private = true
                };
                owner.OwnedRooms.Add(room);
                room.Owners.Add(owner);
                owner.AllowedRooms.Add(room);
                room.AllowedUsers.Add(owner);

                admin.AllowedRooms.Add(room);
                room.AllowedUsers.Add(admin);

                owner.Rooms.Add(room);
                admin.Rooms.Add(room);
                room.Users.Add(owner);
                room.Users.Add(admin);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.UnallowUser(owner, admin, room));
            }

            [Fact]
            public void AdminCanUnallowAdmin()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                var otherAdmin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                repository.Add(admin);
                var room = new ChatRoom
                {
                    Name = "Room",
                    Private = true
                };
                room.Users.Add(admin);
                admin.Rooms.Add(room);

                room.Users.Add(otherAdmin);
                otherAdmin.Rooms.Add(room);

                room.AllowedUsers.Add(admin);
                admin.AllowedRooms.Add(room);
                room.AllowedUsers.Add(otherAdmin);
                otherAdmin.AllowedRooms.Add(room);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.UnallowUser(admin, otherAdmin, room);

                Assert.False(room.Users.Contains(otherAdmin));
                Assert.False(otherAdmin.Rooms.Contains(room));
                Assert.False(room.AllowedUsers.Contains(otherAdmin));
                Assert.False(otherAdmin.AllowedRooms.Contains(room));
            }
        }

        public class AddAdmin
        {
            [Fact]
            public void ThrowsIfActingUserIsNotAdmin()
            {
                var repository = new InMemoryRepository();
                var nonAdmin = new ChatUser
                {
                    Name = "foo"
                };
                var user = new ChatUser
                {
                    Name = "foo2"
                };
                repository.Add(nonAdmin);
                repository.Add(user);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.AddAdmin(nonAdmin, user));
            }

            [Fact]
            public void MakesUserAdmin()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                var user = new ChatUser
                {
                    Name = "foo2",
                    IsAdmin = false
                };
                repository.Add(admin);
                repository.Add(user);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.AddAdmin(admin, user);

                Assert.True(user.IsAdmin);
            }
        }

        public class RemoveAdmin
        {
            [Fact]
            public void ThrowsIfActingUserIsNotAdmin()
            {
                var repository = new InMemoryRepository();
                var nonAdmin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = false
                };
                var user = new ChatUser
                {
                    Name = "foo2",
                    IsAdmin = true
                };
                repository.Add(nonAdmin);
                repository.Add(user);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                Assert.Throws<InvalidOperationException>(() => service.RemoveAdmin(nonAdmin, user));
            }

            [Fact]
            public void MakesUserAdmin()
            {
                var repository = new InMemoryRepository();
                var admin = new ChatUser
                {
                    Name = "foo",
                    IsAdmin = true
                };
                var user = new ChatUser
                {
                    Name = "foo2",
                    IsAdmin = true
                };
                repository.Add(admin);
                repository.Add(user);

                var service = new ChatService(repository, new Mock<ICryptoService>().Object);

                service.RemoveAdmin(admin, user);

                Assert.False(user.IsAdmin);
            }
        }
    }
}