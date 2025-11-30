using System;

namespace BusinessLogic.Exceptions
{
    public class ForbiddenWordException : Exception
    {
        public ForbiddenWordException(string message) : base(message) { }
    }
}