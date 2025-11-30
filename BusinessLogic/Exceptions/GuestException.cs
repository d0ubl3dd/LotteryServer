using System;

namespace BusinessLogic.Exceptions
{
    public class EmptyNicknameException : Exception
    {
        public EmptyNicknameException(string message) : base(message) { }
    }

    public class InvalidNicknameLengthException : Exception
    {
        public InvalidNicknameLengthException(string message) : base(message) { }
    }

    public class InvalidNicknameFormatException : Exception
    {
        public InvalidNicknameFormatException(string message) : base(message) { }
    }
}