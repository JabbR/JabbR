(function ($, window, chat) {
    "use strict";

    function LobbyTab() {
        this.$tab = $('#tabs-lobby');       
        this.$messages = $('#messages-lobby');
        this.$loadMoreRooms = $('#load-more-rooms-item');
        this.$lobbyPrivateRooms = $('#lobby-private');
        this.$lobbyOtherRooms = $('#lobby-other');
        this.$lobbyRoomFilterForm = $('#room-filter-form');
        this.$roomFilterInput = $('#room-filter');
        this.$closedRoomFilter = $('#room-filter-closed');
        this.$lobbyOtherRoomList = $('#userlist-lobby');
        this.$lobbyPrivateRoomList = $('#userlist-lobby-owners');

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
        return this.$tab.data('name');
    };

    LobbyTab.prototype.isActive = function () {
        return this.$tab.hasClass('current');
    };

    LobbyTab.prototype.makeInactive = function () {
        this.$tab.removeClass('current');

        this.$messages.removeClass('current')
                      .hide();
        
        this.$lobbyRoomFilterForm.hide();
    };

    LobbyTab.prototype.makeActive = function () {
        this.$tab.addClass('current');

        this.$messages.addClass('current')
                      .show();
        
        this.$lobbyRoomFilterForm.show();

        this.triggerFocus();
    };

    LobbyTab.prototype.triggerFocus = function () {
        chat.ui.triggerFocus(this.$roomFilterInput);
    };

    LobbyTab.prototype.setInitialized = function () {
        this.$tab.data('initialized', true);
    };

    LobbyTab.prototype.isInitialized = function () {
        return this.$tab.data('initialized') === true;
    };

    chat.LobbyTab = LobbyTab;
}(window.jQuery, window, window.chat));
