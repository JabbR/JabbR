/// <reference path="Scripts/jquery-1.7.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />
/// <reference path="Chat.toast.js" />
/// <reference path="Scripts/livestamp.min.js" />

/*jshint bitwise:false */
(function ($, window, document, utility, emoji, linkify) {
    "use strict";

    var $chatArea = null,
        $tabs = null,
        $submitButton = null,
        $newMessage = null,
        $roomActions = null,
        $toast = null,
        $disconnectDialog = null,
        $downloadIcon = null,
        $downloadDialog = null,
        $downloadDialogButton = null,
        $downloadRange = null,
        $logout = null,
        $help = null,
        $ui = null,
        $sound = null,
        templates = null,
        focus = true,
        readOnly = false,
        Keys = { Up: 38, Down: 40, Esc: 27, Enter: 13, Slash: 47, Space: 32, Tab: 9, Question: 191 },
        scrollTopThreshold = 75,
        toast = window.chat.toast,
        preferences = null,
        $login = null,
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
        lobbyLoaded = false,
        $roomFilterInput = null,
        $closedRoomFilter = null,
        updateTimeout = 15000,
        $richness = null,
        lastPrivate = null,
        roomCache = {},
        $reloadMessageNotification = null,
        popoverTimer = null,
        $connectionStatus = null,
        connectionState = -1,
        $connectionStateChangedPopover = null,
        connectionStateIcon = null,
        $connectionInfoPopover = null,
        $connectionInfoContent = null,
        $fileUploadButton = null,
        $hiddenFile = null,
        $uploadForm = null,
        $fileRoom = null,
        $fileConnectionId = null,
        connectionInfoStatus = null,
        connectionInfoTransport = null,
        $topicBar = null,
        $loadingHistoryIndicator = null,
        trimRoomHistoryMaxMessages = 200,
        trimRoomHistoryFrequency = 1000 * 60 * 2, // 2 minutes in ms
        $loadMoreRooms = null,
        sortedRoomList = null,
        maxRoomsToLoad = 100,
        lastLoadedRoomIndex = 0,
        $lobbyPrivateRooms = null,
        $lobbyOtherRooms = null,
        $roomLoadingIndicator = null,
        roomLoadingDelay = 250,
        roomLoadingTimeout = null;

    function getRoomNameFromHash(hash) {
        if (hash.length && hash[0] === '/') {
            hash = hash.substr(1);
        }

        var parts = hash.split('/');
        if (parts[0] === 'rooms') {
            return parts[1];
        }

        return null;
    }

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

    function Room($tab, $usersContainer, $usersOwners, $usersActive, $messages, $roomTopic) {
        this.tab = $tab;
        this.users = $usersContainer;
        this.owners = $usersOwners;
        this.activeUsers = $usersActive;
        this.messages = $messages;
        this.roomTopic = $roomTopic;

        function glowTab(n) {
            // Stop if we're not unread anymore
            if (!$tab.hasClass('unread')) {
                return;
            }

            // Go light
            $tab.animate({ backgroundColor: '#e5e5e5', color: '#77d42a' }, 800, function () {
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
                $roomActions.show();
            }
        };

        this.makeActive = function () {
            var currUnread = this.getUnread(),
                lastUnread = this.messages.find('.message-separator').data('unread') || 0;

            if (!utility.isMobile && !readOnly) {
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
                $roomActions.hide();
                $lobbyRoomFilterForm.show();

                $messages.hide();
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
            if (userViewModel.owner) {
                this.addUserToList($user, this.owners);
            } else {
                this.changeIdle($user, userViewModel.active);

                this.addUserToList($user, this.activeUsers);

            }
        };

        this.changeIdle = function ($user, isActive) {
            if (isActive) {
                $user.removeClass('idle');
            } else {
                $user.addClass('idle');
            }
        };

        this.addUserToList = function ($user, list) {
            var oldParentList = $user.parent('ul');
            $user.appendTo(list);
            this.setListState(list);
            if (oldParentList.length > 0) {
                this.setListState(oldParentList);
            }
            this.sortList(list);
        };

        this.appearsInList = function ($user, list) {
            return $user.parent('ul').attr('id') === list.attr('id');
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

            if (!this.appearsInList($user, this.activeUsers)) {
                this.changeIdle($user, status);
                this.addUserToList($user, this.activeUsers);
            }
        };

        this.sortUsersByName = function (userListToSort) {
            return userListToSort.sort(function (a, b) {
                var compA = $(a).data('name').toString().toUpperCase();
                var compB = $(b).data('name').toString().toUpperCase();
                return (compA < compB) ? -1 : (compA > compB) ? 1 : 0;
            });
        };

        this.sortLists = function () {
            this.sortList(this.owners);
            this.sortList(this.activeUsers);
        };

        this.sortList = function (listToSort) {
            var listItems = listToSort.children('li').get();

            var activeUsers = [],
                idleUsers = [],
                sortedUsers = [];

            $.each(listItems, function (index, item) {
                if ($(item).data('active')) {
                    activeUsers.push(item);
                } else {
                    idleUsers.push(item);
                }
            });

            activeUsers = this.sortUsersByName(activeUsers);
            idleUsers = this.sortUsersByName(idleUsers);

            sortedUsers = activeUsers.concat(idleUsers);

            $.each(sortedUsers, function (index, item) {
                listToSort.append(item);
            });
        };

        this.canTrimHistory = function () {
            return this.tab.data('trimmable') !== false;
        };

        this.setTrimmable = function (canTrimMessages) {
            this.tab.data('trimmable', canTrimMessages);
        };

        this.trimHistory = function (numberOfMessagesToKeep) {
            var lastIndex = null,
                $messagesToRemove = null,
                $roomMessages = this.messages.find('li'),
                messageCount = $roomMessages.length;

            numberOfMessagesToKeep = numberOfMessagesToKeep || trimRoomHistoryMaxMessages;

            if (this.isLobby() || !this.canTrimHistory()) {
                return;
            }

            if (numberOfMessagesToKeep < trimRoomHistoryMaxMessages) {
                numberOfMessagesToKeep = trimRoomHistoryMaxMessages;
            }

            if (messageCount < numberOfMessagesToKeep) {
                return;
            }

            lastIndex = messageCount - numberOfMessagesToKeep;
            $messagesToRemove = $roomMessages.filter('li:lt(' + lastIndex + ')');

            $messagesToRemove.remove();
        };
    }

    function setRoomLoading(isLoading, roomName) {
        if (isLoading) {
            var room = getRoomElements(roomName);
            if (!room.isInitialized()) {
                roomLoadingTimeout = window.setTimeout(function () {
                    $roomLoadingIndicator.find('i').addClass('icon-spin');
                    $roomLoadingIndicator.show();
                }, roomLoadingDelay);
            }
        } else {
            if (roomLoadingTimeout) {
                clearTimeout(roomLoadingDelay);
            }
            $roomLoadingIndicator.hide();
            $roomLoadingIndicator.find('i').removeClass('icon-spin');
        }
    }

    function populateLobbyRoomList(item, template, listToPopulate, showClosedRooms) {
        $.tmpl(template, item).appendTo(listToPopulate);

        if (!showClosedRooms) {
            var closedRooms = listToPopulate.children('li.closed');
            closedRooms.each(function () {
                $(this).hide();
            });
        }
    }

    function sortRoomList(listToSort) {
        var sortedList = listToSort.sort(function (a, b) {
            if (a.Closed && !b.Closed) {
                return 1;
            } else if (b.Closed && !a.Closed) {
                return -1;
            }

            if (a.Count > b.Count) {
                return -1;
            } else if (b.Count > a.Count) {
                return 1;
            }
            var compA = a.Name.toString().toUpperCase();
            var compB = b.Name.toString().toUpperCase();
            return (compA < compB) ? -1 : (compA > compB) ? 1 : 0;
        });
        return sortedList;
    }

    function getRoomElements(roomName) {
        var roomId = getRoomId(roomName);
        var room = new Room($('#tabs-' + roomId),
                        $('#userlist-' + roomId),
                        $('#userlist-' + roomId + '-owners'),
                        $('#userlist-' + roomId + '-active'),
                        $('#messages-' + roomId),
                        $('#roomTopic-' + roomId));
        return room;
    }

    function getCurrentRoomElements() {
        var $tab = $tabs.find('li.current');
        var room;
        if ($tab.data('name') === 'Lobby') {
            room = new Room($tab,
                $('#userlist-lobby'),
                $('#userlist-lobby-owners'),
                $('#userlist-lobby-active'),
                $('.messages.current'),
                $('.roomTopic.current'));
        } else {
            room = new Room($tab,
                $('.users.current'),
                $('.userlist.current .owners'),
                $('.userlist.current .active'),
                $('.messages.current'),
                $('.roomTopic.current'));
        }
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

    function getRoomsNames() {
        var lobby = getLobby();

        return lobby.users.find('li')
                     .map(function () {
                         var room = $(this).data('name');
                         roomCache[room] = true;
                         return room + ' ';
                     });
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

        roomCache[roomName.toLowerCase()] = true;

        templates.tab.tmpl(viewModel).data('name', roomName).appendTo($tabs);

        $messages = $('<ul/>').attr('id', 'messages-' + roomId)
                              .addClass('messages')
                              .appendTo($chatArea)
                              .hide();

        $roomTopic = $('<div/>').attr('id', 'roomTopic-' + roomId)
                              .addClass('roomTopic')
                              .appendTo($topicBar)
                              .hide();

        if (roomName !== "lobby") {
            userContainer = $('<div/>').attr('id', 'userlist-' + roomId)
                .addClass('users')
                .appendTo($chatArea).hide();
            templates.userlist.tmpl({ listname: '- Room Owners', id: 'userlist-' + roomId + '-owners' })
                .addClass('owners')
                .appendTo(userContainer);
            templates.userlist.tmpl({ listname: '- Users', id: 'userlist-' + roomId + '-active' })
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
                    $ui.trigger(ui.events.scrollRoomTop, [{ name: roomName, messageId: messageId }]);
                }
            }
        };

        // Hookup the scroll handler since event delegation doesn't work with scroll events
        $messages.bind('scroll', scrollHandler);

        // Store the scroll handler so we can remove it later
        $messages.data('scrollHandler', scrollHandler);

        setAccessKeys();
        lobbyLoaded = false;
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
            room.roomTopic.remove();
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
        var hash = (document.location.hash || '#').substr(1),
            hashRoomName = getRoomNameFromHash(hash);

        if (hashRoomName && hashRoomName === roomName) {
            ui.setActiveRoomCore(roomName);
        }
        else {
            document.location.hash = '#/rooms/' + roomName;
        }
    }

    function processMessage(message, roomName) {
        var isFromCollapibleContentProvider = isFromCollapsibleContentProvider(message.message),
            collapseContent = shouldCollapseContent(message.message, roomName);

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
        if (focus === false) {
            focus = true;
            $ui.trigger(ui.events.focusit);
        }
    }

    function loadPreferences() {
        // Restore the global preferences
    }

    function toggleRichness($element, roomName) {
        var blockRichness = roomName ? getRoomPreference(roomName, 'blockRichness') : preferences.blockRichness;

        if (blockRichness === true) {
            $element.addClass('off');
        }
        else {
            $element.removeClass('off');
        }
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
        toggleRichness($richness, roomName);
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
        if (readOnly) {
            return;
        }

        var msg = $.trim($newMessage.val());

        focus = true;

        if (msg) {
            if (msg.toLowerCase() === '/login') {
                ui.showLogin();
            }
            else {
                $ui.trigger(ui.events.sendMessage, [msg]);
            }
        }

        $newMessage.val('');
        $newMessage.focus();

        // always scroll to bottom after new message sent
        var room = getCurrentRoomElements();
        room.scrollToBottom();
        room.removeSeparator();
    }

    function updateNote(userViewModel, $user) {
        var $title = $user.find('.name'),
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
        $title.removeAttr('title');

        if (userViewModel.note) {
            $title.attr('title', noteTextEncoded);
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

        $flag.removeAttr('class');
        $flag.addClass('flag');
        $flag.removeAttr('title');

        if (userViewModel.flagClass) {
            $flag.addClass(userViewModel.flagClass);
            $flag.show();
        } else {
            $flag.hide();
        }

        if (userViewModel.country) {
            $flag.attr('title', userViewModel.country);
        }
    }

    function updateRoomTopic(roomViewModel) {
        var room = getRoomElements(roomViewModel.Name);
        var topic = roomViewModel.Topic;
        var topicHtml = topic === '' ? 'You\'re chatting in ' + roomViewModel.Name : ui.processContent(topic);
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

    function getConnectionStateChangedPopoverOptions(statusText) {
        var options = {
            html: true,
            trigger: 'hover',
            template: $connectionStateChangedPopover,
            content: function () {
                return statusText;
            }
        };
        return options;
    }

    function getConnectionInfoPopoverOptions(transport) {
        var options = {
            html: true,
            trigger: 'hover',
            delay: {
                show: 0,
                hide: 500
            },
            template: $connectionInfoPopover,
            content: function () {
                var connectionInfo = $connectionInfoContent;
                connectionInfo.find(connectionInfoStatus).text('Status: Connected');
                connectionInfo.find(connectionInfoTransport).text('Transport: ' + transport);
                return connectionInfo.html();
            }
        };
        return options;
    }

    function loadMoreLobbyRooms() {
        var lobby = getLobby(),
            showClosedRooms = $closedRoomFilter.is(':checked'),
            moreRooms = sortedRoomList.slice(lastLoadedRoomIndex, lastLoadedRoomIndex + maxRoomsToLoad);

        populateLobbyRoomList(moreRooms, templates.lobbyroom, lobby.users, showClosedRooms);
        lastLoadedRoomIndex = lastLoadedRoomIndex + maxRoomsToLoad;
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
            preferencesChanged: 'jabbr.ui.preferencesChanged',
            loggedOut: 'jabbr.ui.loggedOut',
            reloadMessages: 'jabbr.ui.reloadMessages',
            fileUploaded: 'jabbr.ui.fileUploaded'
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
            $roomActions = $('#room-actions');
            $toast = $('#room-preferences .toast');
            $sound = $('#room-preferences .sound');
            $richness = $('#room-preferences .richness');
            $downloadIcon = $('#room-preferences .download');
            $downloadDialog = $('#download-dialog');
            $downloadDialogButton = $('#download-dialog-button');
            $downloadRange = $('#download-range');
            $logout = $('#preferences .logout');
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
                commandhelp: $('#command-help-template'),
                multiline: $('#multiline-content-template'),
                lobbyroom: $('#new-lobby-room-template'),
                otherlobbyroom: $('#new-other-lobby-room-template')
            };
            $reloadMessageNotification = $('#reloadMessageNotification');
            $fileUploadButton = $('.upload-button');
            $hiddenFile = $('#hidden-file');
            $uploadForm = $('#upload');
            $fileRoom = $('#file-room');
            $fileConnectionId = $('#file-connection-id');
            $connectionStatus = $('#connectionStatus');

            $connectionStateChangedPopover = $('#connection-state-changed-popover');
            connectionStateIcon = '#popover-content-icon';
            $connectionInfoPopover = $('#connection-info-popover');
            $connectionInfoContent = $('#connection-info-content');
            connectionInfoStatus = '#connection-status';
            connectionInfoTransport = '#connection-transport';
            $topicBar = $('#topic-bar');
            $loadingHistoryIndicator = $('#loadingRoomHistory');

            $loadMoreRooms = $('#load-more-rooms-item');
            $lobbyPrivateRooms = $('#lobby-private');
            $lobbyOtherRooms = $('#lobby-other');
            $roomLoadingIndicator = $('#room-loading');

            if (toast.canToast()) {
                $toast.show();
            }
            else {
                $richness.css({ left: '55px' });
                $downloadIcon.css({ left: '90px' });
                // We need to set the toast setting to false
                preferences.canToast = false;
                $toast.hide();
            }

            // DOM events
            $document.on('click', 'h3.collapsible_title', function () {
                var nearEnd = ui.isNearTheEnd();

                $(this).next().toggle(0, function () {
                    if (nearEnd) {
                        ui.scrollToBottom();
                    }
                });
            });

            $document.on('click', '#tabs li', function () {
                ui.setActiveRoom($(this).data('name'));
            });

            $document.on('click', 'li.room .room-row', function () {
                var roomName = $(this).parent().data('name'),
                    room = getRoomElements(roomName);

                if (room.exists()) {
                    ui.setActiveRoom(roomName);
                }
                else {
                    $ui.trigger(ui.events.openRoom, [roomName]);
                }
            });

            $document.on('click', '#load-more-rooms-item', function () {
                var spinner = $loadMoreRooms.find('i'),
                    lobby = getLobby();
                spinner.addClass('icon-spin');
                spinner.show();
                var loader = $loadMoreRooms.find('.load-more-rooms a');
                loader.html(' Loading more rooms...');
                loadMoreLobbyRooms();
                spinner.hide();
                spinner.removeClass('icon-spin');
                loader.html('Load More...');
                if (lastLoadedRoomIndex < sortedRoomList.length) {
                    $loadMoreRooms.appendTo(lobby.users);
                } else {
                    $loadMoreRooms.hide();
                }
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

            $document.on('click', '#reloadMessageNotification a', function () {
                $ui.trigger(ui.events.reloadMessages);
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
                    if (!readOnly) {
                        $newMessage.focus();
                    }
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
                if (readOnly) {
                    return false;
                }

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
                message = '@' + $(this).text().trim() + ' ' + message;
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
                setRoomPreference(room.getName(), 'blockRichness', !enabled);

                // toggle all rich-content for current room
                $richContentMessages.each(function (index) {
                    var $this = $(this),
                        isCurrentlyVisible = $this.next().is(":visible");

                    if (enabled) {
                        $this.attr('title', 'Content collapsed because you have Rich-Content disabled');
                    } else {
                        $this.removeAttr('title');
                    }

                    if (isCurrentlyVisible ^ enabled) {
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

            $logout.click(function () {
                $ui.trigger(ui.events.loggedOut);
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
                focus = false;
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
                            return getRoomsNames();

                        case '/':
                            return ui.getCommands()
                                         .map(function (cmd) { return cmd.Name + ' '; });

                        case ':':
                            return emoji.getIcons();
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

            if (!readOnly) {
                $newMessage.focus();
            }

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

            // Crazy browser hack
            $hiddenFile[0].style.left = '-800px';

            $hiddenFile.change(function () {
                if (!$hiddenFile.val()) {
                    return;
                }

                var path = $hiddenFile.val(),
                    slash = path.lastIndexOf('\\'),
                    name = path.substring(slash + 1),
                    uploader = {
                        submitFile: function (connectionId, room) {
                            $fileConnectionId.val(connectionId);

                            $fileRoom.val(room);

                            $uploadForm.submit();

                            $hiddenFile.val('');
                        }
                    };

                ui.addMessage('Uploading \'' + name + '\'.', 'broadcast');

                $ui.trigger(ui.events.fileUploaded, [uploader]);
            });

            setInterval(function () {
                ui.trimRoomMessageHistory();
            }, trimRoomHistoryFrequency);
        },
        run: function () {
            $.history.init(function (hash) {
                var roomName = getRoomNameFromHash(hash);

                if (roomName) {
                    if (ui.setActiveRoomCore(roomName) === false && roomName !== 'Lobby') {
                        $ui.trigger(ui.events.openRoom, [roomName]);
                    }
                }
            });
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
        setActiveRoom: navigateToRoom,
        setActiveRoomCore: function (roomName) {
            var room = getRoomElements(roomName);

            loadRoomPreferences(roomName);

            if (room.isActive()) {
                // Still trigger the event (just do less overall work)
                $ui.trigger(ui.events.activeRoomChanged, [roomName]);
                return true;
            }

            var currentRoom = getCurrentRoomElements();

            if (room.exists()) {
                if (currentRoom.exists()) {
                    currentRoom.makeInactive();
                }

                triggerFocus();
                room.makeActive();

                ui.toggleMessageSection(room.isClosed());

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
        updatePrivateLobbyRooms: function (roomName) {
            var lobby = getLobby(),
                $room = lobby.users.find('li[data-name="' + roomName + '"]');

            $room.appendTo(lobby.owners);
        },
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
        setRoomLoading: setRoomLoading,
        populateLobbyRooms: function (rooms, privateRooms) {
            var lobby = getLobby(),
                i;
            if (!lobby.isInitialized()) {

                // Populate the room cache
                for (i = 0; i < rooms.length; ++i) {
                    roomCache[rooms[i].Name] = true;
                }

                for (i = 0; i < privateRooms.length; ++i) {
                    roomCache[privateRooms[i].Name] = true;
                }

                var showClosedRooms = $closedRoomFilter.is(':checked'),
                    // sort lobby by room open ascending then count descending
                    privateSorted = sortRoomList(privateRooms);
                // sort lobby by room open ascending then count descending and
                // filter the other rooms so that there is no duplication 
                // between the lobby lists
                sortedRoomList = sortRoomList(rooms).filter(function (room) {
                    return !privateSorted.some(function (allowed) {
                        return allowed.Name === room.Name;
                    });
                });

                lobby.owners.empty();
                lobby.users.empty();

                var listOfPrivateRooms = $('<ul/>');
                if (privateSorted.length > 0) {
                    populateLobbyRoomList(privateSorted, templates.lobbyroom, listOfPrivateRooms, showClosedRooms);
                    listOfPrivateRooms.children('li').appendTo(lobby.owners);
                    $lobbyPrivateRooms.show();
                    $lobbyOtherRooms.find('nav-header').html('Other Rooms');
                } else {
                    $lobbyPrivateRooms.hide();
                    $lobbyOtherRooms.find('nav-header').html('Rooms');
                }

                var listOfRooms = $('<ul/>');
                populateLobbyRoomList(sortedRoomList.splice(0, maxRoomsToLoad), templates.lobbyroom, listOfRooms, showClosedRooms);
                lastLoadedRoomIndex = listOfRooms.children('li').length;
                listOfRooms.children('li').appendTo(lobby.users);
                if (lastLoadedRoomIndex < sortedRoomList.length) {
                    $loadMoreRooms.appendTo(lobby.users);
                    $loadMoreRooms.show();
                }
                $lobbyOtherRooms.show();
            }

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
                active = $user.data('active'),
                $idleSince = $user.find('.idle-since');

            if (userViewModel.active === true) {
                if ($user.hasClass('idle')) {
                    $user.removeClass('idle');
                    $idleSince.livestamp('destroy');
                }
            } else {
                if (!$user.hasClass('idle')) {
                    $user.addClass('idle');
                }

                if (!$idleSince.html()) {
                    $idleSince.livestamp(userViewModel.lastActive);
                }
            }

            updateNote(userViewModel, $user);
        },
        setUserActive: function ($user) {
            var $idleSince = $user.find('.idle-since');
            if ($user.data('active') === true) {
                return false;
            }
            $user.attr('data-active', true);
            $user.data('active', true);
            $user.removeClass('idle');
            if ($idleSince.livestamp('isLiveStamp')) {
                $idleSince.livestamp('destroy');
            }
            return true;
        },
        setUserInActive: function ($user) {
            if ($user.data('active') === false) {
                return false;
            }
            $user.attr('data-active', false);
            $user.data('active', false);
            $user.addClass('idle');
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
                    } else {
                        room.setListState(room.activeUsers);
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
        setLoadingHistory: function (loadingHistory) {
            if (loadingHistory) {
                var room = getCurrentRoomElements();
                $loadingHistoryIndicator.appendTo(room.messages);
                $loadingHistoryIndicator.fadeIn('slow');
            } else {
                $loadingHistoryIndicator.hide();
            }
        },
        setRoomTrimmable: function (roomName, canTrimMessages) {
            var room = getRoomElements(roomName);
            room.setTrimmable(canTrimMessages);
        },
        prependChatMessages: function (messages, roomName) {
            var room = getRoomElements(roomName),
                $messages = room.messages,
                $target = $messages.children().first(),
                $previousMessage = null,
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
            $.each(messages, function () {
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

            $message = this.appendMessage(templates.message.tmpl(message), room);

            if (message.htmlContent) {
                ui.addChatMessageContent(message.id, message.htmlContent, room.getName());
            }

            if (room.isInitialized()) {
                if (isMention) {
                    // Always do sound notification for mentions if any room as sound enabled
                    if (anyRoomPreference('hasSound') === true) {
                        ui.notify(true);
                    }

                    if (focus === false && anyRoomPreference('canToast') === true) {
                        // Only toast if there's no focus (even on mentions)
                        ui.toast(message, true, roomName);
                    }
                }
                else {
                    // Only toast if chat isn't focused
                    if (focus === false) {
                        ui.notifyRoom(roomName);
                        ui.toastRoom(roomName, message);
                    }
                }
            }
        },
        overwriteMessage: function (id, message) {
            var $message = $('#m-' + id);
            processMessage(message);

            $message.find('.middle').html(message.message);
            $message.attr('id', 'm-' + message.id);

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

            $message.find('.middle').append('<p>' + content + '</p>');
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
                    message: ui.processContent(content),
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
            return focus;
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
            $login.modal({ backdrop: true, keyboard: true });
            return true;
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
        showReloadMessageNotification: function () {
            $reloadMessageNotification.appendTo($chatArea);
            $reloadMessageNotification.show();
        },
        showStatus: function (status, transport) {
            // Change the status indicator here
            if (connectionState !== status) {
                if (popoverTimer) {
                    clearTimeout(popoverTimer);
                }
                connectionState = status;
                $connectionStatus.popover('destroy');
                switch (status) {
                    case 0: // Connected
                        $connectionStatus.removeClass('reconnecting disconnected');
                        $connectionStatus.popover(getConnectionStateChangedPopoverOptions('You\'re connected.'));
                        $connectionStateChangedPopover.find(connectionStateIcon).addClass('icon-ok-sign');
                        $connectionStatus.popover('show');
                        popoverTimer = setTimeout(function () {
                            $connectionStatus.popover('destroy');
                            ui.initializeConnectionStatus(transport);
                            popoverTimer = null;
                        }, 2000);
                        break;
                    case 1: // Reconnecting
                        $connectionStatus.removeClass('disconnected').addClass('reconnecting');
                        $connectionStatus.popover(getConnectionStateChangedPopoverOptions('The connection to JabbR has been temporarily lost, trying to reconnect.'));
                        $connectionStateChangedPopover.find(connectionStateIcon).addClass('icon-question-sign');
                        $connectionStatus.popover('show');
                        popoverTimer = setTimeout(function () {
                            $connectionStatus.popover('hide');
                            popoverTimer = null;
                        }, 5000);
                        break;
                    case 2: // Disconnected
                        $connectionStatus.removeClass('reconnecting').addClass('disconnected');
                        $connectionStatus.popover(getConnectionStateChangedPopoverOptions('The connection to JabbR has been lost, trying to reconnect.'));
                        $connectionStateChangedPopover.find(connectionStateIcon).addClass('icon-exclamation-sign');
                        $connectionStatus.popover('show');
                        popoverTimer = setTimeout(function () {
                            $connectionStatus.popover('hide');
                            popoverTimer = null;
                        }, 5000);
                        break;
                }
            }
        },
        setReadOnly: function (isReadOnly) {
            readOnly = isReadOnly;

            if (readOnly === true) {
                $hiddenFile.attr('disabled', 'disabled');
                $submitButton.attr('disabled', 'disabled');
                $newMessage.attr('disabled', 'disabled');
                $fileUploadButton.attr('disabled', 'disabled');
            }
            else {
                $hiddenFile.removeAttr('disabled');
                $submitButton.removeAttr('disabled');
                $newMessage.removeAttr('disabled');
                $fileUploadButton.removeAttr('disabled');
            }
        },
        initializeConnectionStatus: function (transport) {
            $connectionStatus.popover(getConnectionInfoPopoverOptions(transport));
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
                // disable send button, textarea and file upload
                $newMessage.attr('disabled', 'disabled');
                $submitButton.attr('disabled', 'disabled');
                $fileUploadButton.attr('disabled', 'disabled');
                $hiddenFile.attr('disabled', 'disabled');
            } else if (!readOnly) {
                // re-enable textarea button
                $newMessage.attr('disabled', '');
                $newMessage.removeAttr('disabled');

                // re-enable submit button
                $submitButton.attr('disabled', '');
                $submitButton.removeAttr('disabled');

                // re-enable file upload button
                $fileUploadButton.attr('disabled', '');
                $fileUploadButton.removeAttr('disabled');
                $hiddenFile.attr('disabled', '');
                $hiddenFile.removeAttr('disabled');
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
        },
        processContent: function (content) {
            content = content || '';

            var hasNewline = content.indexOf('\n') !== -1;

            if (hasNewline) {
                // Multiline detection
                return templates.multiline.tmpl({ content: content }).html();
            }
            else {
                // Emoji
                content = utility.parseEmojis(content);

                // Html encode
                content = utility.encodeHtml(content);

                // Transform emoji to html
                content = utility.transformEmojis(content);

                // Create rooms links
                content = content.replace(/#([A-Za-z0-9-_]{1,30}\w*)/g, function (m) {
                    var roomName = m.substr(1);

                    if (roomCache[roomName.toLowerCase()]) {
                        return '<a href="#/rooms/' + roomName + '" title="' + roomName + '">' + m + '</a>';
                    }
                    return m;
                });

                // Convert normal links
                content = linkify(content, {
                    callback: function (text, href) {
                        return href ? '<a rel="nofollow external" target="_blank" href="' + href + '" title="' + href + '">' + text + '</a>' : text;
                    }
                });

                return content;
            }
        },
        trimRoomMessageHistory: function (roomName) {
            var rooms = roomName ? [getRoomElements(roomName)] : getAllRoomElements();

            for (var i = 0; i < rooms.length; i++) {
                rooms[i].trimHistory();
            }
        }
    };

    if (!window.chat) {
        window.chat = {};
    }
    window.chat.ui = ui;
})(jQuery, window, window.document, window.chat.utility, window.Emoji, window.linkify);
