using System;

namespace BusinessLogic.Exceptions
{
    public class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }
    }

    public class UserNotFoundException : AuthException
    {
        public UserNotFoundException(string message) : base(message) { }
    }

    public class IncorrectPasswordException : AuthException
    {
        public IncorrectPasswordException(string message) : base(message) { }
    }

    public class AccountLockedException : AuthException
    {
        public AccountLockedException(string message) : base(message) { }
    }
}