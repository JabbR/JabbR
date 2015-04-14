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
        banDialogShown = false;

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
                        ui.addErrorToActiveRoom(e.message);
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
            })
            .always(function (e) {
                ui.hideSplashScreen();
            });
    }

    var getRoomInfoMaxRetries = 5;
    var getRoomInfoRetries = 0;
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
                    getRoomInfoRetries++;
                    if (getRoomInfoRetries < getRoomInfoMaxRetries) {
                        // This was causing a forever loading screen if a user attempts to join a
                        // private room that is not in their allowed rooms list.
                        // Added a retry count so it will stop trying to populate the room
                        // and close the loading screen
                        setTimeout(function () {
                            populateRoom(room, deferred);
                        },
                        1000);
                    }
                    else
                    {
                        deferred.rejectWith(chat);
                    }
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
        var re = new RegExp("\\b@?" + chat.state.name.replace(/\./g, '\\.') + "\\b", "i");
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
        ui.setRoomLoading(true, room.Name);
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
                ui.setRoomLoading(false);
                ui.addNotification(utility.getLanguageResource('Chat_YouEnteredRoom', room.Name), room.Name);

                if (room.Welcome) {
                    ui.addWelcome(room.Welcome, room.Name);
                }
            });
        }
        else {
            ui.setRoomLoading(false);
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
        ui.setUserHash(chat.state.hash);
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
            })
            .fail(function () {
                loadRooms();
                //display error message
                console.log('logOn.populateRoom(' + this.state.activeRoom + ') failed');
                ui.addMessage('Failed to populate \'' + this.state.activeRoom + '\'', 'error', this.state.activeRoom);
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
            ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_UserLockedRoom', user.Name, room));
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
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_RoomNowLocked', room));
    };

    chat.client.roomClosed = function (room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_RoomNowClosed', room));

        ui.closeRoom(room);

        if (this.state.activeRoom === room) {
            ui.toggleMessageSection(true);
        }
    };

    chat.client.roomUnClosed = function (room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_RoomNowOpen', room));

        ui.unCloseRoom(room);

        if (this.state.activeRoom === room) {
            ui.toggleMessageSection(false);
        }
    };

    chat.client.addOwner = function (user, room) {
        ui.setRoomOwner(user.Name, room);
    };

    chat.client.removeOwner = function (user, room) {
        ui.clearRoomOwner(user.Name, room);
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
        scrollIfNecessary(function () {
            ui.addChatMessageContent(id, content, room);
        }, room);

        updateUnread(room, false /* isMentioned: this is outside normal messages and user shouldn't be mentioned */);

        ui.watchMessageScroll([id], room);
    };

    chat.client.replaceMessage = function (id, message, room) {
        ui.confirmMessage(id);

        var viewModel = getMessageViewModel(message);

        scrollIfNecessary(function () {

            // Update your message when it comes from the server
            ui.overwriteMessage(id, viewModel);
        }, room);

        var isMentioned = viewModel.highlight === 'highlight';

        updateUnread(room, isMentioned);
    };

    chat.client.addMessage = function (message, room) {
        var viewModel = getMessageViewModel(message);

        scrollIfNecessary(function () {
            // Update your message when it comes from the server
            if (ui.messageExists(viewModel.id)) {
                ui.replaceMessage(viewModel);
            } else {
                ui.addChatMessage(viewModel, room);
            }
        }, room);

        var isMentioned = viewModel.highlight === 'highlight';

        updateUnread(room, isMentioned);
    };

    chat.client.addUser = function (user, room, isOwner) {
        var viewModel = getUserViewModel(user, isOwner);

        var added = ui.addUser(viewModel, room);

        if (added) {
            if (!isSelf(user)) {
                ui.addNotification(utility.getLanguageResource('Chat_UserEnteredRoom', user.Name, room), room);
            }
        }
    };

    chat.client.changeUserName = function (oldName, user, room) {
        ui.changeUserName(oldName, user, room);

        if (!isSelf(user)) {
            ui.addNotification(utility.getLanguageResource('Chat_UserNameChanged', oldName, user.Name), room);
        }
    };

    chat.client.changeGravatar = function (user, room) {
        ui.changeGravatar(user, room);

        if (!isSelf(user)) {
            ui.addNotification(utility.getLanguageResource('Chat_UserGravatarChanged', user.Name), room);
        }
    };

    // User single client commands

    chat.client.allowUser = function (room, roomInfo) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YouGrantedRoomAccess', room));

        ui.updateLobbyRoom(roomInfo);
    };

    chat.client.userAllowed = function (user, room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_UserGrantedRoomAccess', user, room));
    };

    chat.client.unallowUser = function (room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YourRoomAccessRevoked', room));

        ui.removeLobbyRoom(room);
    };

    chat.client.userUnallowed = function (user, room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YouRevokedUserRoomAccess', user, room));
    };

    // Called when you make someone an owner
    chat.client.ownerMade = function (user, room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_UserGrantedRoomOwnership', user, room));
    };

    chat.client.ownerRemoved = function (user, room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_UserRoomOwnershipRevoked', user, room));
    };

    // Called when you've been made an owner
    chat.client.makeOwner = function (room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YouGrantedRoomOwnership', room));
    };

    // Called when you've been removed as an owner
    chat.client.demoteOwner = function (room) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YourRoomOwnershipRevoked', room));
    };

    // Called when your gravatar has been changed
    chat.client.gravatarChanged = function (hash) {
        ui.setUserHash(hash);

        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YourGravatarChanged'));
    };

    // Called when the server sends a notification message
    chat.client.postNotification = function (msg, room) {
        ui.addNotification(msg, room);
    };

    chat.client.postMessage = function (msg, type, room) {
        ui.addMessage(msg, type, room);
    };

    chat.client.forceUpdate = function () {
        ui.showUpdateUI();
    };

    chat.client.ban = function (userInfo, room, callingUser, reason) {
        var message;

        if (isSelf(userInfo)) {
            // don't show multiple instances of dialog
            if (banDialogShown === true) {
                return;
            }
            banDialogShown = true;

            var title = utility.getLanguageResource('Chat_YouBannedTitle');
            if (reason !== null) {
                message = utility.getLanguageResource('Chat_YouBannedReason', callingUser.Name, reason);
            } else {
                message = utility.getLanguageResource('Chat_YouBanned', callingUser.Name);
            }

            ui.addModalMessage(title, message, 'icon-ban-circle').done(function() {
                performLogout();
            });
        } else {
            if (reason !== null) {
                message = utility.getLanguageResource('Chat_UserBannedReason', userInfo.Name, callingUser.Name, reason);
            } else {
                message = utility.getLanguageResource('Chat_UserBanned', userInfo.Name, callingUser.Name);
            }

            ui.addNotification(message, room);
            ui.removeUser(userInfo, room);
        }
    };

    chat.client.unbanUser = function (userInfo) {
        var msg = 'User ' + userInfo.Name + ' was unbanned';
        ui.addNotificationToActiveRoom(msg);
    };

    chat.client.checkBanned = function (userInfo) {
        var msg = 'User ' + userInfo.Name + ' is ' + (userInfo.IsBanned ? '' : 'not ') + 'banned ';
        ui.addNotificationToActiveRoom(msg);
    };

    chat.client.showUserInfo = function (userInfo) {
        var lastActivityDate = userInfo.LastActivity.fromJsonDate(),
            header,
            list = [];
        var status = "Currently " + userInfo.Status;
        if (userInfo.IsAfk) {
            status += userInfo.Status === 'Active' ? ' but ' : ' and ';
            status += ' is Afk';
        }
        
        header = 'User information for ' + userInfo.Name + ' (' + status + ' - last seen ' + $.timeago(lastActivityDate) + ')';

        if (userInfo.AfkNote) {
            list.push('Afk: ' + userInfo.AfkNote);
        }
        else if (userInfo.Note) {
            list.push('Note: ' + userInfo.Note);
        }

        ui.addListToActiveRoom(header, list);

        if (userInfo.Hash) {
            $.getJSON('https://secure.gravatar.com/' + userInfo.Hash + '.json?callback=?').done(function (profile) {
                if (profile && profile.entry) {
                    ui.showGravatarProfile(profile.entry[0]);
                }
            });
        }

        chat.client.showUsersOwnedRoomList(userInfo.Name, userInfo.OwnedRooms);
    };

    chat.client.changeAfk = function (user, room) {
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

        ui.addNotification(message, room);
    };

    // Make sure all the people in all the rooms know that a user has changed their note.
    chat.client.changeNote = function (user, room) {
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

        ui.addNotification(message, room);
    };

    chat.client.topicChanged = function (roomName, topic, who) {
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

        ui.addNotification(message, roomName);

        ui.changeRoomTopic(roomName, topic);
    };

    chat.client.welcomeChanged = function (isCleared, welcome) {
        var message;

        if (!isCleared) {
            message = utility.getLanguageResource('Chat_YouSetRoomWelcome', welcome);
        } else {
            message = utility.getLanguageResource('Chat_YouClearedRoomWelcome');
        }

        ui.addNotificationToActiveRoom(message);
        if (welcome) {
            ui.addWelcomeToActiveRoom(welcome);
        }
    };

    // Called when you have added or cleared a flag
    chat.client.flagChanged = function (isCleared, country) {
        var message;

        if (!isCleared) {
            message = utility.getLanguageResource('Chat_YouSetFlag', country);
        } else {
            message = utility.getLanguageResource('Chat_YouClearedFlag');
        }

        ui.addNotificationToActiveRoom(message);
    };

    // Make sure all the people in the all the rooms know that a user has changed their flag
    chat.client.changeFlag = function (user, room) {
        var viewModel = getUserViewModel(user),
            message;

        ui.changeFlag(viewModel, room);

        if (!isSelf(user)) {
            if (user.Flag) {
                message = utility.getLanguageResource('Chat_UserSetFlag', user.Name, viewModel.country);
            } else {
                message = utility.getLanguageResource('Chat_UserClearedFlag', user.Name);
            }

            ui.addNotification(message, room);
        }
    };

    chat.client.userNameChanged = function (user) {
        // Update the client state
        chat.state.name = user.Name;
        ui.setUserName(chat.state.name);
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YourNameChanged', user.Name));
    };

    chat.client.setTyping = function (user, room) {
        var viewModel = getUserViewModel(user);
        ui.setUserTyping(viewModel, room);
    };

    chat.client.sendMeMessage = function (name, message, room) {
        ui.addAction(utility.getLanguageResource('Chat_UserPerformsAction', name, message), room);
    };

    chat.client.sendPrivateMessage = function (from, to, message) {
        if (isSelf({ Name: to })) {
            // Force notification for direct messages
            ui.notify(true);
            ui.setLastPrivate(from);
        }

        ui.addPrivateMessage(utility.getLanguageResource('Chat_PrivateMessage', from, to, message));
    };

    chat.client.sendInvite = function (from, to, room) {
        if (isSelf({ Name: to })) {
            ui.notify(true);
            ui.addPrivateMessage(utility.getLanguageResource('Chat_UserInvitedYouToRoom', from, room));
        }
        else {
            ui.addPrivateMessage(utility.getLanguageResource('Chat_YouInvitedUserToRoom', to, room));
        }
    };

    chat.client.nudge = function (from, to, roomName) {
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

        if (to) {
            if (isSelf({ Name: to })) {
                message = utility.getLanguageResource('Chat_UserNudgedYou', from);
                
                var toastMessage = {
                    message: message,
                    name: from
                };
                ui.nudge(toastMessage, from);
            } else {
                message = utility.getLanguageResource('Chat_UserNudgedUser', from, to);
            }

            // TODO: make this more consistent (ie make it a broadcast, proper pm to all rooms, or something)
            ui.addPrivateMessage(message);
        } else {
            ui.addPrivateMessage(utility.getLanguageResource('Chat_UserNudgedRoom', from, roomName));
        }
    };

    chat.client.leave = function (user, room) {
        if (isSelf(user)) {
            ui.setRoomLoading(false);
            if (chat.state.activeRoom === room) {
                ui.setActiveRoom('Lobby');
            }
            
            ui.removeRoom(room);
        }
        else {
            ui.removeUser(user, room);
            ui.addNotification(utility.getLanguageResource('Chat_UserLeftRoom', user.Name, room), room);
        }
    };

    chat.client.kick = function (user, room, callingUser, reason) {
        if (isSelf(user)) {
            var title = utility.getLanguageResource('Chat_YouKickedTitle'),
                message;
            
            if (chat.state.activeRoom === room) {
                ui.setActiveRoom('Lobby');
            }

            ui.removeRoom(room);

            if (reason !== null) {
                message = utility.getLanguageResource('Chat_YouKickedFromRoomReason', room, callingUser.Name, reason);
            } else {
                message = utility.getLanguageResource('Chat_YouKickedFromRoom', room, callingUser.Name);
            }

            ui.addModalMessage(title, message, 'icon-ban-circle');
        } else {
            ui.removeUser(user, room);

            if (reason !== null) {
                ui.addNotification(utility.getLanguageResource('Chat_UserKickedFromRoomReason', user.Name, room, callingUser.Name, reason), room);
            } else {
                ui.addNotification(utility.getLanguageResource('Chat_UserKickedFromRoom', user.Name, room, callingUser.Name), room);
            }
        }
    };

    // Helpish commands
    chat.client.showCommands = function () {
        ui.showHelp();
    };

    chat.client.showUsersInRoom = function (room, names) {
        var header = utility.getLanguageResource('Chat_RoomUsersHeader', room);
        if (names.length === 0) {
            ui.addListToActiveRoom(header, [utility.getLanguageResource('Chat_RoomUsersEmpty')]);
        } else {
            ui.addListToActiveRoom(header, $.map(names, function (name) {
                return '- ' + name;
            }));
        }
    };

    chat.client.listUsers = function (users) {
        if (users.length === 0) {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_RoomSearchEmpty'), []);
        } else {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_RoomSearchResults'), [users.join(', ')]);
        }
    };

    chat.client.listAllowedUsers = function (room, isPrivate, users) {
        if (!isPrivate) {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_RoomNotPrivateAllowed', room), []);
        } else if (users.length === 0) {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_RoomPrivateNoUsersAllowed', room), []);
        } else {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_RoomPrivateUsersAllowedResults', room), [users.join(', ')]);
        }
    };

    chat.client.listOwners = function (room, users, creator) {
        if (users.length === 0) {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_RoomOwnersEmpty', room), []);
        } else {
            // we don't want admins or owners tagged, so we don't provide them.
            users = utility.tagUsers(users, null, null, null, creator);
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_RoomOwnersResults', room), [users.join(', ')]);
        }
    };

    chat.client.showUsersRoomList = function (user, rooms) {
        if (rooms.length === 0) {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_UserNotInRooms', user.Name, user.Status), []);
        }
        else {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_UserInRooms', user.Name, user.Status), [rooms.join(', ')]);
        }
    };

    chat.client.showUsersOwnedRoomList = function (user, rooms) {
        if (rooms.length === 0) {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_UserOwnsNoRooms', user), []);
        }
        else {
            ui.addListToActiveRoom(utility.getLanguageResource('Chat_UserOwnsRooms', user), [rooms.join(', ')]);
        }
    };

    chat.client.addAdmin = function (user, room) {
        ui.setRoomAdmin(user.Name, room);
    };

    chat.client.removeAdmin = function (user, room) {
        ui.clearRoomAdmin(user.Name, room);
    };

    // Called when you make someone an admin
    chat.client.adminMade = function (user) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_UserAdminAllowed', user));
    };

    chat.client.adminRemoved = function (user) {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_UserAdminRevoked', user));
    };

    // Called when you've been made an admin
    chat.client.makeAdmin = function () {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YouAdminAllowed'));
    };

    // Called when you've been removed as an admin
    chat.client.demoteAdmin = function () {
        ui.addNotificationToActiveRoom(utility.getLanguageResource('Chat_YouAdminRevoked'));
    };

    chat.client.broadcastMessage = function (message, room) {
        ui.addBroadcast(utility.getLanguageResource('Chat_AdminBroadcast', message), room);
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

    $ui.bind(ui.events.sendMessage, function (ev, msg, msgId, isCommand) {
        clearUnread();

        var id = msgId || utility.newId(),
            clientMessage = {
                id: id,
                content: msg,
                room: chat.state.activeRoom
            },
            messageCompleteTimeout = null;

        if (!isCommand) {
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
                    isCommand = msg.match(/^\/[A-Za-z0-9?]+?/) !== null;
                    ui.failMessage(id, isCommand);
                    if (e.source === 'HubException') {
                        ui.addErrorToActiveRoom(e.message);
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
                        ui.addErrorToActiveRoom(e.message);
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
                        ui.addErrorToActiveRoom(e.message);
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
        // Do nothing if we're loading history already or if we recently loaded history
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
                    window.setTimeout(function() {
                        loadingHistory = false;
                    }, 1000);

                    ui.setLoadingHistory(false);
                })
                .fail(function (e) {
                    connection.hub.log('getPreviousMessages.failed(' + roomInfo.name + ', ' + e + ')');
                    window.setTimeout(function () {
                        loadingHistory = false;
                    }, 1000);

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

        // TODO: something smarter than this - currently we write them to a hidden area in the lobby.
        for (var i = 0; i < welcomeMessages.length; i++) {
            ui.addNotificationToActiveRoom(welcomeMessages[i]);
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
