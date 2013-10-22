/*jshint bitwise:false */
(function ($, ui, utility) {
    "use strict";

    function ConnectionStatus() {
        this._connectionState = -1;
        this._$connectionStatus = $('#connectionStatus');
        this._$connectionStateChangedPopover = $('#connection-state-changed-popover');
        this._connectionStateIcon = '#popover-content-icon';
        this._$connectionInfoPopover = $('#connection-info-popover');
        this._$connectionInfoContent = $('#connection-info-content');
        this._connectionInfoStatus = '#connection-status';
        this._connectionInfoTransport = '#connection-transport';
        this._popoverTimer = null;
    }

    ConnectionStatus.prototype = {
        _getConnectionInfoPopoverOptions: function (transport) {
            var options = {
                html: true,
                trigger: 'hover',
                delay: {
                    show: 0,
                    hide: 500
                },
                template: this._$connectionInfoPopover,
                content: $.proxy(function () {
                    var connectionInfo = this._$connectionInfoContent;
                    connectionInfo.find(this._connectionInfoStatus).text(utility.getLanguageResource('Client_ConnectedStatus'));
                    connectionInfo.find(this._connectionInfoTransport).text(utility.getLanguageResource('Client_Transport', transport));
                    return connectionInfo.html();
                }, this)
            };
            return options;
        },

        _getConnectionStateChangedPopoverOptions: function (statusText) {
            var options = {
                html: true,
                trigger: 'hover',
                template: this._$connectionStateChangedPopover,
                content: function () {
                    return statusText;
                }
            };
            return options;
        },

        initialize: function (transport) {
            this._$connectionStatus.popover(this._getConnectionInfoPopoverOptions(transport));
        },

        update: function (status, transport) {
            // Change the status indicator here
            if (this._connectionState !== status) {
                if (this._popoverTimer) {
                    clearTimeout(this._popoverTimer);
                }
                this._connectionState = status;
                this._$connectionStatus.popover('destroy');
                switch (status) {
                    case 0: // Connected
                        this._$connectionStatus.removeClass('reconnecting disconnected');
                        this._$connectionStatus.popover(this._getConnectionStateChangedPopoverOptions(utility.getLanguageResource('Client_Connected')));
                        this._$connectionStateChangedPopover.find(this._connectionStateIcon).addClass('icon-ok-sign');
                        this._$connectionStatus.popover('show');
                        this._popoverTimer = setTimeout($.proxy(function () {
                            this._$connectionStatus.popover('destroy');
                            this.initialize(transport);
                            this._popoverTimer = null;
                        }, this), 2000);
                        break;
                    case 1: // Reconnecting
                        this._$connectionStatus.removeClass('disconnected').addClass('reconnecting');
                        this._$connectionStatus.popover(this._getConnectionStateChangedPopoverOptions(utility.getLanguageResource('Client_Reconnecting')));
                        this._$connectionStateChangedPopover.find(this._connectionStateIcon).addClass('icon-question-sign');
                        this._$connectionStatus.popover('show');
                        this._popoverTimer = setTimeout($.proxy(function () {
                            this._$connectionStatus.popover('hide');
                            this._popoverTimer = null;
                        }, this), 5000);
                        break;
                    case 2: // Disconnected
                        this._$connectionStatus.removeClass('reconnecting').addClass('disconnected');
                        this._$connectionStatus.popover(this._getConnectionStateChangedPopoverOptions(utility.getLanguageResource('Client_Disconnected')));
                        this._$connectionStateChangedPopover.find(this.connectionStateIcon).addClass('icon-exclamation-sign');
                        this._$connectionStatus.popover('show');
                        this._popoverTimer = setTimeout($.proxy(function () {
                            this._$connectionStatus.popover('hide');
                            this._popoverTimer = null;
                        }, this), 5000);
                        break;
                }
            }
        }
    };
    
    ui.connectionStatus = new ConnectionStatus();
})(window.jQuery, window.chat.ui, window.chat.utility);
