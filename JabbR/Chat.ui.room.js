(function ($, window, chat) {
    "use strict";
    
    var trimRoomHistoryMaxMessages = 200;

    function getUserClassName(userName) {
        return '[data-name="' + userName + '"]';
    }

    function Room($tab, $usersContainer, $usersOwners, $usersActive, $messages, $roomTopic) {
        this.tab = $tab;
        this.users = $usersContainer;
        this.owners = $usersOwners;
        this.activeUsers = $usersActive;
        this.messages = $messages;
        this.roomTopic = $roomTopic;

        this.templates = {
            separator: $('#message-separator-template')
        };

    }

    Room.prototype.isLocked = function () {
        return this.tab.hasClass('locked');
    };

    Room.prototype.isLobby = function () {
        return this.tab.hasClass('lobby');
    };

    Room.prototype.hasUnread = function () {
        return this.tab.hasClass('unread');
    };

    Room.prototype.hasMessages = function () {
        return this.tab.data('messages');
    };

    Room.prototype.updateMessages = function (value) {
        this.tab.data('messages', value);
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

    Room.prototype.exists = function () {
        return this.tab.length > 0;
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

    Room.prototype.clear = function () {
        this.messages.empty();
        this.owners.empty();
        this.activeUsers.empty();
    };

    Room.prototype.makeInactive = function () {
        this.tab.removeClass('current');

        this.messages.removeClass('current')
                     .hide();

        this.users.removeClass('current')
                  .hide();

        this.roomTopic.removeClass('current')
                  .hide();
    };

    Room.prototype.makeActive = function () {
        var currUnread = this.getUnread(),
            lastUnread = this.messages.find('.message-separator').data('unread') || 0;

        this.tab.addClass('current')
                .removeClass('unread')
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

        // if no unread since last separator
        // remove previous separator
        if (currUnread <= lastUnread) {
            this.removeSeparator();
        }
    };

    Room.prototype.setInitialized = function () {
        this.tab.data('initialized', true);
    };

    Room.prototype.isInitialized = function () {
        return this.tab.data('initialized') === true;
    };

    // Users
    Room.prototype.getUser = function (userName) {
        return this.users.find(getUserClassName(userName));
    };

    Room.prototype.getUserReferences = function (userName) {
        return $.merge(this.getUser(userName),
                       this.messages.find(getUserClassName(userName)));
    };

    Room.prototype.setLocked = function () {
        this.tab.addClass('locked');
        this.tab.find('.lock').removeClass('hide');
    };

    Room.prototype.setListState = function (list) {
        var emptyStatus = list.children('li.empty'),
            visibleItems = list.children('li:not(.empty)').filter(function() { return $(this).css('display') !== 'none'; });
        
        if (visibleItems.length > 0) {
            emptyStatus.remove();
        } else if (emptyStatus.length === 0) {
            list.append($('<li class="empty" />').text(list.data('emptyMessage')));
        }
    };

    Room.prototype.addUser = function (userViewModel, $user) {
        if (userViewModel.owner) {
            this.addUserToList($user, this.owners);
        } else {
            this.changeInactive($user, userViewModel.active);

            this.addUserToList($user, this.activeUsers);

        }
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
        this.setListState(list);
        if (oldParentList.length > 0) {
            this.setListState(oldParentList);
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

    chat.Room = Room;
}(window.jQuery, window, window.chat));
