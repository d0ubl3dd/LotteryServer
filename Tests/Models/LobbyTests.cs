using Xunit;
using Moq;
using System.Threading.Tasks;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.Callbacks;
using Tests.Builders;

namespace Tests.Models
{
    public class LobbyTests
    {
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly PlayerClient _host;
        private readonly Lobby _lobby;

        public LobbyTests()
        {
            _mockCallback = new Mock<ILotteryCallback>();
            var hostUser = new UserBuilder().WithId(1).WithNickname("Host").Build();
            _host = new PlayerClient(hostUser, _mockCallback.Object);

            // Creamos un Lobby real para probar su lógica interna
            _lobby = new Lobby("TEST01", _host);
        }

        // --- Gestión de Jugadores ---

        [Fact]
        public void AddPlayer_WhenSpaceAvailable_ShouldAdd()
        {
            var user = new UserBuilder().WithId(2).Build();
            var player = new PlayerClient(user, _mockCallback.Object);

            bool result = _lobby.AddPlayer(player);

            Assert.True(result);
            Assert.Contains(player, _lobby.Players);
            Assert.Equal(_lobby, player.CurrentLobby);
        }

        [Fact]
        public void AddPlayer_WhenFull_ShouldReturnFalse()
        {
            // Llenamos el lobby (Host + 3 = 4)
            _lobby.AddPlayer(new PlayerClient(new UserBuilder().WithId(2).Build(), _mockCallback.Object));
            _lobby.AddPlayer(new PlayerClient(new UserBuilder().WithId(3).Build(), _mockCallback.Object));
            _lobby.AddPlayer(new PlayerClient(new UserBuilder().WithId(4).Build(), _mockCallback.Object));

            // Intento 5
            var extra = new PlayerClient(new UserBuilder().WithId(5).Build(), _mockCallback.Object);
            bool result = _lobby.AddPlayer(extra);

            Assert.False(result);
            Assert.DoesNotContain(extra, _lobby.Players);
        }

        [Fact]
        public void AddPlayer_WhenBanned_ShouldReturnFalse()
        {
            _lobby.BanPlayer(99);
            var bannedPlayer = new PlayerClient(new UserBuilder().WithId(99).Build(), _mockCallback.Object);

            bool result = _lobby.AddPlayer(bannedPlayer);

            Assert.False(result);
        }

        // --- Chat y Spam ---

        [Fact]
        public void BroadcastChat_WhenSpamming_ShouldThrowChatException()
        {
            /* DOCUMENTACIÓN
             * ✔ Escenario: Un usuario envía el mismo mensaje más de 10 veces.
             * ✔ Salida Esperada: ChatException ("Spam detectado").
             */

            var spammer = new PlayerClient(new UserBuilder().WithId(2).WithNickname("Spammer").Build(), _mockCallback.Object);
            _lobby.AddPlayer(spammer);

            // Enviar 10 veces (límite)
            for (int i = 0; i < 10; i++)
            {
                _lobby.BroadcastChatMessage("Spammer", "Spam");
            }

            // La 11va vez debe explotar
            var ex = Assert.Throws<ChatException>(() =>
                _lobby.BroadcastChatMessage("Spammer", "Spam"));

            Assert.Equal("Spam detectado.", ex.Message);
        }

        // Nota: No probamos 'ForbiddenWordException' aquí porque depende del archivo .txt físico.
        // Esa sería una prueba de integración.
    }
}