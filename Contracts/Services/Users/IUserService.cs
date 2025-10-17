using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Contracts.DTOs;


namespace Contracts.Services.Users
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        void RegisterUser(UserRegisterDTO user);
        [OperationContract]
        void RegisterGuest(String nickname);
        [OperationContract]
        void ChangePassword(String password);
        [OperationContract]
        void RecoverPassword(String email);
        
    }
}
