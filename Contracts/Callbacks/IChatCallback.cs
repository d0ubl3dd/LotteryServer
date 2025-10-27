using System.ServiceModel;

namespace Contracts.Callbacks
{
    public interface IChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveChatMessage(string nickname, string message);
    }
}