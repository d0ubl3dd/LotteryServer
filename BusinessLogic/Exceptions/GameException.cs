using System;

namespace BusinessLogic.Exceptions
{
    public class GameException : Exception
    {
        public GameException(string msg) : base(msg) { }
    }

    public class LobbyNotFoundException : GameException
    {
        public LobbyNotFoundException(string msg) : base(msg) { }
    }

    public class GameAlreadyRunningException : GameException
    {
        public GameAlreadyRunningException(string msg) : base(msg) { }
    }
}