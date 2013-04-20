namespace JabbR.Client
{
    public static class ClientEvents
    {
        public static readonly string AddMessage = "addMessage";
        public static readonly string AddMessageContent = "addMessageContent";
        public static readonly string AddUser = "addUser";
        public static readonly string Leave = "leave";
        public static readonly string LogOn = "logOn";
        public static readonly string LogOut = "logOut";
        public static readonly string Kick = "kick";
        public static readonly string UpdateRoomCount = "updateRoomCount";
        public static readonly string UpdateActivity = "updateActivity";
        public static readonly string MarkInactive = "markInactive";
        public static readonly string SendPrivateMessage = "sendPrivateMessage";
        public static readonly string SetTyping = "setTyping";
        public static readonly string JoinRoom = "joinRoom";
        public static readonly string RoomCreated = "roomCreated";
        public static readonly string GravatarChanged = "changeGravatar";
        public static readonly string MeMessageReceived = "sendMeMessage";
        public static readonly string UsernameChanged = "changeUserName";
        public static readonly string NoteChanged = "changeNote";
        public static readonly string FlagChanged = "changeFlag";
        public static readonly string TopicChanged = "changeTopic";
        public static readonly string OwnerAdded = "addOwner";
        public static readonly string OwnerRemoved = "removeOwner";
    }
}
