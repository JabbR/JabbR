(function ($, window, ui) {
    'use strict';

    var collapsibleUtility = {
        handlePinClick: function () {
            if ($(this).attr('floating') === 'true') {
                collapsibleUtility.handlePopIn($(this));
            }
            else {
                collapsibleUtility.handlePopOut($(this));
            }
        },
        handlePopOut: function (el) {
            $('#page').height($(window).height()).width($(window).width());
            var divId = window.chat.utility.randomUniqueId('pop-out-');
            el.attr('floating', 'true')
                    .attr('prev-id', collapsibleUtility.getPrevId(el.parent().prev()))
                    .parent()
                    .wrap('<div id=' + divId + '/>').parent()
                        .addClass('collapsible_wrapper')
                        .appendTo('#page')
                        .draggable({ containment: '#page', scroll: false });
            $('#' + divId).css('position', 'absolute');
        },
        handlePopIn: function (el) {
            $('#page').height($(window).height()).width($(window).width());
            el.attr('floating', 'false');
            $(el.parent().parent().html()).
                    insertAfter('#' + el.attr('prev-id'));
            el.parent().parent().remove();
        },
        getPrevId: function (el) {
            var prevId = el.attr('id');
            if (prevId === null || prevId === '') {
                prevId = window.chat.utility.randomUniqueId('prev-pop-out-');
                el.attr('id', prevId);
            }
            return prevId;
        },
        hookPins: function () {
            $(document).off('click', 'div.collapsible_pin');
            $(document).on('click', 'div.collapsible_pin', collapsibleUtility.handlePinClick);
        }
    }
    collapsibleUtility.hookPins();

})(jQuery, window, chat.ui);