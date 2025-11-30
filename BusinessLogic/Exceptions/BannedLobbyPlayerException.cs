using System;

namespace BusinessLogic.Exceptions
{
    public class PlayerBannedException : Exception
    {
        public PlayerBannedException(string message) : base(message) { }
    }
}