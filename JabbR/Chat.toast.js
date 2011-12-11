﻿/// <reference path="Scripts/jquery-1.7.js" />
(function($) {
    "use strict";

    var toastTimeOut = 10000,
        toastEnabled = false,
        chromeToast = null;

    var toast = {
        initializeToast: function (enableDisableToast) {
            if (window.webkitNotifications) {
                if (window.webkitNotifications.checkPermission() === 0) {
                    enableDisableToast.html('Disable notifications');
                    toastEnabled = true;
                }
            }
        },
        toastMessage: function(message) {
            if (!toastEnabled || !window.webkitNotifications || !(window.webkitNotifications.checkPermission() === 0)) {
                return;
            }
            
            // replace any previous toast
            if (chromeToast && chromeToast.cancel) {
                chromeToast.cancel();
            }
            
            chromeToast = window.webkitNotifications.createNotification(
                "Content/images/logo32.png",
                message.trimmedName,
                $('<div />').html(message.message).text());

            chromeToast.ondisplay = function() {
                setTimeout(function() {
                    chromeToast.cancel();
                }, toastTimeOut);
            };

            chromeToast.onclick = function() {
                hideToast();
            };

            chromeToast.show();
        },
        hideToast: function() {
            if (chromeToast && chromeToast.cancel) {
                chromeToast.cancel();
            }
        },
        toggleEnableToast: function(enableDisableToast) {
            if (window.webkitNotifications) {
                if (!toastEnabled) {
                    window.webkitNotifications.requestPermission(function() {
                        enableDisableToast.html('Disable notifications');
                        toastEnabled = true;
                    });
                }
                else {
                    enableDisableToast.html('Enable notifications');
                    toastEnabled = false;
                }
            }
        }
    };
    
    if (!window.chat) {
        window.chat = {};
    }
    window.chat.toast = toast;
})(jQuery);