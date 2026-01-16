using Contracts.DTOs;
using System.Collections.Generic;
using System.ServiceModel;

namespace Contracts.Callbacks
{
    [ServiceContract]
    public interface IGameCallback
    {
        [OperationContract(IsOneWay = true)]
        void PlayerDeclaredWinner(int playerId, string nickname);
    }
}
