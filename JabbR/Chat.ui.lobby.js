(function ($, window, chat, ui, utility) {
    "use strict";

    var $loadMoreRooms = null,
        publicRoomList = null,
        sortedRoomList = null,
        maxRoomsToLoad = 100,
        lastLoadedRoomIndex = 0,
        $lobbyPrivateRooms = null,
        $lobbyOtherRooms = null,
        $lobbyRoomFilterForm = null,
        $roomFilterInput = null,
        $closedRoomFilter = null,
        $lobbyOtherRoomList = null,
        $lobbyPrivateRoomList = null,
        roomCache = {},
        templates = null,
        Keys = { Enter: 13 };
    
    function activateOrOpenRoom(roomName) {
        var room = ui.getRoomElements(roomName);

        if (room.exists()) {
            ui.setActiveRoom(roomName);
        }
        else {
            $(ui).trigger(ui.events.openRoom, [roomName]);
        }
    }
    
    function filterIndividualRoom($room) {
        var filter = $roomFilterInput.val().toUpperCase(),
            showClosedRooms = $closedRoomFilter.is(':checked');

        if ($room.data('room').toString().toUpperCase().score(filter) > 0.0 && (showClosedRooms || !$room.is('.closed'))) {
            $room.show();
        } else {
            $room.hide();
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

            return a.Name.toString().toUpperCase().localeCompare(b.Name.toString().toUpperCase());
        });
        return sortedList;
    }
    
    function getNextRoomListElement($targetList, roomName, count, closed) {
        var nextListElement = null;

        // move the item to before the next element
        $targetList.find('li').each(function () {
            var $this = $(this),
                liRoomCount = $this.data('count'),
                liRoomClosed = $this.hasClass('closed'),
                name = $this.data('name'),
                nameComparison;

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
    
    function addRoomToLobby(roomViewModel) {
        var $room = null,
            roomName = roomViewModel.Name.toString().toUpperCase(),
            $targetList = roomViewModel.Private ? $lobbyPrivateRoomList : $lobbyOtherRoomList,
            i = null;
        
        roomViewModel.processedTopic = ui.processContent(roomViewModel.Topic);
        $room = templates.lobbyroom.tmpl(roomViewModel);

        var nextListElement = getNextRoomListElement($targetList, roomName, roomViewModel.Count, roomViewModel.Closed);

        if (nextListElement !== null) {
            $room.insertBefore(nextListElement);
        } else {
            $room.appendTo($targetList);
        }

        filterIndividualRoom($room);
        utility.updateEmptyListItem($targetList);

        roomCache[roomName] = true;

        // don't try to populate the sortedRoomList while we're initially filling up the lobby
        if (sortedRoomList) {
            var sortedRoomInsertIndex = sortedRoomList.length;
            for (i = 0; i < sortedRoomList.length; i++) {
                if (sortedRoomList[i].Name.toString().toUpperCase().localeCompare(roomName) > 0) {
                    sortedRoomInsertIndex = i;
                    break;
                }
            }
            sortedRoomList.splice(sortedRoomInsertIndex, 0, roomViewModel);
        }

        // handle updates on rooms not currently displayed to clients by removing from the public room list
        if (publicRoomList) {
            for (i = 0; i < publicRoomList.length; i++) {
                if (publicRoomList[i].Name.toString().toUpperCase().localeCompare(roomName) === 0) {
                    publicRoomList.splice(i, 1);
                    break;
                }
            }
        }

        // if it's a private room, make sure that we're displaying the private room section
        if (roomViewModel.Private) {
            $lobbyPrivateRooms.show();
            $lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_OtherRooms'));
        }
    }
    
    var lobby = {
        initialize: function () {
            $loadMoreRooms = $('#load-more-rooms-item');
            $lobbyPrivateRooms = $('#lobby-private');
            $lobbyOtherRooms = $('#lobby-other');
            $lobbyRoomFilterForm = $('#room-filter-form');
            $roomFilterInput = $('#room-filter');
            $closedRoomFilter = $('#room-filter-closed');
            $lobbyOtherRoomList = $('#userlist-lobby');
            $lobbyPrivateRoomList = $('#userlist-lobby-owners');
            templates = {
                lobbyroom: $('#new-lobby-room-template'),
                otherlobbyroom: $('#new-other-lobby-room-template')
            };

            $('li.room .room-row').on('click', function () {
                var roomName = $(this).parent().data('name');
                activateOrOpenRoom(roomName);
            });

            $roomFilterInput.keypress(function (ev) {
                var key = ev.keyCode || ev.which,
                    roomName = $(this).val();

                switch (key) {
                    case Keys.Enter:
                        // if it's an exact match, open/activate the room
                        if (roomCache[roomName.toUpperCase()]) {
                            activateOrOpenRoom(roomName);

                            return;
                        }
                }
            });

            $roomFilterInput.bind('input', function () { $lobbyRoomFilterForm.submit(); })
                .keyup(function () { $lobbyRoomFilterForm.submit(); });

            $closedRoomFilter.click(function () { $lobbyRoomFilterForm.submit(); });

            $lobbyRoomFilterForm.submit(function () {
                var $lobbyRoomsLists = $lobbyPrivateRooms.add($lobbyOtherRooms);

                // hide all elements except those that match the input / closed filters
                $lobbyRoomsLists
                    .find('li:not(.empty)')
                    .each(function () { filterIndividualRoom($(this)); });

                $lobbyRoomsLists.find('ul').each(function () {
                    utility.updateEmptyListItem($(this));
                });
                return false;
            });

            $loadMoreRooms.on('click', function () {
                $.tmpl(templates.lobbyroom, publicRoomList.slice(lastLoadedRoomIndex, lastLoadedRoomIndex + maxRoomsToLoad)).appendTo($lobbyOtherRoomList);
                lastLoadedRoomIndex += maxRoomsToLoad;

                if (lastLoadedRoomIndex < publicRoomList.length) {
                    $loadMoreRooms.show();
                } else {
                    $loadMoreRooms.hide();
                }

                // re-filter lists
                $lobbyRoomFilterForm.submit();
            });
        },
        hideForm: function() {
            $lobbyRoomFilterForm.hide();
        },
        showForm: function() {
            $lobbyRoomFilterForm.show();
        },
        getRooms: function() {
            return sortedRoomList;
        },
        addRoomToLobby: addRoomToLobby,
        updateLobbyRoom: function(room) {
            var $targetList = room.Private === true ? $lobbyPrivateRoomList : $lobbyOtherRoomList,
                $room = $targetList.find('[data-room="' + room.Name + '"]'),
                $count = $room.find('.count'),
                $topic = $room.find('.topic'),
                roomName = room.Name.toString().toUpperCase(),
                processedTopic = ui.processContent(room.Topic);

            // if we don't find the room, we need to create it
            if ($room.length === 0) {
                addRoomToLobby(room);
                return;
            }

            if (room.Count === 0) {
                $count.text(utility.getLanguageResource('Client_OccupantsZero'));
            } else if (room.Count === 1) {
                $count.text(utility.getLanguageResource('Client_OccupantsOne'));
            } else {
                $count.text(utility.getLanguageResource('Client_OccupantsMany', room.Count));
            }

            if (room.Private === true) {
                $room.addClass('locked');
            } else {
                $room.removeClass('locked');
            }

            if (room.Closed === true) {
                $room.addClass('closed');
            } else {
                $room.removeClass('closed');
            }

            $topic.html(processedTopic);

            var nextListElement = getNextRoomListElement($targetList, roomName, room.Count, room.Closed);

            $room.data('count', room.Count);
            if (nextListElement !== null) {
                $room.insertBefore(nextListElement);
            } else {
                $room.appendTo($targetList);
            }

            // Do a little animation
            $room.css('-webkit-animation-play-state', 'running').css('animation-play-state', 'running');
        },
        removeLobbyRoom: function (roomName) {
            var roomNameUppercase = roomName.toString().toUpperCase(),
                i;
            
            if (roomCache[roomNameUppercase]) {
                delete roomCache[roomNameUppercase];
            }
            
            // find the element in the sorted room list and remove it
            for (i = 0; i < sortedRoomList.length; i++) {
                if (sortedRoomList[i].Name.toString().toUpperCase().localeCompare(roomNameUppercase) === 0) {
                    sortedRoomList.splice(i, 1);
                    break;
                }
            }
            
            // find the element in the lobby public room list and remove it
            for (i = 0; i < publicRoomList.length; i++) {
                if (publicRoomList[i].Name.toString().toUpperCase().localeCompare(roomNameUppercase) === 0) {
                    publicRoomList.splice(i, 1);
                    break;
                }
            }
            
            // remove the items from the lobby screen
            var $room = $lobbyOtherRoomList.add($lobbyPrivateRoomList).find('[data-room="' + roomName + '"]');
            $room.remove();
            
            // if we have no private rooms, hide the private rooms section and change the text on the rooms header
            if ($lobbyPrivateRoomList.find('li:not(.empty)').length === 0) {
                $lobbyPrivateRooms.hide();
                $lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_Rooms'));
            }
        },
        updatePrivateLobbyRooms: function (roomName) {
            var $room = $lobbyOtherRoomList.find('li[data-name="' + roomName + '"]');
            $room.addClass('locked').appendTo($lobbyPrivateRoomList);
        },
        populateLobbyRooms: function (rooms, privateRooms) {           
            // Process the topics and populate the roomCache
            $.each(rooms, function(idx, elem) {
                elem.processedTopic = ui.processContent(elem.Topic);
                roomCache[elem.Name.toString().toUpperCase()] = true;
            });
            $.each(privateRooms, function (idx, elem) {
                elem.processedTopic = ui.processContent(elem.Topic);
                roomCache[elem.Name.toString().toUpperCase()] = true;
            });

            // sort private lobby rooms
            var privateSorted = sortRoomList(privateRooms);

            // sort other lobby rooms but filter out private rooms
            publicRoomList = sortRoomList(rooms).filter(function (room) {
                return !privateSorted.some(function (allowed) {
                    return allowed.Name === room.Name;
                });
            });

            sortedRoomList = rooms.sort(function (a, b) {
                return a.Name.toString().toUpperCase().localeCompare(b.Name.toString().toUpperCase());
            });

            $lobbyPrivateRoomList.empty();
            $lobbyOtherRoomList.empty();

            var listOfPrivateRooms = $('<ul/>');
            if (privateSorted.length > 0) {
                $.tmpl(templates.lobbyroom, privateSorted).appendTo(listOfPrivateRooms);
                listOfPrivateRooms.children('li').appendTo($lobbyPrivateRoomList);
                $lobbyPrivateRooms.show();
                $lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_OtherRooms'));
            } else {
                $lobbyPrivateRooms.hide();
                $lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_Rooms'));
            }

            var listOfRooms = $('<ul/>');
            $.tmpl(templates.lobbyroom, publicRoomList.slice(0, maxRoomsToLoad)).appendTo(listOfRooms);
            lastLoadedRoomIndex = listOfRooms.children('li').length;
            listOfRooms.children('li').appendTo($lobbyOtherRoomList);
            if (lastLoadedRoomIndex < publicRoomList.length) {
                $loadMoreRooms.show();
            }
            $lobbyOtherRooms.show();

            if ($('#tabs-lobby').is('.current')) {
                this.showForm();
            } else {
                this.hideForm();
            }

            // re-filter lists
            $lobbyRoomFilterForm.submit();
        },
        roomCache: roomCache
    };

    ui.lobby = lobby;

}(window.jQuery, window, window.chat, window.chat.ui, window.chat.utility));
