(function ($, window, ui, utility) {
    "use strict";

    window.captureDocumentWrite = function (documentWritePath, headerText, elementToAppendTo) {
        $.fn.captureDocumentWrite(documentWritePath, function (content) {
            var nearEnd = ui.isNearTheEnd(),
                roomName = null,
                collapsible = null,
                insertContent = null,
                links = null;

            roomName = elementToAppendTo.closest('ul.messages').attr('id');
            roomName = roomName.substring(9);

            collapsible = $('<div><h3 class="collapsible_title">' + utility.getLanguageResource('Content_HeaderAndToggle', headerText) + '</h3><div class="collapsible_box captureDocumentWrite_content"></div></div>');
            $('.captureDocumentWrite_content', collapsible).append(content);

            // Since IE doesn't render the css if the links are not in the head element, we move those to the head element
            links = $('link', collapsible);
            links.remove();
            $('head').append(links);

            // Remove the target off any existing anchor tags, then re-add target as _blank so it opens new tab (or window)
            $('a', collapsible).removeAttr('target').attr('target', '_blank');

            insertContent = collapsible[0].outerHTML;

            if (ui.shouldCollapseContent(insertContent, roomName)) {
                insertContent = ui.collapseRichContent(insertContent);
            }

            elementToAppendTo.append(insertContent);

            if (nearEnd) {
                ui.scrollToBottom();
            }
        });
    };
})(window.jQuery, window, chat.ui, chat.utility);