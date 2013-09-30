(function ($, window, chat, utility) {
    "use strict";

    var Keys = { Enter: 13 };
    
    function getNextRoomListElement($targetList, roomName, count, closed) {
        var nextListElement = null,
            $lastListElement = $targetList.find('li:last'),
            liRoomCount = $lastListElement.data('count'),
            liRoomClosed = $lastListElement.hasClass('closed'),
            name = $lastListElement.data('name'),
            nameComparison = null;
        
        // short circuit the below for insert-at-end
        if ($lastListElement.length > 0) {
            nameComparison = name.toString().toUpperCase().localeCompare(roomName);
            if (nameComparison === 0 ||
                (!liRoomClosed && closed) ||
                liRoomCount > count ||
                liRoomCount === count && nameComparison < 0) {
                return null;
            }
        }

        // move the item to before the next element
        $targetList.find('li').each(function () {
            var $this = $(this);
            liRoomCount = $this.data('count');
            liRoomClosed = $this.hasClass('closed');
            name = $this.data('name');

            if (name === undefined || name === null) {
                return true;
            }

            nameComparison = name.toString().toUpperCase().localeCompare(roomName);

            // return true (saying it's not the next element) if any of the following are true:
            // 1. The list room is the same as the insert room
            // 2. The list room is open and the insert room is closed
            // 3. Same state, but the list room has more occupants than the insert room
            // 4. The list room comes earlier in the alphabet than the insert room
            if (nameComparison === 0 ||
                (!liRoomClosed && closed) ||
                liRoomCount > count ||
                liRoomCount === count && nameComparison < 0) {
                return true;
            }

            nextListElement = $this;
            return false;
        });

        return nextListElement;
    }

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
        this.$lobbyRoomLists = $('#lobby-private, #lobby-other');

        this.templates = {
            lobbyroom: $('#new-lobby-room-template')
        };

        var _this = this;
        $(document).on('click', 'li.room .room-row', function () {
            var roomName = $(this).parent().data('name');
            _this.activateOrOpenRoom(roomName);
        });

        this.$roomFilterInput.keypress(function (ev) {
            var key = ev.keyCode || ev.which,
                roomName = $(this).val();

            switch (key) {
                case Keys.Enter:
                    // if it's an exact match, open/activate the room
                    if (_this.roomCache[roomName.toUpperCase()]) {
                        _this.activateOrOpenRoom(roomName);

                        return;
                    }
            }
        });

        this.$roomFilterInput.bind('input', function () { _this.$lobbyRoomFilterForm.submit(); })
            .keyup(function () { _this.$lobbyRoomFilterForm.submit(); });

        this.$closedRoomFilter.click(function () { _this.$lobbyRoomFilterForm.submit(); });

        this.$lobbyRoomFilterForm.submit(function () {
            var $lobbyRoomsLists = _this.$lobbyPrivateRooms.add(_this.$lobbyOtherRooms);

            // hide all elements except those that match the input / closed filters
            $lobbyRoomsLists
                .find('li:not(.empty)')
                .each(function() {
                    _this.filterIndividualRoom($(this));
                });

            $lobbyRoomsLists.find('ul').each(function () {
                utility.updateEmptyListItem($(this));
            });
            
            return false;
        });

        this.$loadMoreRooms.on('click', function () {
            $.tmpl(_this.templates.lobbyroom, _this.publicRoomList.slice(_this.lastLoadedRoomIndex, _this.lastLoadedRoomIndex + _this.maxRoomsToLoad)).appendTo(_this.$lobbyOtherRoomList);
            _this.lastLoadedRoomIndex += _this.maxRoomsToLoad;

            if (_this.lastLoadedRoomIndex < _this.publicRoomList.length) {
                _this.$loadMoreRooms.show();
            } else {
                _this.$loadMoreRooms.hide();
            }

            // re-filter lists
            _this.$lobbyRoomFilterForm.submit();
        });
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

    LobbyTab.prototype.addOrUpdateRoom = function(roomViewModel) {
        var $targetList = roomViewModel.Private === true ? this.$lobbyPrivateRoomList : this.$lobbyOtherRoomList,
                $room = this.$lobbyRoomLists.find('li[data-room="' + roomViewModel.Name + '"]'),
                $count = $room.find('.count'),
                $topic = $room.find('.topic'),
                roomName = roomViewModel.Name.toString().toUpperCase(),
                nextListElement = null;

        // if we don't find the room, we need to create it
        if ($room.length === 0) {
            $room = this.templates.lobbyroom.tmpl(roomViewModel);

            nextListElement = getNextRoomListElement($targetList, roomName, roomViewModel.Count, roomViewModel.Closed);

            if (nextListElement !== null) {
                $room.insertBefore(nextListElement);
            } else {
                $room.appendTo($targetList);
            }

            this.filterIndividualRoom($room);
            utility.updateEmptyListItem($targetList);

            // handle updates on rooms not currently displayed to clients by removing from the public room list
            if (this.publicRoomList) {
                for (var i = 0; i < this.publicRoomList.length; i++) {
                    if (this.publicRoomList[i].Name.toString().toUpperCase().localeCompare(roomName) === 0) {
                        this.publicRoomList.splice(i, 1);
                        break;
                    }
                }
            }
        } else {
            if (roomViewModel.Count === 0) {
                $count.text(utility.getLanguageResource('Client_OccupantsZero'));
            } else if (roomViewModel.Count === 1) {
                $count.text(utility.getLanguageResource('Client_OccupantsOne'));
            } else {
                $count.text(utility.getLanguageResource('Client_OccupantsMany', roomViewModel.Count));
            }

            if (roomViewModel.Private === true) {
                $room.addClass('locked');
            } else {
                $room.removeClass('locked');
            }

            if (roomViewModel.Closed === true) {
                $room.addClass('closed');
            } else {
                $room.removeClass('closed');
            }

            $topic.html(roomViewModel.processedTopic);

            nextListElement = getNextRoomListElement($targetList, roomName, roomViewModel.Count, roomViewModel.Closed);

            $room.data('count', roomViewModel.Count);
            if (nextListElement !== null) {
                $room.insertBefore(nextListElement);
            } else {
                $room.appendTo($targetList);
            }
        }

        // Do a little animation
        $room.css('-webkit-animation-play-state', 'running').css('animation-play-state', 'running');
        
        this.updateListsAndHeaders();
    };

    LobbyTab.prototype.removeRoom = function(roomName) {
        this.$lobbyRoomLists.find('li[data-room="' + roomName + "']").remove();
        utility.updateEmptyListItem(this.$lobbyRoomLists);
        this.updateListHeaders();
    };

    LobbyTab.prototype.getName = function () {
        return this.$tab.data('name');
    };

    LobbyTab.prototype.isActive = function () {
        return this.$tab.hasClass('current');
    };

    LobbyTab.prototype.makeInactive = function () {
        this.$messages.removeClass('current')
                      .hide();
        
        this.$lobbyRoomFilterForm.hide();
    };

    LobbyTab.prototype.makeActive = function () {
        this.$messages.addClass('current')
                      .show();
        
        this.$lobbyRoomFilterForm.show();

        this.triggerFocus();
    };

    LobbyTab.prototype.triggerFocus = function () {
        chat.ui.triggerFocus(this.$roomFilterInput);
    };
    
    LobbyTab.prototype.afterSend = function () {
    };

    LobbyTab.prototype.setInitialized = function () {
        this.$tab.data('initialized', true);
    };

    LobbyTab.prototype.isInitialized = function () {
        return this.$tab.data('initialized') === true;
    };

    LobbyTab.prototype.updateListsAndHeaders = function () {
        var privateRooms = this.$lobbyPrivateRoomList.find('li:not(.empty)');
        if (privateRooms.length > 0) {
            this.$lobbyPrivateRooms.show();
            this.$lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_OtherRooms'));
        } else {
            this.$lobbyPrivateRooms.hide();
            this.$lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_Rooms'));
        }
    };

    LobbyTab.prototype.filterIndividualRoom = function($room) {
        var filter = this.$roomFilterInput.val().toUpperCase(),
            showClosedRooms = this.$closedRoomFilter.is(':checked');

        if ($room.data('room').toString().toUpperCase().score(filter) > 0.0 && (showClosedRooms || !$room.is('.closed'))) {
            $room.show();
        } else {
            $room.hide();
        }
    };

    LobbyTab.prototype.activateOrOpenRoom = function(roomName) {
        var room = chat.ui.getRoomElements(roomName);

        if (room !== null) {
            chat.ui.setActiveRoom(roomName);
        } else {
            $(chat.ui).trigger(chat.ui.events.openRoom, [roomName]);
        }
    };

    chat.LobbyTab = LobbyTab;
}(window.jQuery, window, window.chat, window.chat.utility));
