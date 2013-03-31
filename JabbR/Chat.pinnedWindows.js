/*global document:true */
(function ($, window, ui) {
    'use strict';

    var collapsibleUtility = {
        ensureContainmentContainerSize: function () {
            $('#page').height($(window).height()).width($(window).width());
        },
        getPrevId: function (el) {
            var prevId = el.attr('id');
            if (prevId === undefined || prevId === '') {
                prevId = window.chat.utility.randomUniqueId('prev-pop-out-');
                el.attr('id', prevId);
            }
            return prevId;
        }
    };

})(jQuery, window, chat.ui);