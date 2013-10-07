/// <reference path="Scripts/jquery-2.0.3.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />
/// <reference path="Chat.ui.js" />
/// <reference path="Scripts/moment.min.js" />

(function ($, connection, window, ui, utility) {
    "use strict";

    var chat = connection.chat,
        messageHistory = [],
        historyLocation = 0,
        originalTitle = document.title,
        unread = 0,
        isUnreadMessageForUser = false,
        loadingHistory = false,
        checkingStatus = false,
        typing = false,
        $ui = $(ui),
        messageSendingDelay = 1500,
        pendingMessages = {},
        privateRooms = null,
        roomsToLoad = 0,
        commandNameLookup = {};
    
    function awaitRoomReady(roomName, funcThis, func) {
        return ui.awaitRoomReady(roomName, funcThis, func, (chat.state ? chat.state.activeRoom : null));
    }

    function failPendingMessages() {
        for (var id in pendingMessages) {
            if (pendingMessages.hasOwnProperty(id)) {
                clearTimeout(pendingMessages[id]);
                ui.failMessage(id);
                delete pendingMessages[id];
            }
        }
    }

    function isSelf(user) {
        return chat.state.name === user.Name;
    }

    function getNoteCssClass(user) {
        if (user.IsAfk === true) {
            return 'afk';
        }
        else if (user.Note) {
            return 'message';
        }
        return '';
    }

    function getNote(user) {
        if (user.IsAfk === true) {
            if (user.AfkNote) {
                return 'AFK - ' + user.AfkNote;
            }
            return 'AFK';
        }

        return user.Note;
    }

    function isCommand(msg) {
        var parts = msg.substr(1).split(' ');
        if (msg[0] === '/' && parts.length > 0) {
            return commandNameLookup[parts[0]];
        }

        return false;
    }

    function getFlagCssClass(user) {
        return (user.Flag) ? 'flag flag-' + user.Flag : '';
    }

    function performLogout() {
        var d = $.Deferred();
        $.post('account/logout', {}).done(function () {
            d.resolveWith(null);
            document.location = document.location.pathname;
        });

        return d.promise();
    }

    function logout() {
        performLogout().done(function () {
            chat.server.send('/logout', chat.state.activeRoom)
                .fail(function (e) {
                    if (e.source === 'HubException') {
                        ui.addMessage(e.message, 'error', chat.state.activeRoom);
                    }
                });
        });
    }

    function populateRooms(rooms) {
        connection.hub.log('loadRooms(' + rooms.join(', ') + ')');

        // Populate the list of users rooms and messages 
        chat.server.loadRooms(rooms)
            .done(function () {
                connection.hub.log('loadRooms.done(' + rooms.join(', ') + ')');
            })
            .fail(function (e) {
                connection.hub.log('loadRooms.failed(' + rooms.join(', ') + ', ' + e + ')');
            });
    }

    function populateRoom(room, d) {
        var deferred = d || $.Deferred();

        connection.hub.log('getRoomInfo(' + room + ')');

        // Populate the list of users rooms and messages 
        chat.server.getRoomInfo(room)
                .done(function (roomInfo) {
                    connection.hub.log('getRoomInfo.done(' + room + ')');

                    populateRoomFromInfo(roomInfo);

                    deferred.resolveWith(chat);
                })
                .fail(function (e) {
                    connection.hub.log('getRoomInfo.failed(' + room + ', ' + e + ')');

                    setTimeout(function () {
                        populateRoom(room, deferred);
                    },
                    1000);
                });

        return deferred.promise();
    }

    function populateRoomFromInfo(roomInfo) {
        var room = roomInfo.Name;

        $.each(roomInfo.Users, function () {
            var userViewModel = getUserViewModel(this);
            ui.addUser(userViewModel, room);
            ui.setUserActivity(userViewModel);
        });

        $.each(roomInfo.Owners, function () {
            ui.setRoomOwner(this, room);
        });

        var messageIds = [];
        $.each(roomInfo.RecentMessages, function () {
            var viewModel = getMessageViewModel(this);

            messageIds.push(viewModel.id);
            ui.addChatMessage(viewModel, room);
        });

        ui.changeRoomTopic(roomInfo.Name, roomInfo.Topic);

        // mark room as initialized to differentiate messages
        // that are added after initial population
        ui.setInitialized(room);
        ui.scrollToBottom(room);
        ui.setRoomListStatuses(room);

        // Watch the messages after the defer, since room messages
        // may be appended if we are just joining the room
        ui.watchMessageScroll(messageIds, room);
    }

    function populateLobbyRooms() {
        var d = $.Deferred();

        try {
            // Populate the user list with room names
            chat.server.getRooms()
                .done(function (rooms) {
                    ui.populateLobbyRooms(rooms, privateRooms);
                    ui.setInitialized('Lobby');
                    d.resolveWith(chat);
                });
        }
        catch (e) {
            connection.hub.log('getRooms failed');
            d.rejectWith(chat);
        }

        return d.promise();
    }

    function scrollIfNecessary(callback, room) {
        var nearEnd = ui.isNearTheEnd(room);

        callback();

        if (nearEnd) {
            ui.scrollToBottom(room);
        }
    }

    function getUserViewModel(user, isOwner) {
        var lastActive = user.LastActivity.fromJsonDate();
        return {
            name: user.Name,
            hash: user.Hash,
            owner: isOwner,
            active: user.Active,
            noteClass: getNoteCssClass(user),
            note: getNote(user),
            flagClass: getFlagCssClass(user),
            flag: user.Flag,
            country: user.Country,
            lastActive: lastActive,
            timeAgo: $.timeago(lastActive),
            admin: user.IsAdmin,
            afk: user.IsAfk
        };
    }

    function getMessageViewModel(message) {
        var re = new RegExp("\\b@?" + chat.state.name.replace(/\./, '\\.') + "\\b", "i");
        return {
            name: message.User.Name,
            hash: message.User.Hash,
            message: message.HtmlEncoded ? message.Content : ui.processContent(message.Content),
            htmlContent: message.HtmlContent,
            id: message.Id,
            date: message.When.fromJsonDate(),
            highlight: re.test(message.Content) ? 'highlight' : '',
            isOwn: re.test(message.User.name),
            isMine: message.User.Name === chat.state.name,
            imageUrl: message.ImageUrl,
            source: message.Source,
            messageType: message.MessageType,
            presence: (message.UserRoomPresence || 'absent').toLowerCase(),
            status: getMessageUserStatus(message.User).toLowerCase()
        };
    }

    function getMessageUserStatus(user) {
        if (user.Status === 'Active' && user.IsAfk === true) {
            return 'Inactive';
        }

        return (user.Status || 'Offline');
    }

    // Save some state in a cookie
    function updateCookie() {
        var state = {
            activeRoom: chat.state.activeRoom,
            preferences: ui.getState()
        },
        jsonState = window.JSON.stringify(state);

        $.cookie('jabbr.state', jsonState, { path: '/', expires: 30 });
    }

    function updateTitle() {
        // ugly hack via http://stackoverflow.com/a/2952386/188039
        setTimeout(function () {
            if (unread === 0) {
                document.title = originalTitle;
            } else {
                document.title = (isUnreadMessageForUser ? '*' : '') + '(' + unread + ') ' + originalTitle;
            }
        }, 200);
    }

    function clearUnread() {
        isUnreadMessageForUser = false;
        unread = 0;
        updateUnread(chat.state.activeRoom, false);
    }

    function updateUnread(room, isMentioned) {
        if (ui.hasFocus() === false) {
            isUnreadMessageForUser = (isUnreadMessageForUser || isMentioned);

            unread = unread + 1;
        } else {
            //we're currently focused so remove
            //the * notification
            isUnreadMessageForUser = false;
        }

        ui.updateUnread(room, isMentioned);

        updateTitle();
    }

    // Room commands

    // When the /join command gets raised this is called
    chat.client.joinRoom = function (room) {
        var added = ui.addRoom(room);

        ui.setActiveRoom(room.Name);

        if (room.Private) {
            ui.setRoomLocked(room.Name);
        }
        if (room.Closed) {
            ui.setRoomClosed(room.Name);
        }

        if (added) {
            populateRoom(room.Name).done(function () {
                ui.addMessage(utility.getLanguageResource('Chat_YouEnteredRoom', room.Name), 'notification', room.Name);

                if (room.Welcome) {
                    ui.addMessage(room.Welcome, 'welcome', room.Name);
                }
            });
        }
    };

    // Called when a returning users join chat
    chat.client.logOn = function (rooms, myRooms, userPreferences) {
        privateRooms = myRooms;

        var loadRooms = function () {
            var filteredRooms = [];
            $.each(rooms, function (index, room) {
                if (chat.state.activeRoom !== room.Name) {
                    filteredRooms.push(room.Name);
                }
            });

            // Set the amount of rooms to load
            roomsToLoad = filteredRooms.length;

            populateRooms(filteredRooms);
        };

        var loadCommands = function () {
            // get list of available commands
            chat.server.getCommands()
                .done(function (commands) {
                    ui.setCommands(commands);

                    for (var i = 0; i < commands.length; ++i) {
                        commandNameLookup[commands[i].Name] = true;
                    }
                });

            // get list of available shortcuts
            chat.server.getShortcuts()
                .done(function (shortcuts) {
                    ui.setShortcuts(shortcuts);
                });
        };

        $.each(rooms, function (index, room) {
            ui.addRoom(room);
            if (room.Private) {
                ui.setRoomLocked(room.Name);
            }
            if (room.Closed) {
                ui.setRoomClosed(room.Name);
            }
        });

        chat.state.tabOrder = userPreferences.TabOrder;
        ui.updateTabOrder(chat.state.tabOrder);

        ui.setUserName(chat.state.name);
        ui.setUnreadNotifications(chat.state.unreadNotifications);

        // Process any urls that may contain room names
        ui.run();

        // Otherwise set the active room
        ui.setActiveRoom(this.state.activeRoom || 'Lobby');

        if (this.state.activeRoom) {
            // Always populate the active room first then load the other rooms so it looks fast :)
            populateRoom(this.state.activeRoom).done(function () {
                loadCommands();
                populateLobbyRooms();

                loadRooms();

                // No rooms to load just hide the splash screen
                if (roomsToLoad === 0) {
                    ui.hideSplashScreen();
                }
            });
        }
        else {
            // Populate the lobby first then everything else
            populateLobbyRooms().done(function () {
                loadCommands();
                loadRooms();

                // No rooms to load just hide the splash screen
                if (roomsToLoad === 0) {
                    ui.hideSplashScreen();
                }
            });
        }
    };

    chat.client.roomLoaded = function (roomInfo) {
        populateRoomFromInfo(roomInfo);

        if (roomsToLoad === 1) {
            ui.hideSplashScreen();
        }
        else {
            roomsToLoad = roomsToLoad - 1;
        }
    };

    chat.client.logOut = function () {
        performLogout();
    };

    chat.client.lockRoom = function (user, room, userHasAccess) {
        if (!isSelf(user) && this.state.activeRoom === room) {
            awaitRoomReady(room, this, function() {
                ui.addMessage(utility.getLanguageResource('Chat_UserLockedRoom', user.Name, room), 'notification', room);
            });
        }

        if (userHasAccess) {
            ui.setRoomLocked(room);
            ui.updatePrivateLobbyRooms(room);
        } else {
            ui.removeLobbyRoom(room);
        }
    };

    // Called when this user locked a room
    chat.client.roomLocked = function (room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_RoomNowLocked', room), 'notification', this.state.activeRoom);
        });
    };

    chat.client.roomClosed = function(room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_RoomNowClosed', room), 'notification', this.state.activeRoom);
        });

        ui.closeRoom(room);

        if (this.state.activeRoom === room) {
            ui.toggleMessageSection(true);
        }
    };

    chat.client.roomUnClosed = function (room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_RoomNowOpen', room), 'notification', this.state.activeRoom);
        });

        ui.unCloseRoom(room);

        if (this.state.activeRoom === room) {
            ui.toggleMessageSection(false);
        }
    };

    chat.client.addOwner = function (user, room) {
        awaitRoomReady(room, this, function () {
            ui.setRoomOwner(user.Name, room);
        });
    };

    chat.client.removeOwner = function (user, room) {
        awaitRoomReady(room, this, function () {
            ui.clearRoomOwner(user.Name, room);
        });
    };

    chat.client.updateRoom = function (room) {
        ui.updateLobbyRoom(room);
    };

    chat.client.markInactive = function (users) {
        $.each(users, function () {
            var viewModel = getUserViewModel(this);
            ui.setUserActivity(viewModel);
        });
    };

    chat.client.updateActivity = function (user) {
        var viewModel = getUserViewModel(user);
        ui.setUserActivity(viewModel);
    };

    chat.client.addMessageContent = function (id, content, room) {
        awaitRoomReady(room, this, function () {
            scrollIfNecessary(function() {
                ui.addChatMessageContent(id, content, room);
            }, room);

            updateUnread(room, false /* isMentioned: this is outside normal messages and user shouldn't be mentioned */);

            ui.watchMessageScroll([id], room);
        });
    };

    chat.client.replaceMessage = function (id, message, room) {
        awaitRoomReady(room, this, function() {
            ui.confirmMessage(id);

            var viewModel = getMessageViewModel(message);

            scrollIfNecessary(function() {

                // Update your message when it comes from the server
                ui.overwriteMessage(id, viewModel);
            }, room);

            var isMentioned = viewModel.highlight === 'highlight';

            updateUnread(room, isMentioned);
        });
    };

    chat.client.addMessage = function (message, room) {
        awaitRoomReady(room, this, function() {
            var viewModel = getMessageViewModel(message);

            scrollIfNecessary(function() {
                // Update your message when it comes from the server
                if (ui.messageExists(viewModel.id)) {
                    ui.replaceMessage(viewModel);
                } else {
                    ui.addChatMessage(viewModel, room);
                }
            }, room);

            var isMentioned = viewModel.highlight === 'highlight';

            updateUnread(room, isMentioned);
        });
    };

    chat.client.addUser = function (user, room, isOwner) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            var viewModel = getUserViewModel(user, isOwner);

            var added = ui.addUser(viewModel, room);

            if (added) {
                if (!isSelf(user)) {
                    ui.addMessage(utility.getLanguageResource('Chat_UserEnteredRoom', user.Name, room), 'notification', room);
                }
            }
        });
    };

    chat.client.changeUserName = function (oldName, user, room) {
        awaitRoomReady(room, this, function() {
            ui.changeUserName(oldName, user, room);

            if (!isSelf(user)) {
                ui.addMessage(utility.getLanguageResource('Chat_UserNameChanged', oldName, user.Name), 'notification', room);
            }
        });
    };

    chat.client.changeGravatar = function (user, room) {
        awaitRoomReady(room, this, function() {
            ui.changeGravatar(user, room);

            if (!isSelf(user)) {
                ui.addMessage(utility.getLanguageResource('Chat_UserGravatarChanged', user.Name), 'notification', room);
            }
        });
    };

    // User single client commands

    chat.client.allowUser = function (room, roomInfo) {
        awaitRoomReady(this.state.activeRoom, this, function () {
            ui.addMessage(utility.getLanguageResource('Chat_YouGrantedRoomAccess', room), 'notification', this.state.activeRoom);
        });

        ui.updateLobbyRoom(roomInfo);
    };

    chat.client.userAllowed = function (user, room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_UserGrantedRoomAccess', user, room), 'notification', this.state.activeRoom);
        });
    };

    chat.client.unallowUser = function (room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_YourRoomAccessRevoked', room), 'notification', this.state.activeRoom);
        });

        ui.removeLobbyRoom(room);
    };

    chat.client.userUnallowed = function (user, room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_YouRevokedUserRoomAccess', user, room), 'notification', this.state.activeRoom);
        });
    };

    // Called when you make someone an owner
    chat.client.ownerMade = function (user, room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_UserGrantedRoomOwnership', user, room), 'notification', this.state.activeRoom);
        });
    };

    chat.client.ownerRemoved = function (user, room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_UserRoomOwnershipRevoked', user, room), 'notification', this.state.activeRoom);
        });
    };

    // Called when you've been made an owner
    chat.client.makeOwner = function (room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_YouGrantedRoomOwnership', room), 'notification', this.state.activeRoom);
        });
    };

    // Called when you've been removed as an owner
    chat.client.demoteOwner = function (room) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_YourRoomOwnershipRevoked', room), 'notification', this.state.activeRoom);
        });
    };

    // Called when your gravatar has been changed
    chat.client.gravatarChanged = function () {
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_YourGravatarChanged'), 'notification', this.state.activeRoom);
        });
    };

    // Called when the server sends a notification message
    chat.client.postNotification = function (msg, room) {
        awaitRoomReady(room, this, function() {
            ui.addMessage(msg, 'notification', room);
        });
    };

    chat.client.postMessage = function (msg, type, room) {
        awaitRoomReady(room, this, function() {
            ui.addMessage(msg, type, room);
        });
    };

    chat.client.forceUpdate = function () {
        ui.showUpdateUI();
    };

    chat.client.showUserInfo = function (userInfo) {
        awaitRoomReady(null, this, function() {
            var lastActivityDate = userInfo.LastActivity.fromJsonDate();
            var status = "Currently " + userInfo.Status;
            if (userInfo.IsAfk) {
                status += userInfo.Status === 'Active' ? ' but ' : ' and ';
                status += ' is Afk';
            }
            ui.addMessage('User information for ' + userInfo.Name +
                " (" + status + " - last seen " + $.timeago(lastActivityDate) + ")", 'list-header');

            if (userInfo.AfkNote) {
                ui.addMessage('Afk: ' + userInfo.AfkNote, 'list-item');
            } else if (userInfo.Note) {
                ui.addMessage('Note: ' + userInfo.Note, 'list-item');
            }

            if (userInfo.Hash) {
                $.getJSON('https://secure.gravatar.com/' + userInfo.Hash + '.json?callback=?').done(function(profile) {
                    if (profile && profile.entry) {
                        ui.showGravatarProfile(profile.entry[0]);
                    }
                });
            }

            chat.client.showUsersOwnedRoomList(userInfo.Name, userInfo.OwnedRooms);
        });
    };

    chat.client.setPassword = function () {
        ui.addMessage(utility.getLanguageResource('Chat_YourPasswordSet'), 'notification', this.state.activeRoom);
    };

    chat.client.changePassword = function () {
        ui.addMessage(utility.getLanguageResource('Chat_YourPasswordSet'), 'notification', this.state.activeRoom);
    };

    chat.client.changeAfk = function (user, room) {
        awaitRoomReady(room, this, function() {
            var viewModel = getUserViewModel(user);

            ui.changeNote(viewModel, room);

            var message;

            if (!isSelf(user)) {
                if (user.AfkNote) {
                    message = utility.getLanguageResource('Chat_UserIsAfkNote', user.Name, user.AfkNote);
                } else {
                    message = utility.getLanguageResource('Chat_UserIsAfk', user.Name);
                }
            } else {
                if (user.AfkNote) {
                    message = utility.getLanguageResource('Chat_YouAreAfkNote', user.AfkNote);
                } else {
                    message = utility.getLanguageResource('Chat_YouAreAfk');
                }
            }

            ui.addMessage(message, 'notification', room);
        });
    };

    // Make sure all the people in all the rooms know that a user has changed their note.
    chat.client.changeNote = function (user, room) {
        awaitRoomReady(room, this, function() {
            var viewModel = getUserViewModel(user);

            ui.changeNote(viewModel, room);

            var message;

            if (!isSelf(user)) {
                if (user.Note) {
                    message = utility.getLanguageResource('Chat_UserNoteSet', user.Name, user.Note);
                } else {
                    message = utility.getLanguageResource('Chat_UserNoteCleared', user.Name);
                }
            } else {
                if (user.Note) {
                    message = utility.getLanguageResource('Chat_YourNoteSet', user.Note);
                } else {
                    message = utility.getLanguageResource('Chat_YourNoteCleared');
                }
            }

            ui.addMessage(message, 'notification', room);
        });
    };

    chat.client.topicChanged = function (roomName, topic, who) {
        awaitRoomReady(roomName, this, function() {
            var message,
                isCleared = (topic === '');

            if (who === ui.getUserName()) {
                if (!isCleared) {
                    message = utility.getLanguageResource('Chat_YouSetRoomTopic', topic);
                } else {
                    message = utility.getLanguageResource('Chat_YouClearedRoomTopic');
                }
            } else {
                if (!isCleared) {
                    message = utility.getLanguageResource('Chat_UserSetRoomTopic', who, topic);
                } else {
                    message = utility.getLanguageResource('Chat_UserClearedRoomTopic', who);
                }
            }

            ui.addMessage(message, 'notification', roomName);

            ui.changeRoomTopic(roomName, topic);
        });
    };

    chat.client.welcomeChanged = function (isCleared, welcome) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            var message;

            if (!isCleared) {
                message = utility.getLanguageResource('Chat_YouSetRoomWelcome', welcome);
            } else {
                message = utility.getLanguageResource('Chat_YouClearedRoomWelcome');
            }

            ui.addMessage(message, 'notification', this.state.activeRoom);
            if (welcome) {
                ui.addMessage(welcome, 'welcome', this.state.activeRoom);
            }
        });
    };

    // Called when you have added or cleared a flag
    chat.client.flagChanged = function (isCleared, country) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            var message;

            if (!isCleared) {
                message = utility.getLanguageResource('Chat_YouSetFlag', country);
            } else {
                message = utility.getLanguageResource('Chat_YouClearedFlag');
            }

            ui.addMessage(message, 'notification', this.state.activeRoom);
        });
    };

    // Make sure all the people in the all the rooms know that a user has changed their flag
    chat.client.changeFlag = function (user, room) {
        awaitRoomReady(room, this, function() {
            var viewModel = getUserViewModel(user),
                message;

            ui.changeFlag(viewModel, room);

            if (!isSelf(user)) {
                if (user.Flag) {
                    message = utility.getLanguageResource('Chat_UserSetFlag', user.Name, viewModel.country);
                } else {
                    message = utility.getLanguageResource('Chat_UserClearedFlag', user.Name);
                }

                ui.addMessage(message, 'notification', room);
            }
        }, this);
    };

    chat.client.userNameChanged = function (user) {
        // Update the client state
        chat.state.name = user.Name;
        ui.setUserName(chat.state.name);
        awaitRoomReady(this.state.activeRoom, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_YourNameChanged', user.Name), 'notification', this.state.activeRoom);
        });
    };

    chat.client.setTyping = function (user, room) {
        var viewModel = getUserViewModel(user);
        awaitRoomReady(room, this, function() {
            ui.setUserTyping(viewModel, room);
        });
    };

    chat.client.sendMeMessage = function (name, message, room) {
        awaitRoomReady(room, this, function() {
            ui.addMessage(utility.getLanguageResource('Chat_UserPerformsAction', name, message), 'action', room);
        });
    };

    chat.client.sendPrivateMessage = function (from, to, message) {
        if (isSelf({ Name: to })) {
            // Force notification for direct messages
            ui.notify(true);
            ui.setLastPrivate(from);
        }

        awaitRoomReady(null, this, function() {
            ui.addPrivateMessage(utility.getLanguageResource('Chat_PrivateMessage', from, to, message), 'pm');
        });
    };

    chat.client.sendInvite = function (from, to, room) {
        awaitRoomReady(null, this, function() {
            if (isSelf({ Name: to })) {
                ui.notify(true);
                ui.addPrivateMessage(utility.getLanguageResource('Chat_UserInvitedYouToRoom', from, room), 'pm');
            } else {
                ui.addPrivateMessage(utility.getLanguageResource('Chat_YouInvitedUserToRoom', to, room), 'pm');
            }
        });
    };

    chat.client.nudge = function (from, to) {
        var message;

        function shake(n) {
            var move = function (x, y) {
                parent.moveBy(x, y);
            };
            for (var i = n; i > 0; i--) {
                for (var j = 1; j > 0; j--) {
                    move(i, 0);
                    move(0, -i);
                    move(-i, 0);
                    move(0, i);
                    move(i, 0);
                    move(0, -i);
                    move(-i, 0);
                    move(0, i);
                    move(i, 0);
                    move(0, -i);
                    move(-i, 0);
                    move(0, i);
                }
            }
        }
        // the method is called if we're the sender, or recipient of a nudge.
        if (!isSelf({ Name: from })) {
            $("#chat-area").pulse({ opacity: 0 }, { duration: 300, pulses: 3 });
            window.setTimeout(function () {
                shake(20);
            }, 300);
        }

        awaitRoomReady(null, this, function() {
            if (to) {
                if (isSelf({ Name: to })) {
                    message = utility.getLanguageResource('Chat_UserNudgedYou', from);
                } else {
                    message = utility.getLanguageResource('Chat_UserNudgedUser', from, to);
                }

                ui.addMessage(message, 'pm');
            } else {
                message = utility.getLanguageResource('Chat_UserNudgedRoom', from);
                ui.addMessage(message, 'notification');
            }
        });
    };

    chat.client.leave = function (user, room) {
        if (isSelf(user)) {
            ui.setActiveRoom('Lobby');
            ui.removeRoom(room);
        }
        else {
            awaitRoomReady(room, this, function() {
                ui.removeUser(user, room);
                var message = utility.getLanguageResource('Chat_UserLeftRoom', user.Name, room);
                ui.addMessage(message, 'notification', room);
            });
        }
    };

    chat.client.kick = function (room) {
        ui.setActiveRoom('Lobby');
        ui.removeRoom(room);
        // todo: We used to add a notification to the lobby, though this will never be seen.  Maybe we should add a modal or something for this case?
        // var message = utility.getLanguageResource('Chat_YouKickedFromRoom', room);
    };

    // Helpish commands
    chat.client.showRooms = function (rooms) {
        awaitRoomReady(null, this, function() {
            ui.addMessage('Rooms', 'list-header');
            if (!rooms.length) {
                ui.addMessage(utility.getLanguageResource('Chat_NoRoomsAvailable'), 'list-item');
            } else {
                // sort rooms by count descending then name
                var sorted = rooms.sort(function(a, b) {
                    if (a.Closed && !b.Closed) {
                        return 1;
                    } else if (b.Closed && !a.Closed) {
                        return -1;
                    }

                    if (a.Count > b.Count) {
                        return -1;
                    } else if (b.Count > a.Count) {
                        return 1;
                    }

                    return a.Name.toString().toUpperCase().localeCompare(b.Name.toString().toUpperCase());
                });

                $.each(sorted, function() {
                    ui.addMessage(this.Name + ' (' + this.Count + ')', 'list-item');
                });
            }
        });
    };

    chat.client.showCommands = function () {
        ui.showHelp();
    };

    chat.client.showUsersInRoom = function (room, names) {
        awaitRoomReady(null, this, function() {
            var message = utility.getLanguageResource('Chat_RoomUsersHeader', room);
            ui.addMessage(message, 'list-header');
            if (names.length === 0) {
                var emptyMessage = utility.getLanguageResource('Chat_RoomUsersEmpty');
                ui.addMessage(emptyMessage, 'list-item');
            } else {
                $.each(names, function() {
                    ui.addMessage('- ' + this, 'list-item');
                });
            }
        });
    };

    chat.client.listUsers = function (users) {
        awaitRoomReady(room, this, function() {
            var header;

            if (users.length === 0) {
                header = utility.getLanguageResource('Chat_RoomSearchEmpty');
                ui.addMessage(header, 'list-header');
            } else {
                header = utility.getLanguageResource('Chat_RoomSearchResults');
                ui.addMessage(header, 'list-header');
                ui.addMessage(users.join(', '), 'list-item');
            }
        });
    };

    chat.client.listAllowedUsers = function (room, isPrivate, users) {
        awaitRoomReady(null, this, function() {
            var message;

            if (!isPrivate) {
                message = utility.getLanguageResource('Chat_RoomNotPrivateAllowed', room);
            } else if (users.length === 0) {
                message = utility.getLanguageResource('Chat_RoomPrivateNoUsersAllowed', room);
            } else {
                message = utility.getLanguageResource('Chat_RoomPrivateUsersAllowedResults', room);
            }

            ui.addMessage(message, 'list-header');

            if (isPrivate && users.length > 0) {
                ui.addMessage(users.join(', '), 'list-item');
            }
        });
    };

    chat.client.showUsersRoomList = function (user, rooms) {
        awaitRoomReady(null, this, function() {
            var message;
            if (rooms.length === 0) {
                message = utility.getLanguageResource('Chat_UserNotInRooms', user.Name, user.Status);
                ui.addMessage(message, 'list-header');
            } else {
                message = utility.getLanguageResource('Chat_UserInRooms', user.Name, user.Status);
                ui.addMessage(message, 'list-header');
                ui.addMessage(rooms.join(', '), 'list-item');
            }
        });
    };

    chat.client.showUsersOwnedRoomList = function (user, rooms) {
        awaitRoomReady(null, this, function() {
            var message;

            if (rooms.length === 0) {
                message = utility.getLanguageResource('Chat_UserOwnsNoRooms', user);
                ui.addMessage(message, 'list-header');
            } else {
                message = utility.getLanguageResource('Chat_UserOwnsRooms', user);
                ui.addMessage(message, 'list-header');
                ui.addMessage(rooms.join(', '), 'list-item');
            }
        });
    };

    chat.client.addAdmin = function (user, roomName) {
        awaitRoomReady(roomName, this, function() {
            ui.setRoomAdmin(user.Name, roomName);
        });
    };

    chat.client.removeAdmin = function (user, roomName) {
        awaitRoomReady(roomName, this, function() {
            ui.clearRoomAdmin(user.Name, roomName);
        });
    };

    // Called when you make someone an admin
    chat.client.adminMade = function (user) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            var message = utility.getLanguageResource('Chat_UserAdminAllowed', user);
            ui.addMessage(message, 'notification', this.state.activeRoom);
        });
    };

    chat.client.adminRemoved = function (user) {
        awaitRoomReady(this.state.activeRoom, this, function() {
            var message = utility.getLanguageResource('Chat_UserAdminRevoked', user);
            ui.addMessage(message, 'notification', this.state.activeRoom);
        });
    };

    // Called when you've been made an admin
    chat.client.makeAdmin = function () {
        awaitRoomReady(this.state.activeRoom, this, function() {
            var message = utility.getLanguageResource('Chat_YouAdminAllowed');
            ui.addMessage(message, 'notification', this.state.activeRoom);
        });
    };

    // Called when you've been removed as an admin
    chat.client.demoteAdmin = function () {
        awaitRoomReady(this.state.activeRoom, this, function() {
            var message = utility.getLanguageResource('Chat_YouAdminRevoked');
            ui.addMessage(message, 'notification', this.state.activeRoom);
        });
    };

    chat.client.broadcastMessage = function (message, room) {
        awaitRoomReady(room, this, function() {
            var broadcastMessage = utility.getLanguageResource('Chat_AdminBroadcast', message);
            ui.addMessage(broadcastMessage, 'broadcast', room);
        });
    };

    chat.client.outOfSync = function () {
        ui.showUpdateUI();
    };

    chat.client.updateUnreadNotifications = function (read) {
        ui.setUnreadNotifications(read);
    };

    chat.client.updateTabOrder = function (tabOrder) {
        ui.updateTabOrder(tabOrder);
    };

    $ui.bind(ui.events.typing, function () {
        // If not in a room, don't try to send typing notifications
        if (!chat.state.activeRoom) {
            return;
        }

        if (checkingStatus === false && typing === false) {
            typing = true;

            try {
                ui.setRoomTrimmable(chat.state.activeRoom, typing);
                chat.server.typing(chat.state.activeRoom);
            }
            catch (e) {
                connection.hub.log('Failed to send via websockets');
            }

            window.setTimeout(function () {
                typing = false;
            },
            3000);
        }
    });

    $ui.bind(ui.events.fileUploaded, function (ev, uploader) {
        uploader.submitFile(connection.hub.id, chat.state.activeRoom);
    });

    $ui.bind(ui.events.sendMessage, function (ev, msg) {
        clearUnread();

        var id = utility.newId(),
            clientMessage = {
                id: id,
                content: msg,
                room: chat.state.activeRoom
            },
            messageCompleteTimeout = null;

        if (!isCommand(msg)) {

            // if you're in the lobby, you can't send mesages (only commands)
            if (chat.state.activeRoom === undefined) {
                awaitRoomReady(null, this, function() {
                    var message = utility.getLanguageResource('Chat_CannotSendLobby');
                    ui.addMessage(message, 'error');
                });
                return false;
            }

            // Added the message to the ui first
            var viewModel = {
                name: chat.state.name,
                hash: chat.state.hash,
                message: ui.processContent(clientMessage.content),
                id: clientMessage.id,
                date: new Date(),
                highlight: '',
                isMine: true
            };

            ui.addChatMessage(viewModel, clientMessage.room);

            // If there's a significant delay in getting the message sent
            // mark it as pending
            messageCompleteTimeout = window.setTimeout(function () {
                if ($.connection.hub.state === $.connection.connectionState.reconnecting) {
                    ui.failMessage(id);
                }
                else {
                    // If after a second
                    ui.markMessagePending(id);
                }
            },
            messageSendingDelay);

            pendingMessages[id] = messageCompleteTimeout;
        }

        try {
            chat.server.send(clientMessage)
                .done(function () {
                    if (messageCompleteTimeout) {
                        clearTimeout(messageCompleteTimeout);
                        delete pendingMessages[id];
                    }

                    ui.confirmMessage(id);
                })
                .fail(function (e) {
                    ui.failMessage(id);
                    if (e.source === 'HubException') {
                        ui.addMessage(e.message, 'error');
                    }
                });
        }
        catch (e) {
            connection.hub.log('Failed to send via websockets');

            clearTimeout(pendingMessages[id]);
            ui.failMessage(id);
        }

        // Store message history
        messageHistory.push(msg);

        // REVIEW: should this pop items off the top after a certain length?
        historyLocation = messageHistory.length;
    });

    $ui.bind(ui.events.focusit, function () {
        clearUnread();

        try {
            chat.server.updateActivity();
        }
        catch (e) {
            connection.hub.log('updateActivity failed');
        }
    });

    $ui.bind(ui.events.blurit, function () {
        updateTitle();
    });

    $ui.bind(ui.events.openRoom, function (ev, room) {
        try {
            chat.server.send('/join ' + room, chat.state.activeRoom)
                .fail(function (e) {
                    ui.setActiveRoom('Lobby');
                    if (e.source === 'HubException') {
                        ui.addMessage(e.message, 'error');
                    }
                });
        }
        catch (e) {
            connection.hub.log('openRoom failed');
        }
    });

    $ui.bind(ui.events.closeRoom, function (ev, room) {
        try {
            chat.server.send('/leave ' + room, chat.state.activeRoom)
                .fail(function (e) {
                    if (e.source === 'HubException') {
                        ui.addMessage(e.message, 'error');
                    }
                });
        }
        catch (e) {
            // This can fail if the server is offline
            connection.hub.log('closeRoom room failed');
        }
    });

    $ui.bind(ui.events.prevMessage, function () {
        historyLocation -= 1;
        if (historyLocation < 0) {
            historyLocation = messageHistory.length - 1;
        }
        ui.setMessage(messageHistory[historyLocation]);
    });

    $ui.bind(ui.events.nextMessage, function () {
        historyLocation = (historyLocation + 1) % messageHistory.length;
        ui.setMessage(messageHistory[historyLocation]);
    });

    $ui.bind(ui.events.activeRoomChanged, function (ev, room) {
        if (room === 'Lobby') {
            // Remove the active room
            chat.state.activeRoom = undefined;
        }
        else {
            // When the active room changes update the client state and the cookie
            chat.state.activeRoom = room;
        }

        ui.scrollToBottom(room);
        updateCookie();
    });

    $ui.bind(ui.events.scrollRoomTop, function (ev, roomInfo) {
        // Do nothing if we're loading history already
        if (loadingHistory === true) {
            return;
        }

        loadingHistory = true;

        try {
            // Show a little animation so the user experience looks fancy
            ui.setLoadingHistory(true);

            ui.setRoomTrimmable(roomInfo.name, false);
            connection.hub.log('getPreviousMessages(' + roomInfo.name + ')');
            chat.server.getPreviousMessages(roomInfo.messageId)
                .done(function (messages) {
                    connection.hub.log('getPreviousMessages.done(' + roomInfo.name + ')');
                    ui.prependChatMessages($.map(messages, getMessageViewModel), roomInfo.name);
                    loadingHistory = false;

                    ui.setLoadingHistory(false);
                })
                .fail(function (e) {
                    connection.hub.log('getPreviousMessages.failed(' + roomInfo.name + ', ' + e + ')');
                    loadingHistory = false;

                    ui.setLoadingHistory(false);
                });
        }
        catch (e) {
            connection.hub.log('getPreviousMessages failed');
            ui.setLoadingHistory(false);
        }
    });

    $(ui).bind(ui.events.reloadMessages, function () {

    });

    $(ui).bind(ui.events.preferencesChanged, function () {
        updateCookie();
    });

    $(ui).bind(ui.events.loggedOut, function () {
        logout();
    });

    $ui.bind(ui.events.tabOrderChanged, function (ev, tabOrder) {
        var orderChanged = false;

        if (chat.tabOrder === undefined || chat.tabOrder.length !== tabOrder.length) {
            orderChanged = true;
        }

        if (orderChanged === false) {
            for (var i = 0; i < tabOrder.length; i++) {
                if (chat.tabOrder[i] !== tabOrder[i]) {
                    orderChanged = true;
                }
            }
        }

        if (orderChanged === false) {
            return;
        }

        chat.server.tabOrderChanged(tabOrder)
            .done(function () {
                chat.tabOrder = tabOrder;
            })
            .fail(function () {
                // revert ordering
                ui.updateTabOrder(chat.tabOrder);
            });
    });

    $(function () {
        var stateCookie = $.cookie('jabbr.state'),
            state = stateCookie ? JSON.parse(stateCookie) : {},
            initial = true,
            initialized = false,
            welcomeMessages = utility.getLanguageResource('Chat_InitialMessages').split('\n');

        // Initialize the ui, passing the user preferences
        ui.initialize(state.preferences);

        for (var i = 0; i < welcomeMessages.length; i++) {
            ui.addMessage(welcomeMessages, 'notification');
        }

        function initConnection() {
            var logging = $.cookie('jabbr.logging') === '1',
                transport = $.cookie('jabbr.transport') || ['webSockets', 'serverSentEvents', 'longPolling'],
                options = {};

            if (transport) {
                options.transport = transport;
            }

            connection.hub.logging = logging;
            connection.hub.qs = "version=" + window.jabbrVersion;
            connection.hub.start(options)
                          .done(function () {

                              chat.server.join()
                                  .fail(function () {
                                      // So refresh the page, our auth token is probably gone
                                      performLogout();
                                  });

                              initialized = true;
                          });

            connection.hub.stateChanged(function (change) {
                if (change.newState === $.connection.connectionState.reconnecting) {
                    failPendingMessages();

                    ui.showStatus(1, '');
                }
                else if (change.newState === $.connection.connectionState.connected) {
                    if (!initial) {
                        ui.showStatus(0, $.connection.hub.transport.name);
                        ui.setReadOnly(false);
                    } else {
                        ui.initializeConnectionStatus($.connection.hub.transport.name);
                    }

                    initial = false;
                }
                else if (change.newState === $.connection.connectionState.disconnected && initial === true) {
                    initial = false;
                }
            });

            connection.hub.disconnected(function () {
                if (initialized === true) {
                    connection.hub.log('Dropped the connection from the server. Restarting in 5 seconds.');

                    failPendingMessages();
                }

                ui.showStatus(2, '');
                ui.setReadOnly(true);

                // Restart the connection
                setTimeout(function () {
                    connection.hub.start(options)
                                  .done(function () {
                                      // When this works uncomment it.
                                      // ui.showReloadMessageNotification();

                                      // Turn the firehose back on
                                      chat.server.join(initialized).fail(function () {
                                          // So refresh the page, our auth token is probably gone
                                          performLogout();
                                      });

                                      initialized = true;
                                  });
                }, 5000);
            });

            connection.hub.error(function () {
                // Make all pending messages failed if there's an error
                failPendingMessages();
            });
        }

        initConnection();
    });

})(window.jQuery, window.jQuery.connection, window, window.chat.ui, window.chat.utility);
