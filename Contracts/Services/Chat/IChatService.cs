using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Services.Chat
{
    [ServiceContract]
    public interface IChatService
    {
        [OperationContract(IsOneWay = true)]
        void SendMessage(string message);
    }
}
