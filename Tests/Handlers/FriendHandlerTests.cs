using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ServiceModel;
using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using DataAccess.DAOs;
using DataAccess; // Para entidad User y Friendship
using Contracts.Faults;
using Contracts.Callbacks;
using Tests.Builders;

namespace Tests.Handlers
{
    public class FriendHandlerTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IFriendshipDao> _mockFriendshipDao;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly FriendHandler _handler;

        public FriendHandlerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockFriendshipDao = new Mock<IFriendshipDao>();
            _mockCallback = new Mock<ILotteryCallback>();

            _handler = new FriendHandler(_mockSessionManager.Object, _mockFriendshipDao.Object);
        }

        // ----------------------------------------------------------------------------------
        // REGIÓN: SOLICITUD DE AMISTAD
        // ----------------------------------------------------------------------------------

        [Fact]
        public async Task SendRequest_WhenValid_ShouldCallDao()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: IDs válidos, sin relación previa.
             * ✔ Salida Esperada: Llamada a RequestFriendshipAsync.
             */

            // Arrange
            int userId = 1, targetId = 2;
            _mockFriendshipDao.Setup(d => d.FriendshipExistsAsync(userId, targetId))
                              .ReturnsAsync(false);

            // Act
            await _handler.SendRequestFriendship(userId, targetId);

            // Assert
            _mockFriendshipDao.Verify(d => d.RequestFriendshipAsync(userId, targetId), Times.Once);
        }

        [Fact]
        public async Task SendRequest_WhenGuest_ShouldThrowFault_GuestRestricted()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: ID negativo (Invitado).
             * ✔ Salida Esperada: Fault GUEST_RESTRICTED.
             */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendRequestFriendship(-1, 2));

            Assert.Equal("GUEST_RESTRICTED", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendRequest_WhenSelf_ShouldThrowFault_FriendInvalid()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Mismo ID para sender y target.
             * ✔ Salida Esperada: Fault FRIEND_INVALID.
             */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendRequestFriendship(1, 1));

            Assert.Equal("FRIEND_INVALID", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendRequest_WhenDuplicate_ShouldThrowFault_FriendDuplicate()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Relación ya existente en BD.
             * ✔ Salida Esperada: Fault FRIEND_DUPLICATE.
             */

            // Arrange
            _mockFriendshipDao.Setup(d => d.FriendshipExistsAsync(1, 2)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendRequestFriendship(1, 2));

            Assert.Equal("FRIEND_DUPLICATE", ex.Detail.ErrorCode);
        }

        // ----------------------------------------------------------------------------------
        // REGIÓN: ACEPTAR / RECHAZAR / ELIMINAR
        // ----------------------------------------------------------------------------------

        [Fact]
        public async Task AcceptRequest_WhenRequestExists_ShouldAccept()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Request pendiente existente.
             * ✔ Salida Esperada: Llamada a AcceptRequestAsync.
             */

            // Arrange
            var request = new Friendship { id_user_sender = 2, id_user_receiver = 1 };

            _mockFriendshipDao.Setup(d => d.GetPendingRequestAsync(2, 1)).ReturnsAsync(request);

            // Act
            await _handler.AcceptFriendRequest(1, 2);

            // Assert
            _mockFriendshipDao.Verify(d => d.AcceptRequestAsync(request), Times.Once);
        }

        [Fact]
        public async Task AcceptRequest_WhenNotFound_ShouldThrowFault_FriendNotFound()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Request inexistente (null).
             * ✔ Salida Esperada: Fault FRIEND_NOT_FOUND.
             */

            // Arrange
            _mockFriendshipDao.Setup(d => d.GetPendingRequestAsync(It.IsAny<int>(), It.IsAny<int>()))
                              .ReturnsAsync((Friendship)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.AcceptFriendRequest(1, 2));

            Assert.Equal("FRIEND_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task GetFriends_WhenCalled_ShouldReturnDtoList()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario con 2 amigos.
             * ✔ Salida Esperada: Lista de 2 FriendDto mapeados correctamente.
             */

            // Arrange
            var friends = new List<User> {
                new User { id_user = 2, nickname = "Friend1", status = "Online" },
                new User { id_user = 3, nickname = "Friend2", status = "Offline" }
            };
            _mockFriendshipDao.Setup(d => d.GetAcceptedFriendsAsync(1)).ReturnsAsync(friends);

            // Act
            var result = await _handler.GetFriends(1);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Friend1", result[0].Nickname);
            Assert.Equal("Online", result[0].Status);
        }

        // ----------------------------------------------------------------------------------
        // REGIÓN: INVITACIÓN A LOBBY (COMPLEJA)
        // ----------------------------------------------------------------------------------

        [Fact]
        public async Task InviteToLobby_WhenConditionsMet_ShouldSendCallback()
        {
            /* DOCUMENTACIÓN
             * ✔ Escenario Complejo: 
             * - Solicitante en Contexto WCF válido.
             * - Solicitante tiene Lobby.
             * - Amigo objetivo está online y sin lobby.
             * ✔ Salida Esperada: El canal del amigo recibe ReceiveLobbyInvite.
             */

            // Arrange
            string lobbyCode = "LOBBY1";
            var inviterUser = new UserBuilder().WithId(1).WithNickname("Inviter").Build();
            var targetUser = new UserBuilder().WithId(2).WithNickname("Target").Build();

            // Clientes
            // FIX: Se pasan los parametros desglosados (id, nickname, avatar, callback)
            var inviterClient = new PlayerClient(inviterUser.id_user, inviterUser.nickname, inviterUser.id_avatar, _mockCallback.Object);

            var targetCallbackMock = new Mock<ILotteryCallback>(); // Callback que vamos a verificar
            // FIX: Se pasan los parametros desglosados
            var targetClient = new PlayerClient(targetUser.id_user, targetUser.nickname, targetUser.id_avatar, targetCallbackMock.Object);

            // Setup Lobby para el inviter
            var mockLobby = new Mock<Lobby>(lobbyCode, inviterClient);
            inviterClient.CurrentLobby = mockLobby.Object;

            // Setup SessionManager
            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns(1);
            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(inviterClient);
            _mockSessionManager.Setup(sm => sm.GetClient(2)).Returns(targetClient);

            // Act
            await _handler.InviteFriendToLobby(lobbyCode, 2);

            // Assert
            targetCallbackMock.Verify(cb => cb.ReceiveLobbyInvite("Inviter", lobbyCode), Times.Once);
        }

        [Fact]
        public async Task InviteToLobby_WhenInviterNotInLobby_ShouldThrowFault_LobbyError()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Solicitante válido pero CurrentLobby es null.
             * ✔ Salida Esperada: Fault LOBBY_ERROR.
             */

            // Arrange
            var inviterUser = new UserBuilder().WithId(1).Build();

            // FIX: Se pasan los parametros desglosados
            var client = new PlayerClient(inviterUser.id_user, inviterUser.nickname, inviterUser.id_avatar, _mockCallback.Object);

            client.CurrentLobby = null; // Sin lobby

            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns(1);
            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.InviteFriendToLobby("CODE", 2));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task InviteToLobby_WhenTargetAlreadyInSameLobby_ShouldThrowFault_LobbyUserAlreadyIn()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: El amigo ya está en EL MISMO lobby.
             * ✔ Salida Esperada: Fault LOBBY_USER_ALREADY_IN.
             */

            // Arrange
            var inviterUser = new UserBuilder().WithId(1).Build();
            var targetUser = new UserBuilder().WithId(2).Build();

            // FIX: Instanciamos pasando propiedades individuales
            var inviterClient = new PlayerClient(inviterUser.id_user, inviterUser.nickname, inviterUser.id_avatar, _mockCallback.Object);
            var targetClient = new PlayerClient(targetUser.id_user, targetUser.nickname, targetUser.id_avatar, _mockCallback.Object);

            var lobby = new Mock<Lobby>("CODE", inviterClient);

            // Ambos en el mismo lobby
            inviterClient.CurrentLobby = lobby.Object;
            targetClient.CurrentLobby = lobby.Object;

            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns(1);
            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(inviterClient);
            _mockSessionManager.Setup(sm => sm.GetClient(2)).Returns(targetClient);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.InviteFriendToLobby("CODE", 2));

            Assert.Equal("LOBBY_USER_ALREADY_IN", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task InviteToLobby_WhenSessionContextFails_ShouldThrowFault_UserNotConnected()
        {
            /* DOCUMENTACIÓN
            * ✔ Entrada: GetUserIdFromContext devuelve null (fallo WCF).
            * ✔ Salida Esperada: Fault USER_OFFLINE.
            */

            // Arrange
            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns((int?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.InviteFriendToLobby("CODE", 2));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }
    }
}