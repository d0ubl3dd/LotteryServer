using BusinessLogic.Models;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class LobbyHandler
    {
        private PlayerClient GetClient(User user)
        {
            var client = GlobalSessionManager.Instance.GetClient(user.id_user);
            if (client == null)
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "Error de sesión. No se encontró el cliente." });
            }
            return client;
        }

        public Task<LobbyStateDTO> CreateLobby(User currentUser)
        {
            var hostClient = GetClient(currentUser);
            if (hostClient.CurrentLobby != null)
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "Ya estás en un lobby." });
            }

            var lobbyState = LobbyManager.Instance.CreateLobby(hostClient);
            return Task.FromResult(lobbyState);
        }

        public Task<LobbyStateDTO> JoinLobby(User currentUser, string lobbyCode)
        {
            var playerClient = GetClient(currentUser);
            if (playerClient.CurrentLobby != null)
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "Ya estás en un lobby." });
            }

            var lobbyState = LobbyManager.Instance.JoinLobby(playerClient, lobbyCode);
            return Task.FromResult(lobbyState);
        }

        public Task LeaveLobby(User currentUser)
        {
            var client = GlobalSessionManager.Instance.GetClient(currentUser.id_user);
            if (client != null)
            {
                LobbyManager.Instance.LeaveLobby(client);
            }
            return Task.CompletedTask;
        }

        public Task KickPlayer(User currentUser, int targetPlayerId)
        {
            var hostClient = GetClient(currentUser);
            LobbyManager.Instance.KickPlayer(hostClient, targetPlayerId);
            return Task.CompletedTask;
        }
    }
}