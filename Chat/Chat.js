/// <reference path="Scripts/jquery-1.6.3.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />

$(function () {
    var chat = $.connection.chat;

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

    function clearMessages() {
        $('#messages').html('');
    }

    function refreshMessages() { refreshList($('#messages')); }

    function clearUsers() {
        $('#users').html('');
    }

    function refreshUsers() { refreshList($('#users')); }

    function refreshList(list) {
        if (list.is('.ui-listview')) {
            list.listview('refresh');
        }
    }

    function markUserInactive(user) {
        id = 'u-' + user.Id;
        if (user.Active === false) {
            $('#' + id).fadeTo('slow', 0.5);
        }
    }

    function addMessage(content, type) {
        var nearEnd = $('#messages').isNearTheEnd();
        var e = $('<li/>').html(content).appendTo($('#messages'));

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
        var messages = $('#messages');
        messages.scrollTop(messages[0].scrollHeight);
    }

    window.scrollToBottom = scrollToBottom;

    chat.joinRoom = function (room) {
        clearMessages();
        clearUsers();

        chat.getUsers()
            .done(function (users) {
                $.each(users, function () {
                    chat.addUser(this, true);
                    markUserInactive(this);
                });

                refreshUsers();

                $('#new-message').focus();
            });

        chat.getRecentMessages()
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
        $('#' + id).fadeTo('slow', 1);
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
        var nearEnd = $('#messages').isNearTheEnd();

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
        var data = {
            name: message.User.Name,
            hash: message.User.Hash,
            message: message.Content,
            id: message.Id,
            when: toLocal(message.When)
        };

        var nearEnd = $('#messages').isNearTheEnd();
        var e = $('#new-message-template').tmpl(data)
                                          .appendTo($('#messages'))
                                          .resizeMobileContent();
        refreshMessages();

        updateUnread();

        if (!noScroll && nearEnd) {
            scrollToBottom();
        }
    };

    chat.addUser = function (user, exists) {

        // remove all users that are leaving
        $('#users .removing').remove();

        var id = 'u-' + user.Id;
        var element = document.getElementById(id)

        if (element) {
            return;
        }

        var data = {
            name: user.Name,
            hash: user.Hash,
            id: user.Id
        };

        var e = $('#new-user-template').tmpl(data)
                                       .appendTo($('#users'));

        refreshUsers();

        if (!exists && this.id !== user.Id) {
            addMessage(user.Name + ' just entered ' + this.room, 'notification');
            e.hide().fadeIn('slow');
        }

        updateCookie();
    };

    chat.changeUserName = function (user, oldName, newName) {
        $('#u-' + user.Id).replaceWith(
                $('#new-user-template').tmpl({
                    name: user.Name,
                    hash: user.Hash,
                    id: user.Id
                })
        );

        refreshUsers();

        if (user.Id === this.id) {
            addMessage('Your name is now ' + newName, 'notification');
            updateCookie();
        }
        else {
            addMessage(oldName + '\'s nick has changed to ' + newName, 'notification');
        }
    };

    chat.changeGravatar = function (currentUser) {

        $('#u-' + currentUser.Id).replaceWith(
            $('#new-user-template').tmpl({
                name: currentUser.Name,
                hash: currentUser.Hash,
                id: currentUser.Id
            })
        );

        refreshUsers();

        if (currentUser.Id === this.id) {

            chat.hash = currentUser.hash;
            updateCookie();

            addMessage('Your gravatar has been set.', 'notification');
        }
        else {
            addMessage(currentUser.Name + "'s gravatar changed.", 'notification');
        }
    };

    chat.setTyping = function (currentUser, isTyping) {
        if (isTyping) {
            $('li#u-' + currentUser.Id).addClass('typing');
        }
        else {
            $('li#u-' + currentUser.Id).removeClass('typing');
        }
    };

    chat.showCommands = function (commands) {
        addMessage('<h3>Help</h3>');
        $.each(commands, function () {
            addMessage(this.Name + ' - ' + this.Description);
        });
        addMessage('<br />');
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

        if (this.id != user.Id) {

            // remove identifier attribute so no one can use the element
            $('#u-' + user.Id).removeAttr('id').addClass('removing').fadeOut('slow', function () {
                $(this).remove();
            });

            refreshUsers();

            addMessage(user.Name + ' left ' + this.room, 'notification');
        }
        else {
            clearMessages();
            $('#users li').not('#u-' + user.Id).remove();

            addMessage('You have left ' + this.room, 'notification');

            this.room = null;
        }

    };

    $('#send-message').submit(function () {
        var command = $('#new-message').val();

        if (command) {
            chat.send(command)
            .fail(function (e) {
                addMessage(e, 'error');
            });

            // Immediately mark as not-typing when sending
            clearTimeout(chat.typingTimeoutId);
            chat.typingTimeoutId = 0;
            chat.typing(false);

            $('#new-message').val('');
            $('#new-message').focus();
        }

        return false;
    });

    var typingTimeoutId = 0;
    $('#new-message').keypress(function () {
        // If not in a room, don't try to send typing notifications
        if (chat.room == null) {
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
    })

    $(window).blur(function () {
        chat.focus = false;
    });

    $(window).focus(function () {
        chat.focus = true;
        chat.unread = 0;
        document.title = 'SignalR Chat';
    });

    function updateUnread() {
        if (chat.focus === false) {
            if (!chat.unread) {
                chat.unread = 0;
            }
            chat.unread++;
        }
        updateTitle();
    }

    function updateTitle() {
        if (chat.unread == 0) {
            document.title = 'SignalR Chat';
        }
        else {
            document.title = 'SignalR Chat (' + chat.unread + ')';
        }
    }

    function updateCookie() {
        $.cookie('userid', chat.id, { path: '/', expires: 30 });
        $.cookie('username', chat.name, { path: '/', expires: 30 });

        if (chat.room) {
            $.cookie('userroom', chat.room, { path: '/', expires: 30 });
        }

        if (chat.hash) {
            $.cookie('userhash', chat.hash, { path: '/', expires: 30 });
        }
    }

    addMessage('Welcome to the SignalR IRC clone', 'notification');
    addMessage('Type /help to see the list of commands', 'notification');

    $('#new-message').val('');
    $('#new-message').focus();


    $.connection.hub.start(function () {
        chat.join()
            .fail(function (e) {
                addMessage(e, 'error');
            })
            .done(function (success) {
                if (success === false) {
                    $.cookie('userid', '');
                    addMessage('Choose a name using "/nick nickname".', 'notification');
                }
            });
    });
});

// This stuff is to support TweetContentProvider, but should be extracted out if other content providers need custom CSS

function addTweet(tweet) {
    // Keep track of whether we're need the end, so we can auto-scroll once the tweet is added.
    var nearEnd = $('#messages').isNearTheEnd();

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
        var nearEnd = $('#messages').isNearTheEnd();

        //Add headers so we can collapse the captured data
        var collapsible = $('<div class="captureDocumentWrite_collapsible"><h3>' + headerText + ' (click to show/hide)</h3><div class="captureDocumentWrite_content"></div></div>');
        $('.captureDocumentWrite_content', collapsible).append(content);

        //When the header of captured content is clicked, we want to show or hide the content.
        $('h3', collapsible).click(function () {
            var nearEndOnToggle = $('#messages').isNearTheEnd();
            $(this).next().toggle(0,function () {
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
