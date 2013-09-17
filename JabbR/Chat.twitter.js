(function ($, window, ui) {
    "use strict";

    window.addTweet = function (tweet) {
        // Keep track of whether we're near the end, so we can auto-scroll once the tweet is added.
        var nearEnd = ui.isNearTheEnd(),
            elements = null,
            tweetSegment = '/statuses/',
            id = tweet.url.substring(tweet.url.indexOf(tweetSegment) + tweetSegment.length);


        // Grab any elements we need to process.
        elements = $('div.tweet_' + id)
        // Strip the classname off, so we don't process this again if someone posts the same tweet.
        .removeClass('tweet_' + id);

        // Process the template, and add it in to the div.
        $('#tweet-template').tmpl(tweet).appendTo(elements);

        // If near the end, scroll.
        if (nearEnd) {
            ui.scrollToBottom();
        }
    };

})(window.jQuery, window, chat.ui);
