(function ($, window, chat, utility) {
    "use strict";
    
    var trimRoomHistoryMaxMessages = 200;

    function getUserClassName(userName) {
        return '[data-name="' + userName + '"]';
    }

    function LobbyTab() {
        this.tab = $('#tabs-lobby');
        this.users = $('#userlist-lobby');
        this.owners = $('#userlist-lobby-owners');
        this.activeUsers = $('#userlist-lobby-active');
        this.messages = $('#messages-lobby');
        this.roomTopic = $('#roomTopic-lobby');

        this.templates = {
            separator: $('#message-separator-template')
        };
    }

    LobbyTab.prototype.type = function () {
        return 'Lobby';
    };
    
    LobbyTab.prototype.isClosable = function () {
        return false;
    };

    LobbyTab.prototype.isLobby = function () {
        return true;
    };

    LobbyTab.prototype.appendMessage = function (newMessage) {
        // do something?
    };

    LobbyTab.prototype.addMessage = function (content, type) {
        // do something?
    };


    LobbyTab.prototype.getUnread = function () {
        return this.tab.data('unread') || 0;
    };

    LobbyTab.prototype.hasSeparator = function () {
        return this.messages.find('.message-separator').length > 0;
    };

    LobbyTab.prototype.needsSeparator = function () {
        if (this.isActive()) {
            return false;
        }
        return this.isInitialized() && this.getUnread() === 5;
    };

    LobbyTab.prototype.addSeparator = function () {
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

    LobbyTab.prototype.removeSeparator = function () {
        this.messages.find('.message-separator').fadeOut(2000, function () {
            $(this).remove();
        });
    };

    LobbyTab.prototype.updateUnread = function (isMentioned) {
        var $tab = this.tab.addClass('unread'),
            $content = $tab.find('.content'),
            unread = ($tab.data('unread') || 0) + 1,
            hasMentions = $tab.data('hasMentions') || isMentioned; // Whether or not the user already has unread messages to him/her

        $content.text((hasMentions ? '*' : '') + '(' + unread + ') ' + this.getName());

        $tab.data('unread', unread);
        $tab.data('hasMentions', hasMentions);
    };

    LobbyTab.prototype.scrollToBottom = function () {
        // IE will repaint if we do the Chrome bugfix and look jumpy
        if ($.browser.webkit) {
            // Chrome fix for hiding and showing scroll areas
            this.messages.scrollTop(this.messages.scrollTop() - 1);
        }
        this.messages.scrollTop(this.messages[0].scrollHeight);
    };

    LobbyTab.prototype.isNearTheEnd = function () {
        return this.messages.isNearTheEnd();
    };

    LobbyTab.prototype.getName = function () {
        return this.tab.data('name');
    };

    LobbyTab.prototype.isActive = function () {
        return this.tab.hasClass('current');
    };

    LobbyTab.prototype.exists = function () {
        return this.tab.length > 0;
    };

    LobbyTab.prototype.isClosed = function () {
        return this.tab.attr('data-closed') === 'true';
    };

    LobbyTab.prototype.close = function () {
        this.tab.attr('data-closed', true);
        this.tab.addClass('closed');
        this.tab.find('.readonly').removeClass('hide');
    };

    LobbyTab.prototype.unClose = function () {
        this.tab.attr('data-closed', false);
        this.tab.removeClass('closed');
        this.tab.find('.readonly').addClass('hide');
    };

    LobbyTab.prototype.clear = function () {
        this.messages.empty();
        this.owners.empty();
        this.activeUsers.empty();
    };

    LobbyTab.prototype.makeInactive = function () {
        this.tab.removeClass('current');

        this.messages.removeClass('current')
                     .hide();

        this.users.removeClass('current')
                  .hide();

        this.roomTopic.removeClass('current')
                  .hide();
    };

    LobbyTab.prototype.makeActive = function () {
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

    LobbyTab.prototype.setInitialized = function () {
        this.tab.data('initialized', true);
    };

    LobbyTab.prototype.isInitialized = function () {
        return this.tab.data('initialized') === true;
    };

    // Users
    LobbyTab.prototype.getUser = function (userName) {
        return this.users.find(getUserClassName(userName));
    };

    LobbyTab.prototype.getUserReferences = function (userName) {
        return $.merge(this.getUser(userName),
                       this.messages.find(getUserClassName(userName)));
    };

    LobbyTab.prototype.setLocked = function () {
        this.tab.addClass('locked');
        this.tab.find('.lock').removeClass('hide');
    };

    LobbyTab.prototype.addUser = function (userViewModel, $user) {
        if (userViewModel.owner) {
            this.addUserToList($user, this.owners);
        } else {
            this.changeInactive($user, userViewModel.active);

            this.addUserToList($user, this.activeUsers);

        }
    };

    LobbyTab.prototype.changeInactive = function ($user, isActive) {
        if (isActive) {
            $user.removeClass('inactive');
        } else {
            $user.addClass('inactive');
        }
    };

    LobbyTab.prototype.addUserToList = function ($user, list) {
        var oldParentList = $user.parent('ul');
        $user.appendTo(list);
        utility.updateEmptyListItem(list);
        if (oldParentList.length > 0) {
            utility.updateEmptyListItem(oldParentList);
        }
        this.sortList(list, $user);
    };

    LobbyTab.prototype.appearsInList = function ($user, list) {
        return $user.parent('ul').attr('id') === list.attr('id');
    };

    LobbyTab.prototype.updateUserStatus = function ($user) {
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

    LobbyTab.prototype.sortLists = function (user) {
        var isOwner = $(user).data('owner');
        if (isOwner) {
            this.sortList(this.owners, user);
        } else {
            this.sortList(this.activeUsers, user);
        }
    };

    LobbyTab.prototype.sortList = function (listToSort, user) {
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

    LobbyTab.prototype.canTrimHistory = function () {
        return this.tab.data('trimmable') !== false;
    };

    LobbyTab.prototype.setTrimmable = function (canTrimMessages) {
        this.tab.data('trimmable', canTrimMessages);
    };

    LobbyTab.prototype.trimHistory = function (numberOfMessagesToKeep) {
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

    chat.LobbyTab = LobbyTab;
}(window.jQuery, window, window.chat, window.chat.utility));
