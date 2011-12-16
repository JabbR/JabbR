## JabbR
JabbR is a chat application built with ASP.NET using SignalR. 

## Contributing
Before you contribute anything make sure you set autoclrf to true.


    git config --global core.autocrlf true


A few more things:

* Sort your usings
* Spaces not tabs (I won't even look at the PR if you use tabs)
* Follow the **existing** conventions you see in the project (that means, brace style, formatting etc).
* If you aren't sure about something, ask in the [meta](http://jabbr.net/#/rooms/meta) room on jabbr.


## JabbR Features and Commands
#### help
    Type /help - to show the list of the following commands.
    
#### nick 
    Type /nick [user] [password] - to create a user or change your nickname. 
    Type /nick [user] [oldpassword] [newpassword] - to change your password.

#### join 
    Type /join [room] [inviteCode] - to join the room of your choice. 
    If it is private and you have an invite code, enter it after the room name.

#### create 
    Type /create [room] - to create a room.
    
#### me 
    Type /me 'does anything' - to do anything (quotes not needed)...
    
#### msg 
    Type /msg @nickname (message) - to send a private message to a nickname. @ is optional.
    
#### leave 
    Type /leave - to leave the current room. 
    Type /leave [room name] - to leave a specific room.
    
#### rooms 
    Type /rooms - to show the list of rooms available.
    
#### where 
    Type /where [name] - to list the rooms that user is currently in.
    
#### who 
    Type /who - to show a list of all users.
    Type /who [name] - to show specific information about that user (current rooms and rooms owned).
    
#### list 
    Type /list (room) - to show a list of users in the specified room.
    
#### gravatar 
    Type /gravatar [email] - to set your gravatar.

#### nudge 
    Type /nudge - to send a nudge to the whole room.
    Type /nudge @nickname - to nudge a particular user. @ is optional.

#### kick 
    Type /kick [user] - to kick a user from the room. 
    (Only works if you're the creator of that room)

#### logout 
    Type /logout - to logout from this client (chat cookie will be removed).

#### addowner 
    Type /addowner [user] [room] - to add a user as an owner to the specified room. 
    (Only works if you're an owner of that room)

#### removeowner 
    Type /removeowner [user] [room] - to remove an owner from the specified room. 
    (Only works if you're the creator of that room)

#### lock 
    Type /lock [room] - to make a room private. 
    (Only works if you're the creator of that room)

#### allow 
    Type /allow [user] [room] - to give a user permission to a private room. 
    (Only works if you're an owner of that room)

#### unallow 
    Type /unallow [user] [room] - to revoke a user's permission to a private room. 
    (Only works if you're an owner of that room)

#### invitecode 
    Type /invitecode - to show the current invite code

#### resetinvitecode 
    Type /resetinvitecode - to reset the current invite code. 
    NOTE: This will render the previous invite code invalid.