using System;

namespace BusinessLogic.Exceptions
{
    public class UserManagementException : Exception
    {
        public UserManagementException(string message) : base(message) { }

        public UserManagementException(string message, Exception inner) : base(message, inner) { }
    }

    public class UserAlreadyExistsException : UserManagementException
    {
        public UserAlreadyExistsException(string message) : base(message) { }
    }

    public class VerificationException : UserManagementException
    {
        public VerificationException(string message) : base(message) { }

        public VerificationException(string message, Exception inner) : base(message, inner) { }
    }

    public class EmailDeliveryException : VerificationException
    {
        public EmailDeliveryException(string message) : base(message) { }

        public EmailDeliveryException(string message, Exception inner) : base(message, inner) { }
    }
}