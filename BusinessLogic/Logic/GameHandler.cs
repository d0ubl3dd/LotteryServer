using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class GameHandler
    {
        public Task StartGame()
        {
            Console.WriteLine("Lógica de StartGame manejada por GameHandler.");
            // TODO: Lógica para cambiar el estado del juego y notificar a los jugadores.
            return Task.CompletedTask;
        }

        public Task UpdateGameSettings(/* DTO con settings */)
        {
            Console.WriteLine("Lógica de UpdateGameSettings manejada por GameHandler.");
            // TODO: Lógica para guardar la nueva configuración y notificar.
            return Task.CompletedTask;
        }

        public Task GetScoreboard()
        {
            Console.WriteLine("Lógica de GetScoreboard manejada por GameHandler.");
            // TODO: Lógica para obtener las puntuaciones de la BD.
            return Task.CompletedTask;
        }
    }
}
