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
        Task<int> RequestUserVerification(UserDto userData);
        
        [OperationContract]
        Task<int> RegisterUser(UserDto userData);

        [OperationContract]
        Task<int> RegisterGuest();

        [OperationContract]
        Task<bool> VerifyPassword(int userId, string password);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<bool> ChangePassword(int userId, string newPassword);

        [OperationContract]
        Task RecoverPassword(string email);

        [OperationContract]
        Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto userData);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<FriendDto> FindUserByNickname(string nickname);

        [OperationContract]
        Task<UserDto> GetUserProfile(int userId);

        [OperationContract]
        Task<bool> RequestEmailChange(int userId, string newEmail);

        [OperationContract]
        Task<bool> ConfirmEmailChange(int userId, string newEmail, string verificationCode);
    }
}