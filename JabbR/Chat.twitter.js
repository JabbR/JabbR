(function ($, window, ui) {
    "use strict";

    window.addTweet = function (tweet) {
        // Keep track of whether we're need the end, so we can auto-scroll once the tweet is added.
        var nearEnd = ui.isNearTheEnd(),
            elements = null;

        // Grab any elements we need to process.
        elements = $('div.tweet_' + tweet.id_str)
        // Strip the classname off, so we don't process this again if someone posts the same tweet.
        .removeClass('tweet_' + tweet.id_str)
        // Add the CSS class for formatting (this is so we don't get height/border while loading).
        .addClass('tweet');
        tweet.text = chat.utility.markdownToHtml(tweet.text);
        // Process the template, and add it in to the div.
        $('#tweet-template').tmpl(tweet).appendTo(elements);
        $("time.js-relative-date").timeago();

        // After the string has been added to the template etc, remove any existing targets and re-add with _blank
        $('a', elements).removeAttr('target').attr('target', '_blank');
        
        // If near the end, scroll.
        if (nearEnd) {
            ui.scrollToBottom();
        }
    };

})(jQuery, window, chat.ui);