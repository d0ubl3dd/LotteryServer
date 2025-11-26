using System;

namespace BusinessLogic.Exceptions
{
    public class SessionException : Exception
    {
        public SessionException(string message) : base(message) { }
    }

    public class ClientNotFoundException : SessionException
    {
        public ClientNotFoundException(string message) : base(message) { }
    }

    public class SessionContextException : SessionException
    {
        public SessionContextException(string message) : base(message) { }
    }
}