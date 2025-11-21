using System;

namespace BusinessLogic.Exceptions
{
    public class FriendshipException : Exception
    {
        public FriendshipException(string message) : base(message) { }
    }

    public class InvalidFriendshipRequestException : FriendshipException
    {
        public InvalidFriendshipRequestException(string message) : base(message) { }
    }

    public class FriendshipDuplicateException : FriendshipException
    {
        public FriendshipDuplicateException(string message) : base(message) { }
    }

    public class FriendshipNotFoundException : FriendshipException
    {
        public FriendshipNotFoundException(string message) : base(message) { }
    }
}