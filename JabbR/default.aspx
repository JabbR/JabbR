<%@ Page Language="C#" %>
<%@ Import namespace="System.Configuration" %>
<%@ Import Namespace="SquishIt.Framework" %>
<%
    string appName = ConfigurationManager.AppSettings["auth.appName"];
    string apiKey = ConfigurationManager.AppSettings["auth.apiKey"];
    string googleAnalytics = ConfigurationManager.AppSettings["googleAnalytics"];
%>

<!DOCTYPE html>
<html>
<head>
    <title>JabbR</title>
    <meta http-equiv="X-UA-Compatible" content="IE=Edge" />
    <meta name="description" content="A real-time chat application. IRC without the R." />
    <meta name="keywords" content="chat,realtime chat,signalr,jabbr" />
    <%= Bundle.Css()
            .Add("~/Chat.css",
                  "~/Chat.nuget.css",
                  "~/Chat.bbcnews.css",
                  "~/Chat.githubissues.css",
                  "~/Chat.dictionary.css",
                  "~/Content/KeyTips.css",
                  "~/Content/bootstrap.min.css",
                  "~/Content/emoji20.css")
            .ForceRelease()
            .Render("~/Content/JabbR_#.css")
  %>

    <% if (!String.IsNullOrEmpty(apiKey)) { %>
    <script type="text/javascript">
        (function () {
            if (typeof window.janrain !== 'object') window.janrain = {};
            window.janrain.settings = {};

            var url = document.location.href;
            var nav = url.indexOf('#');
            url = nav > 0 ? url.substring(0, nav) : url;
            url = url.replace('default.aspx', '');
            janrain.settings.tokenUrl = url + 'Auth/Login.ashx?hash=' + escape(document.location.hash);
            janrain.settings.type = 'embed';

            function isReady() { janrain.ready = true; };
            if (document.addEventListener) {
                document.addEventListener("DOMContentLoaded", isReady, false);
            } else {
                window.attachEvent('onload', isReady);
            }

            var e = document.createElement('script');
            e.type = 'text/javascript';
            e.id = 'janrainAuthWidget';

            if (document.location.protocol === 'https:') {
                e.src = 'https://rpxnow.com/js/lib/<%:appName %>/engage.js';
            } else {
                e.src = 'http://widget-cdn.rpxnow.com/js/lib/<%:appName %>/engage.js';
            }

            var s = document.getElementsByTagName('script')[0];
            s.parentNode.insertBefore(e, s);
        })();
    </script>
    <% } %>
    <% if (!String.IsNullOrEmpty(googleAnalytics)) { %>
    <script type="text/javascript">
        var _gaq = _gaq || [];
        _gaq.push(['_setAccount', '<%:googleAnalytics %>']);
        _gaq.push(['_trackPageview']);

        (function () {
            var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
            ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
            var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
        })();

    </script>
    <% } %>
    <script id="new-message-template" type="text/x-jquery-tmpl">
        <li class="message ${highlight} clearfix" id="m-${id}" data-name="${name}" data-timestamp="${date}">
            <div class="left">
                {{if showUser}}
                <img src="https://secure.gravatar.com/avatar/${hash}?s=16&d=mm" class="gravatar" />
                <span class="name">${trimmedName}</span>
                {{/if}}
                <span class="state"></span>
            </div>
            <div class="middle">
                {{html message}}
            </div>
            <div class="right">
                <span id="t-${id}" class="time" title="${fulldate}">${when}</span>
            </div>
        </li>
    </script>
    <script id="new-notification-template" type="text/x-jquery-tmpl">
        <li class="${type}" data-timestamp="${date}">
            <div class="content">
                {{html message}}
                <a class="info" href="#"></a>
            </div>
            <div class="right">
                <span class="time" title="${fulldate}">${when}</span>
            </div>
        </li>
    </script>
    <script id="message-separator-template" type="text/x-jquery-tmpl">
        <li class="message-separator">
        </li>
    </script>
    <script id="new-user-template" type="text/x-jquery-tmpl">
        <li class="user" data-name="${name}">
            <img class="gravatar" src="https://secure.gravatar.com/avatar/${hash}?s=16&d=mm" />
            <div class="details">
                <span class="name">${name}</span>
                <span class="admin">{{if admin}}(admin){{/if}}</span>
                <span class="note{{if noteClass}} ${noteClass}{{/if}}" {{if note}}title="${note}"{{/if}}></span>
                <span class="flag{{if flagClass}} ${flagClass}{{/if}}" {{if flag}}title="${country}"{{/if}}></span>
            </div>
        </li>
    </script>
    <script id="new-userlist-template" type="text/x-jquery-tmpl">
        <h3 class="userlist-header nav-header">
            ${listname}
        </h3>
        <div>
            <ul id="${id}" />
        </div>
    </script>
    <script id="new-tab-template" type="text/x-jquery-tmpl">
        <li id="tabs-${id}" class="room" data-name="${name}" role="tab">
            <span class="lock"></span>
            <button> 
                <span class="content">${name}</span>
            </button>
            <div class="close"></div>
        </li>
    </script>
    <!-- TweetContentProvider: Should be extracted out if other content providers need templates -->
    <script id="tweet-template" type="text/x-jquery-tmpl">
        <div class="user">
            <img src="${user.profile_image_url_https}" />
            <span class="name">${user.screen_name}</span> (${user.name})
        </div>
        {{html text}}
        <time class="js-relative-date" datetime="${created_at}">${created_at}</time> 
    </script>
    <!-- /TweetContentProvider -->
    <!-- /GitHub Issues Content Provider -->
    <script id="github-issues-template" type="text/x-jquery-tmpl">
        <div class="new-comments">
            <div class="avatar-bubble js-comment-container">
                <span class="avatar">
                    <img height="48" width="48" src="${user.avatar_url}">
                    <span class="overlay size-48"></span>
                </span>
                <div class="bubble">
                    <div class="comment starting-comment ">
                        <div class="body">
                            <p class="author">
                                <a href="#" class='github-issue-user-${user.login}' target="_blank">${user.login}</a> opened this issue
                                <time class="js-relative-date" datetime="${created_at}">${created_at}</time>
                            </p>
                            <a href="${html_url}" target="_blank"><h2 class="content-title">${title}</h2></a>
                            <div class="infobar clearfix">
                                <p class="milestone js-milestone-infobar-item-wrapper">No milestone</p>
                                {{if assignee}}
                                    <p class="assignee">
                                        <span class="avatar">
                                            <img height="20" width="20" src="${assignee.avatar_url}">
                                            <span class="overlay size-20"></span>
                                        </span>
                                        <a href="#" class="github-issue-user-${assignee.login}" target="_blank">${assignee.login}</a> is assigned
                                    </p>
                                {{/if}}
                            </div>
                            <div class="formatted-content">
                                <div class="content-body wikistyle markdown-format">
                                    <p>
                                        {{html body}}
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
           
        </div>   
    </script>
    <!-- /Github Issus Content Provider -->
</head>
<body>
  <section id="page" role="application">
    <header id="heading" role="heading">
      <div class="banner" role="banner">
        <h1>Jabb</h1>
        <div class="jabbrLogo" id="logo"></div>
        <div>
          Powered by <a href="https://github.com/SignalR/SignalR" target="_blank">SignalR</a>
        </div>
      </div>
      <a href="https://github.com/davidfowl/JabbR" class="forkme" target="_blank">
        </a>
          <div style="clear: both">
    </div>
    <nav>
      <ul id="tabs" role="tablist">
        <li id="tabs-lobby" class="current lobby" data-name="Lobby" role="tab">
          <button accesskey="l">
            <span class="content">Lobby</span>
          </button>
        </li>
      </ul>
    </nav>
    </header>
    <div id="chat-area" role="tabpanel">
      <ul id="messages-lobby" class="messages current" role="log">
      </ul>
      <form id="users-filter-form" action="#">
      <input id="users-filter" class="filter" type="text" />
      </form>
      <ul id="userlist-lobby" class="users current">
      </ul>
      <div id="preferences">
        <a class="sound" title="audible notifications"></a>
        <a class="richness" title="toggle rich-content"></a>
        <a class="toast" title="popup notifications" aria-haspopup="true"></a>
        <a class="download" title="download messages" aria-haspopup="true"></a>
      </div>
      <form id="send-message" action="#">
      <div id="message-box">
        <textarea id="new-message" autocomplete="off" aria-autocomplete="none" accesskey="m"></textarea>
      </div>
      <input type="submit" id="send" value="Send" class="send" />
      <div id="message-instruction">
        Type @ then press TAB to auto-complete nicknames
      </div>
      </form>
    </div>
    <audio src="Content/sounds/notification.wav" id="noftificationSound" hidden="hidden" aria-hidden="true">
    </audio>
    <section aria-hidden="true" aria-haspopup="true">
      <div id="disconnect-dialog" class="modal hide fade">
        <div class="modal-header">
          <a class="close" data-dismiss="modal">&times;</a>
          <div class="jabbrLogo" id="logo"></div><h3>JabbR Error</h3>
        </div>
        <div class="modal-body">
          <p>
            There was an error contacting the server, please refresh in a few minutes.
          </p>
        </div>
      </div>
      <div id="download-dialog" class="modal hide fade">
        <div class="modal-header">
          <a class="close" data-dismiss="modal">&times;</a>
          <h3>Download Messages</h3>
        </div>
        <div class="modal-body">
          <p>
            Select date range for messages:
          </p>
          <p>
            <select id="download-range">
              <option value="last-hour">Last hour</option>
              <option value="last-day">Last day</option>
              <option value="last-week">Last week</option>
              <option value="last-month">Last month</option>
              <option value="all">All</option>
            </select>
          </p>
        </div>
        <div class="modal-footer">
          <a href="#" class="btn btn-primary" id="download-dialog-button">Download</a>
        </div>
      </div>
      <div id="jabbr-update" class="modal hide fade">
        <div class="modal-header">
          <a class="close" data-dismiss="modal">&times;</a>
          <div class="jabbrLogo" id="logo"></div><h3>JabbR Update</h3>
        </div>
        <div class="modal-body">
          <p>
            Refresh your browser to get the latest. Auto update will take place in 15 seconds.
          </p>
        </div>
      </div>
      <div id="jabbr-login" class="modal hide fade">
        <div class="modal-header">
          <a class="close" data-dismiss="modal">&times;</a>
          <h3>JabbR Login</h3>
        </div>
        <div class="modal-body">
          <div id="janrainEngageEmbed">
          </div>
        </div>
      </div>
    </section>
  </section> 
  <%= Bundle.JavaScript()
            .Add("~/Scripts/jquery-1.7.min.js",
            "~/Scripts/json2.min.js",
        "~/Scripts/bootstrap.js",
        "~/Scripts/jquery.KeyTips.js",
        "~/Scripts/jquery-ui-1.8.17.min.js",
        "~/Scripts/jquery.signalR-0.5.0.min.js")
            .ForceRelease()
            .Render("~/Scripts/JabbR1_#.js")
  %>
  <script type="text/javascript" src='<%= ResolveClientUrl("~/signalr/hubs") %>'></script>
  <%= Bundle.JavaScript()
            .Add("~/Scripts/jQuery.tmpl.min.js",
        "~/Scripts/jquery.cookie.js",
        "~/Scripts/jquery.autotabcomplete.js",
        "~/Scripts/jquery.timeago.0.10.js",
        "~/Scripts/jquery.captureDocumentWrite.min.js",
        "~/Scripts/jquery.sortElements.js",
        "~/Scripts/quicksilver.js",
        "~/Scripts/jquery.livesearch.js",
        "~/Scripts/Markdown.Converter.js",
        "~/Scripts/jquery.history.js",
        "~/Chat.utility.js",
        "~/Chat.emoji.js",
        "~/Chat.toast.js",
        "~/Chat.ui.js",
        "~/Chat.documentOnWrite.js",
        "~/Chat.twitter.js",
        "~/Chat.pinnedWindows.js",
        "~/Chat.githubissues.js",
        "~/Chat.js")
            .ForceRelease()
            .Render("~/Scripts/JabbR2_#.js")
  %>
</body>
</html>
