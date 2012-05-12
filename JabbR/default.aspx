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
            janrain.settings.tokenUrl = url + 'Auth/Login.ashx';
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
                <img src="http://www.gravatar.com/avatar/${hash}?s=16&d=mm" class="gravatar" />
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
            <img class="gravatar" src="http://www.gravatar.com/avatar/${hash}?s=16&d=mm" />
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
        <li id="tabs-${id}" class="room" data-name="${name}">
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
            <img src="${user.profile_image_url}" />
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
    <div id="page">
        <div id="disconnect-dialog" class="modal hide fade">
            <div class="modal-header">
              <a class="close" data-dismiss="modal" >&times;</a>
              <img alt="logo" src="Content/images/logo32.png" /><h3>JabbR Error</h3>
            </div>
            <div class="modal-body">
              <p>There was an error contacting the server, please refresh in a few minutes.</p>
            </div>
        </div>

        <div id="download-dialog" class="modal hide fade">
            <div class="modal-header">
                <a class="close" data-dismiss="modal">&times;</a>
                <h3>Download Messages</h3>
            </div>
            <div class="modal-body">
                <p>Select date range for messages:</p>
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
              <a class="close" data-dismiss="modal" >&times;</a>
              <img alt="logo" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAIAAAD8GO2jAAAABGdBTUEAALGPC/xhBQAAAAlwSFlz
AAAOwwAADsMBx2+oZAAAABp0RVh0U29mdHdhcmUAUGFpbnQuTkVUIHYzLjUuMTAw9HKhAAAE1UlE
QVRIS62WDTSVdxzHb7bV1s7ZijLaWaYNWydqs9oi614vkVAYKSTmdeJ6CQdzCJW8hBtmM51uN4SY
vLZuC6GXrRTL2Ym8RM3ZlkXIS5f25W+Pp+fe7nYP9zyc3//t+3n+/9/L/2GxlmymnkWKhmExWX1/
D5RU1NP7Fy43WLnG9mxlww/ldc7ecZ8Zem00+gr/13M8tHRd1LTtlTWsFitvoS+ZtSlLRdO2san1
2fRvcPCJnDwbQ/KqZqs/dYKclUNETf0t8vAyiz439UUn41mn9yVIcvKc50ik8a7Wzp77fxL1G7da
N5nsW7zCWFPHmS7hvT+lrOoyYRw/Vam31UecgR5ttrvCKovndvCqklHT7XainsDLe3mZvrq2PTeE
Z2ARQCQs7MJ8g3kcMz99C/8sfjlhJKXlSwSQTtW1dgvI4eMvJuEkUT+cnIPmbteYCzU36BKFJTVo
lp27jKFNJj7ZgkrCcPI6LIWBU5kCyKuaDw2PQP3Kzy0vKXB8Q3gN125jcXXdTb/QNLI+OaOQKApr
rtu5HDCyDKwSXkMzp0AoBYChN1easvYFpZDXZ5txtXSdO+/1YiUCxmZvJMMBP9U2Yqi4rA6nHxad
RZDWjhFSGB9u2MPCxqHe0fU7tpNTKGz5rRNClvZfYxl2A2dmHi+1d4tF0z8sjYjinYytgogdeuA7
KYAN+p6s7vt/AMDPO4e47B8Yam7pwIFgTWB4OhWXFy812jhF6hp7n62oR2dU3AlMyC+uhh19hC8F
gFxhjY2NAwA/K6lbwmjvfOAXegxrThddpADE4SbWwa4+8fGpecgJ2Hs8D0XH801tQnTEEoJCrtV1
YY1OA2ITBW+p7YABh/uHpWNGbuEFOkCKLay+zs/7ERGBLTJ2o/yBFauruxe6pwrOL1jK7nv0GHZx
2SXM8wlK/Z8AalrG9yU6W2YZqCJyS9mskoo6iMITAMClsEUikaPHQTDcuAnZggqZMIHhGWQTH+m5
LlQ0nMoDd24iCVMT6yD1TxxGR6dO7K+H/S7TRc3WOUomAKoh1DXWO6IczFSLN94xRfBA9GZz2yvL
DVx94ycnCfFZTDxfHJDyzRmvgKN4fIJTE4/lIx/pb4ByuWbjXmaxQywTxYzsEow5uMcOPB4eGRlL
4J0GoLah6U5bD4pV3ZVfoRUSmUn3ZFJaASagPnZ09Q4OPYEI0pAJwItf/aWFMLJOlqP2Kart8PRP
Ym/jIp+HpwsJ+Y2MjgVHZiqomi97zwIlc4nKNiTjJLXl6Tmo5BLKtZKGZevdHqICY/vucFRdxfe3
ww2UOjH2R2TQ16O4MiasWrdLAgBdyAOcAJmNS43EgDjgSEouijnCgWPuhzMUTUzQAfd6pqJRMgC9
qKZegUfvdjw4kVtFJokDGO/LaAaEpzMvTskX6b8XtUwApPRsdFJX/bwA4PzUzDOvKRlJUJs7YGJi
4mCiYCZvad8oM8qyAkKivlXR3FkpvMo4/dKqBqZ7qTtZCuNFYYqL9lH/IIPh4Zc4D0dE5UFccg4D
8LCv//UVxnONIgrw9uovxsefMhju4puQ1Qf0TBbkn2cAmlva520HEPp4s5t43uHr5IWZLL6b/6xF
KNcMRlFp7XwCzHeFMgBPRSJ8is8y5uIDrMXHzp22bgaDfIKS5x/esfBAlPv1lgAAAABJRU5ErkJg
gg==" id="logo" /><h3>JabbR Update</h3>
            </div>
            <div class="modal-body">
              <p>Refresh your browser to get the latest. Auto update will take place in 15 seconds.</p>
            </div>
          </div>

        <a href="https://github.com/davidfowl/JabbR" target="_blank">
            <img style="position: absolute; top: 0; right: 0; border: 0; z-index: 1000" src="data:image/png;base64,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"
                alt="Fork me on GitHub" />
        </a>
        <div id="heading">
            <div class="banner">
                <h1>
                    Jabb</h1>
                <img alt="logo" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAIAAAD8GO2jAAAABGdBTUEAALGPC/xhBQAAAAlwSFlz
AAAOwwAADsMBx2+oZAAAABp0RVh0U29mdHdhcmUAUGFpbnQuTkVUIHYzLjUuMTAw9HKhAAAE1UlE
QVRIS62WDTSVdxzHb7bV1s7ZijLaWaYNWydqs9oi614vkVAYKSTmdeJ6CQdzCJW8hBtmM51uN4SY
vLZuC6GXrRTL2Ym8RM3ZlkXIS5f25W+Pp+fe7nYP9zyc3//t+3n+/9/L/2GxlmymnkWKhmExWX1/
D5RU1NP7Fy43WLnG9mxlww/ldc7ecZ8Zem00+gr/13M8tHRd1LTtlTWsFitvoS+ZtSlLRdO2san1
2fRvcPCJnDwbQ/KqZqs/dYKclUNETf0t8vAyiz439UUn41mn9yVIcvKc50ik8a7Wzp77fxL1G7da
N5nsW7zCWFPHmS7hvT+lrOoyYRw/Vam31UecgR5ttrvCKovndvCqklHT7XainsDLe3mZvrq2PTeE
Z2ARQCQs7MJ8g3kcMz99C/8sfjlhJKXlSwSQTtW1dgvI4eMvJuEkUT+cnIPmbteYCzU36BKFJTVo
lp27jKFNJj7ZgkrCcPI6LIWBU5kCyKuaDw2PQP3Kzy0vKXB8Q3gN125jcXXdTb/QNLI+OaOQKApr
rtu5HDCyDKwSXkMzp0AoBYChN1easvYFpZDXZ5txtXSdO+/1YiUCxmZvJMMBP9U2Yqi4rA6nHxad
RZDWjhFSGB9u2MPCxqHe0fU7tpNTKGz5rRNClvZfYxl2A2dmHi+1d4tF0z8sjYjinYytgogdeuA7
KYAN+p6s7vt/AMDPO4e47B8Yam7pwIFgTWB4OhWXFy812jhF6hp7n62oR2dU3AlMyC+uhh19hC8F
gFxhjY2NAwA/K6lbwmjvfOAXegxrThddpADE4SbWwa4+8fGpecgJ2Hs8D0XH801tQnTEEoJCrtV1
YY1OA2ITBW+p7YABh/uHpWNGbuEFOkCKLay+zs/7ERGBLTJ2o/yBFauruxe6pwrOL1jK7nv0GHZx
2SXM8wlK/Z8AalrG9yU6W2YZqCJyS9mskoo6iMITAMClsEUikaPHQTDcuAnZggqZMIHhGWQTH+m5
LlQ0nMoDd24iCVMT6yD1TxxGR6dO7K+H/S7TRc3WOUomAKoh1DXWO6IczFSLN94xRfBA9GZz2yvL
DVx94ycnCfFZTDxfHJDyzRmvgKN4fIJTE4/lIx/pb4ByuWbjXmaxQywTxYzsEow5uMcOPB4eGRlL
4J0GoLah6U5bD4pV3ZVfoRUSmUn3ZFJaASagPnZ09Q4OPYEI0pAJwItf/aWFMLJOlqP2Kart8PRP
Ym/jIp+HpwsJ+Y2MjgVHZiqomi97zwIlc4nKNiTjJLXl6Tmo5BLKtZKGZevdHqICY/vucFRdxfe3
ww2UOjH2R2TQ16O4MiasWrdLAgBdyAOcAJmNS43EgDjgSEouijnCgWPuhzMUTUzQAfd6pqJRMgC9
qKZegUfvdjw4kVtFJokDGO/LaAaEpzMvTskX6b8XtUwApPRsdFJX/bwA4PzUzDOvKRlJUJs7YGJi
4mCiYCZvad8oM8qyAkKivlXR3FkpvMo4/dKqBqZ7qTtZCuNFYYqL9lH/IIPh4Zc4D0dE5UFccg4D
8LCv//UVxnONIgrw9uovxsefMhju4puQ1Qf0TBbkn2cAmlva520HEPp4s5t43uHr5IWZLL6b/6xF
KNcMRlFp7XwCzHeFMgBPRSJ8is8y5uIDrMXHzp22bgaDfIKS5x/esfBAlPv1lgAAAABJRU5ErkJg
gg==" id="logo" />
                <div>
                    Powered by <a href="https://github.com/SignalR/SignalR" target="_blank">SignalR</a></div>
            </div>
            <div style="clear: both">
            </div>
            <ul id="tabs">
                <li id="tabs-lobby" class="current lobby" data-name="Lobby">
                    <button accesskey="l">
                        <span class="content">Lobby</span></button>
                </li>
            </ul>
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
        <div id="chat-area">
            <ul id="messages-lobby" class="messages current">
            </ul>
            <form id="users-filter-form" action="#">
                <input id="users-filter" class="filter" type="text" />
            </form>
            <ul id="userlist-lobby" class="users current">
            </ul>
            <div id="preferences">
                <a class="sound" title="audible notifications"></a>
                <a class="toast" title="popup notifications"></a>
                <a class="download" title="download messages"></a>
            </div>
            <form id="send-message" action="#">
            <div id="message-box">
                <textarea id="new-message" autocomplete="off" accesskey="m"></textarea>
            </div>
            <input type="submit" id="send" value="Send" class="send" />
            <div id="message-instruction">Type @ then press TAB to auto-complete nicknames</div>
            </form>
        </div>
        <audio src="Content/sounds/notification.wav" id="noftificationSound" hidden="hidden" />

    </div>
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
   <%-- <script src="Scripts/json2.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery-1.7.min.js" type="text/javascript"></script>
    <script src="Scripts/bootstrap.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.KeyTips.js" type="text/javascript"></script>
    <script src="Scripts/jquery-ui-1.8.17.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.signalR-0.5.0.js" type="text/javascript"></script>
    <script src="signalr/hubs" type="text/javascript"></script>
    <script src="Scripts/modernizr.js" type="text/javascript"></script>
    <script src="Scripts/jQuery.tmpl.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.cookie.js" type="text/javascript"></script>
    <script src="Scripts/jquery.autotabcomplete.js" type="text/javascript"></script>
    <script src="Scripts/jquery.timeago.0.10.js" type="text/javascript"></script>
    <script src="Scripts/jquery.captureDocumentWrite.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.sortElements.js" type="text/javascript"></script>
    <script src="Scripts/quicksilver.js" type="text/javascript"></script>
    <script src="Scripts/jquery.livesearch.js" type="text/javascript"></script>
    <script src="Scripts/Markdown.Converter.js" type="text/javascript"></script>
    <script src="Scripts/jquery.history.js" type="text/javascript"></script>
    <script src="Chat.utility.js" type="text/javascript"></script>
    <script src="Chat.emoji.js" type="text/javascript"></script>
    <script src="Chat.toast.js" type="text/javascript"></script>
    <script src="Chat.ui.js" type="text/javascript"></script>
    <script src="Chat.documentOnWrite.js" type="text/javascript"></script>
    <script src="Chat.twitter.js" type="text/javascript"></script>
    <script src="Chat.pinnedWindows.js" type="text/javascript"></script>
    <script src="Chat.githubissues.js" type="text/javascript"></script>    
    <script src="Chat.js" type="text/javascript"></script>   --%> 
</body>
</html>
