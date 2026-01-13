using System;

namespace BusinessLogic.Exceptions
{
    public class GameException : Exception
    {
        public GameException(string message) : base(message) { }
    }

    public class LobbyNotFoundException : GameException
    {
        public LobbyNotFoundException(string message) : base(message) { }
    }

    public class GameAlreadyRunningException : GameException
    {
        public GameAlreadyRunningException(string message) : base(message) { }
    }

    public class InvalidGameActionException : GameException
    {
        public InvalidGameActionException(string message) : base(message) { }
    }

    public class NotEnoughPlayersException : GameException
    {
        public NotEnoughPlayersException(string message) : base(message) { }
    }
}