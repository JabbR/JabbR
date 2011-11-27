/// <reference path="Scripts/jquery-1.7.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />
/// <reference path="Chat.ui.js" />

(function ($, connection, window, undefined, ui) {
    var chat = connection.chat,
        messageHistory = [],
        historyLocation = 0,
        originalTitle = document.title,
        unread = 0,
        focus = true;

    function isSelf(user) {
        return chat.name === user.Name;
    }

    function populateRoom(room) {
        var d = $.Deferred();
        // Populate the list of users rooms and messages 
        chat.getRoomInfo(room)
                .done(function (roomInfo) {
                    $.each(roomInfo.Users, function () {
                        var owner = roomInfo.Owner ? roomInfo.Owner.Name === this.Name : false,
                            viewModel = {
                                name: this.Name,
                                hash: this.Hash,
                                owner: owner
                            };

                        ui.addUser(viewModel, room);
                    });

                    $.each(roomInfo.RecentMessages, function () {
                        var viewModel = getMessageViewModel(this);

                        ui.addChatMessage(viewModel, room);
                    });

                    ui.scrollToBottom(room);

                    d.resolveWith(chat);
                })
                .fail(function () {
                    d.rejectWith(chat);
                });

        return d;
    }

    function scrollIfNecessary(callback, room) {
        var nearEnd = ui.isNearTheEnd(room);

        callback();

        if (nearEnd) {
            ui.scrollToBottom(room);
        }
    }

    function getMessageViewModel(message) {
        var re = new RegExp("\\b@?" + chat.name.replace(/\./, '\\.') + "\\b", "i");
        return {
            name: message.User.Name,
            hash: message.User.Hash,
            message: message.Content,
            id: message.Id,
            date: message.When.fromJsonDate(),
            highlight: re.test(message.Content) ? 'highlight' : ''
        };
    }

    // Save some state in a cookie
    function updateCookie() {
        var legacyCookies = ['userid', 'username', 'userroom', 'userhash', 'currentroom'],
            state = {
                userId: chat.id,
                activeRoom: chat.activeRoom
            },
            jsonState = window.JSON.stringify(state);

        // Clear the legacy cookies
        $.each(legacyCookies, function () {
            $.cookie(this, null);
        });

        $.cookie('jabbr.state', jsonState, { path: '/', expires: 30 });
    }

    function updateTitle() {
        if (unread === 0) {
            document.title = originalTitle;
        }
        else {
            document.title = '(' + unread + ') ' + originalTitle;
        }
    }

    function updateUnread(room) {
        if (focus === false) {
            unread = unread + 1;
        }

        ui.updateUnread(unread, room);

        updateTitle();
    }

    // When the /join command gets raised this is called
    chat.joinRoom = function (room) {
        ui.addRoom(room);
        populateRoom(room).done(function () {
            ui.setActiveRoom(room);
            ui.addMessage('You just entered ' + room, 'notification', room);
        });
    };

    // When the user joins the chat app and has to be re-added to these rooms
    chat.rejoinRooms = function (rooms) {
        $.each(rooms, function (index, room) {
            ui.addRoom(room);
            populateRoom(room);
        });
    };

    chat.markInactive = function (users) {
    };

    chat.updateActivity = function (user) {
    };

    chat.showRooms = function (rooms) {
    };

    chat.addMessageContent = function (id, content, room) {
        var nearTheEnd = ui.isNearTheEnd(room);

        scrollIfNecessary(function () {
            ui.addChatMessageContent(id, content, room);
        }, room);

        updateUnread(room);

        // Adding external content can sometimes take a while to load
        // Since we don't know when it'll become full size in the DOM
        // we're just going to wait a little bit and hope for the best :) (still a HACK tho)
        window.setTimeout(function () {
            ui.resize();
            if (nearTheEnd) {
                ui.scrollToBottom();
            }
        }, 1000);
    };

    chat.addMessage = function (message, room) {
        scrollIfNecessary(function () {
            var viewModel = getMessageViewModel(message);
            ui.addChatMessage(viewModel, room);

        }, room);

        updateUnread(room);
    };

    chat.addUser = function (user, room, owner) {
        var viewModel = {
            name: user.Name,
            hash: user.Hash,
            owner: owner ? owner.Name === user.Name : false
        };

        var added = ui.addUser(viewModel, room);

        if (added) {
            if (!isSelf(user)) {
                ui.addMessage(user.Name + ' just entered ' + room, 'notification', room);
            }
        }
    };

    chat.changeUserName = function (oldName, user, room) {
        ui.changeUserName(oldName, user, room);

        if (!isSelf(user)) {
            ui.addMessage(oldName + '\'s nick has changed to ' + user.Name, 'notification', room);
        }
    };

    chat.changeGravatar = function (user, room) {
        ui.changeGravatar(user, room);

        if (!isSelf(user)) {
            ui.addMessage(user.Name + "'s gravatar changed.", 'notification', room);
        }
    };

    chat.gravatarChanged = function (user) {
        ui.addMessage('Your gravatar has been set', 'notification', this.activeRoom);
    };

    chat.userCreated = function (user) {
        ui.addMessage('Your nick is ' + user.Name, 'notification');

        // Update the cookie
        updateCookie();
    };

    chat.userNameChanged = function (user) {
        ui.addMessage('Your name is now ' + user.Name, 'notification', this.activeRoom);
    };

    chat.setTyping = function (currentUser, isTyping, room) {

    };

    chat.showCommands = function (commands) {

    };

    chat.showUsersInRoom = function (room, names) {

    };

    chat.listUsers = function (users) {

    };

    chat.showUsersRoomList = function (user, rooms) {

    };

    chat.sendMeMessage = function (name, message) {
        ui.addMessage('*' + name + ' ' + message, 'notification');
    };

    chat.sendPrivateMessage = function (from, to, message) {
        ui.addMessage('<emp>*' + from + '* &raquo; *' + to + '*</emp> ' + message, 'pm');
    };

    chat.nudge = function (from, to) {
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
            return this;
        };
        $("body").effect("pulsate", { times: 3 }, 300);
        window.setTimeout(function () {
            shake(20);
        }, 300);

        ui.addMessage('*' + from + ' nudged ' + (to ? 'you' : 'the room'), to ? 'pm' : 'notification');
    };

    chat.leave = function (user, room) {
        if (isSelf(user)) {
            ui.setActiveRoom('Lobby');
            ui.removeRoom(room);

            ui.addMessage('You have left ' + room, 'notification');
        }
        else {
            ui.removeUser(user, room);
            ui.addMessage(user.Name + ' left ' + room, 'notification', room);
        }
    };

    chat.kick = function (room) {
        ui.addMessage('You were kicked from ' + room, 'notification');
    };

    $(ui).bind('ui.sendMessage', function (ev, msg) {
        chat.send(msg)
            .fail(function (e) {
                ui.addMessage(e, 'error');
            });

        messageHistory.push(msg);

        // REVIEW: should this pop items off the top after a certain length?
        historyLocation = messageHistory.length;
    });

    $(ui).bind('ui.focus', function () {
        focus = true;
        unread = 0;
        updateTitle();
    });

    $(ui).bind('ui.blur', function () {
        focus = false;

        updateTitle();
    });

    $(ui).bind('ui.joinRoom', function (ev, room) {
        chat.send('/join ' + room)
            .fail(function (e) {
                ui.addMessage(e, 'error');
            });
    });

    $(ui).bind('ui.prevMessage', function () {
        historyLocation -= 1;
        if (historyLocation < 0) {
            historyLocation = messageHistory.length - 1;
        }
        ui.setMessage(messageHistory[historyLocation]);
    });

    $(ui).bind('ui.nextMessage', function () {
        historyLocation = (historyLocation + 1) % messageHistory.length;
        ui.setMessage(messageHistory[historyLocation]);
    });

    $(ui).bind('ui.activeRoomChanged', function (ev, room) {
        if (room === 'Lobby') {
            // Populate the user list with room names
            chat.getRooms()
                .done(function (rooms) {
                    ui.populateLobbyRooms(rooms);
                });

            // Remove the active room
            chat.activeRoom = undefined;
        }
        else {
            // When the active room changes update the client state and the cookie
            chat.activeRoom = room;
        }

        updateCookie();
    });

    $(function () {
        // Initialize the ui
        ui.initialize();

        ui.addMessage('Welcome to the ' + originalTitle, 'notification');
        ui.addMessage('Type /help to see the list of commands', 'notification');

        connection.hub.start(function () {
            chat.join()
                .fail(function (e) {
                    ui.addMessage(e, 'error');
                })
                .done(function (success) {
                    if (success === false) {
                        ui.addMessage('Choose a name using "/nick nickname".', 'notification');
                    }
                    else {
                        ui.addMessage('Welcome back ' + chat.name, 'notification', 'lobby');
                        ui.addMessage('You can join any of the rooms on the right', 'notification', 'lobby');
                    }

                    // If there's an active room, navigate to it
                    if (this.activeRoom) {
                        ui.addRoom(this.activeRoom);
                        ui.setActiveRoom(this.activeRoom);
                        ui.scrollToBottom(this.activeRoom);
                    }
                    else {
                        ui.setActiveRoom('Lobby');
                    }
                });
        });
    });

})(jQuery, $.connection, window, undefined, window.chat.ui);