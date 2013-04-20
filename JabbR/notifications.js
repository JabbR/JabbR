(function ($, utility) {
    var notificationsMode = null,
        templates = {
            multiline: $('#multiline-content-template')
        };

    notificationsMode = $('#notifications-container').data('mode');

    $('#notifications-container').on('click', '.js-mark-as-read', function (ev) {
        var $this = $(this),
            dataUrl = $this.data('actionUrl'),
            notificationId = $this.data('notificationId');

        ev.preventDefault();
        
        if ($this.hasClass('disabled')) {
            return false;
        }

        $.publish('notifications.mark', [{ url: dataUrl, notificationId: notificationId }]);
    });

    $('.js-mark-all-as-read').on('click', function (ev) {
        var $this = $(this),
            dataUrl = $this.data('actionUrl');

        ev.preventDefault();

        $.publish('notifications.markAll', [{ url: dataUrl }]);
    });

    $('.linkify').each(function () {
        var $this = $(this),
            content = $this.html();

        content = utility.processContent(content, templates);
        $this.html(content);
    });

    $('.notification-item .time').each(function () {
        var $time = $(this),
            date = $time.data('timestamp').fromJsonDate();

        $time.text(date.toLocaleString());
    });

    $.subscribe('notifications.mark', function (ev, markAsReadRequest) {
        var readMention = $.ajax(markAsReadRequest.url, {
            type: "POST",
            dataType: "json",
            data: {
                notificationId: markAsReadRequest.notificationId
            }
        });

        readMention.done(function () {
            $.publish('notifications.read', [markAsReadRequest.notificationId]);
        });

        readMention.fail(function () {
            console.log('failed to mark notification as read', 'notification id: ', notificationId);
        });
    });

    $.subscribe('notifications.read', function (ev, notificationId) {
        var $targetNotification = $('li[data-notification-id="' + notificationId + '"]'),
            $anchor = $targetNotification.find('.js-mark-as-read');

        if (notificationsMode === 'unread') {
            $targetNotification.fadeOut('slow', function () {
                $targetNotification.remove();
            });
        } else { // remove the action anchor
            $targetNotification.removeClass('notification-unread');

            $anchor.fadeOut('slow', function () {
                $anchor.remove();
            });
        }
    });

    $.subscribe('notifications.markAll', function (ev, request) {
        var readMention = $.ajax(request.url, {
            type: "POST",
            dataType: "json"
        });

        readMention.done(function () {
            $.publish('notifications.readAll');
        });

        readMention.fail(function () {
            console.log('failed to mark ALL notifications as read');
        });
    });

    $.subscribe('notifications.readAll', function (ev) {
        var $targetNotifications = $('.notification-unread'),
            $anchor = $targetNotifications.find('.js-mark-as-read');

        if (notificationsMode === 'unread') {
            $targetNotifications.fadeOut('slow', function () {
                $targetNotifications.remove();
                $('#no-notifications').fadeIn('fast');
                $('#notifications-pager').fadeOut('fast');
            });
        } else { // remove the action anchor
            $targetNotifications.removeClass('notification-unread');

            $anchor.fadeOut('slow', function () {
                $anchor.remove();
            });
        }
    });

    $.subscribe('notifications.empty', function (ev) {
        $('.js-mark-all-as-read').addClass('disabled');
    });
}(jQuery, window.chat.utility));