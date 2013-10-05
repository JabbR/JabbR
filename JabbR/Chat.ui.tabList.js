/// <reference path="Scripts/jquery-2.0.3.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />
/// <reference path="Chat.toast.js" />
/// <reference path="Scripts/livestamp.min.js" />

/*jshint bitwise:false */
(function ($, window, chat, ui) {
    "use strict";

    var $tabs = null,
        $tabsList = null,
        $tabsDropdown = null,
        $tabsDropdownButton = null,
        templates = null,
        Keys = { Tab: 9 };

    var tabList = {
        initialize: function () {
            $tabs = $('#tabs, #tabs-dropdown');
            $tabsList = $('#tabs');
            $tabsDropdown = $('#tabs-dropdown');
            $tabsDropdownButton = $('#tabs-dropdown-rooms');
            templates = {
                tab: $('#new-tab-template')
            };
            
            var activateOrOpenRoom = function (roomName) {
                var room = ui.getRoomElements(roomName);

                if (room !== null) {
                    ui.setActiveRoom(roomName);
                }
                else {
                    $(ui).trigger(ui.events.openRoom, [roomName]);
                }
            };

            var _this = this;
            
            // handle tab cycling
            $(document).on('keydown', function(ev) {
                // ctrl + tab event is sent to the page in firefox when the user probably means to change browser tabs
                if (ev.keyCode === Keys.Tab && !ev.ctrlKey && ui.getMessage() === "") {
                    var tabName = null;

                    if (!ev.shiftKey) {
                        // Next tab
                        tabName = _this.getNextTabName();
                    } else {
                        // Prev tab
                        tabName = _this.getPreviousTabName();
                    }

                    activateOrOpenRoom(tabName);
                }
            });
            
            $(document).on('click', '#tabs li, #tabs-dropdown li', function () {
                var roomName = $(this).data('name');
                activateOrOpenRoom(roomName);
            });

            $(document).on('mousedown', '#tabs li.room, #tabs-dropdown li.room', function (ev) {
                // if middle mouse
                if (ev.which === 2) {
                    $(ui).trigger(ui.events.closeRoom, [$(this).data('name')]);
                }
            });

            $(document).on('click', '#tabs li .close, #tabs-dropdown li .close', function (ev) {
                var roomName = $(this).closest('li').data('name');

                $(ui).trigger(ui.events.closeRoom, [roomName]);

                ev.preventDefault();
                return false;
            });

            $('#tabs, #tabs-dropdown').dragsort({
                placeHolderTemplate: '<li class="room placeholder"><a><span class="content"></span></a></li>',
                dragBetween: true,
                dragStart: function () {
                    var roomName = $(this).closest('li').data('name'),
                        closeButton = $(this).find('.close');

                    // if we have a close that we're over, close the window and bail, otherwise activate the tab
                    if (closeButton.length > 0 && closeButton.is(':hover')) {
                        $(ui).trigger(ui.events.closeRoom, [roomName]);
                        return false;
                    } else {
                        activateOrOpenRoom(roomName);
                        return true;
                    }
                },
                dragEnd: function () {
                    var roomTabOrder = [],
                        $roomTabs = $('#tabs li, #tabs-dropdown li');

                    for (var i = 0; i < $roomTabs.length; i++) {
                        roomTabOrder[i] = $($roomTabs[i]).data('name');
                    }

                    $(ui).trigger(ui.events.tabOrderChanged, [roomTabOrder]);

                    // check for tab overflow for one edge case - sort order hasn't changed but user 
                    // dragged the last item in the main list to be the first item in the dropdown.
                    // todo: check if this is necessary
                    ui.tabList.updateTabOverflow();
                }
            });
        },
        
        getTabNames: function() {
            var tabNames = [];
            $tabs.children('li').each(function () {
                tabNames.push($(this).data('name'));
            });
            return tabNames;
        },
        getRoomTabNames: function() {
            var roomTabNames = [];
            $tabs.children('li.room').each(function () {
                roomTabNames.push($(this).data('name'));
            });
            return roomTabNames;
        },
        getCurrentTabName: function() {
            var $tab = $tabs.children('li.current');
            return $tab.data('name');
        },
        setCurrentTab: function(tabName) {
            $tabs.children('li.current').removeClass('current');
            $tabs.children('li[data-name="' + tabName + '"]').addClass('current');
        },
        getNextTabName: function() {
            var tabNames = this.getTabNames(),
                currentTabName = this.getCurrentTabName(),
                tabIndex = tabNames.indexOf(currentTabName);

            return tabNames[(tabIndex + 1) % tabNames.length];
        },
        getPreviousTabName: function() {
            var tabNames = this.getTabNames(),
                currentTabName = this.getCurrentTabName(),
                tabIndex = tabNames.indexOf(currentTabName);

            return tabNames[(tabIndex + tabNames.length - 1) % tabNames.length];
        },

        addTab: function(tabViewModel) {
            templates.tab.tmpl(tabViewModel).data('name', tabViewModel.name).appendTo($tabsDropdown);
            
            this.updateTabOverflow();
            this._updateAccessKeys();
        },
        removeTab: function (tabName) {
            $tabs.children('li[data-name="' + tabName + '"]').remove();
            
            this.updateTabOverflow();
            this._updateAccessKeys();
        },
        updateTabOrder: function(tabNameArray) {
            $.each(tabNameArray.reverse(), function (el, name) {
                $tabs.children('li[data-name="' + name + '"]').prependTo($tabsList);
            });

            this.updateTabOverflow();
            this._updateAccessKeys();
        },
        updateTabOverflow: function () {
            var lastOffsetLeft = 0,
                sliceIndex = -1,
                $roomTabs = null,
                overflowedRoomTabs = null;

            // move all (non-dragsort) tabs to the first list
            $tabs.last().find('li:not(.placeholder)').each(function () { $(this).detach().appendTo($tabsList); });

            // find overflow and move it all to the dropdown list ul
            $roomTabs = $tabsList.find('li:not(.placeholder)');
            $roomTabs.each(function (idx) {
                var thisOffsetLeft = $(this).offset().left;
                if (thisOffsetLeft <= lastOffsetLeft) {
                    sliceIndex = idx;
                    return false;
                }
                lastOffsetLeft = thisOffsetLeft;
                return true;
            });

            // move all elements from here to the dropdown list
            if (sliceIndex !== -1) {
                $tabsDropdownButton.fadeIn('slow');
                overflowedRoomTabs = $roomTabs.slice(sliceIndex);
                for (var i = overflowedRoomTabs.length - 1; i >= 0; i--) {
                    $(overflowedRoomTabs[i]).prependTo($tabsDropdown);
                }
            } else {
                $tabsDropdownButton.fadeOut('slow').parent().removeClass('open');
            }

            return;
        },
        
        _updateAccessKeys: function () {
            // make first 10 non-lobby tabs have accesskeys 1-9,0
            $.each($tabs.find('li:not(.lobby) > a'), function (index, item) {
                if (index < 10) {
                    $(item).attr('accesskey', ((index + 1) % 10).toString());
                } else {
                    $(item).attr('accesskey', null);
                }
            });
        }
    };

    ui.tabList = tabList;
}(window.jQuery, window, window.chat, window.chat.ui));
