using Contracts.DTOs;
using Contracts.Faults;
using System.Collections.Generic;
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
        Task<bool> VerifyPassword(int currentUserId, string password);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<bool> ChangePassword(int currentUserId, string newPassword);

        [OperationContract]
        Task<bool> RecoverPassword(string email, string newPassword);

        [OperationContract]
        Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto userData);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<FriendDto> FindUserByNickname(string nickname);

        [OperationContract]
        Task<UserDto> GetUserProfile(int currentUserId);

        [OperationContract]
        Task<bool> RequestEmailChange(int currentUserId, string newEmail);

        [OperationContract]
        Task<bool> ConfirmEmailChange(int currentUserId, string newEmail, string verificationCode);
        [OperationContract]
        Task<bool> RecoverPasswordRequest(string email);
        [OperationContract]
        Task<List<LeaderboardPlayerDto>> GetLeaderboard();
    }
}