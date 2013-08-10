(function ($) {
    "use strict";
    
    var $unreadCounter = $('#js-unread-counter'),
        count = $unreadCounter.data('unread');

    function set(newCount) {
        count = newCount;
        $unreadCounter.text(count);
        $unreadCounter.data('unread', count);
    }

    $.subscribe('notifications.read', function () {
        set(count - 1);
        handleChange();
    });

    $.subscribe('notifications.readAll', function () {
        set(0);
        handleChange();
    });

    function handleChange() {
        if (count === 0) {
            $.publish('notifications.empty');
        }
    }
}(window.jQuery));
