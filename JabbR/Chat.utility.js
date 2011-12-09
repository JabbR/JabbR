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

    // returns the date portion only (strips time)
    Date.prototype.toDate = function () {
        return new Date(this.getFullYear(), this.getMonth(), this.getDate());
    }

    // returns difference (this - d) in days
    Date.prototype.diffDays = function (d) {
        var t1 = this.getTime(),
            t2 = d.getTime();

        return parseInt((t1 - t2) / (24 * 3600 * 1000));
    };

    var utility = {
        trim: function (value, length) {
            if (value.length > length) {
                return value.substr(0, length - 3) + '...';
            }
            return value;
        },
        randomUniqueId: function (prefix) {
            var n = Math.floor(Math.random() * 100);
            while ($("#" + prefix + n.toString()).length > 0)
                n = Math.Floor(Math.random() * 100);
            return prefix + n;
        } 
    };

    if (!window.chat) {
        window.chat = {};
    }

    window.chat.utility = utility;

})(jQuery, window);