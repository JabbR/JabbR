/// <reference path="Scripts/jquery-1.7.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />

$(function () {
    var chat = $.connection.chat;
    var Keys = { Up: 38, Down: 40, Esc: 27 };

    $.fn.isNearTheEnd = function () {
        return this[0].scrollTop + this.height() >= this[0].scrollHeight;
    };

    $.fn.resizeMobileContent = function () {
        if ($.mobile) {
            this.find('embed')
                .attr('width', 250)
                .attr('height', 202);
        }
        return this;
    };

    function formatTime(dt) {
        var ap = "";
        var hr = dt.getHours();

        if (hr < 12) {
            ap = "AM";
        }
        else {
            ap = "PM";
        }

        if (hr == 0) {
            hr = 12;
        }

        if (hr > 12) {
            hr = hr - 12;
        }

        var mins = padZero(dt.getMinutes());
        var seconds = padZero(dt.getSeconds());
        return hr + ":" + mins + ":" + seconds + " " + ap;
    }

    function trimUserName(name) {
        if (name.length > 21) {
            return name.substr(0, 18) + '...';
        }
        return name;
    }

    function padZero(s) {
        s = s.toString();
        if (s.length == 1) {
            return "0" + s;
        }
        return s;
    }

    function toLocal(dts) {
        var s = dts.substr('/Date('.length);
        var ticks = parseInt(s.substr(0, s.length - 2));
        var dt = new Date(ticks);
        return formatTime(dt);
    }

    function isiPad() {
        return (navigator.platform.indexOf("iPad") != -1);
    }

    function clearMessages(roomId) {
        $('#messages-' + roomId).html('');
    }

    function refreshMessages(roomId) { refreshList($('#messages-' + roomId)); }

    function clearUsers(roomId) {
        $('#users-' + roomId).html('');
    }

    function refreshUsers(roomId) { refreshList($('#users-' + roomId)); }

    function refreshList(list) {
        if (list.is('.ui-listview')) {
            list.listview('refresh');
        }
    }

    function markUserInactive(user) {
        id = 'u-' + user.Id;
        if (user.Active === false) {
            $('.' + id).fadeTo('slow', 0.5);
        }
    }

    function addMessage(content, type, room) {
        var m = $('.messages.current'),
            roomId = null;
        if (room) {
            roomId = getRoomId(room);
            m = $('#messages-' + roomId);
        }
        var nearEnd = m.isNearTheEnd();

        // var converter = new Showdown.converter();
        // var html = converter.makeHtml(content);

        var e = $('<li/>').html(content).appendTo(m);

        refreshMessages();

        if (type) {
            e.addClass(type);
        }

        // notifications are not that important (issue #79)
        if (type !== 'notification') {
            updateUnread();
        }

        if (roomId) {
            updateRoomMessageDimensions(roomId);
        }

        if (nearEnd) {
            scrollToBottom();
        }
        return e;
    }

    function scrollToBottom() {
        var messages = $('.messages.current');
        messages.scrollTop(messages[0].scrollHeight);
    }

    function getRoomId(room) {
        return escape(room.toLowerCase()).replace(/[^a-z0-9]/, '_');
    }

    function updateMessageDimensions($message) {
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

    function updateRoomMessageDimensions(roomId) {
        var $messages = $('#messages-' + roomId + ' .message');
        $.each($messages, function () {
            updateMessageDimensions($(this));
        });
    }

    function resizeActiveRoom() {
        var roomId = getCurrentRoomId();

        updateRoomMessageDimensions(roomId);
    }

    window.scrollToBottom = scrollToBottom;

    chat.joinRoom = function (room, makeCurrent) {
        var roomId = getRoomId(room);
        addRoom(roomId, room);
        if (makeCurrent) {
            showRoom(roomId);
        }

        clearUsers(roomId);

        chat.getUsers(room)
            .done(function (users) {
                $.each(users, function () {
                    chat.addUser(this, true);
                    markUserInactive(this);
                });

                refreshUsers();

                $('#new-message').focus();
            });

        chat.getRecentMessages(room)
            .done(function (messages) {
                $.each(messages, function () {
                    chat.addMessage(this, true);
                });
            });

        if (makeCurrent) {
            addMessage('Entered ' + room, 'notification');
        }
        updateCookie();
    };


    chat.markInactive = function (users) {
        $.each(users, function () {
            markUserInactive(this);
        });
    };

    chat.updateActivity = function (user) {
        var id = 'u-' + user.Id;
        $('.' + id).fadeTo('slow', 1);
    };

    chat.showRooms = function (rooms) {
        addMessage('<h3>Rooms</h3>');
        if (!rooms.length) {
            addMessage('No rooms available', 'notification');
        }
        else {
            $.each(rooms, function () {
                addMessage(this.Name + ' (' + this.Count + ')');
            });
        }
        addMessage('<br/>');
    };

    chat.addMessageContent = function (id, content) {
        var nearEnd = $('.messages.current').isNearTheEnd();

        var e = $('#m-' + id + ' .middle').append(content)
                             .resizeMobileContent();

        updateUnread();

        if (nearEnd) {
            setTimeout(function () {
                updateMessageDimensions($('#m-' + id));
                scrollToBottom();
            }, 150);
        }

        updateMessageDimensions($('#m-' + id));
    };

    chat.addMessage = function (message, restore) {
        var roomId = getRoomId(message.Room),
            $messages = $('#messages-' + roomId),
            $lastMessage = $messages.find('.message').last(),
            currentUserName = $.cookie('username'),
            re = new RegExp("\\b@?" + currentUserName.replace(/\./, '\\.') + "\\b", "i"),
            previousUser = null,
            showUser = null,
            data = null,
            nearEnd = null;


        // var converter = new Showdown.converter();
        // var html = converter.makeHtml(message.Content);

        if ($lastMessage) {
            previousUser = $lastMessage.data('user');
        }

        showUser = previousUser !== message.User.Name;

        data = {
            trimmedName: trimUserName(message.User.Name),
            name: message.User.Name,
            showUser: showUser,
            hash: message.User.Hash,
            message: message.Content,
            id: message.Id,
            when: toLocal(message.When),
            highlight: re.test(message.Content) ? 'highlight' : ''
        };

        if (showUser === false) {
            $lastMessage.addClass('continue');
        }

        nearEnd = $messages.isNearTheEnd();
        $('#new-message-template').tmpl(data)
                                  .appendTo($messages)
                                  .resizeMobileContent();
        refreshMessages(roomId);

        if (!restore) {
            updateUnread(roomId);
        }

        var $message = $('#m-' + message.Id);
        if ($message.is(':visible')) {
            updateMessageDimensions($message);
        }

        if (!restore && nearEnd) {
            scrollToBottom();
        }
    };

    chat.addUser = function (user, exists) {
        // remove all users that are leaving
        $('.user.removing').remove();

        var data = {
            name: user.Name,
            hash: user.Hash,
            id: user.Id,
            owner: user.IsOwner
        };

        if (user.Id === this.id) {
            updateCookie();
        };

        var room = user.Room;
        if (!room) {
            return;
        }

        var roomId = getRoomId(room);

        // if user already listed in room
        if ($('#users-' + roomId + ' li.u-' + user.Id).length > 0) {
            return;
        }

        var e = $('#new-user-template').tmpl(data)
                                       .appendTo($('#users-' + roomId));

        refreshUsers(roomId);

        if (!exists && this.id !== user.Id) {
            addMessage(user.Name + ' just entered ' + room, 'notification', room);
        }

        updateCookie();
    };

    chat.changeUserName = function (user, oldName, newName) {
        var roomId = getRoomId(user.Room),
            $user = $('#users-' + roomId + ' .u-' + user.Id);

        $user.replaceWith(
                $('#new-user-template').tmpl({
                    name: user.Name,
                    hash: user.Hash,
                    id: user.Id,
                    owner: user.IsOwner
                })
        );

        refreshUsers();

        if (user.Id === this.id) {
            return;
        }
        else {
            addMessage(oldName + '\'s nick has changed to ' + newName, 'notification', user.Room);
        }
    };

    chat.userNameChanged = function (newName) {
        addMessage('Your name is now ' + newName, 'notification');
        updateCookie();
    };

    chat.changeGravatar = function (currentUser) {

        if (currentUser.Room) {
            var roomId = getRoomId(currentUser.Room),
                user = $('#users-' + roomId + ' .u-' + currentUser.Id);

            $user.replaceWith(
                $('#new-user-template').tmpl({
                    name: currentUser.Name,
                    hash: currentUser.Hash,
                    id: currentUser.Id,
                    owner: currentUser.IsOwner
                })
            );
        }
        refreshUsers();

        if (currentUser.Id === this.id) {
            return;
        }
        else {
            addMessage(currentUser.Name + "'s gravatar changed.", 'notification', currentUser.Room);
        }
    };

    chat.gravatarChanged = function (currentUser) {
        addMessage('Your gravatar has been set.', 'notification');
        chat.hash = currentUser.Hash;
        updateCookie();
    };

    chat.setTyping = function (currentUser, isTyping) {
        var roomId = getRoomId(currentUser.Room);
        if (isTyping) {
            $('#users-' + roomId + ' li.u-' + currentUser.Id).addClass('typing');
        }
        else {
            $('#users-' + roomId + ' li.u-' + currentUser.Id).removeClass('typing');
        }
    };

    chat.showCommands = function (commands) {
        addMessage('<h3>Help</h3>');
        $.each(commands, function () {
            addMessage(this.Name + ' - ' + this.Description);
        });
        addMessage('<br />');
    };

    chat.showUsersInRoom = function (room, names) {
        addMessage("<h3> Users in " + room + "</h3>");
        if (names.length === 0) {
            addMessage("Room is empty");
        }
        else {
            $.each(names, function () {
                addMessage("- " + this);
            });
        }
    };

    chat.listUsers = function (users) {
        if (users.length === 0) {
            addMessage("<h3>No users matched your search</h3>");
        } else {
            addMessage("<h3> The following users match your search </h3>");
            addMessage(users.join(", "));
        }
    };

    chat.showUsersRoomList = function (user, rooms) {
        if (rooms.length == 0) {
            addMessage("<h3>" + user + " is not in any rooms</h3>");
        } else {
            addMessage("<h3>" + user + " is in the following rooms</h3>");
            addMessage(rooms.join(", "));
        }
    };

    chat.sendMeMessage = function (name, message) {
        addMessage('*' + name + ' ' + message, 'notification');
    };

    chat.sendPrivateMessage = function (from, to, message) {
        addMessage('<emp>*' + from + '* &raquo; *' + to + '*</emp> ' + message, 'pm');
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
        addMessage('*' + from + ' nudged ' + (to ? 'you' : 'the room'), to ? 'pm' : 'notification');
    };

    chat.leave = function (user) {
        var room = user.Room;
        var roomId = getRoomId(room);

        if (this.id !== user.Id) {
            // remove user from specified room
            $('#users-' + roomId + ' li.u-' + user.Id).addClass('removing').fadeOut('slow', function () {
                $(this).remove();
            });

            refreshUsers();

            addMessage(user.Name + ' left ' + room, 'notification', room);
        }
        else {
            removeRoom(roomId);
            showRoom('lobby');
            updateCookie();

            addMessage('You have left ' + room, 'notification');

            this.room = null;
        }
    };

    chat.kick = function (room) {
        addMessage('You were kicked from ' + room, 'notification');
    };

    $('#send-message').submit(function () {
        var command = $('#new-message').val();
        chat.room = getCurrentRoom();
        if (command) {
            chat.send(command)
            .fail(function (e) {
                addMessage(e, 'error');
            });

            // Immediately mark as not-typing when sending
            clearTimeout(chat.typingTimeoutId);
            chat.typingTimeoutId = 0;
            chat.typing(false);
            updateChatHistory(command);

            $('#new-message').val('');
            $('#new-message').focus();
        }

        return false;
    });

    var typingTimeoutId = 0;
    $('#new-message').keypress(function (e) {
        chat.room = getCurrentRoom();
        // If not in a room, don't try to send typing notifications
        if (chat.room === null) {
            return;
        }

        // Clear any previous timeout
        if (chat.typingTimeoutId > 0) {
            clearTimeout(chat.typingTimeoutId);
        }
        // Otherwise, mark as typing
        else {
            chat.typing(true);
        }

        // Set timeout to turn off
        chat.typingTimeoutId = setTimeout(function () {
            chat.typingTimeoutId = 0;
            chat.typing(false);
        }, 3000);
    });

    $('#new-message').keydown(function (e) {
        // cycle through the history 
        var key = (e.keyCode ? e.keyCode : e.which);
        switch (key) {
            case Keys.Up:
                historyLocation -= 1;
                if (historyLocation < 0) {
                    historyLocation = chatHistory.length - 1;
                }
                $(this).val(chatHistory[historyLocation]);
                break;

            case Keys.Down:
                historyLocation = (historyLocation + 1) % chatHistory.length;
                $(this).val(chatHistory[historyLocation]);
                break;

            case Keys.Esc:
                $(this).val('');
                break;
        }
    });

    $(window).blur(function () {
        chat.focus = false;
    });

    $(window).focus(function () {
        chat.focus = true;
        chat.unread = 0;
        document.title = 'JabbR';

        updateTitle();
    });

    function updateUnread(roomId) {
        if (chat.focus === false) {
            if (!chat.unread) {
                chat.unread = 0;
            }
            chat.unread++;
        }
        var currentId = getCurrentRoomId();
        if (roomId && currentId !== roomId) {
            var $tab = $('#tabs-' + roomId);
            $tab.addClass('unread');
            var room = $tab.data('name');
            var unread = $tab.data('unread');
            if (!unread) {
                unread = 0;
            }
            unread++;
            $tab.text('(' + unread + ') ' + room).data('unread', unread);
        }

        updateTitle();
    }

    function updateTitle() {
        if (chat.unread === 0) {
            document.title = 'JabbR';
        }
        else {
            document.title = '(' + chat.unread + ') JabbR ';
        }
    }

    function updateCookie() {
        $.cookie('userid', chat.id, { path: '/', expires: 30 });
        $.cookie('username', chat.name, { path: '/', expires: 30 });

        var rooms = $('#tabs li')
                    .filter(function (index) { return index > 0; })    // skip 1st tab (Lobby)
                    .map(function () { return $(this).data('name'); })
                    .get()
                    .join(';');
        $.cookie('userroom', rooms, { path: '/', expires: 30 });

        if (chat.hash) {
            $.cookie('userhash', chat.hash, { path: '/', expires: 30 });
        }

        if (chat.currentRoom) {
            $.cookie('currentroom', chat.currentRoom, { path: '/', expires: 30 });
        }
    }

    $(window).focus();

    addMessage('Welcome to the JabbR', 'notification');
    addMessage('Type /help to see the list of commands', 'notification');
    addMessage('You can join any of the rooms on the right', 'notification');

    function ltrim(s) {
        return s.replace(/^\s+/g, "");
    }
    function rtrim(s) {
        return s.replace(/\s+$/g, "");
    }
    function trim(s) {
        return ltrim(rtrim(s));
    }

    $('#new-message').val('');
    $('#new-message').focus();
    $('#new-message').autoTabComplete({
        get: function () {
            return $('.users.current li')
                .map(function () {
                    return trim($(this).data('name'));
                })
                .get();
        }
    });

    $(document).on('click', '#tabs li', function () {
        var roomId = $(this).attr('id').substr(5);  // id = tabs-roomId
        showRoom(roomId);
    });

    $(document).on('click', 'li.room', function () {
        var room = $(this).data('name');
        var roomId = getRoomId(room);
        if ($('#messages-' + roomId).length > 0) {
            showRoom(roomId);
        }
        else {
            chat.room = "Lobby";
            chat.send("/join " + room)
                .fail(function (e) {
                    addMessage(e, 'error');
                });
        }
    });


    function getCurrentRoomId() {
        return $('#tabs li.current').attr('id').substr(5);
    }

    function getCurrentRoom() {
        var room = $('#tabs li.current').data('name');
        return room === 'Lobby' ? null : room;
    }

    function addRoom(roomId, room) {
        if ($('#tabs-' + roomId).length) {
            return;
        }

        $('<li/>').attr('id', 'tabs-' + roomId).html(room).appendTo($('#tabs')).data('name', room);
        $('<ul/>').attr('id', 'messages-' + roomId).addClass('messages').appendTo($('#chat-area')).hide();
        $('<ul/>').attr('id', 'users-' + roomId).addClass('users').appendTo($('#chat-area')).hide();
    }

    function removeRoom(roomId) {
        $('#messages-' + roomId).remove();
        $('#users-' + roomId).remove();
        $('#tabs-' + roomId).remove();
    }

    function showRoom(roomId) {
        $('#tabs li.current').removeClass('current');
        $('.messages.current').removeClass('current').hide();
        $('.users.current').removeClass('current').hide();
        var room = $('#tabs-' + roomId).data('name');
        $('#tabs-' + roomId).addClass('current').removeClass('unread').text(room).data('unread', 0);
        $('#messages-' + roomId).addClass('current').show();
        $('#users-' + roomId).addClass('current').show();

        if (roomId === "lobby") {
            $('#users-lobby').html('');
            chat.getRooms()
                .done(function (rooms) {
                    // Empty the lobby rooms
                    $('#users-lobby').empty();

                    $.each(rooms, function () {
                        $('<li/>').addClass('room').data('name', this.Name).html(this.Name + ' (' + this.Count + ')').appendTo('#users-lobby');
                    });
                });

        }

        chat.currentRoom = room;
        updateCookie();

        if (isiPad() === false) {
            $('#new-message').focus();
        }

        updateRoomMessageDimensions(roomId);

        scrollToBottom();
    }

    $.connection.hub.start(function () {
        chat.join()
            .fail(function (e) {
                addMessage(e, 'error');
            })
            .done(function (success) {
                var room = this.currentRoom || 'lobby';
                var roomId = getRoomId(room);
                addRoom(roomId, room);
                showRoom(roomId);

                if (success === false) {
                    $.cookie('userid', '');
                    addMessage('Choose a name using "/nick nickname".', 'notification');
                }
            });
    });

    $(document).on('click', 'h3.collapsible_title', function () {
        var $message = $(this).closest('.message');
        var nearEnd = $('.messages.current').isNearTheEnd();

        $(this).next().toggle(0, function () {
            updateMessageDimensions($message);
            if (nearEnd) {
                scrollToBottom();
            }
        });
    });

    $(window).resize(resizeActiveRoom);

    //Chat history setup
    var chatHistory = [];
    var historyLocation = history.length;

    function updateChatHistory(message) {
        chatHistory.push(message);
        //should this pop items off the top after a certain length?
        historyLocation = chatHistory.length;
    }

    // This stuff is to support TweetContentProvider, but should be extracted out if other content providers need custom CSS

    window.addTweet = function (tweet) {
        // Keep track of whether we're need the end, so we can auto-scroll once the tweet is added.
        var nearEnd = $('.messages.current').isNearTheEnd();

        // Grab any elements we need to process.
        var elements = $('div.tweet_' + tweet.id_str)
        // Strip the classname off, so we don't process this again if someone posts the same tweet.
        .removeClass('tweet_' + tweet.id_str)
        // Add the CSS class for formatting (this is so we don't get height/border while loading).
        .addClass('tweet');

        // Process the template, and add it in to the div.
        $('#tweet-template').tmpl(tweet)
        .appendTo(elements);

        $.each(elements.closest('.message'), function () {
            updateMessageDimensions($(this));
        });

        // If near the end, scroll.
        if (nearEnd) {
            scrollToBottom();
        }
    }

    // End of Tweet Content Provider JS

    window.captureDocumentWrite = function (documentWritePath, headerText, elementToAppendTo) {
        $.fn.captureDocumentWrite(documentWritePath, function (content) {
            var nearEnd = $('.messages.current').isNearTheEnd(),
                $message = elementToAppendTo.closest('.message');

            //Add headers so we can collapse the captured data
            var collapsible = $('<div class="captureDocumentWrite_collapsible"><h3>' + headerText + ' (click to show/hide)</h3><div class="captureDocumentWrite_content"></div></div>');
            $('.captureDocumentWrite_content', collapsible).append(content);

            //When the header of captured content is clicked, we want to show or hide the content.
            $('h3', collapsible).click(function () {
                var nearEndOnToggle = $('.messages.current').isNearTheEnd();
                $(this).next().toggle(0, function () {
                    updateMessageDimensions($message);
                    if (nearEndOnToggle) {
                        scrollToBottom();
                    }
                });
                return false;
            });

            //Since IE doesn't render the css if the links are not in the head element, we move those to the head element
            var links = $('link', collapsible);
            links.remove();
            $('head').append(links);

            elementToAppendTo.append(collapsible);

            updateMessageDimensions($message);

            if (nearEnd) {
                scrollToBottom();
            }
        });
    }

});