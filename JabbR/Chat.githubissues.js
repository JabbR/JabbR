(function ($, window, ui) {
    'use strict';

    window.addGitHubIssue = function (issue) {
        // Keep track of whether we're need the end, so we can auto-scroll once the tweet is added.
        var nearEnd = ui.isNearTheEnd(),
            elements = null;

        elements = $('div.git-hub-issue-' + issue.data.number)
            .removeClass('git-hub-issue-' + issue.data.number);

        // Process the template, and add it in to the div.
        var  converter = new Markdown.Converter().makeHtml;
        issue.data.body = converter(issue.data.body);

        $('#github-issues-template').tmpl(issue.data).appendTo(elements);

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