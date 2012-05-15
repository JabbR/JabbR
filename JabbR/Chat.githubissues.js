(function ($, window, ui) {
    'use strict';

    window.addGitHubIssue = function (issue) {
        // Keep track of whether we're need the end, so we can auto-scroll once the tweet is added.
        var nearEnd = ui.isNearTheEnd(),
            elements = null;

        elements = $('div.git-hub-issue-' + issue.data.number)
            .removeClass('git-hub-issue-' + issue.data.number);


        issue.data.body = chat.utility.markdownToHtml(chat.utility.encodeHtml(issue.data.body));

        // Process the template, and add it in to the div.
        $('#github-issues-template').tmpl(issue.data).appendTo(elements);

        // After the string has been added to the template etc, remove any existing targets and re-add with _blank
        $('a', elements).removeAttr('target').attr('target', '_blank');

        $('.js-relative-date').timeago();
        // If near the end, scroll.
        if (nearEnd) {
            ui.scrollToBottom();
        }
        elements.append('<script src="https://api.github.com/users/' + issue.data.user.login + '?callback=addGitHubIssuesUser"></script>');
        if (issue.data.assignee != undefined) {
            elements.append('<script src="https://api.github.com/users/' + issue.data.assignee.login + '?callback=addGitHubIssuesUser"></script>');
        }
    };

    window.addGitHubIssuesUser = function (user) {
        var elements = $("a.github-issue-user-" + user.data.login);
        elements.attr("href", user.data.html_url);
    };

})(jQuery, window, chat.ui);