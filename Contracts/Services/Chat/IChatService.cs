using Contracts.Callbacks;
using Contracts.Faults;
using System.ServiceModel;

namespace Contracts.Services.Chat
{
    [ServiceContract(CallbackContract = typeof(IChatCallback))]
    public interface IChatService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        void SendMessage(string message);
    }
}