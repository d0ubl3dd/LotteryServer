using Contracts.DTOs;
using Contracts.Faults;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Contracts.Services.Users
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<int> RequestUserVerification(UserRegisterDTO userData);
        
        [OperationContract]
        Task<int> RegisterUser(UserRegisterDTO userData);

        [OperationContract]
        Task<int> RegisterGuest();

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<bool> ChangePassword(int currentUserId, string oldPassword, string newPassword);

        [OperationContract]
        Task RecoverPassword(string email);

        [OperationContract]
        Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserRegisterDTO userData);

        [OperationContract]
        Task<FriendDTO> FindUserByNickname(string nickname);

        [OperationContract]
        Task<UserRegisterDTO> GetUserProfile(int userId);
    }
}