/// <reference path="../../Scripts/jquery-1.6.2.js" />
/// <reference path="../../Scripts/jQuery.tmpl.js" />
/// <reference path="../../Scripts/jquery.cookie.js" />

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
            scrollTo(e[0]);
        }
        return e;
    }

    function scrollTo(e) {
        e.scrollIntoView();
    }

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
            scrollTo(e[0]);
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
            scrollTo(e[0]);
        }
    };

    chat.addUser = function (user, exists) {
        var id = 'u-' + user.Id;
        if (document.getElementById(id)) {
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

        if (!exists && this.name !== user.Name) {
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
            addMessage('Your gravatar has been set.', 'notification');
        }
        else {
            addMessage(currentUser.Name + "'s gravatar changed.", 'notification');
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
        addMessage('<emp>*' + from + '*</emp> ' + message, 'pm');
    };

    chat.leave = function (user) {
        if (this.id != user.Id) {
            $('#u-' + user.Id).fadeOut('slow', function () {
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

            $('#new-message').val('');
            $('#new-message').focus();
        }

        return false;
    });

    $(window).blur(function () {
        chat.focus = false;
    });

    $(window).focus(function () {
        chat.focus = true;
        chat.unread = 0;
        document.title = 'SignalR Chat';
    });

    $(window).unload(function () {
        if (chat.room)
            chat.send("/leave");
        //ignore errors
    });

    function updateUnread() {
        if (!chat.focus) {
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