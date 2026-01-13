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
        [FaultContract(typeof(ServiceFault))]
        Task<int> RegisterUserWithCode(UserDto userData, string code);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<int> RegisterGuest();

        [OperationContract]
        Task<bool> VerifyPassword(int currentUserId, string password);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<bool> ChangePassword(int currentUserId, string newPassword);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<bool> RecoverPassword(string email, string newPassword);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto userData);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<FriendDto> FindUserByNickname(string nickname);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<UserDto> GetUserProfile(int currentUserId);
        
        [OperationContract]
        Task<bool> ChangeEmailWithCodeAsync(int currentUserId, string newEmail, string verificationCode);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<bool> RecoverPasswordRequest(string email);

        [OperationContract]
        Task<List<LeaderboardPlayerDto>> GetLeaderboard();
    }
}