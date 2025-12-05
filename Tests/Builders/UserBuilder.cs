using System;
using DataAccess;
using BusinessLogic.Logic;

namespace Tests.Builders
{
    public class UserBuilder
    {
        private readonly User _user;
        private string _rawPassword;

        public UserBuilder()
        {
            _rawPassword = "PasswordSeguro123";
            byte[] hash, salt;

            PasswordHasher.CreatePasswordHash(_rawPassword, out hash, out salt);

            _user = new User
            {
                id_user = 1,
                nickname = "JugadorDefault",
                email = "test@example.com",
                passwordHash = hash,
                passwordSalt = salt,
                status = "Offline",
                isLocked = false,
                failedLoginAttempts = 0,
                id_avatar = 1
            };
        }

        public UserBuilder WithNickname(string nickname)
        {
            _user.nickname = nickname;
            return this;
        }

        public UserBuilder WithId(int id)
        {
            _user.id_user = id;
            return this;
        }

        public UserBuilder WithPassword(string password)
        {
            _rawPassword = password;
            byte[] hash, salt;
            PasswordHasher.CreatePasswordHash(password, out hash, out salt);
            _user.passwordHash = hash;
            _user.passwordSalt = salt;
            return this;
        }

        public UserBuilder WithFailedAttempts(int attempts)
        {
            _user.failedLoginAttempts = attempts;
            return this;
        }

        public UserBuilder Locked()
        {
            _user.isLocked = true;
            return this;
        }

        public UserBuilder Online()
        {
            _user.status = "Online";
            return this;
        }

        public User Build()
        {
            return _user;
        }
    }
}