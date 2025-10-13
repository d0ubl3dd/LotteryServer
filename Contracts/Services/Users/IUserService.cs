using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;


namespace Contracts.Services.Users
{
        [ServiceContract]
        public interface IUserService
        {
            [OperationContract]
            Task<int> RegisterUser(/* UserRegisterDTO userData */);

            [OperationContract]
            Task<int> RegisterGuest();

            [OperationContract]
            Task ChangePassword(string oldPassword, string newPassword);

            [OperationContract]
            Task RecoverPassword(string email);

            [OperationContract]
            Task UpdateProfile(/* UserProfileDTO profileData */);
        }
    }
