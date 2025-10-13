using Contracts.Callbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Services.Game
{
    [ServiceContract]
    public interface IGameService
    {
        [OperationContract]
        Task StartGame();

        [OperationContract]
        Task UpdateGameSettings(/* DTO con settings */);

        [OperationContract]
        Task GetScoreboard();
    }
}
