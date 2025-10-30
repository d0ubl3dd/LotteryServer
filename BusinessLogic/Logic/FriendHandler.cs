using Contracts.DTOs;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;
using Contracts.Faults;

namespace BusinessLogic.Logic
{
    public class FriendHandler
    {
        public async Task SendRequestFriendship(int currentUserId, int targetUserId)
        {
            try
            {
                if (currentUserId == targetUserId)
                {
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            ErrorCode = "FR-001",
                            Message = "No puedes agregarte a ti mismo como amigo."
                        },
                        new FaultReason("Solicitud inválida")
                    );
                }

                using (var context = new lottery_databaseEntities())
                {
                    bool exists = await context.Friendship.AnyAsync(f =>
                        (f.id_user_sender == currentUserId && f.id_user_receiver == targetUserId) ||
                        (f.id_user_sender == targetUserId && f.id_user_receiver == currentUserId));

                    if (exists)
                    {
                        throw new FaultException<ServiceFault>(
                            new ServiceFault
                            {
                                ErrorCode = "FR-002",
                                Message = "Ya existe una solicitud de amistad o ya son amigos."
                            },
                            new FaultReason("Solicitud duplicada")
                        );
                    }

                    var newRequest = new Friendship
                    {
                        id_user_sender = currentUserId,
                        id_user_receiver = targetUserId,
                        status = "Pending"
                    };
                    context.Friendship.Add(newRequest);
                    await context.SaveChangesAsync();
                }
            }
            catch (FaultException<ServiceFault> ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        ErrorCode = "FR-500",
                        Message = $"Error inesperado en la base de datos: {ex.Message}"
                    },
                    new FaultReason("Error interno del servidor")
                );
            }
        }


        public async Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var request = await context.Friendship.FirstOrDefaultAsync(f =>
                    f.id_user_sender == requesterId &&
                    f.id_user_receiver == currentUserId &&
                    f.status == "Pending");

                if (request != null)
                {
                    request.status = "Accepted";
                    await context.SaveChangesAsync();
                }
                else
                {
                    throw new Exception("No se encontró la solicitud de amistad. Es posible que el usuario la haya cancelado.");
                }
            }
        }

        public async Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var request = await context.Friendship.FirstOrDefaultAsync(f =>
                    f.id_user_sender == requesterId &&
                    f.id_user_receiver == currentUserId &&
                    f.status == "Pending");

                if (request != null)
                {
                    context.Friendship.Remove(request);
                    await context.SaveChangesAsync();
                }
                else
                {
                    throw new FaultException<ServiceFault>(new ServiceFault
                    {
                        Message = "No se encontró la solicitud de amistad."
                    });
                }
            }
        }

        public async Task CancelFriendRequest(int currentUserId, int targetUserId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var request = await context.Friendship.FirstOrDefaultAsync(f =>
                    f.id_user_sender == currentUserId &&
                    f.id_user_receiver == targetUserId &&
                    f.status == "Pending");

                if (request != null)
                {
                    context.Friendship.Remove(request);
                    await context.SaveChangesAsync();
                }
                else
                {
                    throw new FaultException<ServiceFault>(new ServiceFault
                    {
                        Message = "No se ha encontrado ninguna solicitud enviada a ese jugador."
                    });
                }
            }
        }

        public async Task RemoveFriend(int currentUserId, int friendUserId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var friendship = await context.Friendship.FirstOrDefaultAsync(f =>
                    ((f.id_user_sender == currentUserId && f.id_user_receiver == friendUserId) ||
                     (f.id_user_sender == friendUserId && f.id_user_receiver == currentUserId)) &&
                    f.status == "Accepted");

                if (friendship != null)
                {
                    context.Friendship.Remove(friendship);
                    await context.SaveChangesAsync();
                }
                else
                {
                    throw new FaultException<ServiceFault>(new ServiceFault
                    {
                        Message = "No se encontró la amistad. Es posible que ya la hayas eliminado."
                    });
                }
            }
        }

        public async Task<List<FriendDTO>> GetFriends(int currentUserId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var friends = await context.Friendship
                    .Where(f => (f.id_user_sender == currentUserId || f.id_user_receiver == currentUserId)
                                && f.status == "Accepted")
                    .Select(f => new
                    {
                        FriendUserId = f.id_user_sender == currentUserId ? f.id_user_receiver : f.id_user_sender
                    })
                    .ToListAsync();

                var friendIds = friends.Select(f => f.FriendUserId).ToList();
                var friendDetails = await context.User
                    .Where(u => friendIds.Contains(u.id_user))
                    .Select(u => new FriendDTO
                    {
                        UserId = u.id_user,
                        Nickname = u.nickname,
                        Status = u.status
                    })
                    .ToListAsync();

                return friendDetails;
            }
        }

        public async Task<List<FriendRequestDTO>> GetPendingRequests(int currentUserId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var requests = await context.Friendship
                    .Where(f => f.id_user_receiver == currentUserId && f.status == "Pending")
                    .Join(context.User,
                        friendship => friendship.id_user_sender,
                        user => user.id_user,
                        (friendship, user) => new FriendRequestDTO
                        {
                            RequesterId = user.id_user,
                            Nickname = user.nickname
                        })
                    .ToListAsync();

                return requests;
            }
        }

        public async Task<List<FriendRequestDTO>> GetSentRequests(int currentUserId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var requests = await context.Friendship
                    .Where(f => f.id_user_sender == currentUserId && f.status == "Pending")
                    .Join(context.User,
                        friendship => friendship.id_user_receiver,
                        user => user.id_user,
                        (friendship, user) => new FriendRequestDTO
                        {
                            RequesterId = currentUserId,
                            TargetUserId = user.id_user,
                            Nickname = user.nickname
                        })
                    .ToListAsync();

                return requests;
            }
        }


        public Task InviteFriendToLobby(int currentUserId, int targetFriendId, string lobbyCode)
        {
            if (string.IsNullOrEmpty(lobbyCode))
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "Error de invitación: El código del lobby es nulo." });
            }

            var inviter = GlobalSessionManager.Instance.GetClient(currentUserId);
            if (inviter == null || inviter.CurrentLobby == null)
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "Error de sesión: No se encontró tu lobby." });
            }

            if (inviter.CurrentLobby.LobbyCode != lobbyCode)
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "Error de invitación: No estás en el lobby correcto." });
            }

            var target = GlobalSessionManager.Instance.GetClient(targetFriendId);
            if (target == null)
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "Tu amigo no está conectado." });
            }

            if (target.CurrentLobby != null)
            {
                if (target.CurrentLobby == inviter.CurrentLobby)
                {
                    throw new FaultException<ServiceFault>(new ServiceFault { Message = "El jugador ya se encuentra en el lobby." });
                }
                else
                {
                    throw new FaultException<ServiceFault>(new ServiceFault { Message = "Tu amigo ya está en otro lobby." });
                }
            }

            target.CallbackChannel.ReceiveLobbyInvite(inviter.Nickname, lobbyCode);
            return Task.CompletedTask;
        }
    }
}