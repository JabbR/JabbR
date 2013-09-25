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


    LobbyTab.prototype.getName = function () {
        return this.tab.data('name');
    };

    LobbyTab.prototype.isActive = function () {
        return this.tab.hasClass('current');
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
        this.tab.addClass('current');

        this.messages.addClass('current')
                     .show();

        this.users.addClass('current')
                  .show();

        this.roomTopic.addClass('current')
                  .show();
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
