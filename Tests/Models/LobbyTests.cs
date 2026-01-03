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

            // FIX: Constructor actualizado con 4 parámetros
            _host = new PlayerClient(hostUser.id_user, hostUser.nickname, hostUser.id_avatar, _mockCallback.Object);

            // Creamos un Lobby real para probar su lógica interna
            _lobby = new Lobby("TEST01", _host);
        }

        // --- Gestión de Jugadores ---

        [Fact]
        public void AddPlayer_WhenSpaceAvailable_ShouldAdd()
        {
            var user = new UserBuilder().WithId(2).Build();

            // FIX: Constructor actualizado con 4 parámetros
            var player = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            bool result = _lobby.AddPlayer(player);

            Assert.True(result);
            Assert.Contains(player, _lobby.Players);
            Assert.Equal(_lobby, player.CurrentLobby);
        }

        [Fact]
        public void AddPlayer_WhenFull_ShouldReturnFalse()
        {
            // Llenamos el lobby (Host + 3 = 4)
            // FIX: Instanciamos usuarios auxiliares para pasar propiedades individuales
            var u2 = new UserBuilder().WithId(2).Build();
            _lobby.AddPlayer(new PlayerClient(u2.id_user, u2.nickname, u2.id_avatar, _mockCallback.Object));

            var u3 = new UserBuilder().WithId(3).Build();
            _lobby.AddPlayer(new PlayerClient(u3.id_user, u3.nickname, u3.id_avatar, _mockCallback.Object));

            var u4 = new UserBuilder().WithId(4).Build();
            _lobby.AddPlayer(new PlayerClient(u4.id_user, u4.nickname, u4.id_avatar, _mockCallback.Object));

            // Intento 5
            var u5 = new UserBuilder().WithId(5).Build();
            var extra = new PlayerClient(u5.id_user, u5.nickname, u5.id_avatar, _mockCallback.Object);

            bool result = _lobby.AddPlayer(extra);

            Assert.False(result);
            Assert.DoesNotContain(extra, _lobby.Players);
        }

        [Fact]
        public void AddPlayer_WhenBanned_ShouldReturnFalse()
        {
            _lobby.BanPlayer(99);

            var bannedUser = new UserBuilder().WithId(99).Build();
            // FIX: Constructor actualizado con 4 parámetros
            var bannedPlayer = new PlayerClient(bannedUser.id_user, bannedUser.nickname, bannedUser.id_avatar, _mockCallback.Object);

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

            var spammerUser = new UserBuilder().WithId(2).WithNickname("Spammer").Build();
            // FIX: Constructor actualizado con 4 parámetros
            var spammer = new PlayerClient(spammerUser.id_user, spammerUser.nickname, spammerUser.id_avatar, _mockCallback.Object);

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