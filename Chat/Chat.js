/// <reference path="../../Scripts/jquery-1.6.2.js" />
/// <reference path="../../Scripts/jQuery.tmpl.js" />
/// <reference path="../../Scripts/jquery.cookie.js" />

$(function () {
    var chat = $.connection.chat;

    $.fn.isNearTheEnd = function () {
        return this[0].scrollTop + this.height() >= this[0].scrollHeight;
    }

    function clearMessages() {
        $('#messages').html('');
    }

    function clearUsers() {
        $('#users').html('');
    }

    function addMessage(content, type) {
        var nearEnd = $('#messages').isNearTheEnd();
        var e = $('<li/>').html(content).appendTo($('#messages'));
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
        console.log('joinRoom(' + room + ')');
        clearMessages();
        clearUsers();

        chat.getUsers()
            .done(function (users) {
                $.each(users, function () {
                    chat.addUser(this, true);
                });

                $('#new-message').focus();
            });

        chat.getRecentMessages()
            .done(function (messages) {
                $.each(messages, function () {
                    chat.addMessage(this.Id, this.User, this.Content);
                });
            });

        addMessage('Entered ' + room, 'notification');
    };


    chat.markInactive = function (user) {
        var id = 'u-' + user.Id;
        $('#' + id).fadeTo('slow', 0.5);
    };

    chat.updateActivity = function (user) {
        var id = 'u-' + user.Id;
        $('#' + id).fadeTo('slow', 1);
    };

    chat.showRooms = function (rooms) {
        console.log('showRooms(' + rooms.length + ')');
        addMessage('<h3>Rooms</h3>');
        if (!rooms.length) {
            addMessage('No rooms available', 'notification')
        }
        else {
            $.each(rooms, function () {
                addMessage(this.Name + ' (' + this.Count + ')');
            });
        }
        addMessage('<br/>');
    };

    chat.addMessageContent = function (id, content) {
        console.log('addMessageContent(' + id + ', ' + content + ')');
        var nearEnd = $('#messages').isNearTheEnd();
        var e = $('#m-' + id).append(content);
        updateUnread();
        if (nearEnd) {
            scrollTo(e[0]);
        }
    };

    chat.addMessage = function (id, user, message) {
        console.log('addMessage()');
        var data = {
            name: user.Name,
            hash: user.Hash,
            message: message,
            id: id
        };

        var nearEnd = $('#messages').isNearTheEnd();
        var e = $('#new-message-template').tmpl(data)
                                          .appendTo($('#messages'));
        updateUnread();

        if (nearEnd) {
            scrollTo(e[0]);
        }
    };

    chat.addUser = function (user, exists) {
        console.log('addUser()');
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

        if (!exists && this.name != user.Name) {
            addMessage(user.Name + ' just entered ' + this.room, 'notification');
            e.hide().fadeIn('slow');
        }

        updateCookie();
    };

    chat.changeUserName = function (oldUser, newUser) {
        console.log('changeUserName()');
        $('#u-' + oldUser.Id).replaceWith(
                $('#new-user-template').tmpl({
                    name: newUser.Name,
                    hash: newUser.Hash,
                    id: newUser.Id
                })
        );

        if (oldUser.Name === this.name) {
            addMessage('Your name is now ' + newUser.Name, 'notification');
            updateCookie();
        }
        else {
            addMessage(oldUser.Name + '\'s nick has changed to ' + newUser.Name, 'notification');
        }
    };

    chat.showCommands = function (commands) {
        console.log('showCommands()');
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
        console.log('leave()');
        if (this.id != user.Id) {
            $('#u-' + user.Id).fadeOut('slow', function () {
                $(this).remove();
            });

            addMessage(user.Name + ' left ' + this.room, 'notification');
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
            .done(function (success) {
                if (success === false) {
                    $.cookie('userid', '')
                    addMessage('Choose a name using "/nick nickname".', 'notification');
                }
            });
    });
});