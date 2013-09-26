(function ($, window, chat) {
    "use strict";

    function LobbyTab() {
        this.tab = $('#tabs-lobby');
        this.users = $('#userlist-lobby');
        this.owners = $('#userlist-lobby-owners');
        this.activeUsers = $('#userlist-lobby-active');
        this.messages = $('#messages-lobby');
        this.roomTopic = $('#roomTopic-lobby');

        this.templates = {
            
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

    LobbyTab.prototype.appendMessage = function (/*newMessage*/) {
        // do something?
    };

    LobbyTab.prototype.addMessage = function (/*content, type*/) {
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

    chat.LobbyTab = LobbyTab;
}(window.jQuery, window, window.chat));
