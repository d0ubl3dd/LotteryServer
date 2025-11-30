using Contracts.DTOs;
using Contracts.Faults;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Contracts.Services.Users
{
    [ServiceContract]
    public interface IAuthenticationService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<UserDto> LoginUser(string username, string password);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<UserDto> LoginGuest(string nickname);

        [OperationContract]
        Task LogoutUser();
    }
}
