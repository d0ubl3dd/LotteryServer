using System;

namespace BusinessLogic.Exceptions
{
    public class LobbyException : Exception
    {
        public LobbyException(string message) : base(message) { }
    }

    public class UserNotConnectedException : LobbyException
    {
        public UserNotConnectedException(string message) : base(message) { }
    }

    public class UserAlreadyInLobbyException : LobbyException
    {
        public UserAlreadyInLobbyException(string message) : base(message) { }
    }

    public class LobbyFullException : LobbyException
    {
        public LobbyFullException(string message) : base(message) { }
    }

    public class LobbyActionNotAllowedException : LobbyException
    {
        public LobbyActionNotAllowedException(string message) : base(message) { }
    }
}