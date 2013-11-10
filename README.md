## JabbR [![Build Status](https://travis-ci.org/JabbR/JabbR.png)](https://travis-ci.org/JabbR/JabbR)
JabbR is a chat application built with ASP.NET using SignalR. 

![jabbr.net](https://jabbr.blob.core.windows.net/jabbr-uploads/Screen%20Shot%202013-04-01%20at%207.57.53%20PM_d6a3.png)

### Features and Bugs
If you want to discuss the features join discussion in the [meta](https://jabbr.net/#/rooms/meta) room on jabbr. 

## JabbR Features and Commands
    
### Public and private chat rooms
Quickly join a public chat room with

    /join [roomName]
    
And join any private room with an invite code

    /join [roomName] [inviteCode]
    
### Gravatar
Assign a gravatar to your nick. Be recognized, even in JabbR!

    Type /gravatar [email] - to set your gravatar.
    
### Notifications
* Integrated into Chrome to provide you with popup desktop notifications. 
* Live Twitter mentions powered by twitterbot, so that you never miss out on the conversation.
* Audio notifications.
    
### Content Provider Support
Inline image and content support for your favorite sites:

* Bash Quote DB
* BBC News
* CollegeHumor
* Dictionary.com
* GitHub Issues
* GISTs
* Google Docs
* imgur
* join.me
* Mixcloud
* NerdDinner
* Nuget Packages
* Pastie
* Screencast
* SlideShare
* SoundCloud
* Twitter
* Uservoice
* UStream
* Vimeo
* Youtube

#### And if you ever happen to get lost...
    Type /? - to show the full list of JabbR Commands

### Getting started with the code
We use the nightly builds of the ASP.NET WebStack, so while master always contains stable code, if you don't have the packages on your machine already, they might have been deleted from the myget source which will cause errors during a build.  As such, you likely want to work on the [dev branch](https://github.com/JabbR/JabbR/tree/dev), which is updated more frequently.

To perform your first build, grab the source, and open a visual studio command prompt and run build.cmd from the main directory.  JabbR.sln should now open and run in Visual Studio without problems.  Note that you may need to update the web.config to point to a valid SQL Server installation.  The first created user is assigned the admin role.  Any questions or issues can be raised in the [meta](https://jabbr.net/#/rooms/meta) room on jabbr.

#### Getting Involved
We welcome contributions from experienced developers.  You can get involved by logging bugs in github, hacking on the source, or discussing issues / features in the [meta](https://jabbr.net/#/rooms/meta) room.