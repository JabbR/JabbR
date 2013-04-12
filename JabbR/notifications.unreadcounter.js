(function ($) {
    var $unreadCounter = null,
        count = null;

    function set(newCount) {
        count = newCount;
        $unreadCounter.text(count);
        $unreadCounter.data('unread', count);
    }

    $.subscribe('notifications.read', function (ev) {
        set(count - 1);
        handleChange();
    });

    $.subscribe('notifications.readAll', function (ev) {
        set(0);
        handleChange();
    });

    function handleChange() {
        if (count === 0) {
            $.publish('notifications.empty');
        }
    }

    $unreadCounter = $('#js-unread-counter');
    count = $unreadCounter.data('unread');
}(jQuery));
