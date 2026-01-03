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
            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendRequestFriendship(-1, 2));

            Assert.Equal("GUEST_RESTRICTED", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendRequest_WhenSelf_ShouldThrowFault_FriendInvalid()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendRequestFriendship(1, 1));

            Assert.Equal("FRIEND_INVALID", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendRequest_WhenDuplicate_ShouldThrowFault_FriendDuplicate()
        {
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
             * ✔ Corrección: Configuramos SessionManager para que determine quién está Online.
             */

            // Arrange
            var friends = new List<User> {
                // En DB puede decir lo que sea, pero SessionManager manda
                new User { id_user = 2, nickname = "Friend1", status = "Offline" },
                new User { id_user = 3, nickname = "Friend2", status = "Offline" }
            };

            _mockFriendshipDao.Setup(d => d.GetAcceptedFriendsAsync(1)).ReturnsAsync(friends);

            // --- CORRECCIÓN CLAVE ---
            // Configuramos SessionManager:
            // Friend1 (ID 2) -> ESTÁ CONECTADO (true) -> Status final será "Online"
            // Friend2 (ID 3) -> NO ESTÁ (false) -> Status final será "Offline"
            _mockSessionManager.Setup(sm => sm.IsUserOnline(2)).Returns(true);
            _mockSessionManager.Setup(sm => sm.IsUserOnline(3)).Returns(false);

            // Act
            var result = await _handler.GetFriends(1);

            // Assert
            Assert.Equal(2, result.Count);

            // Verificamos Friend1
            Assert.Equal("Friend1", result[0].Nickname);
            Assert.Equal("Online", result[0].Status); // Debe ser Online gracias al Mock

            // Verificamos Friend2
            Assert.Equal("Friend2", result[1].Nickname);
            Assert.Equal("Offline", result[1].Status);
        }

        // ----------------------------------------------------------------------------------
        // REGIÓN: INVITACIÓN A LOBBY
        // ----------------------------------------------------------------------------------

        [Fact]
        public async Task InviteToLobby_WhenConditionsMet_ShouldSendCallback()
        {
            // Arrange
            string lobbyCode = "LOBBY1";
            var inviterUser = new UserBuilder().WithId(1).WithNickname("Inviter").Build();
            var targetUser = new UserBuilder().WithId(2).WithNickname("Target").Build();

            // Clientes con constructor de 4 params
            var inviterClient = new PlayerClient(inviterUser.id_user, inviterUser.nickname, inviterUser.id_avatar, _mockCallback.Object);

            var targetCallbackMock = new Mock<ILotteryCallback>();
            var targetClient = new PlayerClient(targetUser.id_user, targetUser.nickname, targetUser.id_avatar, targetCallbackMock.Object);

            // Lobby simulado
            var mockLobby = new Mock<Lobby>(lobbyCode, inviterClient);
            inviterClient.CurrentLobby = mockLobby.Object;

            // Session Manager Setup
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
            // Arrange
            var inviterUser = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(inviterUser.id_user, inviterUser.nickname, inviterUser.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

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
            // Arrange
            var inviterUser = new UserBuilder().WithId(1).Build();
            var targetUser = new UserBuilder().WithId(2).Build();

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
            // Arrange
            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns((int?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.InviteFriendToLobby("CODE", 2));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }
    }
}