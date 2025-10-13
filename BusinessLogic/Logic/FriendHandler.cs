using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class FriendHandler
    {
        public Task SendRequestFriendship(int targetUserId)
        {
            Console.WriteLine("Lógica de SendRequestFriendship manejada por FriendHandler.");
            // TODO: Lógica para crear una solicitud de amistad en la BD.
            return Task.CompletedTask;
        }

        public Task AddFriend(int friendshipRequestId)
        {
            Console.WriteLine("Lógica de AddFriend manejada por FriendHandler.");
            // TODO: Lógica para aceptar una solicitud y crear la amistad.
            return Task.CompletedTask;
        }

        public Task RemoveFriend(int friendUserId)
        {
            Console.WriteLine("Lógica de RemoveFriend manejada por FriendHandler.");
            // TODO: Lógica para eliminar una amistad de la BD.
            return Task.CompletedTask;
        }
    }
}
