(function ($, window, chat, utility, moment) {
    "use strict";

    function Room(roomName, roomId) {
        var roomOwnersHeader = utility.getLanguageResource('Chat_UserOwnerHeader'),
            usersHeader = utility.getLanguageResource('Chat_UserHeader'),
            $chatArea = $('#chat-area'),
            $topicBar = $('#topic-bar'),
            
            templates = {
                message: $('#new-message-template'),
                userlist: $('#new-userlist-template'),
                user: $('#new-user-template')
            };

        this.roomName = roomName;
        this.scrollTopThreshold = 75;
        this.trimRoomHistoryMaxMessages = 200;
        this.$roomActions = $('#room-actions');
        this.tab = $('#tabs-' + roomId);

        this.messages = $('<ul/>').attr('id', 'messages-' + roomId)
                              .addClass('messages')
                              .appendTo($chatArea)
                              .hide();

        this.roomTopic = $('<div/>').attr('id', 'roomTopic-' + roomId)
                              .addClass('roomTopic')
                              .appendTo($topicBar)
                              .hide();
        
        this.users = $('<div/>').attr('id', 'userlist-' + roomId)
            .addClass('users')
            .appendTo($chatArea).hide();
        this.owners = templates.userlist.tmpl({ listname: roomOwnersHeader, id: 'userlist-' + roomId + '-owners' })
            .addClass('owners')
            .appendTo(this.users)
            .find('ul');
        this.activeUsers = templates.userlist.tmpl({ listname: usersHeader, id: 'userlist-' + roomId + '-active' })
            .appendTo(this.users)
            .find('ul');
        this.userLists = this.owners.add(this.activeUsers);

        var _this = this;
        var scrollHandler = function () {
            var messageId = null;

            // Do nothing if there's nothing else
            if ($(this).data('full') === true) {
                return;
            }

            // If you're we're near the top, raise the event, but if the scroll
            // bar is small enough that we're at the bottom edge, ignore it.
            // We have to use the ui version because the room object above is
            // not fully initialized, so there are no messages.
            if ($(this).scrollTop() <= _this.scrollTopThreshold && !chat.ui.isNearTheEnd(roomId)) {
                var $child = _this.messages.children('.message:first');
                if ($child.length > 0) {
                    messageId = $child.attr('id')
                                      .substr(2); // Remove the "m-"
                    $(chat.ui).trigger(chat.ui.events.scrollRoomTop, [{ name: roomName, messageId: messageId }]);
                }
            }
        };

        // Hookup the scroll handler since event delegation doesn't work with scroll events
        this.messages.bind('scroll', scrollHandler);

        // Store the scroll handler so we can remove it later
        this.messages.data('scrollHandler', scrollHandler);

        this.templates = {
            separator: $('#message-separator-template')
        };
    }

    Room.prototype.type = function () {
        return 'Room';
    };
    
    Room.prototype.isClosable = function () {
        return true;
    };

    Room.prototype.isLobby = function () {
        return false;
    };

    Room.prototype.appendMessage = function (newMessage) {
        // Determine if we need to show a new date header: Two conditions
        // for instantly skipping are if this message is a date header, or
        // if the room only contains non-chat messages and we're adding a
        // non-chat message.
        var isMessage = $(newMessage).is('.message');
        if (!$(newMessage).is('.date-header') && (isMessage || this.tab.data('messages'))) {
            var lastMessage = this.messages.find('li[data-timestamp]').last(),
                lastDate = new Date(lastMessage.data('timestamp')),
                thisDate = new Date($(newMessage).data('timestamp'));

            if (!lastMessage.length || thisDate.toDate().diffDays(lastDate.toDate())) {
                var dateDisplay = moment(thisDate);
                this.addMessage(dateDisplay.format('dddd, MMMM Do YYYY'), 'date-header list-header')
                    .find('.right').remove(); // remove timestamp on date indicator
            }
        }

        if (isMessage) {
            this.tab.data('messages', true);
        }

        $(newMessage).appendTo(this.messages);
    };

    Room.prototype.addMessage = function (content, type) {
        var nearEnd = this.isNearTheEnd(),
            $element = chat.ui.prepareNotificationMessage(content, type);

        this.appendMessage($element);

        if (type === 'notification') {
            chat.ui.collapseNotifications($element);
        }

        if (nearEnd) {
            this.scrollToBottom();
        }

        return $element;
    };

    Room.prototype.prependChatMessages = function(messages) {
        var $messages = this.messages,
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
        var _this = this;
        $.each(messages, function () {
            chat.ui.processMessage(this, _this.roomName);

            if ($previousMessage) {
                previousUser = $previousMessage.data('name');
                previousTimestamp = new Date($previousMessage.data('timestamp') || new Date());
            }

            if (this.date.toDate().diffDays(previousTimestamp.toDate())) {
                chat.ui.addMessageBeforeTarget(this.date.toLocaleDateString(), 'list-header', $target)
                      .addClass('date-header')
                      .find('.right').remove(); // remove timestamp on date indicator

                // Force a user name to show after the header
                previousUser = null;
            }

            // Determine if we need to show the user
            this.showUser = !previousUser || previousUser !== this.name;

            // Render the new message
            $target.before(_this.templates.message.tmpl(this));

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
    };

    Room.prototype.setTopic = function (topic) {
        var topicHtml = topic === '' ? utility.getLanguageResource('Chat_DefaultTopic', this.getName()) : chat.ui.processContent(topic);

        if (this.isActive()) {
            this.roomTopic.hide();
        }

        this.roomTopic.html(topicHtml);

        if (this.isActive()) {
            this.roomTopic.fadeIn(2000);
        }
    };

    Room.prototype.isLocked = function () {
        return this.tab.hasClass('locked');
    };

    Room.prototype.getUnread = function () {
        return this.tab.data('unread') || 0;
    };

    Room.prototype.hasSeparator = function () {
        return this.messages.find('.message-separator').length > 0;
    };

    Room.prototype.needsSeparator = function () {
        if (this.isActive()) {
            return false;
        }
        return this.isInitialized() && this.getUnread() === 5;
    };

    Room.prototype.addSeparator = function () {
        if (this.isLobby()) {
            return;
        }

        // find first correct unread message
        var n = this.getUnread(),
            $unread = this.messages.find('.message').eq(-(n + 1));

        $unread.after(this.templates.separator.tmpl())
            .data('unread', n); // store unread count

        this.scrollToBottom();
    };

    Room.prototype.removeSeparator = function () {
        this.messages.find('.message-separator').fadeOut(2000, function () {
            $(this).remove();
        });
    };

    Room.prototype.updateUnread = function (isMentioned) {
        var $tab = this.tab.addClass('unread'),
            $content = $tab.find('.content'),
            unread = ($tab.data('unread') || 0) + 1,
            hasMentions = $tab.data('hasMentions') || isMentioned; // Whether or not the user already has unread messages to him/her

        $content.text((hasMentions ? '*' : '') + '(' + unread + ') ' + this.getName());

        $tab.data('unread', unread);
        $tab.data('hasMentions', hasMentions);
    };

    Room.prototype.scrollToBottom = function () {
        // IE will repaint if we do the Chrome bugfix and look jumpy
        if ($.browser.webkit) {
            // Chrome fix for hiding and showing scroll areas
            this.messages.scrollTop(this.messages.scrollTop() - 1);
        }
        this.messages.scrollTop(this.messages[0].scrollHeight);
    };

    Room.prototype.isNearTheEnd = function () {
        return this.messages.isNearTheEnd();
    };

    Room.prototype.getName = function () {
        return this.tab.data('name');
    };

    Room.prototype.isActive = function () {
        return this.tab.hasClass('current');
    };

    Room.prototype.isClosed = function () {
        return this.tab.attr('data-closed') === 'true';
    };

    Room.prototype.close = function () {
        this.tab.attr('data-closed', true);
        this.tab.addClass('closed');
        this.tab.find('.readonly').removeClass('hide');
    };

    Room.prototype.unClose = function () {
        this.tab.attr('data-closed', false);
        this.tab.removeClass('closed');
        this.tab.find('.readonly').addClass('hide');
    };

    Room.prototype.remove = function () {
        this.makeInactive();
        
        // Remove the scroll handler from this room
        var scrollHandler = this.messages.data('scrollHandler');
        this.messages.unbind('scrollHandler', scrollHandler);

        this.messages.remove();
        this.users.remove();
        this.roomTopic.remove();
    };

    Room.prototype.makeInactive = function () {
        this.messages.removeClass('current')
                     .hide();

        this.users.removeClass('current')
                  .hide();

        this.roomTopic.removeClass('current')
                  .hide();
        
        this.$roomActions.hide();
    };

    Room.prototype.makeActive = function () {
        var currUnread = this.getUnread(),
            lastUnread = this.messages.find('.message-separator').data('unread') || 0;

        this.tab.removeClass('unread')
                .data('unread', 0)
                .data('hasMentions', false);

        if (this.tab.is('.room')) {
            this.tab.find('.content').text(this.getName());
        }

        this.messages.addClass('current')
                     .show();

        this.users.addClass('current')
                  .show();

        this.roomTopic.addClass('current')
                  .show();
        
        this.$roomActions.show();

        // if no unread since last separator
        // remove previous separator
        if (currUnread <= lastUnread) {
            this.removeSeparator();
        }

        this.triggerFocus();
    };

    Room.prototype.triggerFocus = function() {
        chat.ui.triggerFocus();
    };

    Room.prototype.afterSend = function() {
        this.scrollToBottom();
        this.removeSeparator();
    };

    Room.prototype.setInitialized = function () {
        this.tab.data('initialized', true);
    };

    Room.prototype.isInitialized = function () {
        return this.tab.data('initialized') === true;
    };

    // Users
    Room.prototype.getUser = function (userName) {
        return this.users.find('[data-name="' + userName + '"]');
    };

    Room.prototype.getUserReferences = function (userName) {
        return $.merge(this.getUser(userName),
                       this.messages.find('[data-name="' + userName + '"]'));
    };

    Room.prototype.setLocked = function () {
        this.tab.addClass('locked');
        this.tab.find('.lock').removeClass('hide');
    };

    Room.prototype.addUser = function (userViewModel, $user) {
        if (userViewModel.owner) {
            this.addUserToList($user, this.owners);
        } else {
            this.changeInactive($user, userViewModel.active);
            this.addUserToList($user, this.activeUsers);
        }
    };

    Room.prototype.removeUser = function(user) {     
        // recolour user to offline
        this.getUserReferences
            .find('.user')
            .removeClass('absent present')
            .addClass('absent');

        var _this = this;
        this.getUser(user.Name)
            .addClass('removing')
            .fadeOut('slow', function() {
                $(this).remove();

                utility.updateEmptyListItem(_this.userLists);
            });
    };

    Room.prototype.changeInactive = function ($user, isActive) {
        if (isActive) {
            $user.removeClass('inactive');
        } else {
            $user.addClass('inactive');
        }
    };

    Room.prototype.addUserToList = function ($user, list) {
        var oldParentList = $user.parent('ul');
        $user.appendTo(list);
        utility.updateEmptyListItem(list);
        if (oldParentList.length > 0) {
            utility.updateEmptyListItem(oldParentList);
        }
        this.sortList(list, $user);
    };

    Room.prototype.appearsInList = function ($user, list) {
        return $user.parent('ul').attr('id') === list.attr('id');
    };

    Room.prototype.updateUserStatus = function ($user) {
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
            this.changeInactive($user, status);

            this.addUserToList($user, this.activeUsers);
        }
    };

    Room.prototype.sortLists = function (user) {
        var isOwner = $(user).data('owner');
        if (isOwner) {
            this.sortList(this.owners, user);
        } else {
            this.sortList(this.activeUsers, user);
        }
    };

    Room.prototype.sortList = function (listToSort, user) {
        var listItems = listToSort.children('li:not(.empty)').get(),
            userName = ($(user).data('name') || '').toString(),
            userActive = $(user).data('active');

        for (var i = 0; i < listItems.length; i++) {
            var otherName = ($(listItems[i]).data('name') || '').toString(),
                otherActive = $(listItems[i]).data('active');

            if (userActive === otherActive &&
                userName.toUpperCase() < otherName.toUpperCase()) {
                $(listItems[i]).before(user);
                break;
            } else if (userActive && !otherActive) {
                $(listItems[i]).before(user);
                break;
            } else if (i === (listItems.length - 1)) {
                $(listItems[i]).after(user);
                break;
            }
        }
    };

    Room.prototype.canTrimHistory = function () {
        return this.tab.data('trimmable') !== false;
    };

    Room.prototype.setTrimmable = function (canTrimMessages) {
        this.tab.data('trimmable', canTrimMessages);
    };

    Room.prototype.trimHistory = function (numberOfMessagesToKeep) {
        var lastIndex = null,
            $messagesToRemove = null,
            $roomMessages = this.messages.find('li'),
            messageCount = $roomMessages.length;

        numberOfMessagesToKeep = numberOfMessagesToKeep || this.trimRoomHistoryMaxMessages;

        if (this.isLobby() || !this.canTrimHistory()) {
            return;
        }

        if (numberOfMessagesToKeep < this.trimRoomHistoryMaxMessages) {
            numberOfMessagesToKeep = this.trimRoomHistoryMaxMessages;
        }

        if (messageCount < numberOfMessagesToKeep) {
            return;
        }

        lastIndex = messageCount - numberOfMessagesToKeep;
        $messagesToRemove = $roomMessages.filter('li:lt(' + lastIndex + ')');

        $messagesToRemove.remove();
    };

    chat.Room = Room;
}(window.jQuery, window, window.chat, window.chat.utility, window.moment));
