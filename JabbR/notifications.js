(function ($) {
    var unreadCounter = null,
        notificationsMode = null;

    var UnreadCounter = (function () {
        var $unreadCounter = null,
            count = null;

        return {
            init: function (selector) {
                $unreadCounter = $(selector);
                count = $unreadCounter.data('unread');
                return this;
            },
            set: function (newCount) {
                count = newCount;
                $unreadCounter.text(count);
                $unreadCounter.data('unread', count);
            },
            get: function () {
                return count;
            }
        };
    }());

    unreadCounter = UnreadCounter.init('#js-unread-counter');
    notificationsMode = $('#notifications-container').data('mode');

    $('#notifications-container').on('click', '.js-mark-as-read', function (ev) {
        var $this = $(this),
            dataUrl = $this.data('actionUrl'),
            notificationId = $this.data('notificationId');

        ev.preventDefault();

        var readMention = $.ajax(dataUrl, {
            type: "POST",
            dataType: "json",
            data: {
                notificationId: notificationId
            }
        });

        readMention.done(function () {
            var $anchorParent = $this.parent(),
                $targetNotification = $('li[data-notification-id="' + notificationId + '"]');

            if (notificationsMode === 'unread') {
                $targetNotification.fadeOut('slow', function () {
                    $targetNotification.remove();
                    unreadCounter.set(unreadCounter.get() - 1);
                });
            } else { // remove the action anchor
                $targetNotification.removeClass('notification-unread');

                $anchorParent.fadeOut('slow', function () {
                    $anchorParent.remove();
                    unreadCounter.set(unreadCounter.get() - 1);
                });
            }
        });

        readMention.fail(function () {
            console.log('failed to mark notification as read', 'notification id: ', notificationId);
        });
    });
}(jQuery));