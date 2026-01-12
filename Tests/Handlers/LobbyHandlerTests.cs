using BusinessLogic.Logic;
using BusinessLogic.Models;
using Contracts.Callbacks;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection; // Necesario para el truco de Reflexión
using System.ServiceModel;
using System.Threading.Tasks;
using Tests.Builders;
using Xunit;

namespace Tests.Handlers
{
    public class LobbyHandlerTests
    {
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly LobbyHandler _handler;

        public LobbyHandlerTests()
        {
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockCallback = new Mock<ILotteryCallback>();

            _handler = new LobbyHandler(_mockLobbyManager.Object, _mockSessionManager.Object);
        }

        // ==========================================
        // HELPER: Reflexión para saltar encapsulamiento
        // ==========================================
        private void ForceSetGameInProgress(Lobby lobby, bool value)
        {
            // Buscamos la propiedad IsGameInProgress
            var property = typeof(Lobby).GetProperty("IsGameInProgress",
                BindingFlags.Public | BindingFlags.Instance);

            if (property != null && property.CanWrite)
            {
                // Si tiene un set (aunque sea privado), lo usamos
                property.SetValue(lobby, value);
            }
            else
            {
                // Si es 'readonly' total, buscamos el campo de respaldo (backing field)
                var field = typeof(Lobby).GetField("<IsGameInProgress>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (field != null)
                {
                    field.SetValue(lobby, value);
                }
            }
        }

        // ==========================================
        // PRUEBAS: CreateLobby
        // ==========================================

        [Fact]
        public async Task CreateLobby_WhenUserIsValidAndNotInLobby_ShouldReturnLobbyState()
        {
            // Arrange
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

            var expectedDto = new LobbyStateDto { LobbyCode = "ABC1234" };

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);
            _mockLobbyManager.Setup(lm => lm.CreateLobby(client)).Returns(expectedDto);

            // Act
            var result = await _handler.CreateLobby(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ABC1234", result.LobbyCode);
            _mockLobbyManager.Verify(lm => lm.CreateLobby(client), Times.Once);
        }

        [Fact]
        public async Task CreateLobby_WhenUserAlreadyInLobby_ShouldThrowFault_AlreadyInLobby()
        {
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var mockUserDao = new Mock<IUserDao>();
            var existingLobby = new Lobby("EXIST", client, mockUserDao.Object);
            client.CurrentLobby = existingLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);
            
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.CreateLobby(user));

            Assert.Equal("LOBBY_USER_ALREADY_IN", ex.Detail.ErrorCode);
        }


        [Fact]
        public async Task CreateLobby_WhenSessionNotFound_ShouldThrowFault_UserOffline()
        {
            // Arrange
            var user = new UserBuilder().Build();
            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns((PlayerClient)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.CreateLobby(user));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS: JoinLobby
        // ==========================================

        [Fact]
        public async Task JoinLobby_WhenCodeIsValid_ShouldReturnLobbyState()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            string code = "CODE12";
            var expectedDto = new LobbyStateDto { LobbyCode = code };

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);
            _mockLobbyManager.Setup(lm => lm.JoinLobby(client, code)).Returns(expectedDto);

            // Act
            var result = await _handler.JoinLobby(user, code);

            // Assert
            Assert.Equal(code, result.LobbyCode);
        }

        [Fact]
        public async Task JoinLobby_WhenCodeIsEmpty_ShouldThrowFault_BadRequest()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.JoinLobby(new UserBuilder().Build(), ""));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS: KickPlayer
        // ==========================================

        [Fact]
        public async Task KickPlayer_WhenHostIsInLobby_ShouldCallManagerKick()
        {            
            var hostUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hostUser.id_user, hostUser.nickname, hostUser.id_avatar, _mockCallback.Object);

            var mockUserDao = new Mock<IUserDao>();

            var realLobby = new Lobby("MYLOBBY", hostClient, mockUserDao.Object);
            hostClient.CurrentLobby = realLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(hostUser.id_user)).Returns(hostClient);

            await _handler.KickPlayer(hostUser, 50);

            _mockLobbyManager.Verify(lm => lm.KickPlayer(hostClient, 50), Times.Once);
        }


        [Fact]
        public async Task KickPlayer_WhenHostNotInLobby_ShouldThrowFault_LobbyError()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.KickPlayer(user, 50));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS NUEVAS: ChooseBoard
        // ==========================================

        [Fact]
        public async Task ChooseBoard_WhenBoardIsValid_ShouldAssignToClient()
        {
            // Arrange
            var mockUserDao = new Mock<IUserDao>();
            var user = new UserBuilder().WithId(10).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var realLobby = new Lobby("LOBBY1", client, mockUserDao.Object);
            client.CurrentLobby = realLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act
            await _handler.ChooseBoard(user, 1);

            // Assert
            Assert.Equal(1, client.SelectedBoardId);
            Assert.NotNull(client.WinningCards);
            Assert.NotEmpty(client.WinningCards);
        }

        [Fact]
        public async Task ChooseBoard_WhenNotInLobby_ShouldThrowFault_LobbyError()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, 1));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ChooseBoard_WhenGameInProgress_ShouldThrowFault_GameError()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var mockUserDao = new Mock<IUserDao>();
            var realLobby = new Lobby("LOBBY1", client, mockUserDao.Object);


            // --- USO DEL HELPER DE REFLEXIÓN ---
            ForceSetGameInProgress(realLobby, true);

            client.CurrentLobby = realLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, 1));

            // Si falla, verifica que 'GameException' esté mapeada en 'ExceptionMapper'
            Assert.Equal("GAME_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ChooseBoard_WhenBoardIdInvalid_ShouldThrowFault_BadRequest()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var mockUserDao = new Mock<IUserDao>();
            var realLobby = new Lobby("LOBBY1", client, mockUserDao.Object);

            client.CurrentLobby = realLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, -99));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }
    }
}