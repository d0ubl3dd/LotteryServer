using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;


namespace Services.Services.Users
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        void RegisterUser(User user);
        [OperationContract]
        void RegisterGuest(String nickName);
        [OperationContract]
        void ChangePassword(String password);
        [OperationContract]
        void RecoverPassword(String email);
        [OperationContract]
        void UpdateUser(User user);
        [OperationContract]
        string GetAvatarPath(int idAvatar);
        [OperationContract]
        User GetUser(int idUser);
    }
}
