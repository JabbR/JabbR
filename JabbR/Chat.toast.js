/// <reference path="Scripts/jquery-1.7.js" />
(function ($, window, utility) {
    "use strict";

    var toast = {
        current: null,
        timeOut: 10000,

        //default all permission checks to not allowed
        isAllowed: function () { return false },
        isNotConfigured: function () { return false },
        isBlocked: function () { return true; },

        canToast: function () {
            return !this.isBlocked();
        },
        ensureToast: function (preferences) {
            if (this.isNotConfigured()) {
                preferences.canToast = false;
            }
        },
        onToastShow: function () {
            setTimeout(function () {
                toast.hideToast();
            }, toast.timeOut);
        },
        onToastClick: function () {
            var room = toast.current.roomName;
            toast.hideToast();
            // Trigger the focus events - focus the window and open the source room
            window.focus();
            $(toast).trigger('toast.focus', [room]);
        },
        toastMessage: function (msg, roomName) {
            if (!this.isAllowed()) {
                return;
            }
            this.hideToast();

            this.current = this.createNotification(msg, roomName);
            //little dirty, but it makes triggering focus later easier, without using a static variable
            this.current.roomName = roomName;
        },
        hideToast: function () {
            if (this.current) {
                this.removeNotification(this.current);
                this.current = null;
            }
        },
        enableToast: function (callback) {
            var deferred = $.Deferred();
            // If not configured, request permission
            if (this.isNotConfigured()) {
                this.requestPermission(function () {
                    if (!toast.isAllowed()) {
                        deferred.reject();
                    }
                    else {
                        deferred.resolve();
                    }
                });
            }
            else if (this.isAllowed()) {
                // If we're allowed then just resolve here
                deferred.resolve();
            }
            else {
                // We don't have permission
                deferred.reject();
            }

            return deferred;
        }
    };


    var html5Toast = function () {
        return {
            isAllowed: function () { return Notification.permission === 'granted' },
            isNotConfigured: function () { return Notification.permission === 'default' },
            isBlocked: function () { return Notification.permission === 'denied' },

            createNotification: function (msg, roomName) {
                //firefox doesnt set a limitation on notification title, but we will use an arbituary sane limit
                var title = utility.trim(message.name, 50) + ' (' + roomName + ')';
                var notification = new Notification(title, {
                    lang: '',
                    dir: 'auto',
                    body: $('<div/>').html(msg.message).text(),
                    tag: roomName,
                    icon: 'Content/images/logo32.png'
                });
                notification.onshow = this.onToastShow;
                notification.onclick = this.onToastClick;
                return notification;
            },
            removeNotification: function (notification) {
                notification.close();
            },
            requestPermission: function (callback) {
                Notification.requestPermission(callback);
            }
        }
    };

    var webkitToast = function () {
        return {
            isAllowed: function () { return window.webkitNotifications.checkPermission() === 0 },
            isNotConfigured: function () { return window.webkitNotifications.checkPermission() === 1 },
            isBlocked: function () { return window.webkitNotifications.checkPermission() === 2 },

            createNotification: function (message, roomName) {
                var toastTitle = utility.trim(message.name, 21);
                // we can reliably show 22 chars
                if (toastTitle.length <= 19) {
                    toastTitle += ' (' + utility.trim(roomName, 19 - toastTitle.length) + ')';
                }

                var notification = window.webkitNotifications.createNotification(
                    'Content/images/logo32.png',
                    toastTitle,
                    $('<div/>').html(message.message).text());

                notification.ondisplay = this.onToastShow;
                notification.onclick = this.onToastClick;
                notification.show();
                return notification;
            },
            removeNotification: function (notification) {
                notification.cancel();
            },
            requestPermission: function (callback) {
                window.webkitNotifications.requestPermission(callback);
            }
        };
    };

    if (!window.chat) {
        window.chat = {};
    }

    //pick which browser implementation we want
    if (window.webkitNotifications) {
        toast = $.extend(toast, webkitToast());
    } else if (typeof (Notification) !== 'undefined') {
        toast = $.extend(toast, html5Toast());
    }

    window.chat.toast = toast;

})(jQuery, window, window.chat.utility);
