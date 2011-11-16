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
        var m = $('.messages.current');
        if (room) {
            var roomId = getRoomId(room);
            m = $('#messages-' + roomId);
        }
        var nearEnd = m.isNearTheEnd();

        var converter = new Showdown.converter();
        var html = converter.makeHtml(content);
        var e = $('<li/>').html(html).appendTo(m);

        refreshMessages();

        if (type) {
            e.addClass(type);
        }

        updateUnread();
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

    window.scrollToBottom = scrollToBottom;

    chat.joinRoom = function (room) {

        var roomId = getRoomId(room);
        addRoom(roomId, room);
        showRoom(roomId);

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

        addMessage('Entered ' + room, 'notification');
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

        var e = $('#m-' + id).append(content)
                             .resizeMobileContent();

        updateUnread();

        if (nearEnd) {
            setTimeout(function () {
                scrollToBottom();
            }, 150);
        }
    };

    chat.addMessage = function (message, noScroll) {
        var currentUserName = $.cookie('username');
        var re = new RegExp("\\b@?" + currentUserName + "\\b", "i");

        var converter = new Showdown.converter();
        var html = converter.makeHtml(message.Content);

        var data = {
            name: message.User.Name,
            hash: message.User.Hash,
            message: html,
            id: message.Id,
            when: toLocal(message.When),
            highlight: re.test(message.Content) ? 'highlight' : ''
        };

        var roomId = getRoomId(message.Room);
        var nearEnd = $('#messages-' + roomId).isNearTheEnd();
        var e = $('#new-message-template').tmpl(data)
                                          .appendTo($('#messages-' + roomId))
                                          .resizeMobileContent();
        refreshMessages(roomId);

        updateUnread(roomId);

        if (!noScroll && nearEnd) {
            scrollToBottom();
        }
    };

    chat.addUser = function (user, exists) {
        // remove all users that are leaving
        $('.user.removing').remove();

        var data = {
            name: user.Name,
            hash: user.Hash,
            id: user.Id
        };

        if (user.Id === this.id) {
            addMessage('Your name is now ' + user.Name, 'notification', user.Room);
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
        var roomId = getRoomId(user.Room);
        $('#users-' + roomId + ' .u-' + user.Id).replaceWith(
                $('#new-user-template').tmpl({
                    name: user.Name,
                    hash: user.Hash,
                    id: user.Id
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
            var roomId = getRoomId(currentUser.Room);
            $('#users-' + roomId + ' .u-' + currentUser.Id).replaceWith(
                $('#new-user-template').tmpl({
                    name: currentUser.Name,
                    hash: currentUser.Hash,
                    id: currentUser.Id
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
        chat.hash = currentUser.hash;
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
    }

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
        document.title = 'SignalR Chat';
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
            document.title = 'SignalR Chat';
        }
        else {
            document.title = '(' + chat.unread + ') SignalR Chat ';
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
    }

    $(window).focus();

    addMessage('Welcome to the SignalR IRC clone', 'notification');
    addMessage('Type /help to see the list of commands', 'notification');

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
                .map(function () { return trim($(this).text()); })
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
        $('<li/>').attr('id', 'tabs-' + roomId).html(room).appendTo($('#tabs')).addClass('current').data('name', room);
        $('<ul/>').attr('id', 'messages-' + roomId).addClass('messages').appendTo($('#chat-area')).addClass('current');
        $('<ul/>').attr('id', 'users-' + roomId).addClass('users').appendTo($('#chat-area')).addClass('current');
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
                    $.each(rooms, function () {
                        $('<li/>').addClass('room').data('name', this.Name).html(this.Name + ' (' + this.Count + ')').appendTo('#users-lobby');
                    });
                });

        }

        scrollToBottom();
        $('#new-message').focus();

    }

    $.connection.hub.start(function () {
        chat.join()
            .fail(function (e) {
                addMessage(e, 'error');
            })
            .done(function (success) {
                showRoom('lobby');
                if (success === false) {
                    $.cookie('userid', '');
                    addMessage('Choose a name using "/nick nickname".', 'notification');
                }
            });
    });

    $(document).on('click', 'h3.collapsible_title', function () {
        var nearEnd = $('#messages').isNearTheEnd();

        $(this).next().toggle(function () {
            if (nearEnd) {
                scrollToBottom();
            }
        });
    });

    //Chat history setup
    var chatHistory = [];
    var historyLocation = history.length;

    function updateChatHistory(message) {
        chatHistory.push(message);
        //should this pop items off the top after a certain length?
        historyLocation = chatHistory.length;
    }

});

// This stuff is to support TweetContentProvider, but should be extracted out if other content providers need custom CSS

function addTweet(tweet) {
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

    // If near the end, scroll.
    if (nearEnd) {
        scrollToBottom();
    }
}

// End of Tweet Content Provider JS

function captureDocumentWrite(documentWritePath, headerText, elementToAppendTo) {
    $.fn.captureDocumentWrite(documentWritePath, function (content) {
        var nearEnd = $('.messages.current').isNearTheEnd();

        //Add headers so we can collapse the captured data
        var collapsible = $('<div class="captureDocumentWrite_collapsible"><h3>' + headerText + ' (click to show/hide)</h3><div class="captureDocumentWrite_content"></div></div>');
        $('.captureDocumentWrite_content', collapsible).append(content);

        //When the header of captured content is clicked, we want to show or hide the content.
        $('h3', collapsible).click(function () {
            var nearEndOnToggle = $('#messages').isNearTheEnd();
            $(this).next().toggle(0, function () {
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

        if (nearEnd) {
            scrollToBottom();
        }
    });
}
