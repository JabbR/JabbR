(function ($, window, ui) {
    "use strict";

    window.captureDocumentWrite = function (documentWritePath, headerText, elementToAppendTo) {
        $.fn.captureDocumentWrite(documentWritePath, function (content) {
            var nearEnd = ui.isNearTheEnd(),
                collapsible = null,
                links = null;

            collapsible = $('<div><h3 class="collapsible_title">' + headerText + ' (click to show/hide)</h3><div class="collapsible_box captureDocumentWrite_content"></div></div>');
            $('.captureDocumentWrite_content', collapsible).append(content);

            // Since IE doesn't render the css if the links are not in the head element, we move those to the head element
            links = $('link', collapsible);
            links.remove();
            $('head').append(links);

            // Remove the target off any existing anchor tags, then re-add target as _blank so it opens new tab (or window)
            $('a', collapsible).removeAttr('target').attr('target', '_blank');

            elementToAppendTo.append(collapsible);

            if (nearEnd) {
                ui.scrollToBottom();
            }
        });
    };
})(jQuery, window, chat.ui);