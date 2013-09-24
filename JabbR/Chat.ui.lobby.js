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
        roomCache = {},
        $document = $(document),
        templates = null,
        Keys = { Enter: 13 },
        Room = chat.Room;
    
    function getLobby() {
        var room = new Room($('#tabs-lobby'),
                        $('#userlist-lobby'),
                        $('#userlist-lobby-owners'),
                        $('#userlist-lobby-active'),
                        $('#messages-lobbu'),
                        $('#roomTopic-lobby'));
        return room;
    }
    
    function loadMoreLobbyRooms() {
        var lobby = getLobby(),
            moreRooms = publicRoomList.slice(lastLoadedRoomIndex, lastLoadedRoomIndex + maxRoomsToLoad);

        populateLobbyRoomList(moreRooms, templates.lobbyroom, lobby.users);
        lastLoadedRoomIndex = lastLoadedRoomIndex + maxRoomsToLoad;

        // re-filter lists
        $lobbyRoomFilterForm.submit();
    }
    
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
    
    function populateLobbyRoomList(item, template, listToPopulate) {
        $.tmpl(template, item).appendTo(listToPopulate);
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

            if (name === undefined) {
                return true;
            }

            nameComparison = name.toString().toUpperCase().localeCompare(roomName);

            // skip this element
            if (nameComparison === 0) {
                return true;
            }

            // skip closed rooms which always go after unclosed ones
            if (!liRoomClosed && closed) {
                return true;
            }

            // skip where we have more occupants
            if (liRoomCount > count) {
                return true;
            }

            // skip where we have the same number of occupants but the room is alphabetically earlier
            if (liRoomCount === count && nameComparison < 0) {
                return true;
            }

            nextListElement = $this;
            return false;
        });

        return nextListElement;
    }
    
    function addRoomToLobby(roomViewModel) {
        var lobby = getLobby(),
            $room = null,
            roomName = roomViewModel.Name.toString().toUpperCase(),
            count = roomViewModel.Count,
            closed = roomViewModel.Closed,
            nonPublic = roomViewModel.Private,
            $targetList = roomViewModel.Private ? lobby.owners : lobby.users,
            i = null;
        
        roomViewModel.processedTopic = ui.processContent(roomViewModel.Topic);
        $room = templates.lobbyroom.tmpl(roomViewModel);

        var nextListElement = getNextRoomListElement($targetList, roomName, count, closed);

        if (nextListElement !== null) {
            $room.insertBefore(nextListElement);
        } else {
            $room.appendTo($targetList);
        }

        filterIndividualRoom($room);
        lobby.setListState($targetList);

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
        if (nonPublic) {
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
            templates = {
                lobbyroom: $('#new-lobby-room-template'),
                otherlobbyroom: $('#new-other-lobby-room-template')
            };

            $document.on('click', 'li.room .room-row', function () {
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
                var room = ui.getCurrentRoomElements(),
                    $lobbyRoomsLists = $lobbyPrivateRooms.add($lobbyOtherRooms);

                // hide all elements except those that match the input / closed filters
                $lobbyRoomsLists
                    .find('li:not(.empty)')
                    .each(function () { filterIndividualRoom($(this)); });

                $lobbyRoomsLists.find('ul').each(function () {
                    room.setListState($(this));
                });
                return false;
            });

            $document.on('click', '#load-more-rooms-item', function () {
                var spinner = $loadMoreRooms.find('i');
                spinner.addClass('icon-spin');
                spinner.show();
                var loader = $loadMoreRooms.find('.load-more-rooms a');
                loader.html(' ' + utility.getLanguageResource('LoadingMessage'));
                loadMoreLobbyRooms();
                spinner.hide();
                spinner.removeClass('icon-spin');
                loader.html(utility.getLanguageResource('Client_LoadMore'));
                if (lastLoadedRoomIndex < publicRoomList.length) {
                    $loadMoreRooms.show();
                } else {
                    $loadMoreRooms.hide();
                }
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
            var lobby = getLobby(),
                $targetList = room.Private === true ? lobby.owners : lobby.users,
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
                i = null;
            
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
            var lobby = getLobby(),
            $room = lobby.users.add(lobby.owners).find('[data-room="' + roomName + '"]');
            $room.remove();
            
            // if we have no private rooms, hide the private rooms section and change the text on the rooms header
            if (lobby.owners.find('li:not(.empty)').length === 0) {
                $lobbyPrivateRooms.hide();
                $lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_Rooms'));
            }
        },
        updatePrivateLobbyRooms: function (roomName) {
            var lobby = getLobby(),
                $room = lobby.users.find('li[data-name="' + roomName + '"]');

            $room.addClass('locked').appendTo(lobby.owners);
        },
        populateLobbyRooms: function (rooms, privateRooms) {
            var lobby = getLobby(),
                i;
            if (!lobby.isInitialized()) {
                // Process the topics
                for (i = 0; i < rooms.length; ++i) {
                    rooms[i].processedTopic = ui.processContent(rooms[i].Topic);
                }

                for (i = 0; i < privateRooms.length; ++i) {
                    privateRooms[i].processedTopic = ui.processContent(privateRooms[i].Topic);
                }

                // Populate the room cache
                for (i = 0; i < rooms.length; ++i) {
                    roomCache[rooms[i].Name.toString().toUpperCase()] = true;
                }

                for (i = 0; i < privateRooms.length; ++i) {
                    roomCache[privateRooms[i].Name.toString().toUpperCase()] = true;
                }

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

                lobby.owners.empty();
                lobby.users.empty();

                var listOfPrivateRooms = $('<ul/>');
                if (privateSorted.length > 0) {
                    populateLobbyRoomList(privateSorted, templates.lobbyroom, listOfPrivateRooms);
                    listOfPrivateRooms.children('li').appendTo(lobby.owners);
                    $lobbyPrivateRooms.show();
                    $lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_OtherRooms'));
                } else {
                    $lobbyPrivateRooms.hide();
                    $lobbyOtherRooms.find('.nav-header').html(utility.getLanguageResource('Client_Rooms'));
                }

                var listOfRooms = $('<ul/>');
                populateLobbyRoomList(publicRoomList.slice(0, maxRoomsToLoad), templates.lobbyroom, listOfRooms);
                lastLoadedRoomIndex = listOfRooms.children('li').length;
                listOfRooms.children('li').appendTo(lobby.users);
                if (lastLoadedRoomIndex < publicRoomList.length) {
                    $loadMoreRooms.show();
                }
                $lobbyOtherRooms.show();
            }

            if (lobby.isActive()) {
                // update cache of room names
                $lobbyRoomFilterForm.show();
            }

            // re-filter lists
            $lobbyRoomFilterForm.submit();
        },
        roomCache: roomCache
    };

    ui.lobby = lobby;

}(window.jQuery, window, window.chat, window.chat.ui, window.chat.utility));
