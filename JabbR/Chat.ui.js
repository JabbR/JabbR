/// <reference path="Scripts/jquery-1.7.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />
/// <reference path="Chat.toast.js" />
/*global Emoji:true, janrain:true */
(function ($, window, document, utility) {
    "use strict";

    var $chatArea = null,
        $tabs = null,
        $submitButton = null,
        $newMessage = null,
        $toast = null,
        $disconnectDialog = null,
        $downloadIcon = null,
        $downloadDialog = null,
        $downloadDialogButton = null,
        $downloadRange = null,
        $help = null,
        $ui = null,
        $sound = null,
        templates = null,
        focus = true,
        commands = [],
        shortcuts = [],
        Keys = { Up: 38, Down: 40, Esc: 27, Enter: 13, Slash: 47, Space: 32, Tab: 9, Question: 191 },
        scrollTopThreshold = 75,
        toast = window.chat.toast,
        preferences = null,
        $login = null,
        name,
        lastCycledMessage = null,
        $helpPopup = null,
        $helpBody = null,
        helpHeight = 0,
        $shortCutHelp = null,
        $globalCmdHelp = null,
        $roomCmdHelp = null,
        $userCmdHelp = null,
        $updatePopup = null,
        $window = $(window),
        $document = $(document),
        $lobbyRoomFilterForm = null,
        $roomFilterInput = null,
        $closedRoomFilter = null,
        updateTimeout = 15000,
        $richness = null,
        lastPrivate = null;

    function getRoomId(roomName) {
        return window.escape(roomName.toLowerCase()).replace(/[^a-z0-9]/, '_');
    }

    function getUserClassName(userName) {
        return '[data-name="' + userName + '"]';
    }

    function getRoomPreferenceKey(roomName) {
        return '_room_' + roomName;
    }

    function showClosedRoomsInLobby() {
        return $closedRoomFilter.is(':checked');
    }

    function Room($tab, $usersContainer, $usersOwners, $usersActive, $usersIdle, $messages, $roomTopic) {
        this.tab = $tab;
        this.users = $usersContainer;
        this.owners = $usersOwners;
        this.activeUsers = $usersActive;
        this.idleUsers = $usersIdle;
        this.messages = $messages;
        this.roomTopic = $roomTopic;

        function glowTab(n) {
            // Stop if we're not unread anymore
            if (!$tab.hasClass('unread')) {
                return;
            }

            // Go light
            $tab.animate({ backgroundColor: '#e5e5e5', color: '#000000' }, 800, function () {
                // Stop if we're not unread anymore
                if (!$tab.hasClass('unread')) {
                    return;
                }

                n--;

                // Check if we're on our last glow
                if (n !== 0) {
                    // Go dark
                    $tab.animate({ backgroundColor: '#164C85', color: '#ffffff' }, 800, function () {
                        // Glow the tab again
                        glowTab(n);
                    });
                }
                else {
                    // Leave the tab highlighted
                    $tab.animate({ backgroundColor: '#043C4C', color: '#ffffff' }, 800);
                }
            });
        }

        this.isLocked = function () {
            return this.tab.hasClass('locked');
        };

        this.isLobby = function () {
            return this.tab.hasClass('lobby');
        };

        this.hasUnread = function () {
            return this.tab.hasClass('unread');
        };

        this.hasMessages = function () {
            return this.tab.data('messages');
        };

        this.updateMessages = function (value) {
            this.tab.data('messages', value);
        };

        this.getUnread = function () {
            return $tab.data('unread') || 0;
        };

        this.hasSeparator = function () {
            return this.messages.find('.message-separator').length > 0;
        };

        this.needsSeparator = function () {
            if (this.isActive()) {
                return false;
            }
            return this.isInitialized() && this.getUnread() === 5;
        };

        this.addSeparator = function () {
            if (this.isLobby()) {
                return;
            }

            // find first correct unread message
            var n = this.getUnread(),
                $unread = this.messages.find('.message').eq(-(n + 1));

            $unread.after(templates.separator.tmpl())
                .data('unread', n); // store unread count

            this.scrollToBottom();
        };

        this.removeSeparator = function () {
            this.messages.find('.message-separator').fadeOut(2000, function () {
                $(this).remove();
            });
        };

        this.updateUnread = function (isMentioned) {
            var $tab = this.tab.addClass('unread'),
                $content = $tab.find('.content'),
                unread = ($tab.data('unread') || 0) + 1,
                hasMentions = $tab.data('hasMentions') || isMentioned; // Whether or not the user already has unread messages to him/her

            $content.text((hasMentions ? '*' : '') + '(' + unread + ') ' + this.getName());

            $tab.data('unread', unread);
            $tab.data('hasMentions', hasMentions);

            if (!this.isActive() && unread === 1) {
                // If this room isn't active then we're going to glow the tab
                // to get the user's attention
                glowTab(6);
            }
        };

        this.scrollToBottom = function () {
            // IE will repaint if we do the Chrome bugfix and look jumpy
            if ($.browser.webkit) {
                // Chrome fix for hiding and showing scroll areas
                this.messages.scrollTop(this.messages.scrollTop() - 1);
            }
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

        this.isClosed = function () {
            return this.tab.attr('data-closed') === 'true';
        };

        this.close = function () {
            this.tab.attr('data-closed', true);
            this.tab.addClass('closed');
        };

        this.unClose = function () {
            this.tab.attr('data-closed', false);
            this.tab.removeClass('closed');
        };

        this.clear = function () {
            this.messages.empty();
            this.owners.empty();
            this.activeUsers.empty();
            this.idleUsers.empty();
        };

        this.makeInactive = function () {
            this.tab.removeClass('current');

            this.messages.removeClass('current')
                         .hide();

            this.users.removeClass('current')
                      .hide();

            this.roomTopic.removeClass('current')
                      .hide();

            if (this.isLobby()) {
                $lobbyRoomFilterForm.hide();
            }
        };

        this.makeActive = function () {
            var currUnread = this.getUnread(),
                lastUnread = this.messages.find('.message-separator').data('unread') || 0;

            if (!utility.isMobile) {
                $newMessage.focus();
            }

            this.tab.addClass('current')
                    .removeClass('unread')
                    .stop(true, true)
                    .css('backgroundColor', '')
                    .css('color', '')
                    .data('unread', 0)
                    .data('hasMentions', false)
                    .find('.content')
                    .text(this.getName());

            this.messages.addClass('current')
                         .show();

            this.users.addClass('current')
                      .show();

            this.roomTopic.addClass('current')
                      .show();

            if (this.isLobby()) {
                $lobbyRoomFilterForm.show();
            }
            // if no unread since last separator
            // remove previous separator
            if (currUnread <= lastUnread) {
                this.removeSeparator();
            }
        };

        this.setInitialized = function () {
            this.tab.data('initialized', true);
        };

        this.isInitialized = function () {
            return this.tab.data('initialized') === true;
        };

        // Users
        this.getUser = function (userName) {
            return this.users.find(getUserClassName(userName));
        };

        this.getUserReferences = function (userName) {
            return $.merge(this.getUser(userName),
                           this.messages.find(getUserClassName(userName)));
        };

        this.setLocked = function () {
            this.tab.addClass('locked');
        };

        this.setListState = function (list) {
            if (list.children('li').length > 0) {
                var roomEmptyStatus = list.children('li.empty');
                if (roomEmptyStatus.length === 0) {
                    return;
                } else {
                    roomEmptyStatus.remove();
                    return;
                }
            }
            list.append($('<li class="empty">No users</li>'));
        };

        this.addUser = function (userViewModel, $user) {
            if (userViewModel.active) {
                this.addUserToList($user, this.activeUsers);
            } else if (userViewModel.owner) {
                this.addUserToList($user, this.owners);
            }
            else {
                this.addUserToList($user, this.idleUsers);
            }
        };

        this.addUserToList = function ($user, list) {
            var oldParentList = $user.parent('ul');
            $user.appendTo(list);
            this.setListState(list);
            if (typeof oldParentList !== undefined) {
                this.setListState(oldParentList);
            }
        };

        this.appearsInList = function ($user, list) {
            return $user.parent('ul').attr('id') == list.attr('id');
        };

        this.updateUserStatus = function ($user) {
            var owner = $user.data('owner') || false;

            if (owner === true) {
                if (!this.appearsInList($user, this.owners)) {
                    this.addUserToList($user, this.owners);
                }
                return;
            }

            var status = $user.data('active');
            if (typeof status === "undefined") {
                return;
            }

            if (status === true) {
                if (!this.appearsInList($user, this.activeUsers)) {
                    this.addUserToList($user, this.activeUsers);
                }
            } else {
                if (!this.appearsInList($user, this.idleUsers)) {
                    this.addUserToList($user, this.idleUsers);
                }
            }
        };

        this.sortLists = function () {
            this.sortList(this.activeUsers);
            this.sortList(this.idleUsers);
        };

        this.sortList = function (listToSort) {
            var listItems = listToSort.children('li').get();
            listItems.sort(function (a, b) {
                var compA = $(a).data('name').toString().toUpperCase();
                var compB = $(b).data('name').toString().toUpperCase();
                return (compA < compB) ? -1 : (compA > compB) ? 1 : 0;
            });
            $.each(listItems, function (index, item) { listToSort.append(item); });
        };
    }

    function getRoomElements(roomName) {
        var roomId = getRoomId(roomName);
        var room = new Room($('#tabs-' + roomId),
                        $('#userlist-' + roomId),
                        $('#userlist-' + roomId + '-owners'),
                        $('#userlist-' + roomId + '-active'),
                        $('#userlist-' + roomId + '-idle'),
                        $('#messages-' + roomId),
                        $('#roomTopic-' + roomId));
        return room;
    }

    function getCurrentRoomElements() {
        var room = new Room($tabs.find('li.current'),
                        $('.users.current'),
                        $('.userlist.current .owners'),
                        $('.userlist.current .active'),
                        $('.userlist.current .idle'),
                        $('.messages.current'),
                        $('.roomTopic.current'));
        return room;
    }

    function getAllRoomElements() {
        var rooms = [];
        $("ul#tabs > li.room").each(function () {
            rooms[rooms.length] = getRoomElements($(this).data("name"));
        });
        return rooms;
    }

    function getLobby() {
        return getRoomElements('Lobby');
    }

    function updateLobbyRoomCount(room, count) {
        var lobby = getLobby(),
            $room = lobby.users.find('[data-room="' + room.Name + '"]'),
            $count = $room.find('.count');

        $room.css('background-color', '#f5f5f5');
        $count.text(' (' + count + ')');

        if (room.Private === true) {
            $room.addClass('locked');
        }
        if (room.Closed === true) {
            $room.addClass('closed');
        }

        // Do a little animation
        $room.animate({ backgroundColor: '#e5e5e5' }, 800);
    }


    function addRoom(roomViewModel) {
        // Do nothing if the room exists
        var roomName = roomViewModel.Name,
            room = getRoomElements(roomViewModel.Name),
            roomId = null,
            viewModel = null,
            $messages = null,
            $roomTopic = null,
            scrollHandler = null,
            userContainer = null;

        if (room.exists()) {
            return false;
        }

        roomId = getRoomId(roomName);

        // Add the tab
        viewModel = {
            id: roomId,
            name: roomName,
            closed: roomViewModel.Closed
        };

        templates.tab.tmpl(viewModel).data('name', roomName).appendTo($tabs);

        $messages = $('<ul/>').attr('id', 'messages-' + roomId)
                              .addClass('messages')
                              .appendTo($chatArea)
                              .hide();

        $roomTopic = $('<div/>').attr('id', 'roomTopic-' + roomId)
                              .addClass('roomTopic')
                              .appendTo($chatArea)
                              .hide();

        if (roomName !== "lobby") {
            userContainer = $('<div/>').attr('id', 'userlist-' + roomId)
                .addClass('users')
                .appendTo($chatArea).hide();
            templates.userlist.tmpl({ listname: '- Room Owners', id: 'userlist-' + roomId + '-owners' })
                .addClass('owners')
                .appendTo(userContainer);
            templates.userlist.tmpl({ listname: '- Online', id: 'userlist-' + roomId + '-active' })
                .addClass('active')
                .appendTo(userContainer);
            templates.userlist.tmpl({ listname: '- Away', id: 'userlist-' + roomId + '-idle' })
                .addClass('idle')
                .appendTo(userContainer);
            userContainer.find('h3').click(function () {
                if ($.trim($(this).text())[0] === '-') {
                    $(this).text($(this).text().replace('-', '+'));
                } else {
                    $(this).text($(this).text().replace('+', '-'));
                }
                $(this).next().toggle(0);
                return false;
            });
        } else {
            $('<ul/>').attr('id', 'userlist-' + roomId)
                .addClass('users')
                .appendTo($chatArea).hide();
        }

        $tabs.find('li')
            .not('.lobby')
            .sortElements(function (a, b) {
                return $(a).data('name').toLowerCase() > $(b).data('name').toLowerCase() ? 1 : -1;
            });

        scrollHandler = function (ev) {
            var messageId = null;

            // Do nothing if there's nothing else
            if ($(this).data('full') === true) {
                return;
            }

            // If you're we're near the top, raise the event, but if the scroll
            // bar is small enough that we're at the bottom edge, ignore it.
            // We have to use the ui version because the room object above is
            // not fully initialized, so there are no messages.
            if ($(this).scrollTop() <= scrollTopThreshold && !ui.isNearTheEnd(roomId)) {
                var $child = $messages.children('.message:first');
                if ($child.length > 0) {
                    messageId = $child.attr('id')
                                      .substr(2); // Remove the "m-"
                    $ui.trigger(ui.events.scrollRoomTop, [{ name: roomName, messageId: messageId}]);
                }
            }
        };

        // Hookup the scroll handler since event delegation doesn't work with scroll events
        $messages.bind('scroll', scrollHandler);

        // Store the scroll handler so we can remove it later
        $messages.data('scrollHandler', scrollHandler);

        setAccessKeys();
        return true;
    }

    function removeRoom(roomName) {
        var room = getRoomElements(roomName),
            scrollHandler = null;

        if (room.exists()) {
            // Remove the scroll handler from this room
            scrollHandler = room.messages.data('scrollHandler');
            room.messages.unbind('scrollHandler', scrollHandler);

            room.tab.remove();
            room.messages.remove();
            room.users.remove();
            setAccessKeys();
        }
    }

    function setAccessKeys() {
        $.each($tabs.find('li.room'), function (index, item) {
            $(item).children('button').attr('accesskey', getRoomAccessKey(index));
        });
    }

    function getRoomAccessKey(index) {
        if (index < 10) {
            return index + 1;
        }
        return 0;
    }

    function navigateToRoom(roomName) {
        $.history.load('/rooms/' + roomName);
    }

    function processMessage(message, roomName) {
        var isFromCollapibleContentProvider = isFromCollapsibleContentProvider(message.message),
            collapseContent = shouldCollapseContent(message.message, roomName);

        message.message = isFromCollapibleContentProvider ? message.message : utility.parseEmojis(message.message);
        message.trimmedName = utility.trim(message.name, 21);
        message.when = message.date.formatTime(true);
        message.fulldate = message.date.toLocaleString();

        if (collapseContent) {
            message.message = collapseRichContent(message.message);
        }
    }

    function isFromCollapsibleContentProvider(content) {
        return content.indexOf('class="collapsible_box') > -1; // leaving off trailing " purposefully
    }

    function shouldCollapseContent(content, roomName) {
        var collapsible = isFromCollapsibleContentProvider(content),
            collapseForRoom = roomName ? getRoomPreference(roomName, 'blockRichness') : getActiveRoomPreference('blockRichness');

        return collapsible && collapseForRoom;
    }

    function collapseRichContent(content) {
        content = content.replace(/class="collapsible_box/g, 'style="display: none;" class="collapsible_box');
        return content.replace(/class="collapsible_title"/g, 'class="collapsible_title" title="Content collapsed because you have Rich-Content disabled"');
    }

    function triggerFocus() {
        ui.focus = true;
        $ui.trigger(ui.events.focusit);
    }

    function loadPreferences() {
        // Restore the global preferences
    }

    function toggleElement($element, preferenceName, roomName) {
        var value = roomName ? getRoomPreference(roomName, preferenceName) : preferences[preferenceName];
        if (value === true) {
            $element.removeClass('off');
        }
        else {
            $element.addClass('off');
        }
    }

    function loadRoomPreferences(roomName) {
        var roomPreferences = getRoomPreference(roomName);

        // Placeholder for room level preferences
        toggleElement($sound, 'hasSound', roomName);
        toggleElement($toast, 'canToast', roomName);
        toggleElement($richness, 'blockRichness', roomName);
    }

    function setPreference(name, value) {
        preferences[name] = value;

        $(ui).trigger(ui.events.preferencesChanged);
    }

    function setRoomPreference(roomName, name, value) {
        var roomPreferences = preferences[getRoomPreferenceKey(roomName)];

        if (!roomPreferences) {
            roomPreferences = {};
            preferences[getRoomPreferenceKey(roomName)] = roomPreferences;
        }

        roomPreferences[name] = value;

        $ui.trigger(ui.events.preferencesChanged);
    }

    function getRoomPreference(roomName, name) {
        return (preferences[getRoomPreferenceKey(roomName)] || {})[name];
    }

    function getActiveRoomPreference(name) {
        var room = getCurrentRoomElements();
        return getRoomPreference(room.getName(), name);
    }

    function anyRoomPreference(name, value) {
        for (var key in preferences) {
            if (preferences[key][name] === value) {
                return true;
            }
        }
        return false;
    }

    function triggerSend() {
        var msg = $.trim($newMessage.val());

        if (msg) {
            if (msg.toLowerCase() == '/login') {
                ui.showLogin();
            }
            else {
                $ui.trigger(ui.events.sendMessage, [msg]);
            }
        }

        $newMessage.val('');
        $newMessage.focus();

        triggerFocus();

        // always scroll to bottom after new message sent
        var room = getCurrentRoomElements();
        room.scrollToBottom();
        room.removeSeparator();
    }

    function updateNote(userViewModel, $user) {
        var $note = $user.find('.note'),
            noteText = userViewModel.note,
            noteTextEncoded = null,
            requireRoomUpdate = false;

        if (userViewModel.noteClass === 'afk') {
            noteText = userViewModel.note + ' (' + userViewModel.timeAgo + ')';
            requireRoomUpdate = ui.setUserInActive($user);
        }
        else if (userViewModel.active) {
            requireRoomUpdate = ui.setUserActive($user);
        }
        else {
            requireRoomUpdate = ui.setUserInActive($user);
        }

        noteTextEncoded = $('<div/>').html(noteText).text();

        // Remove all classes and the text
        $note.removeClass('afk message');
        $note.removeAttr('title');

        $note.addClass(userViewModel.noteClass);
        if (userViewModel.note) {
            $note.attr('title', noteTextEncoded);
        }

        if (requireRoomUpdate) {
            $user.each(function () {
                var room = getRoomElements($(this).data('inroom'));
                room.updateUserStatus($(this));
                room.sortLists();
            });
        }
    }

    function updateFlag(userViewModel, $user) {
        var $flag = $user.find('.flag');

        $flag.removeClass();
        $flag.addClass('flag');
        $flag.removeAttr('title');

        $flag.addClass(userViewModel.flagClass);
        if (userViewModel.country) {
            $flag.attr('title', userViewModel.country);
        }
    }

    function updateRoomTopic(roomViewModel) {
        var room = getRoomElements(roomViewModel.Name);
        var topic = roomViewModel.Topic;
        var topicHtml = topic === '' ? 'You\'re chatting in ' + roomViewModel.Name : '<strong>Topic: </strong>' + topic;
        var roomTopic = room.roomTopic;
        var isVisibleRoom = getCurrentRoomElements().getName() === roomViewModel.Name;
        if (isVisibleRoom) {
            roomTopic.hide();
        }
        roomTopic.html(topicHtml);
        if (isVisibleRoom) {
            roomTopic.fadeIn(2000);
        }
    }

    // Rotating Tips.
    var messages = [
                'Type @ then press TAB to auto-complete nicknames',
                'Use ? or type /? to display the FAQ and list of commands',
                'Type : then press TAB to auto-complete emoji icons',
                'You can create your own private rooms. Use ? or type /? for more info'
            ];

    var cycleTimeInMilliseconds = 60 * 1000; // 1 minute.
    var messageIndex = 0;

    function cycleMessages() {
        setTimeout(function () {
            messageIndex++;
            if (messageIndex >= messages.length) {
                messageIndex = 0;
            }
            $('#message-instruction').fadeOut(2000, function () {
                $('#message-instruction').html(messages[messageIndex]);
            });

            $('#message-instruction').fadeIn(2000, cycleMessages);
        }, cycleTimeInMilliseconds);
    }

    var ui = {

        //lets store any events to be triggered as constants here to aid intellisense and avoid
        //string duplication everywhere
        events: {
            closeRoom: 'jabbr.ui.closeRoom',
            prevMessage: 'jabbr.ui.prevMessage',
            openRoom: 'jabbr.ui.openRoom',
            nextMessage: 'jabbr.ui.nextMessage',
            activeRoomChanged: 'jabbr.ui.activeRoomChanged',
            scrollRoomTop: 'jabbr.ui.scrollRoomTop',
            typing: 'jabbr.ui.typing',
            sendMessage: 'jabbr.ui.sendMessage',
            focusit: 'jabbr.ui.focusit',
            blurit: 'jabbr.ui.blurit',
            preferencesChanged: 'jabbr.ui.preferencesChanged'
        },

        help: {
            shortcut: 'shortcut',
            global: 'global',
            room: 'room',
            user: 'user'
        },

        initialize: function (state) {
            $ui = $(this);
            preferences = state || {};
            $chatArea = $('#chat-area');
            $tabs = $('#tabs');
            $submitButton = $('#send');
            $newMessage = $('#new-message');
            $toast = $('#preferences .toast');
            $sound = $('#preferences .sound');
            $richness = $('#preferences .richness');
            $downloadIcon = $('#preferences .download');
            $downloadDialog = $('#download-dialog');
            $downloadDialogButton = $('#download-dialog-button');
            $downloadRange = $('#download-range');
            $help = $('#preferences .help');
            $disconnectDialog = $('#disconnect-dialog');
            $login = $('#jabbr-login');
            $helpPopup = $('#jabbr-help');
            $helpBody = $('#jabbr-help .help-body');
            $shortCutHelp = $('#jabbr-help #shortcut');
            $globalCmdHelp = $('#jabbr-help #global');
            $roomCmdHelp = $('#jabbr-help #room');
            $userCmdHelp = $('#jabbr-help #user');
            $updatePopup = $('#jabbr-update');
            focus = true;
            $lobbyRoomFilterForm = $('#users-filter-form'),
            $roomFilterInput = $('#users-filter'),
            $closedRoomFilter = $('#users-filter-closed');
            templates = {
                userlist: $('#new-userlist-template'),
                user: $('#new-user-template'),
                message: $('#new-message-template'),
                notification: $('#new-notification-template'),
                separator: $('#message-separator-template'),
                tab: $('#new-tab-template'),
                gravatarprofile: $('#gravatar-profile-template'),
                commandhelp: $('#command-help-template')
            };

            if (toast.canToast()) {
                $toast.show();
            }
            else {
                $richness.css({ left: '55px' });
                $downloadIcon.css({ left: '90px' });
                // We need to set the toast setting to false
                preferences.canToast = false;
            }

            // DOM events
            $document.on('click', 'h3.collapsible_title', function () {
                var $message = $(this).closest('.message'),
                    nearEnd = ui.isNearTheEnd();

                $(this).next().toggle(0, function () {
                    if (nearEnd) {
                        ui.scrollToBottom();
                    }
                });
            });

            $document.on('click', '#tabs li', function () {
                ui.setActiveRoom($(this).data('name'));
            });

            $document.on('click', 'li.room', function () {
                var roomName = $(this).data('name');

                navigateToRoom(roomName);

                return false;
            });

            $document.on('click', '#tabs li .close', function (ev) {
                var roomName = $(this).closest('li').data('name');

                $ui.trigger(ui.events.closeRoom, [roomName]);

                ev.preventDefault();
                return false;
            });

            // handle click on notifications
            $document.on('click', '.notification a.info', function (ev) {
                var $notification = $(this).closest('.notification');

                if ($(this).hasClass('collapse')) {
                    ui.collapseNotifications($notification);
                }
                else {
                    ui.expandNotifications($notification);
                }
            });

            // handle tab cycling - we skip the lobby when cycling
            // handle shift+/ - display help command
            $document.on('keydown', function (ev) {
                if (ev.keyCode === Keys.Tab && $newMessage.val() === "") {
                    var current = getCurrentRoomElements(),
                        index = current.tab.index(),
                        tabCount = $tabs.children().length - 1;

                    if (!ev.shiftKey) {
                        // Next tab
                        index = index % tabCount + 1;
                    } else {
                        // Prev tab
                        index = (index - 1) || tabCount;
                    }

                    ui.setActiveRoom($tabs.children().eq(index).data('name'));
                    $newMessage.focus();
                }

                if (!$newMessage.is(':focus') && ev.shiftKey && ev.keyCode === Keys.Question) {
                    ui.showHelp();
                    // Prevent the ? be recorded in the message box
                    ev.preventDefault();
                }
            });

            // hack to get Chrome to scroll back to top of help body
            // when redisplaying it after scrolling down and closing it
            $helpPopup.on('hide', function () {
                $helpBody.scrollTop(0);
            });

            // set the height of the help body when displaying the help dialog
            // so that the scroll bar does not block the rounded corners
            $helpPopup.on('show', function () {
                if (helpHeight === 0) {
                    helpHeight = $helpPopup.height() - $helpBody.position().top - 10;
                }
                $helpBody.css('height', helpHeight);
            });

            // handle click on names in chat / room list
            var prepareMessage = function (ev) {
                var message = $newMessage.val().trim();

                // If it was a message to another person, replace that
                if (message.indexOf('/msg') === 0) {
                    message = message.replace(/^\/msg \S+/, '');
                }

                // Re-focus because we lost it on the click
                $newMessage.focus();

                // Do not convert this to a message if it is a command
                if (message[0] === '/') {
                    return false;
                }

                // Prepend our target username
                message = '/msg ' + $(this).text().trim() + ' ' + message;
                ui.setMessage(message);
                return false;
            };
            $document.on('click', '.users li.user .name', prepareMessage);
            $document.on('click', '.message .left .name', prepareMessage);

            $submitButton.click(function (ev) {
                triggerSend();

                ev.preventDefault();
                return false;
            });

            $sound.click(function () {
                var room = getCurrentRoomElements();

                if (room.isLobby()) {
                    return;
                }

                $(this).toggleClass('off');

                var enabled = !$(this).hasClass('off');

                // Store the preference
                setRoomPreference(room.getName(), 'hasSound', enabled);
            });

            $richness.click(function () {
                var room = getCurrentRoomElements(),
                    $richContentMessages = room.messages.find('h3.collapsible_title');

                if (room.isLobby()) {
                    return;
                }

                $(this).toggleClass('off');

                var enabled = !$(this).hasClass('off');

                // Store the preference
                setRoomPreference(room.getName(), 'blockRichness', enabled);

                // toggle all rich-content for current room
                $richContentMessages.each(function (index) {
                    var $this = $(this),
                        isCurrentlyVisible = $this.next().is(":visible");

                    if (enabled) {
                        $this.attr('title', 'Content collapsed because you have Rich-Content disabled');
                    } else {
                        $this.removeAttr('title');
                    }

                    if (!(isCurrentlyVisible ^ enabled)) {
                        $this.trigger('click');
                    }
                });
            });

            $toast.click(function () {
                var $this = $(this),
                    enabled = !$this.hasClass('off'),
                    room = getCurrentRoomElements();

                if (room.isLobby()) {
                    return;
                }

                if (enabled) {
                    // If it's enabled toggle the preference
                    setRoomPreference(room.getName(), 'canToast', !enabled);
                    $this.toggleClass('off');
                }
                else {
                    toast.enableToast()
                    .done(function () {
                        setRoomPreference(room.getName(), 'canToast', true);
                        $this.removeClass('off');
                    })
                    .fail(function () {
                        setRoomPreference(room.getName(), 'canToast', false);
                        $this.addClass('off');
                    });
                }
            });

            $(toast).bind('toast.focus', function (ev, room) {
                window.focus();
            });

            $downloadIcon.click(function () {
                var room = getCurrentRoomElements();

                if (room.isLobby()) {
                    return; //Show a message?
                }

                if (room.isLocked()) {
                    return; //Show a message?
                }

                $downloadDialog.modal({ backdrop: true, keyboard: true });
            });

            $downloadDialogButton.click(function () {
                var room = getCurrentRoomElements();

                var url = document.location.href;
                var nav = url.indexOf('#');
                url = nav > 0 ? url.substring(0, nav) : url;
                url = url.replace('default.aspx', '');
                url += 'api/v1/messages/' +
                       encodeURI(room.getName()) +
                       '?download=true&range=' +
                       encodeURIComponent($downloadRange.val());

                $('<iframe style="display:none">').attr('src', url).appendTo(document.body);

                $downloadDialog.modal('hide');
            });
            $help.click(function () {
                ui.showHelp();
            });
            $closedRoomFilter.click(function () {
                var room = getCurrentRoomElements(),
                    show = $(this).is(':checked');

                // bounce on any room other than lobby
                if (!room.isLobby()) {
                    return false;
                }

                // hide the closed rooms from lobby list
                if (show) {
                    room.users.find('.closed').show();
                } else {
                    room.users.find('.closed').hide();
                }

                // clear the search text and update search list
                ui.$roomFilter.update();
            });

            $window.blur(function () {
                ui.focus = false;
                $ui.trigger(ui.events.blurit);
            });

            $window.focus(function () {
                // clear unread count in active room
                var room = getCurrentRoomElements();
                room.makeActive();
                triggerFocus();
            });

            $window.resize(function () {
                var room = getCurrentRoomElements();
                room.scrollToBottom();
            });

            $newMessage.keydown(function (ev) {
                var key = ev.keyCode || ev.which;
                switch (key) {
                    case Keys.Up:
                        if (cycleMessage(ui.events.prevMessage)) {
                            ev.preventDefault();
                        }
                        break;
                    case Keys.Down:
                        if (cycleMessage(ui.events.nextMessage)) {
                            ev.preventDefault();
                        }
                        break;
                    case Keys.Esc:
                        $(this).val('');
                        break;
                    case Keys.Enter:
                        triggerSend();
                        ev.preventDefault();
                        return false;
                    case Keys.Space:
                        // Check for "/r " to reply to last private message
                        if ($(this).val() === "/r" && lastPrivate) {
                            ui.setMessage("/msg " + lastPrivate);
                        }
                        break;
                }
            });

            // Returns true if a cycle was triggered
            function cycleMessage(messageHistoryDirection) {
                var currentMessage = $newMessage[0].value;
                if (currentMessage.length === 0 || lastCycledMessage === currentMessage) {
                    $ui.trigger(messageHistoryDirection);
                    return true;
                }
                return false;
            }

            // Auto-complete for user names
            $newMessage.autoTabComplete({
                prefixMatch: '[@#/:]',
                get: function (prefix) {
                    switch (prefix) {
                        case '@':
                            var room = getCurrentRoomElements();
                            // exclude current username from autocomplete
                            return room.users.find('li[data-name != "' + ui.getUserName() + '"]')
                                         .not('.room')
                                         .map(function () { return ($(this).data('name') + ' ' || "").toString(); });
                        case '#':
                            var lobby = getLobby();
                            return lobby.users.find('li')
                                         .map(function () { return $(this).data('name') + ' '; });

                        case '/':
                            var commands = ui.getCommands();
                            return ui.getCommands()
                                         .map(function (cmd) { return cmd.Name + ' '; });

                        case ':':
                            return Emoji.getIcons();
                        default:
                            return [];
                    }
                }
            });

            $newMessage.keypress(function (ev) {
                var key = ev.keyCode || ev.which;
                if ($newMessage.val()[0] === '/' || key === Keys.Slash) {
                    return;
                }
                switch (key) {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Esc:
                    case Keys.Enter:
                        break;
                    default:
                        $ui.trigger(ui.events.typing);
                        break;
                }
            });

            $newMessage.focus();

            // Make sure we can toast at all
            toast.ensureToast(preferences);

            // Load preferences
            loadPreferences();

            // Initilize liveUpdate plugin for room search
            ui.$roomFilter = $roomFilterInput.liveUpdate('#userlist-lobby', function ($theListItem) {
                if ($theListItem.hasClass('closed') && !showClosedRoomsInLobby()) {
                    return;
                }

                // show it
                $theListItem.show();
            });

            // Start cycling the messages once the document has finished loading.
            cycleMessages();
        },
        run: function () {
            $.history.init(function (hash) {
                if (hash.length && hash[0] == '/') {
                    hash = hash.substr(1);
                }

                var parts = hash.split('/');
                if (parts[0] === 'rooms') {
                    var roomName = parts[1];

                    if (ui.setActiveRoom(roomName) === false) {
                        $ui.trigger(ui.events.openRoom, [roomName]);
                    }
                }
            },
            { unescape: ',/' });
        },
        setMessage: function (value) {
            $newMessage.val(value);
            lastCycledMessage = value;
            if (value) {
                $newMessage.selectionEnd = value.length;
            }
        },
        addRoom: addRoom,
        removeRoom: removeRoom,
        setRoomOwner: function (ownerName, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(ownerName);
            $user
                .attr('data-owner', true)
                .data('owner', true);
            room.updateUserStatus($user);
        },
        clearRoomOwner: function (ownerName, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(ownerName);
            $user
                 .removeAttr('data-owner')
                 .data('owner', false);
            room.updateUserStatus($user);
        },
        setActiveRoom: function (roomName) {
            var room = getRoomElements(roomName);

            loadRoomPreferences(roomName);

            if (room.isActive()) {
                // Still trigger the event (just do less overall work)
                $ui.trigger(ui.events.activeRoomChanged, [roomName]);
                return true;
            }

            var currentRoom = getCurrentRoomElements();

            if (room.exists() && currentRoom.exists()) {
                currentRoom.makeInactive();
                triggerFocus();
                room.makeActive();

                ui.toggleMessageSection(room.isClosed());

                document.location.hash = '#/rooms/' + roomName;
                $ui.trigger(ui.events.activeRoomChanged, [roomName]);
                return true;
            }

            return false;
        },
        setRoomLocked: function (roomName) {
            var room = getRoomElements(roomName);

            room.setLocked();
        },
        setRoomClosed: function (roomName) {
            var room = getRoomElements(roomName);

            room.close();
        },
        updateLobbyRoomCount: updateLobbyRoomCount,
        updateUnread: function (roomName, isMentioned) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();

            if (ui.hasFocus() && room.isActive()) {
                return;
            }

            room.updateUnread(isMentioned);
        },
        scrollToBottom: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();

            if (room.isActive()) {
                room.scrollToBottom();
            }
        },
        watchMessageScroll: function (messageIds, roomName) {
            // Given an array of message ids, if there is any embedded content
            // in it, it may cause the window to scroll off of the bottom, so we
            // can watch for that and correct it.
            messageIds = $.map(messageIds, function (id) { return '#m-' + id; });

            var $messages = $(messageIds.join(',')),
                $content = $messages.expandableContent(),
                room = getRoomElements(roomName),
                nearTheEndBefore = room.messages.isNearTheEnd(),
                scrollTopBefore = room.messages.scrollTop();

            if (nearTheEndBefore && $content.length > 0) {
                // Note that the load event does not bubble, so .on() is not
                // suitable here.
                $content.load(function (event) {
                    // If we used to be at the end and our scrollTop() did not
                    // change, then we can safely call scrollToBottom() without
                    // worrying about interrupting the user. We skip this if the
                    // room is already at the end in the event of multiple
                    // images loading at the same time.
                    if (!room.messages.isNearTheEnd() && scrollTopBefore === room.messages.scrollTop()) {
                        room.scrollToBottom();
                        // Reset our scrollTopBefore so we know we are allowed
                        // to move it again if another image loads and the user
                        // hasn't touched it
                        scrollTopBefore = room.messages.scrollTop();
                    }

                    // unbind the event from this object after it executes
                    $(this).unbind(event);
                });
            }
        },
        isNearTheEnd: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();

            return room.isNearTheEnd();
        },
        populateLobbyRooms: function (rooms) {
            var lobby = getLobby(),
                showClosedRooms = $closedRoomFilter.is(':checked'),
            // sort lobby by room open ascending then count descending
                sorted = rooms.sort(function (a, b) {
                    if (a.Closed && !b.Closed) {
                        return 1;
                    } else if (b.Closed && !a.Closed) {
                        return -1;
                    }

                    return a.Count > b.Count ? -1 : 1;
                });

            lobby.users.empty();

            $.each(sorted, function () {
                var $name = $('<span/>').addClass('name')
                                        .html(this.Name),
                    $count = $('<span/>').addClass('count')
                                         .html(' (' + this.Count + ')')
                                         .data('count', this.Count),
                    $locked = $('<span/>').addClass('lock'),
                    $readonly = $('<span/>').addClass('readonly'),
                    $li = $('<li/>').addClass('room')
                          .attr('data-room', this.Name)
                          .data('name', this.Name)
                          .append($locked)
                          .append($readonly)
                          .append($name)
                          .append($count)
                          .appendTo(lobby.users);

                if (this.Private) {
                    $li.addClass('locked');
                }
                if (this.Closed) {
                    $li.addClass('closed');
                    if (!showClosedRooms) {
                        $li.hide();
                    }
                }
            });

            if (lobby.isActive()) {
                // update cache of room names
                $lobbyRoomFilterForm.show();
            }

            ui.$roomFilter.update();
            $roomFilterInput.val('');
        },
        addUser: function (userViewModel, roomName) {
            var room = getRoomElements(roomName),
                $user = null;

            // Remove all users that are being removed
            room.users.find('.removing').remove();

            // Get the user element
            $user = room.getUser(userViewModel.name);

            if ($user.length) {
                return false;
            }

            $user = templates.user.tmpl(userViewModel);
            $user.data('inroom', roomName);
            $user.data('owner', userViewModel.owner);
            $user.data('admin', userViewModel.admin);

            room.addUser(userViewModel, $user);
            updateNote(userViewModel, $user);
            updateFlag(userViewModel, $user);

            return true;
        },
        setUserActivity: function (userViewModel) {
            var $user = $('.users').find(getUserClassName(userViewModel.name)),
                active = $user.data('active');

            var fadeSpeed = 'slow';
            // If the states match it means they're not changing and the user
            // is joining a room. In that case, set the fade time to be 1ms.
            if (userViewModel.active === active) {
                fadeSpeed = 1;
            }

            if (userViewModel.active === true) {
                $user.fadeTo(fadeSpeed, 1, function () {
                    $user.removeClass('idle');
                });
            } else {
                $user.fadeTo(fadeSpeed, 0.5, function () {
                    $user.addClass('idle');
                });
            }

            updateNote(userViewModel, $user);
        },
        setUserActive: function ($user) {
            if ($user.data('active') === true) {
                return false;
            }
            $user.attr('data-active', true);
            $user.data('active', true);
            return true;
        },
        setUserInActive: function ($user) {
            if ($user.data('active') === false) {
                return false;
            }
            $user.attr('data-active', false);
            $user.data('active', false);
            return true;
        },
        changeUserName: function (oldName, user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUserReferences(oldName);

            // Update the user's name
            $user.find('.name').fadeOut('normal', function () {
                $(this).html(user.Name);
                $(this).fadeIn('normal');
            });
            $user.data('name', user.Name);
            $user.attr('data-name', user.Name);
            room.sortLists();
        },
        changeGravatar: function (user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUserReferences(user.Name),
                src = 'https://secure.gravatar.com/avatar/' + user.Hash + '?s=16&d=mm';

            $user.find('.gravatar')
                 .attr('src', src);
        },
        showGravatarProfile: function (profile) {
            var room = getCurrentRoomElements(),
                nearEnd = ui.isNearTheEnd();

            this.appendMessage(templates.gravatarprofile.tmpl(profile), room);
            if (nearEnd) {
                ui.scrollToBottom();
            }
        },
        removeUser: function (user, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(user.Name);

            $user.addClass('removing')
                .fadeOut('slow', function () {
                    var owner = $user.data('owner') || false;
                    $(this).remove();

                    if (owner === true) {
                        room.setListState(room.owners);
                    }
                });
        },
        setUserTyping: function (userViewModel, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(userViewModel.name),
                timeout = null;

            // if the user is somehow missing from room, add them
            if ($user.length === 0) {
                ui.addUser(userViewModel, roomName);
            }

            // Do not show typing indicator for current user
            if (userViewModel.name === ui.getUserName()) {
                return;
            }

            // Mark the user as typing
            $user.addClass('typing');
            var oldTimeout = $user.data('typing');

            if (oldTimeout) {
                clearTimeout(oldTimeout);
            }

            timeout = window.setTimeout(function () {
                $user.removeClass('typing');
            },
            3000);

            $user.data('typing', timeout);
        },
        prependChatMessages: function (messages, roomName) {
            var room = getRoomElements(roomName),
                $messages = room.messages,
                $target = $messages.children().first(),
                $previousMessage = null,
                $current = null,
                previousUser = null,
                previousTimestamp = new Date().addDays(1); // Tomorrow so we always see a date line

            if (messages.length === 0) {
                // Mark this list as full
                $messages.data('full', true);
                return;
            }

            // If our top message is a date header, it might be incorrect, so we
            // check to see if we should remove it so that it can be inserted
            // again at a more appropriate time.
            if ($target.is('.list-header.date-header')) {
                var postedDate = new Date($target.text()).toDate();
                var lastPrependDate = messages[messages.length - 1].date.toDate();

                if (!lastPrependDate.diffDays(postedDate)) {
                    $target.remove();
                    $target = $messages.children().first();
                }
            }

            // Populate the old messages
            $.each(messages, function (index) {
                processMessage(this, roomName);

                if ($previousMessage) {
                    previousUser = $previousMessage.data('name');
                    previousTimestamp = new Date($previousMessage.data('timestamp') || new Date());
                }

                if (this.date.toDate().diffDays(previousTimestamp.toDate())) {
                    ui.addMessageBeforeTarget(this.date.toLocaleDateString(), 'list-header', $target)
                      .addClass('date-header')
                      .find('.right').remove(); // remove timestamp on date indicator

                    // Force a user name to show after the header
                    previousUser = null;
                }

                // Determine if we need to show the user
                this.showUser = !previousUser || previousUser !== this.name;

                // Render the new message
                $target.before(templates.message.tmpl(this));

                if (this.showUser === false) {
                    $previousMessage.addClass('continue');
                }

                $previousMessage = $('#m-' + this.id);
            });

            // If our old top message is a message from the same user as the
            // last message in our prepended history, we can remove information
            // and continue
            if ($target.is('.message') && $target.data('name') === $previousMessage.data('name')) {
                $target.find('.left').children().not('.state').remove();
                $previousMessage.addClass('continue');
            }

            // Scroll to the bottom element so the user sees there's more messages
            $target[0].scrollIntoView();
        },
        addChatMessage: function (message, roomName) {
            var room = getRoomElements(roomName),
                $previousMessage = room.messages.children().last(),
                previousUser = null,
                previousTimestamp = new Date().addDays(1), // Tomorrow so we always see a date line
                showUserName = true,
                $message = null,
                isMention = message.highlight;

            // bounce out of here if the room is closed
            if (room.isClosed()) {
                return;
            }

            if ($previousMessage.length > 0) {
                previousUser = $previousMessage.data('name');
                previousTimestamp = new Date($previousMessage.data('timestamp') || new Date());
            }

            // Force a user name to show if a header will be displayed
            if (message.date.toDate().diffDays(previousTimestamp.toDate())) {
                previousUser = null;
            }

            // Determine if we need to show the user name next to the message
            showUserName = previousUser !== message.name;
            message.showUser = showUserName;

            processMessage(message, roomName);

            if (showUserName === false) {
                $previousMessage.addClass('continue');
            }

            // check to see if room needs a separator
            if (room.needsSeparator()) {
                // if there's an existing separator, remove it
                if (room.hasSeparator()) {
                    room.removeSeparator();
                }
                room.addSeparator();
            }

            this.appendMessage(templates.message.tmpl(message), room);

            if (room.isInitialized()) {
                if (isMention) {
                    // Always do sound notification for mentions if any room as sound enabled
                    if (anyRoomPreference('hasSound') === true) {
                        ui.notify(true);
                    }

                    if (ui.focus === false && anyRoomPreference('canToast') === true) {
                        // Only toast if there's no focus (even on mentions)
                        ui.toast(message, true, roomName);
                    }
                }
                else {
                    // Only toast if chat isn't focused
                    if (ui.focus === false) {
                        ui.notifyRoom(roomName);
                        ui.toastRoom(roomName, message);
                    }
                }
            }
        },
        replaceMessage: function (message) {
            processMessage(message);

            $('#m-' + message.id).find('.middle')
                                 .html(message.message);
        },
        messageExists: function (id) {
            return $('#m-' + id).length > 0;
        },
        addChatMessageContent: function (id, content, roomName) {
            var $message = $('#m-' + id);

            if (shouldCollapseContent(content, roomName)) {
                content = collapseRichContent(content);
            }

            $message.find('.middle')
                    .append(content);
        },
        addPrivateMessage: function (content, type) {
            var rooms = getAllRoomElements();
            for (var r in rooms) {
                if (rooms[r].getName() !== undefined && rooms[r].isClosed() === false) {
                    this.addMessage(content, type, rooms[r].getName());
                }
            }
        },
        prepareNotificationMessage: function (content, type) {
            var now = new Date(),
                message = {
                    message: utility.parseEmojis(content),
                    type: type,
                    date: now,
                    when: now.formatTime(true),
                    fulldate: now.toLocaleString()
                };

            return templates.notification.tmpl(message);
        },
        addMessageBeforeTarget: function (content, type, $target) {
            var $element = null;

            $element = ui.prepareNotificationMessage(content, type);

            $target.before($element);

            return $element;
        },
        addMessage: function (content, type, roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements(),
                nearEnd = room.isNearTheEnd(),
                $element = null;

            $element = ui.prepareNotificationMessage(content, type);

            this.appendMessage($element, room);

            if (type === 'notification' && room.isLobby() === false) {
                ui.collapseNotifications($element);
            }

            if (nearEnd) {
                ui.scrollToBottom(roomName);
            }

            return $element;
        },
        appendMessage: function (newMessage, room) {
            // Determine if we need to show a new date header: Two conditions
            // for instantly skipping are if this message is a date header, or
            // if the room only contains non-chat messages and we're adding a
            // non-chat message.
            var isMessage = $(newMessage).is('.message');
            if (!$(newMessage).is('.date-header') && (isMessage || room.hasMessages())) {
                var lastMessage = room.messages.find('li[data-timestamp]').last(),
                    lastDate = new Date(lastMessage.data('timestamp')),
                    thisDate = new Date($(newMessage).data('timestamp'));

                if (!lastMessage.length || thisDate.toDate().diffDays(lastDate.toDate())) {
                    ui.addMessage(thisDate.toLocaleDateString(), 'date-header list-header', room.getName())
                      .find('.right').remove(); // remove timestamp on date indicator
                }
            }

            if (isMessage) {
                room.updateMessages(true);
            }

            $(newMessage).appendTo(room.messages);
        },
        hasFocus: function () {
            return ui.focus;
        },
        getShortcuts: function () {
            return ui.shortcuts;
        },
        setShortcuts: function (shortcuts) {
            ui.shortcuts = shortcuts;
        },
        getCommands: function () {
            return ui.commands;
        },
        setCommands: function (commands) {
            ui.commands = commands;
        },
        setInitialized: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();
            room.setInitialized();
        },
        collapseNotifications: function ($notification) {
            // collapse multiple notifications
            var $notifications = $notification.prevUntil(':not(.notification)');
            if ($notifications.length > 3) {
                $notifications
                    .hide()
                    .find('.info').text('');    // clear any prior text
                $notification.find('.info')
                    .text(' (plus ' + $notifications.length + ' hidden... click to expand)')
                    .removeClass('collapse');
            }
        },
        expandNotifications: function ($notification) {
            // expand collapsed notifications
            var $notifications = $notification.prevUntil(':not(.notification)'),
                topBefore = $notification.position().top;

            $notification.find('.info')
                .text(' (click to collapse)')
                .addClass('collapse');
            $notifications.show();

            var room = getCurrentRoomElements(),
                topAfter = $notification.position().top,
                scrollTop = room.messages.scrollTop();

            // make sure last notification is visible
            room.messages.scrollTop(scrollTop + topAfter - topBefore + $notification.height());
        },
        getState: function () {
            return preferences;
        },
        notifyRoom: function (roomName) {
            if (getRoomPreference(roomName, 'hasSound') === true) {
                $('#noftificationSound')[0].play();
            }
        },
        toastRoom: function (roomName, message) {
            if (getRoomPreference(roomName, 'canToast') === true) {
                toast.toastMessage(message, roomName);
            }
        },
        notify: function (force) {
            if (getActiveRoomPreference('hasSound') === true || force) {
                $('#noftificationSound')[0].play();
            }
        },
        toast: function (message, force, roomName) {
            if (getActiveRoomPreference('canToast') === true || force) {
                toast.toastMessage(message, roomName);
            }
        },
        setUserName: function (name) {
            ui.name = name;
        },
        getUserName: function () {
            return ui.name;
        },
        showLogin: function () {
            if (typeof (janrain) !== 'undefined') {
                if (janrain.ready === false) {
                    window.setTimeout(function () {
                        $login.modal({ backdrop: true, keyboard: true });
                    }, 1000);
                }
                else {
                    $login.modal({ backdrop: true, keyboard: true });
                }

                return true;
            }

            return false;
        },
        showDisconnectUI: function () {
            $disconnectDialog.modal();
        },
        showHelp: function () {
            $shortCutHelp.empty();
            $globalCmdHelp.empty();
            $roomCmdHelp.empty();
            $userCmdHelp.empty();
            $.each(ui.getCommands(), function () {
                switch (this.Group) {
                    case ui.help.shortcut:
                        $shortCutHelp.append(templates.commandhelp.tmpl(this));
                        break;
                    case ui.help.global:
                        $globalCmdHelp.append(templates.commandhelp.tmpl(this));
                        break;
                    case ui.help.room:
                        $roomCmdHelp.append(templates.commandhelp.tmpl(this));
                        break;
                    case ui.help.user:
                        $userCmdHelp.append(templates.commandhelp.tmpl(this));
                        break;
                }
            });
            $.each(ui.getShortcuts(), function () {
                $shortCutHelp.append(templates.commandhelp.tmpl(this));
            });
            $helpPopup.modal();
        },
        showUpdateUI: function () {
            $updatePopup.modal();

            window.setTimeout(function () {
                // Reload the page
                document.location = document.location.pathname;
            },
            updateTimeout);
        },
        changeNote: function (userViewModel, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(userViewModel.name);

            updateNote(userViewModel, $user);
        },
        changeFlag: function (userViewModel, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(userViewModel.name);

            updateFlag(userViewModel, $user);
        },
        changeRoomTopic: function (roomViewModel) {
            updateRoomTopic(roomViewModel);
        },
        confirmMessage: function (id) {
            $('#m-' + id).removeClass('failed')
                         .removeClass('loading');
        },
        failMessage: function (id) {
            $('#m-' + id).removeClass('loading')
                         .addClass('failed');
        },
        markMessagePending: function (id) {
            var $message = $('#m-' + id);

            if ($message.hasClass('failed') === false) {
                $message.addClass('loading');
            }
        },
        setRoomAdmin: function (adminName, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(adminName);
            $user
                .attr('data-admin', true)
                .data('admin', true)
                .find('.admin')
                .text('(admin)');
            room.updateUserStatus($user);
        },
        clearRoomAdmin: function (adminName, roomName) {
            var room = getRoomElements(roomName),
                $user = room.getUser(adminName);
            $user
                 .removeAttr('data-admin')
                 .data('admin', false)
                 .find('.admin')
                 .text('');
            room.updateUserStatus($user);
        },
        setLastPrivate: function (userName) {
            lastPrivate = userName;
        },
        shouldCollapseContent: shouldCollapseContent,
        collapseRichContent: collapseRichContent,
        toggleMessageSection: function (disabledIt) {
            if (disabledIt) {
                // disable button and textarea
                $newMessage.attr('disabled', 'disabled');
                $submitButton.attr('disabled', 'disabled');

            } else {
                // re-enable textarea button
                $newMessage.attr('disabled', '');
                $newMessage.removeAttr('disabled');

                // re-enable submit button
                $submitButton.attr('disabled', '');
                $submitButton.removeAttr('disabled');
            }
        },
        closeRoom: function (roomName) {
            var room = getRoomElements(roomName);

            room.close();
        },
        unCloseRoom: function (roomName) {
            var room = getRoomElements(roomName);

            room.unClose();
        },
        setRoomListStatuses: function (roomName) {
            var room = roomName ? getRoomElements(roomName) : getCurrentRoomElements();
            room.setListState(room.owners);
        }
    };

    if (!window.chat) {
        window.chat = {};
    }
    window.chat.ui = ui;
})(jQuery, window, window.document, window.chat.utility);
