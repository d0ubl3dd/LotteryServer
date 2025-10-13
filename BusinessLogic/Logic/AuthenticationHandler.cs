using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class AuthenticationHandler
    {
        public Task<bool> UserLogin(string username, string password)
        {
            Console.WriteLine("Lógica de Login manejada por AuthenticationHandler.");
            // TODO: Lógica para verificar credenciales contra la BD.
            // using (var db = new LotteryEntities()) { ... }
            return Task.FromResult(true); // Devuelve true si el login es exitoso
        }

        public Task UserLogout()
        {
            Console.WriteLine("Lógica de Logout manejada por AuthenticationHandler.");
            // TODO: Lógica para manejar el logout (ej. actualizar estado del jugador).
            return Task.CompletedTask;
        }
    }
}