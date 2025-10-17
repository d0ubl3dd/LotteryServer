using Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Services.Users;
using Microsoft.SqlServer.Server;
using DataAccess;
using DataAccess.DAOs;


namespace BusinessLogic.Logic
{
    public partial class UserHandler : IUserService
    {
        public void RegisterUser(UserRegisterDTO user)
        {
            var userDAO = new DataAccess.User
            {
                nickname = user.Nickname,
                email = user.Email,
                //password = user.Password,
                registration_date = user.RegistrationDate,
                first_name = user.FirstName,
                paternal_last_name = user.PaternalLastName,
                maternal_last_name = user.MaternalLastName,
                score = user.Score,
                id_avatar = user.IdAvatar
            };
            new UserDAO().AddUser(userDAO);
        }

        public void ChangePassword(string password)
        {
            throw new NotImplementedException();
        }

        public void RecoverPassword(string email)
        {
            throw new NotImplementedException();
        }
        
        public void RegisterGuest(string nickname)
        {
            throw new NotImplementedException();
        }
        
    }
}
