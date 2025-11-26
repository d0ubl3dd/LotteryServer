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

    // Para cuando alguien canta victoria pero no tiene las cartas (hace trampa o se equivocó)
    public class InvalidGameActionException : GameException
    {
        public InvalidGameActionException(string message) : base(message) { }
    }
}