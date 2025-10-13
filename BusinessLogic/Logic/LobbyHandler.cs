using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class LobbyHandler
    {
        public Task<string> CreateLobby()
        {
            Console.WriteLine("Lógica de CreateLobby manejada por LobbyHandler.");
            // TODO: Lógica para crear un nuevo lobby y guardarlo.
            string lobbyCode = "XYZ123"; // Genera un código único
            return Task.FromResult(lobbyCode);
        }

        public Task JoinLobby(string lobbyCode)
        {
            Console.WriteLine($"Lógica de JoinLobby manejada por LobbyHandler para el código: {lobbyCode}");
            // TODO: Lógica para agregar al jugador al lobby y notificar a otros jugadores.
            return Task.CompletedTask;
        }
    }
}
