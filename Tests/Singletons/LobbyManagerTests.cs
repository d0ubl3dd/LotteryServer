using Xunit;
using Moq;
using System;
using System.Linq;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.DTOs;
using Contracts.Callbacks;
using DataAccess; // Para User
using Tests.Builders;

namespace Tests.Logic
{
    public class LobbyManagerTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly LobbyManager _manager;

        public LobbyManagerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockCallback = new Mock<ILotteryCallback>();

            // SUT (System Under Test)
            _manager = new LobbyManager(_mockSessionManager.Object);
        }

        // ==========================================
        // PRUEBAS: CreateLobby
        // ==========================================

        [Fact]
        public void CreateLobby_WhenHostIsValid_ShouldCreateLobbyAndReturnState()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Host válido.
             * ✔ Salida Esperada: Objeto LobbyStateDto con código generado y lista de jugadores (solo host).
             */

            // Arrange
            var hostUser = new UserBuilder().WithId(1).Build();

            // FIX: Constructor actualizado con 4 parámetros
            var hostClient = new PlayerClient(hostUser.id_user, hostUser.nickname, hostUser.id_avatar, _mockCallback.Object);

            // Act
            var result = _manager.CreateLobby(hostClient);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.LobbyCode));
            Assert.Single(result.Players); // Solo el host
            Assert.Equal(hostUser.id_user, result.Players[0].UserId);

            // Verificamos que se puede encontrar
            var lobby = _manager.FindLobbyByHostId(hostUser.id_user);
            Assert.NotNull(lobby);
        }

        // ==========================================
        // PRUEBAS: JoinLobby
        // ==========================================

        [Fact]
        public void JoinLobby_WhenLobbyExistsAndOpen_ShouldAddPlayer()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Lobby existente, jugador nuevo.
             * ✔ Salida Esperada: Jugador agregado, broadcast enviado.
             */

            // Arrange
            // 1. Crear Lobby
            var hUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hUser.id_user, hUser.nickname, hUser.id_avatar, _mockCallback.Object);

            var lobbyDto = _manager.CreateLobby(hostClient);

            // 2. Jugador que se une
            var joinerUser = new UserBuilder().WithId(2).Build();
            var joinerClient = new PlayerClient(joinerUser.id_user, joinerUser.nickname, joinerUser.id_avatar, _mockCallback.Object);

            // Act
            var result = _manager.JoinLobby(joinerClient, lobbyDto.LobbyCode);

            // Assert
            Assert.Equal(2, result.Players.Count);

            // Verificar que el host recibió la notificación
            _mockCallback.Verify(cb => cb.PlayerJoined(It.Is<UserDto>(u => u.UserId == 2)), Times.AtLeastOnce);
        }

        [Fact]
        public void JoinLobby_WhenLobbyCodeInvalid_ShouldThrowException()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Código inexistente.
             * ✔ Salida Esperada: LobbyNotFoundException.
             */

            var u = new UserBuilder().Build();
            var client = new PlayerClient(u.id_user, u.nickname, u.id_avatar, _mockCallback.Object);

            Assert.Throws<LobbyNotFoundException>(() =>
                _manager.JoinLobby(client, "INVALID_CODE"));
        }

        [Fact]
        public void JoinLobby_WhenPlayerIsBanned_ShouldThrowException()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Jugador previamente baneado intenta reingresar.
             * ✔ Salida Esperada: PlayerBannedException.
             */

            // Arrange
            var hUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hUser.id_user, hUser.nickname, hUser.id_avatar, _mockCallback.Object);

            var lobbyDto = _manager.CreateLobby(hostClient);
            var lobby = _manager.FindLobbyByHostId(1);

            // Simulamos ban manual (o vía KickPlayer)
            lobby.BanPlayer(99);

            var bUser = new UserBuilder().WithId(99).Build();
            var bannedClient = new PlayerClient(bUser.id_user, bUser.nickname, bUser.id_avatar, _mockCallback.Object);

            // Act & Assert
            // CORRECCIÓN AQUÍ: Usamos PlayerBannedException
            Assert.Throws<PlayerBannedException>(() =>
                _manager.JoinLobby(bannedClient, lobbyDto.LobbyCode));
        }

        // ==========================================
        // PRUEBAS: KickPlayer
        // ==========================================

        [Fact]
        public void KickPlayer_WhenHostKicksPlayer_ShouldRemoveAndBan()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Host expulsa a un jugador unido.
             * ✔ Salida Esperada: Jugador removido, agregado a lista negra, notificación enviada.
             */

            // Arrange
            var hUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hUser.id_user, hUser.nickname, hUser.id_avatar, _mockCallback.Object);

            var lobbyDto = _manager.CreateLobby(hostClient);

            var vUser = new UserBuilder().WithId(2).Build();
            var victimClient = new PlayerClient(vUser.id_user, vUser.nickname, vUser.id_avatar, _mockCallback.Object);

            _manager.JoinLobby(victimClient, lobbyDto.LobbyCode);

            // Configurar SessionManager para que encuentre a la víctima al kickear
            _mockSessionManager.Setup(sm => sm.GetClient(2)).Returns(victimClient);

            // Act
            _manager.KickPlayer(hostClient, 2);

            // Assert
            var lobby = _manager.FindLobbyByHostId(1);
            Assert.Single(lobby.Players); // Solo queda el host
            Assert.True(lobby.IsBanned(2)); // Está baneado

            // Verificar llamada a YouWereKicked
            _mockCallback.Verify(cb => cb.YouWereKicked(), Times.Once);
        }

        // ==========================================
        // PRUEBAS: LeaveLobby (Cierre de Lobby)
        // ==========================================

        [Fact]
        public void LeaveLobby_WhenHostLeaves_ShouldCloseLobbyAndNotifyAll()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: El Host abandona la sala.
             * ✔ Salida Esperada: El lobby se elimina del diccionario y se notifica LobbyClosed a todos.
             */

            // Arrange
            var hUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hUser.id_user, hUser.nickname, hUser.id_avatar, _mockCallback.Object);

            var pUser = new UserBuilder().WithId(2).Build();
            var playerClient = new PlayerClient(pUser.id_user, pUser.nickname, pUser.id_avatar, _mockCallback.Object);

            var lobbyDto = _manager.CreateLobby(hostClient);
            _manager.JoinLobby(playerClient, lobbyDto.LobbyCode);

            // Act
            _manager.LeaveLobby(hostClient);

            // Assert
            // 1. El lobby ya no debe existir
            var lobby = _manager.FindLobbyByHostId(1);
            Assert.Null(lobby);

            // 2. Se notificó cierre
            _mockCallback.Verify(cb => cb.LobbyClosed(), Times.AtLeastOnce);

            // 3. Los clientes quedaron "libres" (CurrentLobby = null)
            Assert.Null(playerClient.CurrentLobby);
        }
    }
}