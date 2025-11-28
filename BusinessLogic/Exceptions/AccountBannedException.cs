using System;
namespace BusinessLogic.Exceptions
{
    public class AccountBannedException : Exception
    {
        public AccountBannedException(string message) : base(message) { }
    }
}