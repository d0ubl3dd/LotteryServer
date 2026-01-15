using Contracts.Callbacks;
using Contracts.DTOs;
using Contracts.Faults;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Contracts.Services.Game
{
    [ServiceContract]
    public interface IGameService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task StartGame(GameSettingsDto settings);

        [OperationContract]
        Task UpdateGameSettings(GameSettingsDto settings);

        [OperationContract]
        Task GetScoreboard();

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task DeclareWin(PlayerBoardDto playerBoard);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<bool> ValidateFalseLoteriaAsync(int challengerUserId);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task ConfirmGameEnd(int winnerId);
    }
}