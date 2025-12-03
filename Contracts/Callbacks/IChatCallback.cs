using System.ServiceModel;

namespace Contracts.Callbacks
{
    [ServiceContract]
    public interface IChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveChatMessage(string nickname, string message);
    }
}