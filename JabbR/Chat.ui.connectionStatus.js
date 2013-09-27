/// <reference path="Scripts/jquery-2.0.3.js" />

/*jshint bitwise:false */
(function ($, window, document, chat, ui, utility) {
    "use strict";

    var popoverTimer = null,
        connectionState = -1,
        $connectionStatus = $('#connectionStatus'),
        $connectionStateChangedPopover = $('#connection-state-changed-popover'),
        connectionStateIcon = '#popover-content-icon',
        $connectionInfoPopover = $('#connection-info-popover'),
        $connectionInfoContent = $('#connection-info-content'),
        connectionInfoStatus = '#connection-status',
        connectionInfoTransport = '#connection-transport';
    
    function getConnectionStateChangedPopoverOptions(statusText) {
        var options = {
            html: true,
            trigger: 'hover',
            template: $connectionStateChangedPopover,
            content: function () {
                return statusText;
            }
        };
        return options;
    }
    
    function getConnectionInfoPopoverOptions(transport) {
        var options = {
            html: true,
            trigger: 'hover',
            delay: {
                show: 0,
                hide: 500
            },
            template: $connectionInfoPopover,
            content: function () {
                var connectionInfo = $connectionInfoContent;
                connectionInfo.find(connectionInfoStatus).text(utility.getLanguageResource('Client_ConnectedStatus'));
                connectionInfo.find(connectionInfoTransport).text(utility.getLanguageResource('Client_Transport', transport));
                return connectionInfo.html();
            }
        };
        return options;
    }

    var connectionStatus = {
        initialize: function(transport) {
            $connectionStatus.popover(getConnectionInfoPopoverOptions(transport));
        },
        showStatus: function(status, transport) {
            // Change the status indicator here
            if (connectionState !== status) {
                if (popoverTimer) {
                    clearTimeout(popoverTimer);
                }
                connectionState = status;
                $connectionStatus.popover('destroy');
                switch (status) {
                    case 0: // Connected
                        $connectionStatus.removeClass('reconnecting disconnected');
                        $connectionStatus.popover(getConnectionStateChangedPopoverOptions(utility.getLanguageResource('Client_Connected')));
                        $connectionStateChangedPopover.find(connectionStateIcon).addClass('icon-ok-sign');
                        $connectionStatus.popover('show');
                        popoverTimer = setTimeout(function () {
                            $connectionStatus.popover('destroy');
                            ui.initializeConnectionStatus(transport);
                            popoverTimer = null;
                        }, 2000);
                        break;
                    case 1: // Reconnecting
                        $connectionStatus.removeClass('disconnected').addClass('reconnecting');
                        $connectionStatus.popover(getConnectionStateChangedPopoverOptions(utility.getLanguageResource('Client_Reconnecting')));
                        $connectionStateChangedPopover.find(connectionStateIcon).addClass('icon-question-sign');
                        $connectionStatus.popover('show');
                        popoverTimer = setTimeout(function () {
                            $connectionStatus.popover('hide');
                            popoverTimer = null;
                        }, 5000);
                        break;
                    case 2: // Disconnected
                        $connectionStatus.removeClass('reconnecting').addClass('disconnected');
                        $connectionStatus.popover(getConnectionStateChangedPopoverOptions(utility.getLanguageResource('Client_Disconnected')));
                        $connectionStateChangedPopover.find(connectionStateIcon).addClass('icon-exclamation-sign');
                        $connectionStatus.popover('show');
                        popoverTimer = setTimeout(function () {
                            $connectionStatus.popover('hide');
                            popoverTimer = null;
                        }, 5000);
                        break;
                }
            }
        }
    };

    ui.connectionStatus = connectionStatus;
})(window.jQuery, window, window.document, window.chat, window.chat.ui, window.chat.utility);
