## JabbR
JabbR is a chat application built with ASP.NET using SignalR. 

## Contributing
Before you contribute anything make sure you set autoclrf to true.


    git config --local core.autocrlf true

### Coding Guidelines
A few more things:

* Sort your usings
* Spaces not tabs (I won't even look at the PR if you use tabs)
* Follow the **existing** conventions you see in the project (that means, brace style, formatting etc).
* If you aren't sure about something, ask in the [meta](http://jabbr.net/#/rooms/meta) room on jabbr.

### Features and Bugs
After deciding you want to work on a feature or bug, comment on that issue saying that you want to do it. If you want to
discuss the feature further, join the [meta](http://jabbr.net/#/rooms/meta) room on jabbr. 

**NOTE:** It's important that you indicate that you want to work on a issue so that there's no overlapping work being done.

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

* CollegeHumor
* GitHub 
* Google Docs
* imgur
* join.me
* NerdDinner
* Pastie
* SlideShare
* Twitter
* UStream
* Vimeo
* Youtube

#### And if you ever happen to get lost...
    Type /? - to show the full list of JabbR Commands
