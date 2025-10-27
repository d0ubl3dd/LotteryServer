using Contracts.Callbacks;
using Contracts.DTOs;
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
        Task UpdateGameSettings(GameSettingsDTO settings);

        [OperationContract]
        Task GetScoreboard();
    }
}
