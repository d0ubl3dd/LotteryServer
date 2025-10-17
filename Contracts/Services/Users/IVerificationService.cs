using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Services.Users
{
    [ServiceContract]
    public interface IVerificationService
    {
        [OperationContract]
        Task<bool> SendVerificationCode(string email);
        [OperationContract]
        Task<bool> VerifyCode(string email, string code);
    }
}
