/*global document:true */
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
            collapsibleUtility.ensureContainmentContainerSize();
            var popoutWrapperDivId = window.chat.utility.randomUniqueId('pop-out-');
            // mark the element as floating          
            el.attr('floating', 'true');
            // to put the content back where it was, we'll grab the previous element
            // and save its ID on the clicked pin -- if the elemented doesn't have an
            // ID, we'll give it one.
            el.attr('prev-id', collapsibleUtility.getPrevId(el.parent().prev()));
            // Now we will wrap our content in a div to give use greater control 
            // over the popout
            el.parent().wrap('<div id="' + popoutWrapperDivId + '" class="collapsible_wrapper" />');
            // Add our new element to the end of the #page element and make it draggable
            el.parent().parent().appendTo('#page').draggable({ containment: '#page', scroll: false });
            el.parent().parent().css('position', 'absolute');
        },
        handlePopIn: function (el) {
            // mark the element as not floating
            el.attr('floating', 'false');
            // here, to ensure that the draggable stuff is
            // completely removed, we grab the inner html of our wrapper
            // and stick it back into it's original place
            $(el.parent().parent().html()).insertAfter('#' + el.attr('prev-id'));
            // Remove our wrapper to keep the DOM clean
            el.parent().parent().remove();
        },
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
    $(document).on('click', 'div.collapsible_pin', collapsibleUtility.handlePinClick);

})(jQuery, window, chat.ui);