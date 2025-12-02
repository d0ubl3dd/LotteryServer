using Contracts.Callbacks;
using Contracts.Faults;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Contracts.Services.Chat
{
    [ServiceContract(CallbackContract = typeof(IChatCallback))]
    public interface IChatService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task SendMessage(string message);
    }
}