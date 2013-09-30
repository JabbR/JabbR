(function ($, window, chat, ui, utility) {
    "use strict";

    var $loadMoreRooms = null,
        publicRoomList = null,
        sortedRoomList = null,
        $lobbyPrivateRooms = null,
        $lobbyOtherRooms = null,
        $lobbyRoomFilterForm = null,
        $roomFilterInput = null,
        $closedRoomFilter = null,
        $lobbyOtherRoomList = null,
        $lobbyPrivateRoomList = null,
        roomCache = {};
    
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
    
    function addRoomToLobby(roomViewModel) {
        var roomName = roomViewModel.Name.toString().toUpperCase(),
            i = null;
        
        roomViewModel.processedTopic = ui.processContent(roomViewModel.Topic);
        chat.ui.lobbyTab.addOrUpdateRoom(roomViewModel);
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
        },
        getRooms: function() {
            return sortedRoomList;
        },
        addRoomToLobby: addRoomToLobby,
        updateLobbyRoom: function (roomViewModel) {
            roomViewModel.processedTopic = ui.processContent(roomViewModel.Topic);
            chat.ui.lobbyTab.addOrUpdateRoom(roomViewModel);
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
            var startDate = new Date();
            // sort private lobby rooms
            var privateSorted = sortRoomList(privateRooms);
            $.each(privateSorted, function (idx, elem) {
                elem.processedTopic = ui.processContent(elem.Topic);
                roomCache[elem.Name.toString().toUpperCase()] = true;
                chat.ui.lobbyTab.addOrUpdateRoom(elem);
            });
            
            // sort other lobby rooms but filter out private rooms
            var publicRoomList = rooms.filter(function (room) {
                return !privateSorted.some(function (allowed) {
                    return allowed.Name === room.Name;
                });
            });
            publicRoomList = sortRoomList(publicRoomList);
            $.each(publicRoomList, function (idx, elem) {
                elem.processedTopic = ui.processContent(elem.Topic);
                roomCache[elem.Name.toString().toUpperCase()] = true;

                if (idx < 100) {
                    chat.ui.lobbyTab.addOrUpdateRoom(elem);
                }
            });
            $('#lobby-other').show();
            alert(new Date() - startDate);
            
            // build alphabetically sorted room list for autocomplete
            sortedRoomList = rooms.sort(function (a, b) {
                return a.Name.toString().toUpperCase().localeCompare(b.Name.toString().toUpperCase());
            });

            /*
            if (lastLoadedRoomIndex < publicRoomList.length) {
                $loadMoreRooms.show();
            }
            $lobbyOtherRooms.show();

            // re-filter lists
            $lobbyRoomFilterForm.submit();*/
        },
        roomCache: roomCache
    };

    ui.lobby = lobby;

}(window.jQuery, window, window.chat, window.chat.ui, window.chat.utility));
