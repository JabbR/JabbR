/// <reference path="Scripts/jquery-1.7.js" />
/// <reference path="Scripts/jQuery.tmpl.js" />
/// <reference path="Scripts/jquery.cookie.js" />

(function ($, window) {
    "use strict";

    function padZero(s) {
        s = s.toString();
        if (s.length == 1) {
            return "0" + s;
        }
        return s;
    }

    $.fn.isNearTheEnd = function () {
        return this[0].scrollTop + this.height() >= this[0].scrollHeight;
    };

    String.prototype.fromJsonDate = function () {
        return eval(this.replace(/\/Date\((\d+)\)\//gi, "new Date($1)"))
    };

    Date.prototype.formatDate = function () {
        var m = this.getMonth() + 1,
            d = this.getDate(),
            y = this.getFullYear();

        return m + "/" + d + "/" + y;
    };

    Date.prototype.formatTime = function (showAp) {
        var ap = "";
        var hr = this.getHours();

        if (hr < 12) {
            ap = "AM";
        }
        else {
            ap = "PM";
        }

        if (hr == 0) {
            hr = 12;
        }

        if (hr > 12) {
            hr = hr - 12;
        }

        var mins = padZero(this.getMinutes());
        var seconds = padZero(this.getSeconds());
        return hr + ":" + mins + ":" + seconds
            + (showAp ? " " + ap : "");
    };

    var utility = {
        trim: function (value, length) {
            if (value.length > length) {
                return value.substr(0, length - 3) + '...';
            }
            return value;
        }
    };

    if (!window.chat) {
        window.chat = {};
    }

    window.chat.utility = utility;

})(jQuery, window);