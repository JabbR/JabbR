/// <reference path="Scripts/jquery-1.7.js" />
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
        privateRooms = null;

    function failPendingMessages() {
        for (var id in pendingMessages) {
            clearTimeout(pendingMessages[id]);
            ui.failMessage(id);
            delete pendingMessages[id];
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
                    ui.addMessage(e, 'error', chat.state.activeRoom);
                });
        });
    }

    function populateRoom(room) {
        var d = $.Deferred();
        
        connection.hub.log('getRoomInfo(' + room + ')');

        // Populate the list of users rooms and messages 
        chat.server.getRoomInfo(room)
                .done(function (roomInfo) {
                    connection.hub.log('getRoomInfo.done(' + room + ')');

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

                    ui.changeRoomTopic(roomInfo);

                    // mark room as initialized to differentiate messages
                    // that are added after initial population
                    ui.setInitialized(room);
                    ui.scrollToBottom(room);
                    ui.setRoomListStatuses(room);

                    d.resolveWith(chat);

                    // Watch the messages after the defer, since room messages
                    // may be appended if we are just joining the room
                    ui.watchMessageScroll(messageIds, room);
                })
                .fail(function (e) {
                    connection.hub.log('getRoomInfo.failed(' + room + ', ' + e + ')');
                    d.rejectWith(chat);
                });

        return d.promise();
    }

    function populateLobbyRooms() {
        try { 
            // Populate the user list with room names
            chat.server.getRooms()
                .done(function (rooms) {
                    ui.populateLobbyRooms(rooms, privateRooms);
                    ui.setInitialized('Lobby');
                });
        }
        catch (e) {
            connection.hub.log('getRooms failed');
        }
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
            admin: user.IsAdmin
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
            isMine: message.User.Name === chat.state.name
        };
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
                ui.addMessage('You just entered ' + room.Name, 'notification', room.Name);

                if (room.Welcome) {
                    ui.addMessage(room.Welcome, 'welcome', room.Name);
                }
            });
        }
    };

    // Called when a returning users join chat
    chat.client.logOn = function (rooms, myRooms) {
        privateRooms = myRooms;

           var loadRooms = function () {
                $.each(rooms, function (index, room) {
                    if (chat.state.activeRoom !== room.Name) {
                        populateRoom(room.Name);
                    }
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
        ui.setUserName(chat.state.name);

        // Process any urls that may contain room names
        ui.run();

        // Otherwise set the active room
        ui.setActiveRoom(this.state.activeRoom || 'Lobby');

        if (this.state.activeRoom) {
            // Populate lobby rooms for intellisense
            populateLobbyRooms();

            // Always populate the active room first then load the other rooms so it looks fast :)
            populateRoom(this.state.activeRoom).done(loadRooms);
        }
        else {
            // There's no active room so we don't care
            loadRooms();
        }
    };

    chat.client.logOut = function () {
        performLogout();
    };

    chat.client.lockRoom = function (user, room) {
        if (!isSelf(user) && this.state.activeRoom === room) {
            ui.addMessage(user.Name + ' has locked ' + room + '.', 'notification', this.state.activeRoom);
        }

        ui.setRoomLocked(room);
        ui.updatePrivateLobbyRooms(room);
    };

    // Called when this user locked a room
    chat.client.roomLocked = function (room) {
        ui.addMessage(room + ' is now locked.', 'notification', this.state.activeRoom);
    };

    chat.client.roomClosed = function (room) {
        ui.addMessage('Room \'' + room + '\' is now closed', 'notification', this.state.activeRoom);

        ui.closeRoom(room);

        if (this.state.activeRoom === room) {
            ui.toggleMessageSection(true);
        }
    };

    chat.client.roomUnClosed = function (room) {
        ui.addMessage('Room \'' + room + '\' is now open', 'notification', this.state.activeRoom);

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

    chat.updateRoomCount = function (room, count) {
        ui.updateLobbyRoomCount(room, count);
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
                ui.addMessage(user.Name + ' just entered ' + room, 'notification', room);
            }
        }
    };

    chat.client.changeUserName = function (oldName, user, room) {
        ui.changeUserName(oldName, user, room);

        if (!isSelf(user)) {
            ui.addMessage(oldName + '\'s nick has changed to ' + user.Name, 'notification', room);
        }
    };

    chat.client.changeGravatar = function (user, room) {
        ui.changeGravatar(user, room);

        if (!isSelf(user)) {
            ui.addMessage(user.Name + "'s gravatar changed.", 'notification', room);
        }
    };

    // User single client commands

    chat.client.allowUser = function (room) {
        ui.addMessage('You were granted access to ' + room, 'notification', this.state.activeRoom);
    };

    chat.client.userAllowed = function (user, room) {
        ui.addMessage(user + ' now has access to ' + room, 'notification', this.state.activeRoom);
    };

    chat.client.unallowUser = function (user, room) {
        ui.addMessage('You access to ' + room + ' was revoked.', 'notification', this.state.activeRoom);
    };

    chat.client.userUnallowed = function (user, room) {
        ui.addMessage('You have revoked ' + user + '"s access to ' + room, 'notification', this.state.activeRoom);
    };

    // Called when you make someone an owner
    chat.client.ownerMade = function (user, room) {
        ui.addMessage(user + ' is now an owner of ' + room, 'notification', this.state.activeRoom);
    };

    chat.client.ownerRemoved = function (user, room) {
        ui.addMessage(user + ' is no longer an owner of ' + room, 'notification', this.state.activeRoom);
    };

    // Called when you've been made an owner
    chat.client.makeOwner = function (room) {
        ui.addMessage('You are now an owner of ' + room, 'notification', this.state.activeRoom);
    };

    // Called when you've been removed as an owner
    chat.client.demoteOwner = function (room) {
        ui.addMessage('You are no longer an owner of ' + room, 'notification', this.state.activeRoom);
    };

    // Called when your gravatar has been changed
    chat.client.gravatarChanged = function () {
        ui.addMessage('Your gravatar has been set', 'notification', this.state.activeRoom);
    };

    // Called when the server sends a notification message
    chat.client.postNotification = function (msg, room) {
        ui.addMessage(msg, 'notification', room);
    };

    chat.client.postMessage = function (msg, type, room) {
        ui.addMessage(msg, type, room);
    };

    chat.client.forceUpdate = function () {
        ui.showUpdateUI();
    };

    chat.client.showUserInfo = function (userInfo) {
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
        }
        else if (userInfo.Note) {
            ui.addMessage('Note: ' + userInfo.Note, 'list-item');
        }

        $.getJSON('https://secure.gravatar.com/' + userInfo.Hash + '.json?callback=?', function (profile) {
            ui.showGravatarProfile(profile.entry[0]);
        });

        chat.showUsersOwnedRoomList(userInfo.Name, userInfo.OwnedRooms);
    };

    chat.client.setPassword = function () {
        ui.addMessage('Your password has been set', 'notification', this.state.activeRoom);
    };

    chat.client.changePassword = function () {
        ui.addMessage('Your password has been changed', 'notification', this.state.activeRoom);
    };

    // Called when you have added or cleared a note
    chat.client.noteChanged = function (isAfk, isCleared) {
        var afkMessage = 'You have gone AFK';
        var noteMessage = 'Your note has been ' + (isCleared ? 'cleared' : 'set');
        ui.addMessage(isAfk ? afkMessage : noteMessage, 'notification', this.state.activeRoom);
    };

    // Make sure all the people in all the rooms know that a user has changed their note.
    chat.client.changeNote = function (user, room) {
        var viewModel = getUserViewModel(user);

        ui.changeNote(viewModel, room);

        if (!isSelf(user)) {
            var message;
            if (user.IsAfk === true) {
                message = user.Name + ' has gone AFK';
            }
            else {
                message = user.Name + ' has ' + (user.Note ? 'set' : 'cleared') + ' their note';
            }

            ui.addMessage(message, 'notification', room);
        }
    };

    chat.client.changeTopic = function (room) {
        ui.changeRoomTopic(room);
    };

    chat.client.topicChanged = function (roomName, isCleared, topic, who) {
        var action = isCleared ? 'cleared' : 'set';
        var to = topic ? ' to ' + '"' + topic + '"' : '';
        var message = action + ' the room topic' + to;
        if (who === ui.getUserName()) {
            message = 'You have ' + message;
        } else {
            message = who + ' has ' + message;
        }
        ui.addMessage(message, 'notification', roomName);
    };

    chat.client.welcomeChanged = function (isCleared, welcome) {
        var action = isCleared ? 'cleared' : 'set';
        var to = welcome ? ' to:' : '';
        var message = 'You have ' + action + ' the room welcome' + to;
        ui.addMessage(message, 'notification', this.state.activeRoom);
        if (welcome) {
            ui.addMessage(welcome, 'welcome', this.state.activeRoom);
        }
    };

    // Called when you have added or cleared a flag
    chat.client.flagChanged = function (isCleared, country) {
        var action = isCleared ? 'cleared' : 'set';
        var place = country ? ' to ' + country : '';
        var message = 'You have ' + action + ' your flag' + place;
        ui.addMessage(message, 'notification', this.state.activeRoom);
    };

    // Make sure all the people in the all the rooms know that a user has changed their flag
    chat.client.changeFlag = function (user, room) {
        var viewModel = getUserViewModel(user);

        ui.changeFlag(viewModel, room);

        if (!isSelf(user)) {
            var action = user.Flag ? 'set' : 'cleared';
            var country = viewModel.country ? ' to ' + viewModel.country : '';
            var message = user.Name + ' has ' + action + ' their flag' + country;
            ui.addMessage(message, 'notification', room);
        }
    };

    chat.client.userNameChanged = function (user) {
        // Update the client state
        chat.state.name = user.Name;
        ui.setUserName(chat.state.name);
        ui.addMessage('Your name is now ' + user.Name, 'notification', this.state.activeRoom);
    };

    chat.client.setTyping = function (user, room) {
        var viewModel = getUserViewModel(user);
        ui.setUserTyping(viewModel, room);
    };

    chat.client.sendMeMessage = function (name, message, room) {
        ui.addMessage('*' + name + ' ' + message, 'action', room);
    };

    chat.client.sendPrivateMessage = function (from, to, message) {
        if (isSelf({ Name: to })) {
            // Force notification for direct messages
            ui.notify(true);
            ui.setLastPrivate(from);
        }

        ui.addPrivateMessage('*' + from + '* *' + to + '* ' + message, 'pm');
    };

    chat.client.sendInvite = function (from, to, room) {
        if (isSelf({ Name: to })) {
            ui.notify(true);
            ui.addPrivateMessage('*' + from + '* has invited you to #' + room + '. Click the room name to join.', 'pm');
        }
        else {
            ui.addPrivateMessage('Invitation to *' + to + '* to join #' + room + ' has been sent.', 'pm');
        }
    };

    chat.client.nudge = function (from, to) {
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
        $("body").effect("pulsate", { times: 3 }, 300);
        window.setTimeout(function () {
            shake(20);
        }, 300);

        ui.addMessage('*' + from + ' nudged ' + (to ? 'you' : 'the room'), to ? 'pm' : 'notification');
    };

    chat.client.leave = function (user, room) {
        if (isSelf(user)) {
            ui.setActiveRoom('Lobby');
            ui.removeRoom(room);
        }
        else {
            ui.removeUser(user, room);
            ui.addMessage(user.Name + ' left ' + room, 'notification', room);
        }
    };

    chat.client.kick = function (room) {
        ui.setActiveRoom('Lobby');
        ui.removeRoom(room);
        ui.addMessage('You were kicked from ' + room, 'notification');
    };

    // Helpish commands
    chat.client.showRooms = function (rooms) {
        ui.addMessage('Rooms', 'list-header');
        if (!rooms.length) {
            ui.addMessage('No rooms available', 'list-item');
        }
        else {
            // sort rooms by count descending
            var sorted = rooms.sort(function (a, b) {
                return a.Count > b.Count ? -1 : 1;
            });

            $.each(sorted, function () {
                ui.addMessage(this.Name + ' (' + this.Count + ')', 'list-item');
            });
        }
    };

    chat.client.showCommands = function () {
        ui.showHelp();
    };

    chat.client.showUsersInRoom = function (room, names) {
        ui.addMessage('Users in ' + room, 'list-header');
        if (names.length === 0) {
            ui.addMessage('Room is empty', 'list-item');
        }
        else {
            $.each(names, function () {
                ui.addMessage('- ' + this, 'list-item');
            });
        }
    };

    chat.client.listUsers = function (users) {
        if (users.length === 0) {
            ui.addMessage('No users matched your search', 'list-header');
        }
        else {
            ui.addMessage('The following users match your search', 'list-header');
            ui.addMessage(users.join(', '), 'list-item');
        }
    };

    chat.client.showUsersRoomList = function (user, rooms) {
        var status = "Currently " + user.Status;
        if (rooms.length === 0) {
            ui.addMessage(user.Name + ' (' + status + ') is not in any rooms', 'list-header');
        }
        else {
            ui.addMessage(user.Name + ' (' + status + ') is in the following rooms', 'list-header');
            ui.addMessage(rooms.join(', '), 'list-item');
        }
    };

    chat.client.showUsersOwnedRoomList = function (user, rooms) {
        if (rooms.length === 0) {
            ui.addMessage(user + ' does not own any rooms', 'list-header');
        }
        else {
            ui.addMessage(user + ' owns the following rooms', 'list-header');
            ui.addMessage(rooms.join(', '), 'list-item');
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
        ui.addMessage(user + ' is now an admin', 'notification', this.state.activeRoom);
    };

    chat.client.adminRemoved = function (user) {
        ui.addMessage(user + ' is no longer an admin', 'notification', this.state.activeRoom);
    };

    // Called when you've been made an admin
    chat.client.makeAdmin = function () {
        ui.addMessage('You are now an admin', 'notification', this.state.activeRoom);
    };

    // Called when you've been removed as an admin
    chat.client.demoteAdmin = function () {
        ui.addMessage('You are no longer an admin', 'notification', this.state.activeRoom);
    };

    chat.client.broadcastMessage = function (message, room) {
        ui.addMessage('ADMIN: ' + message, 'broadcast', room);
    };

    chat.client.outOfSync = function () {
        ui.showUpdateUI();
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

        if (msg[0] !== '/') {

            // if you're in the lobby, you can't send mesages (only commands)
            if (chat.state.activeRoom === undefined) {
                ui.addMessage('You cannot send messages within the Lobby', 'error');
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
                    ui.addMessage(e, 'error');
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
                    ui.addMessage(e, 'error');
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
                    ui.addMessage(e, 'error');
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
            populateLobbyRooms();

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
            ui.setLoadingHistory(loadingHistory);
            ui.setRoomTrimmable(roomInfo.name, false);
            connection.hub.log('getPreviousMessages(' + roomInfo.name + ')');
            chat.server.getPreviousMessages(roomInfo.messageId)
                .done(function (messages) {
                    connection.hub.log('getPreviousMessages.done(' + roomInfo.name + ')');
                    ui.prependChatMessages($.map(messages, getMessageViewModel), roomInfo.name);
                    loadingHistory = false;
                    ui.setLoadingHistory(loadingHistory);
                })
                .fail(function (e) {
                    connection.hub.log('getPreviousMessages.failed(' + roomInfo.name + ', ' + e + ')');
                    loadingHistory = false;
                    ui.setLoadingHistory(loadingHistory);
                });
        }
        catch (e) {
            connection.hub.log('getPreviousMessages failed');
            ui.setLoadingHistory(false);
        }
    });

    $(ui).bind(ui.events.reloadMessages, function () {

    });

    $(ui).bind(ui.events.preferencesChanged, function (ev) {
        updateCookie();
    });

    $(ui).bind(ui.events.loggedOut, function () {
        logout();
    });

    $(function () {
        var stateCookie = $.cookie('jabbr.state'),
            state = stateCookie ? JSON.parse(stateCookie) : {},
            initial = true;

        // Initialize the ui, passing the user preferences
        ui.initialize(state.preferences);

        ui.addMessage('Welcome to ' + originalTitle, 'notification');
        ui.addMessage('Use ? or type /? to display the FAQ and list of commands', 'notification');

        function initConnection() {
            var logging = $.cookie('jabbr.logging') === '1',
                transport = $.cookie('jabbr.transport'),
                options = {};

            if (transport) {
                options.transport = transport;
            }

            connection.hub.logging = logging;
            connection.hub.qs = "version=" + window.jabbrVersion;
            connection.hub.start(options)
                          .done(function () {
                              chat.server.join()
                              .fail(function (e) {
                                  // So refresh the page, our auth token is probably gone
                                  performLogout();
                              })
                              .done(function () {
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
                              });
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
            });

            connection.hub.disconnected(function () {
                connection.hub.log('Dropped the connection from the server. Restarting in 5 seconds.');

                failPendingMessages();

                ui.showStatus(2, '');
                ui.setReadOnly(true);

                // Restart the connection
                setTimeout(function () {
                    connection.hub.start(options)
                                  .done(function () {
                                      // When this works uncomment it.
                                      // ui.showReloadMessageNotification();

                                      // Turn the firehose back on
                                      chat.server.join(true);
                                  });
                }, 5000);
            });

            connection.hub.error(function (err) {
                // Make all pending messages failed if there's an error
                failPendingMessages();
            });
        }

        initConnection();
    });

})(jQuery, $.connection, window, window.chat.ui, window.chat.utility);
