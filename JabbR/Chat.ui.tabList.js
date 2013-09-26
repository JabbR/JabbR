/// <reference path="Scripts/jquery-2.0.3.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />
/// <reference path="Chat.toast.js" />
/// <reference path="Scripts/livestamp.min.js" />

/*jshint bitwise:false */
(function ($, window, chat, ui, utility) {
    "use strict";

    var $tabs = null,
        $tabsList = null,
        $tabsDropdown = null,
        $tabsDropdownButton = null,
        templates = null;

    var tabList = {
        initialize: function () {
            $tabs = $('#tabs, #tabs-dropdown');
            $tabsList = $('#tabs');
            $tabsDropdown = $('#tabs-dropdown');
            $tabsDropdownButton = $('#tabs-dropdown-rooms');
            templates = {
                tab: $('#new-tab-template')
            };
        },
        
        getRoomTabNames: function () {
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
}(window.jQuery, window, window.chat, window.chat.ui, window.chat.utility));
