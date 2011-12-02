/// <reference path="Scripts/jquery-1.7.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />

(function ($, window, utility) {
    "use strict";

    var $chatArea = null,
        $tabs = null,
        $submitButton = null,
        $newMessage = null,
        templates = null,
        app = null,
        focus = true,
        Keys = { Up: 38, Down: 40, Esc: 27 };

    function getRoomId(roomName) {
        return escape(roomName.toLowerCase()).replace(/[^a-z0-9]/, '_');
    }

    function getUserClassName(userName) {
        return '[data-name="' + userName + '"]';
    }

    function Room($tab, $users, $messages) {
        this.tab = $tab;
        this.users = $users;
        this.messages = $messages;

        this.isLobby = function () {
            return this.tab.hasClass('lobby');
        };

        this.hasUnread = function () {
            return this.tab.hasClass('unread');
        };

        this.hasSeparator = function () {
            return this.messages.find('.message-separator').length > 0;
        };

        this.needsSeparator = function (focus) {
            if (this.isActive() && ui.focus === true) {
                return false;
            }
            return this.hasSeparator() === false;
        };

        this.updateUnread = function (isMentioned) {
            var $tab = this.tab.addClass('unread'),
                $content = $tab.find('.content'),
                unread = ($tab.data('unread') || 0) + 1,
                hasMentions = $tab.data('hasMentions') || isMentioned; // Whether or not the user already has unread messages to him/her

            $content.text((hasMentions ? '*' : '') + '(' + unread + ') ' + this.getName());

            $tab.data('unread', unread);
            $tab.data('hasMentions', hasMentions);
        };

        this.scrollToSeparator = function () {
            var $e = this.messages.find('.message-separator');

            var top = $e.position().top,
                scrollHeight = this.messages[0].scrollHeight,
                scrollTop = this.messages.scrollTop(),
                height = this.messages.height()

            // keep separator scrolled half way in message list
            if (top < 0) {
                this.messages.scrollTop(scrollTop + top - (height / 2));
            }
            else if (top > height / 2) {
                this.messages.scrollTop(scrollHeight - (height / 2));
            }
        };

        this.scrollToBottom = function () {
            this.messages.scrollTop(this.messages[0].scrollHeight);
        };

        this.isNearTheEnd = function () {
            return this.messages.isNearTheEnd();
        };

        this.getName = function () {
            return this.tab.data('name');
        };

        this.isActive = function () {
            return this.tab.hasClass('current');
        };

        this.exists = function () {
            return this.tab.length > 0;
        };

        this.clear = function () {
            this.messages.empty();
            this.users.empty();
        };

        this.makeInactive = function () {
            this.tab.removeClass('current');

            this.messages.removeClass('current')
                         .hide();

            this.users.removeClass('current')
                      .hide();
        };

        this.makeActive = function () {
            this.tab.addClass('current')
                    .removeClass('unread')
                    .data('unread', 0)
                    .data('hasMentions', false)
                    .find('.content')
                    .text(this.getName());

            this.messages.addClass('current')
                         .show();

            this.users.addClass('current')
                      .show();

            // force scroll handling
            this.messages.scroll();

        };

        // Users
        this.getUser = function (userName) {
            return this.users.find(getUserClassName(userName));
        };

        this.getUserReferences = function (userName) {
            return $.merge(this.getUser(userName),
                           this.messages.find(getUserClassName(userName)));
        };
    }

    function getRoomElements(roomName) {
        var roomId = getRoomId(roomName);
        return new Room($('#tabs-' + roomId),
                        $('#users-' + roomId),
                        $('#messages-' + roomId));
    }

    function getCurrentRoomElements() {
        return new Room($tabs.find('li.current'),
                        $('.users.current'),
                        $('.messages.current'));
    }

    function getLobby() {
        return getRoomElements('Lobby');
    }

    function updateLobbyRoomCount(roomName, count) {
        var lobby = getLobby(),
            $room = lobby.users.find('[data-room="' + roomName + '"]'),
            $count = $room.find('.count');


        $room.css('background-color', '#f5f5f5');
        $count.text(' (' + count + ')');
        // Do a little animation
        $room.animate({ backgroundColor: '#e5e5e5' }, 800);
    }


    function addRoom(roomName) {
        // Do nothing if the room exists
        var room = getRoomElements(roomName);
        if (room.exists()) {
            return false;
        }

        var roomId = getRoomId(roomName);

        // Add the tab
        var viewModel = {
            id: roomId,
            name: roomName
        };

        templates.tab.tmpl(viewModel).appendTo($tabs);

        $('<ul/>').attr('id', 'messages-' + roomId)
                  .addClass('messages')
                  .appendTo($chatArea)
                  .hide()
                  // add scroll hander to each messages list because
                  // global handler doesn't always trigger
                  .scroll(handleScroll);


        $('<ul/>').attr('id', 'users-' + roomId)
                  .addClass('users')
                  .appendTo($chatArea).hide();

        $tabs.find('li')
            .not('.lobby')
            .sortElements(function (a, b) {
                return $(a).data('name').toLowerCase() > $(b).data('name').toLowerCase() ? 1 : -1;
            });

        setAccessKeys();
        return true;
    }

    function removeRoom(roomName) {
        var room = getRoomElements(roomName);

        if (room.exists()) {
            room.tab.remove();
            room.messages.remove();
            room.users.remove();
            setAccessKeys();
        }
    }

    function setAccessKeys() {
        $.each($tabs.find('li.room'), function (index, item) {
            $(item).children('button:first-child').attr('accesskey', getRoomAccessKey(index));
        });
    }

    function getRoomAccessKey(index) {
        if (index < 10) {
            return index + 1;
        }
        return 0;
    }

    function navigateToRoom(roomName) {
        app.runRoute('get', '#/rooms/' + roomName, {
            room: roomName
        });
    }

    function handleScroll() {
        var roomName = $(this).attr('id').substring(9);
        var room = getRoomElements(roomName);

        // remove separator once use has scrolled to bottom of messages list
        if ($(this).isNearTheEnd() && room.hasSeparator() && room.isActive() && ui.hasFocus()) {
            $(this).find('.message-separator').fadeOut(1500, function () {
                $(this).remove();
            });
        }
    }

    var ui = {
        initialize: function () {
            $chatArea = $('#chat-area');
            $tabs = $('#tabs');
            $submitButton = $('#send-message');
            $newMessage = $('#new-message');
            focus = true;
            templates = {
                user: $('#new-user-template'),
                message: $('#new-message-template'),
                notification: $('#new-notification-template'),
                separator: $('#message-separator-template'),
                tab: $('#new-tab-template')
            },
            app = Sammy(function () {
                // Process this route
                this.get('#/rooms/:room', function () {
                    var roomName = this.params.room;

                    if (ui.setActiveRoom(roomName) === false) {
                        $(ui).trigger('ui.openRoom', [roomName]);
                    }
                });
            });

            // DOM events
            $(document).on('click', 'h3.collapsible_title', function () {
                var $message = $(this).closest('.message'),
                    nearEnd = ui.isNearTheEnd();

                $(this).next().toggle(0, function () {
                    if (nearEnd) {
                        ui.scrollToBottom();
                    }
                });
            });

            $(document).on('click', '#tabs li', function () {
                ui.setActiveRoom($(this).data('name'))
            });

            $(document).on('click', 'li.room', function () {
                var roomName = $(this).data('name');

                navigateToRoom(roomName);

                return false;
            });

            $(document).on('click', '#tabs li .close', function (ev) {
                var roomName = $(this).closest('li').data('name');

                $(ui).trigger('ui.closeRoom', [roomName]);

                ev.preventDefault();
                return false;
            });

            $submitButton.submit(function (ev) {
                var msg = $.trim($newMessage.val());

                if (msg) {
                    $(ui).trigger('ui.sendMessage', [msg]);
                }

                $newMessage.val('');
                $newMessage.focus();

                // always scroll to bottom after new message sent
                var room = getCurrentRoomElements();
                room.scrollToBottom();

                ev.preventDefault();
                return false;
            });

            $(window).blur(function () {
                ui.focus = false;
                $(ui).trigger('ui.blur');
            });

            $(window).focus(function () {
                ui.focus = true;
                // clear unread count in active room
                var room = getCurrentRoomElements();
                room.makeActive();
                $(ui).trigger('ui.focus');
            });

            $newMessage.keydown(function (e) {
                var key = e.keyCode || e.which;
                switch (key) {
                    case Keys.Up:
                        $(ui).trigger('ui.prevMessage');
                        break;

                    case Keys.Down:
                        $(ui).trigger('ui.nextMessage');
                        break;

                    case Keys.Esc:
                        $(this).val('');
                        break;
                }
            });

            // Auto-complete for user names
            $newMessage.autoTabComplete({
                get: function () {
                    var room = getCurrentRoomElements();
                    return room.users.find('li')
                                     .not('.room')
                                     .map(function () { return $(this).data('name'); });
                }
            });

            $newMessage.keypress(function (e) {
                $(ui).trigger('ui.typing');
            });

            $newMessage.focus();
        },
        run: function () {
            app.run();
        },
        setMessage: function (value) {
            $newMessage.val(value);
        },
        addRoom: addRoom,
        removeRoom: removeRoom,
        setRoomOwner: function (ownerName, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(ownerName);

            $user.find('.owner')
                 .text('(owner)');
        },
        setActiveRoom: function (roomName) {
            var room = getRoomElements(roomName);

            if (room.isActive()) {
                // Still trigger the event (just do less overall work)
                $(ui).trigger('ui.activeRoomChanged', [roomName]);
                // force scoll logic
                room.messages.scroll();
                return true;
            }

            var currentRoom = getCurrentRoomElements();

            if (room.exists() && currentRoom.exists()) {
                var hasUnread = room.hasUnread();
                currentRoom.makeInactive();
                room.makeActive();

                app.setLocation('#/rooms/' + roomName);
                $(ui).trigger('ui.activeRoomChanged', [roomName]);
                return true;
            }

            return false;
        },
        updateLobbyRoomCount: updateLobbyRoomCount,
        updateUnread: function (roomName, isMentioned) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();

            if (ui.focus && room.isActive()) {
                return;
            }

            room.updateUnread(isMentioned);
        },
        scrollToBottom: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();

            if (room.hasSeparator()) {
                // scoll to separator
                room.scrollToSeparator();
                return;
            }

            if (ui.focus && room.isActive()) {
                room.scrollToBottom();
            }
        },
        isNearTheEnd: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();

            return room.isNearTheEnd();
        },
        populateLobbyRooms: function (rooms) {
            var lobby = getLobby();

            lobby.users.empty();

            $.each(rooms, function () {
                var $name = $('<span/>').addClass('name')
                                        .html(this.Name),
                    $count = $('<span/>').addClass('count')
                                         .html(' (' + this.Count + ')')
                                         .data('count', this.Count);

                $('<li/>').addClass('room')
                          .attr('data-room', this.Name)
                          .data('name', this.Name)
                          .append($name)
                          .append($count)
                          .appendTo(lobby.users);
            });

            lobby.users.find('li')
                       .sortElements(function (a, b) {
                           return $(a).data('name').toLowerCase() > $(b).data('name').toLowerCase() ? 1 : -1;
                       });
        },
        addUser: function (user, roomName) {
            var room = getRoomElements(roomName),
                $user = null;

            // Remove all users that are being removed
            room.users.find('.removing').remove();

            // Get the user element
            $user = room.getUser(user.name);

            if ($user.length) {
                return false;
            }

            templates.user.tmpl(user).appendTo(room.users);

            return true;
        },
        setUserActivity: function (user) {
            var $user = $('.users').find(getUserClassName(user.Name));

            if (user.Active === true) {
                $user.fadeTo('slow', 1, function () {
                    $user.removeClass('idle');
                });
            }
            else {
                $user.fadeTo('slow', 0.5, function () {
                    $user.addClass('idle');
                });
            }
        },
        changeUserName: function (oldName, user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUserReferences(oldName);

            // Update the user's name
            $user.find('.name').html(user.Name);
            $user.attr('data-name', user.Name);
        },
        changeGravatar: function (user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUserReferences(user.Name),
                src = 'http://www.gravatar.com/avatar/' + user.Hash + '?s=16&d=mm';

            $user.find('.gravatar')
                 .attr('src', src);
        },
        removeUser: function (user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(user.Name);

            $user.addClass('removing')
                .fadeOut('slow', function () {
                    $(this).remove();
                });
        },
        setUserTyping: function (user, roomName, isTyping) {
            var room = getRoomElements(roomName),
                $user = room.getUser(user.Name);

            if (isTyping) {
                $user.addClass('typing');
            }
            else {
                $user.removeClass('typing');
            }
        },
        addChatMessage: function (message, roomName) {
            var room = getRoomElements(roomName),
                $previousMessage = room.messages.children().last(),
                previousUser = null,
                showUserName = true,
                $message = null;


            if ($previousMessage) {
                previousUser = $previousMessage.data('name');
            }

            // Determine if we need to show the user name next to the message
            showUserName = previousUser !== message.name;

            // Set the trimmed name and date
            message.trimmedName = utility.trim(message.name, 21);
            message.when = message.date.formatTime(true);
            message.showUser = showUserName;
            message.fulldate = message.date.formatDate() + ' ' + message.date.formatTime(true);

            if (showUserName === false) {
                $previousMessage.addClass('continue');
            }

            if (room.needsSeparator(ui.focus)) {
                ui.addSeparator(roomName);
            }

            var $e = templates.message.tmpl(message).appendTo(room.messages);

        },
        addChatMessageContent: function (id, content, roomName) {
            var $message = $('#m-' + id);

            $message.find('.middle')
                    .append(content);
        },
        addMessage: function (content, type, roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements(),
                nearEnd = room.isNearTheEnd(),
                $element = null,
                now = new Date(),
                message = {
                    message: content,
                    type: type,
                    when: now.formatTime(true),
                    fulldate: now.formatDate() + ' ' + now.formatTime(true)
                };

            if (room.needsSeparator(ui.focus)) {
                ui.addSeparator(roomName);
            }

            $element = templates.notification.tmpl(message).appendTo(room.messages);

            if (nearEnd) {
                ui.scrollToBottom(roomName);
            }

            return $element;
        },
        addSeparator: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();
            if (room.isLobby()) {
                return;
            }
            templates.separator.tmpl().appendTo(room.messages);
            ui.scrollToBottom(roomName);
        },
        removeSeparator: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();
            room.messages.find('.message-separator').remove();
        },
        hasFocus: function () {
            return ui.focus;
        }
    };

    if (!window.chat) {
        window.chat = {};
    }
    window.chat.ui = ui;

})(jQuery, window, window.chat.utility);
