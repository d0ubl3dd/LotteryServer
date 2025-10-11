using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Services.Game;

namespace BusinessLogic
{
    public class GameService : IGameService
    {
        public void StartGame()
        {
            throw new System.NotImplementedException();
        }

        void IGameService.UpdateGameSettings()
        {
            throw new NotImplementedException();
        }

        Task IGameService.GetScoreboard()
        {
            throw new NotImplementedException();
        }
    }
}
