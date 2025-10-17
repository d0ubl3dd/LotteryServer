using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Services.Users
{
    [ServiceContract]
    public interface IAuthenticationService
    {
        [OperationContract]
        Task<bool> LoginUser(string username, string password);

        [OperationContract]
        Task LogoutUser();
    }
}
