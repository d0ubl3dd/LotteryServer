using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services.Users
{
    internal class UserService : IUserService
    {
        public void RegisterUser(User user)
        {
            var userDAO = new Core.User
            {
                id_user = user.id_user,
                id_avatar = user.id_avatar,
                nickname = user.nickname,
                email = user.email,
                password = user.password,
                registration_date = user.registration_date,
                frist_name = user.frist_name,
                paternal_last_name = user.paternal_last_name,
                maternal_last_name = user.maternal_last_name,
                score = user.score
            };
            //new UserDAO().AddUser(userDAO);
        }
        public void RegisterGuest(string nickName)
        {
            throw new NotImplementedException();
        }
        public void ChangePassword(string password)
        {
            throw new NotImplementedException();
        }
        public void RecoverPassword(string email)
        {
            throw new NotImplementedException();
        }
        public void UpdateUser(User user)
        {
            throw new NotImplementedException();
        }
        public string GetAvatarPath(int idAvatar)
        {
            throw new NotImplementedException();
        }
        public User GetUser(int idUser)
        {
            throw new NotImplementedException();
        }
    }
}
