using System;

namespace BusinessLogic.Exceptions
{
    public class ChatException : Exception
    {
        public ChatException(string message) : base(message) { }
    }

    public class UserNotInLobbyException : ChatException
    {
        public UserNotInLobbyException(string message) : base(message) { }
    }

    public class UserNotOnlineException : ChatException
    {
        public UserNotOnlineException(string message) : base(message) { }
    }
}