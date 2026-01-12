using Contracts.Callbacks;
using Contracts.DTOs;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Contracts.Services.Game
{
    [ServiceContract]
    public interface IGameService
    {
        [OperationContract]
        Task StartGame(GameSettingsDto settings);

        [OperationContract]
        Task UpdateGameSettings(GameSettingsDto settings);

        [OperationContract]
        Task GetScoreboard();

        [OperationContract]
        Task DeclareWin(PlayerBoardDto playerBoard);

        [OperationContract]
        Task<bool> ValidateFalseLoteriaAsync(int challengerUserId);

    }
}