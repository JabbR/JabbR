/// <reference path="Scripts/jquery-1.7.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />

(function ($, window, undefined, utility) {
    var $chatArea = null,
        $tabs = null,
        $submitButton = null,
        $newMessage = null,
        templates = null,
        Keys = { Up: 38, Down: 40, Esc: 27 };

    function getRoomId(roomName) {
        return escape(roomName.toLowerCase()).replace(/[^a-z0-9]/, '_');
    }

    function room($tabs, $users, $messages) {
        this.tabs = $tabs;
        this.users = $users;
        this.messages = $messages;

        this.scrollToBottom = function () {
            this.messages.scrollTop(this.messages[0].scrollHeight);
        }

        this.getName = function () {
            return this.tabs.data('name');
        }

        this.isActive = function () {
            return this.tabs.hasClass('current');
        }

        this.exists = function () {
            return this.tabs.length > 0;
        }

        this.clear = function () {
            this.messages.empty();
            this.users.empty();
        }

        this.makeInactive = function () {
            this.tabs.removeClass('current');
            this.messages.removeClass('current').hide();
            this.users.removeClass('current').hide();
        }

        this.makeActive = function () {
            this.tabs.addClass('current').removeClass('unread');
            this.messages.addClass('current').show();
            this.users.addClass('current').show();
        }

        // Users
        this.getUser = function (userName) {
            return this.users.find('[data-name="' + userName + '"]');
        }
    }

    function addRoom(roomName) {
        // Do nothing if the room exists
        var room = getRoomElements(roomName);
        if (room.exists()) {
            return;
        }

        var roomId = getRoomId(roomName);

        $('<li/>').attr('id', 'tabs-' + roomId)
                  .attr('data-name', roomName)
                  .html(roomName)
                  .appendTo($tabs)

        $('<ul/>').attr('id', 'messages-' + roomId)
                  .addClass('messages')
                  .appendTo($chatArea)
                  .hide();

        $('<ul/>').attr('id', 'users-' + roomId)
                  .addClass('users')
                  .appendTo($chatArea).hide();
    }

    function removeRoom(roomName) {
        var room = getRoomElements(roomName);

        if (room.exists()) {
            room.tabs.remove();
            room.messages.remove();
            room.users.remove();
        }
    }

    function getRoomElements(roomName) {
        var roomId = getRoomId(roomName);
        return new room($('#tabs-' + roomId),
                        $('#users-' + roomId),
                        $('#messages-' + roomId));
    }

    function getCurrentRoomElements() {
        return new room($tabs.find('li.current'),
                        $('.users.current'),
                        $('.messages.current'));
    }

    function resizeRoom(roomName) {
        var room = getRoomElements(roomName);
        doResizeRoom(room);
    }

    function resizeActiveRoom() {
        var room = getCurrentRoomElements();
        doResizeRoom(room);
    }

    function doResizeRoom(room) {
        var $messages = room.messages.find('.message');

        $.each($messages, function () {
            resize($(this));
        });
    }

    function resize($message) {
        // Clear the previous heights and widths
        $message.css('height', '');
        $message.find('.middle').css('width', '');
        $message.find('.left').css('height', '');

        var $left = $message.find('.left'),
            $middle = $message.find('.middle'),
            $right = $message.find('.right'),
            width = $message.width(),
            leftWidth = $left.outerWidth(true),
            rightWidth = $right.outerWidth(true),
            middleExtra = $middle.outerWidth(true) - $middle.width(),
            middleWidth = width - (leftWidth + rightWidth + middleExtra) - 20;

        $middle.css('width', middleWidth + 'px');

        var height = $message.height(),
            leftExtra = $left.outerHeight() - $left.height(),
            leftHeightCalculated = height - leftExtra;

        $message.css('height', height + 'px');
        $left.css('height', leftHeightCalculated + 'px');
    }

    var ui = {
        initialize: function () {
            $chatArea = $('#chat-area');
            $tabs = $('#tabs');
            $submitButton = $('#send-message');
            $newMessage = $('#new-message');
            templates = {
                user: $('#new-user-template'),
                message: $('#new-message-template')
            };

            // DOM events
            $(document).on('click', '#tabs li', function () {
                ui.setActiveRoom($(this).data('name'))
            });

            $(document).on('click', 'li.room', function () {
                var roomName = $(this).data('name');

                if (ui.setActiveRoom(roomName) === false) {
                    $(ui).trigger('ui.joinRoom', [roomName]);
                }

                return false;
            });

            $submitButton.submit(function (ev) {
                var msg = $.trim($newMessage.val());

                if (msg) {
                    $(ui).trigger('ui.sendMessage', [msg]);
                }

                $newMessage.val('');
                $newMessage.focus();

                ev.preventDefault();
                return false;
            });

            $(window).resize(resizeActiveRoom);

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

            $newMessage.focus();
        },
        setMessage: function (value) {
            $newMessage.val(value);
        },
        addRoom: addRoom,
        removeRoom: removeRoom,
        setActiveRoom: function (roomName) {
            var room = getRoomElements(roomName);

            if (room.isActive()) {
                // Still trigger the event (just do less overall work)
                $(ui).trigger('ui.activeRoomChanged', [roomName]);
                return true;
            }

            var currentRoom = getCurrentRoomElements();

            if (room.exists() && currentRoom.exists()) {
                currentRoom.makeInactive();
                room.makeActive();

                resizeRoom(roomName);

                $(ui).trigger('ui.activeRoomChanged', [roomName]);
                return true;
            }

            return false;
        },
        scrollToBottom: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();

            room.scrollToBottom();
        },
        populateLobbyRooms: function (rooms) {
            var lobby = getRoomElements('Lobby');

            lobby.users.empty();

            $.each(rooms, function () {
                var $count = $('<span/>').addClass('count')
                                         .html(' (' + this.Count + ')')
                                         .data('count', this.Count);

                $('<li/>').addClass('room')
                          .data('name', this.Name)
                          .html(this.Name)
                          .append($count)
                          .appendTo(lobby.users);
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
        changeUserName: function (oldName, user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(oldName);

            // Update the user's name
            $user.find('.name').html(user.Name);
            $user.attr('data-name', user.Name);
        },
        changeGravatar: function (user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(user.Name);

            $user.find('.gravatar')
                 .attr('src', 'http://www.gravatar.com/avatar/' + user.Hash + '?s=16&d=mm');
        },
        removeUser: function (user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(user.Name);

            $user.addClass('removing')
                .fadeOut('slow', function () {
                    $(this).remove();
                });
        },
        addChatMessage: function (message, roomName) {
            var room = getRoomElements(roomName),
                $previousMessage = room.messages.find('.message').last(),
                previousUser = null,
                showUserName = true,
                $message = null;


            if ($previousMessage) {
                previousUser = $previousMessage.data('user');
            }

            // Determine if we need to show the user name next to the message
            showUser = previousUser !== message.name;

            // Set the trimmed name and date
            message.trimmedName = utility.trim(message.name, 21);
            message.when = message.date.formatTime();

            if (showUser === false) {
                $previousMessage.addClass('continue');
            }

            templates.message.tmpl(message).appendTo(room.messages);

            // Resize this message
            $message = $('#m-' + message.id);
            resize($message);
        },
        addChatMessageContent: function (id, content, roomName) {
            var $message = $('#m-' + id);

            $message.find('.middle')
                    .append(content);

            // Resize this message
            resize($message);
        },
        addMessage: function (content, type, roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();

            $element = $('<li/>').html(content).appendTo(room.messages);

            if (type) {
                $element.addClass(type);
            }

            // Notifications are not that important (issue #79)
            if (type !== 'notification') {
                // updateUnread();
            }

            if (roomName) {
                resizeRoom(roomName);
            }

            return $element;
        }
    };

    if (!window.chat) {
        window.chat = {};
    }
    window.chat.ui = ui;

})(jQuery, window, undefined, window.chat.utility);