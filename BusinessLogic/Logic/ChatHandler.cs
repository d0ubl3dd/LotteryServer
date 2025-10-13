using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class ChatHandler
    {
        public void SendMessage(string message)
        {
            Console.WriteLine($"Lógica de SendMessage manejada por ChatHandler. Mensaje: {message}");
            // TODO: Lógica para procesar el mensaje y notificar a los jugadores en el chat.
        }
    }
}
