using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using Contracts.Callbacks;
using Contracts.DTOs;
using Contracts.GameData;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogic.Models
{
    public class Lobby
    {
        public string LobbyCode { get; }
        public PlayerClient Host { get; }
        private readonly IUserDao _userDao;
        public List<PlayerClient> Players { get; } = new List<PlayerClient>();
        private readonly HashSet<int> _bannedPlayers = new HashSet<int>();
        public const int MAX_PLAYERS = 4;

        public virtual bool IsGameInProgress { get; private set; }
        private Deck _gameDeck;
        private CancellationTokenSource _gameCts;

        private readonly List<int> _drawnCards = new List<int>();
        public IReadOnlyList<int> DrawnCards => _drawnCards.AsReadOnly();

        private PlayerClient _lastDeclarer;
        private List<int> _lastMarkedPositions;

        private readonly HashSet<string> _forbiddenWords;
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(Lobby));

        private readonly Dictionary<int, Dictionary<string, int>> _messageHistory
        = new Dictionary<int, Dictionary<string, int>>();

        public int HostUserId => Host.UserId;

        public Lobby(string code, PlayerClient host, IUserDao userDao)
        {
            LobbyCode = code;
            Host = host;
            _userDao = userDao ?? throw new ArgumentNullException(nameof(userDao));
            AddPlayer(host);

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, "Resources", "forbidden_words.txt");
            _forbiddenWords = LoadForbiddenWords(fullPath);
        }

        public bool IsBanned(int userId)
        {
            return _bannedPlayers.Contains(userId);
        }

        public bool AddPlayer(PlayerClient player)
        {
            if (_bannedPlayers.Contains(player.UserId))
            {
                return false;
            }

            if (Players.Count >= MAX_PLAYERS || Players.Any(p => p.UserId == player.UserId))
            {
                return false;
            }

            Players.Add(player);
            player.CurrentLobby = this;
            return true;
        }

        public void RemovePlayer(PlayerClient player)
        {
            Players.Remove(player);
            player.CurrentLobby = null;

            if (player.UserId == Host.UserId)
            {
                StopLobbyGame();
            }
        }

        private static HashSet<string> LoadForbiddenWords(string path)
        {
            if (!File.Exists(path))
            {
                return new HashSet<string>();
            }

            var lines = File.ReadAllLines(path)
                .Select(w => w.Trim().ToLower())
                .Where(w => !string.IsNullOrWhiteSpace(w));

            return new HashSet<string>(lines);
        }

        public virtual bool BroadcastChatMessage(string nickname, string message)
        {
            var sender = Players.FirstOrDefault(p => p.Nickname == nickname);
            if (sender == null) return false;

            int userId = sender.UserId;

            if (!_messageHistory.ContainsKey(userId))
                _messageHistory[userId] = new Dictionary<string, int>();

            var history = _messageHistory[userId];

            var forbiddenWord = _forbiddenWords.FirstOrDefault(word =>
                message.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

            if (forbiddenWord != null)
            {
                if (sender.UserId == Host.UserId)
                {
                    Host.CallbackChannel.ReceiveChatMessage(
                        "Sistema",
                        "No puedes decir groserías. Modera tu lenguaje."
                    );
                    return false;
                }

                throw new ForbiddenWordException("Uso de lenguaje prohibido detectado.");
            }

            if (!history.ContainsKey(message))
            {
                history[message] = 0;
            }

            history[message]++;

            if (history[message] > 10)
            {
                throw new ChatException("Spam detectado.");
            }

            BroadcastToAll(client => client.ReceiveChatMessage(nickname, message));
            return true;
        }

        public void BroadcastBoardSelected(int userId, int boardId)
        {
            var player = Players.FirstOrDefault(p => p.UserId == userId);
            if (player != null)
            {
                player.SelectedBoardId = boardId;
            }
            BroadcastToAll(client => client.BoardSelected(userId, boardId));
        }

        public void BroadcastPlayerJoined(PlayerClient newPlayer)
        {
            var dto = newPlayer.ToUserDto(newPlayer.UserId == Host.UserId);
            BroadcastToAll(client => client.PlayerJoined(dto));
        }

        public void BroadcastPlayerLeft(int playerId)
        {
            BroadcastToAll(client => client.PlayerLeft(playerId));
        }

        public void BroadcastKicked(int playerId)
        {
            BroadcastToAll(client => client.PlayerKicked(playerId));
        }

        public void BroadcastLobbyClosed()
        {
            BroadcastToAll(client => client.LobbyClosed());
        }

        public void BroadcastGameStarted(GameSettingsDto settings)
        {
            foreach (var player in Players)
            {
                try
                {
                    player.CallbackChannel.OnGameStarted(settings);
                }
                catch (Exception ex)
                {                    
                    _logger.Warn($"No se pudo notificar al jugador {player.UserId}: {ex.Message}");
                }
            }
        }

        public void BroadcastCardDrawn(CardDto card)
        {
            BroadcastToAll(client => client.OnCardDrawn(card));
        }

        public void BroadcastGameFinished()
        {
            BroadcastToAll(client => client.OnGameFinished());
        }

        public async Task NotifyGameWinAsync(int winnerId)
        {
            var winnerClient = Players.FirstOrDefault(p => p.UserId == winnerId);
            if (winnerClient == null) return;

            StopLobbyGame();

            var user = await _userDao.GetUserByIdAsync(winnerClient.UserId);
            if (user != null)
            {
                user.score += 1000;
                await _userDao.SaveChangesAsync();
            }

            string mensajeGanador = $"¡El jugador {winnerClient.Nickname} ha ganado la partida y recibe 1000 puntos!";
            BroadcastSystemMessage(mensajeGanador);

            BroadcastToAll(client =>
            {
                try
                {
                    client.NotifyWinner(winnerClient.Nickname);
                }
                catch { }
            });
        }

        public virtual void StartLobbyGame(GameSettingsDto settings)
        {
            if (IsGameInProgress) return;

            IsGameInProgress = true;
            _gameDeck = new Deck();
            _gameCts = new CancellationTokenSource();

            BroadcastGameStarted(settings);

            Task.Run(() => RunGameLoop(settings, _gameCts.Token));
        }

        public void StopLobbyGame()
        {
            if (!IsGameInProgress) return;

            IsGameInProgress = false;
            if (_gameCts != null)
            {
                _gameCts.Cancel();
                _gameCts.Dispose();
                _gameCts = null;
            }
        }

        private async Task RunGameLoop(GameSettingsDto settings, CancellationToken token)
        {
            int cardDrawDelayMs = (settings?.CardDrawSpeedSeconds ?? 1) * 1000;

            try
            {
                while (IsGameInProgress && _gameDeck.CardsRemaining > 0)
                {
                    await Task.Delay(cardDrawDelayMs, token);

                    Card card = _gameDeck.DrawCard();
                    if (card == null)
                    {
                        break;
                    }

                    _drawnCards.Add(card.Id);

                    var cardDto = new CardDto { Id = card.Id };

                    BroadcastCardDrawn(cardDto);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.InfoFormat("Juego del lobby {0} cancelado.", LobbyCode);
            }
            catch (Exception exception)
            {
                _logger.Error(string.Format("Error en el bucle del juego {0}: {1}", LobbyCode, exception.Message), exception);
            }
            finally
            {
                IsGameInProgress = false;

                if (!token.IsCancellationRequested)
                {
                    BroadcastGameFinished();
                }
            }
        }

        public async Task DeclareWinAsync(PlayerBoardDto playerBoard)
        {
            var playerId = playerBoard.PlayerId;
            _lastDeclarer = Players.FirstOrDefault(p => p.UserId == playerId);
            _lastMarkedPositions = new List<int>(playerBoard.MarkedPositions);

            foreach (var player in Players)
            {
                try
                {
                    player.CallbackChannel.NotifyWinner(_lastDeclarer.Nickname);
                }
                catch
                {
                    
                }
            }
        }

        public async Task<bool> ValidateFalseLoteriaAsync(int challengerUserId)
        {
            if (_lastDeclarer == null)
                return false;

            var boardConfig = BoardConfigurations.GetBoardById(_lastDeclarer.SelectedBoardId);
            bool isDeclarerCorrect = _lastMarkedPositions.All(pos => _drawnCards.Contains(boardConfig[pos]));

            var declarerUser = await _userDao.GetUserByIdAsync(_lastDeclarer.UserId);
            var challenger = Players.FirstOrDefault(p => p.UserId == challengerUserId);
            User challengerUser = null;
            if (challenger != null)
                challengerUser = await _userDao.GetUserByIdAsync(challenger.UserId);

            if (isDeclarerCorrect)
            {
                if (declarerUser != null)
                {
                    int score = declarerUser.score ?? 0;
                    score = Math.Min(score + 1000, sbyte.MaxValue);
                    declarerUser.score = (sbyte)score;
                }

                if (challengerUser != null)
                {
                    int score = challengerUser.score ?? 0;
                    score = Math.Max(0, score - 500);
                    challengerUser.score = (sbyte)score;
                }

                if (challenger != null)
                    BroadcastSystemMessage($"¡Jugador {challenger.Nickname} se equivocó al acusar Falsa Lotería!");
            }
            else
            {
                if (declarerUser != null)
                {
                    int score = declarerUser.score ?? 0;
                    score = Math.Max(0, score - 500);
                    declarerUser.score = (sbyte)score;
                }

                if (challengerUser != null)
                {
                    int score = challengerUser.score ?? 0;
                    score = Math.Min(score + 500, sbyte.MaxValue);
                    challengerUser.score = (sbyte)score;
                }

                BroadcastSystemMessage($"¡Jugador {_lastDeclarer.Nickname} declaró Lotería falsa!");
            }

            await _userDao.SaveChangesAsync();

            _lastDeclarer = null;
            _lastMarkedPositions = null;

            return isDeclarerCorrect;
        }

        public void BanPlayer(int userId)
        {
            _bannedPlayers.Add(userId);
        }

        public List<UserDto> GetPlayerDTOs()
        {
            return Players.Select(p => p.ToUserDto(p.UserId == Host.UserId)).ToList();
        }

        private void BroadcastSystemMessage(string message)
        {
            BroadcastToAll(client =>
            {
                try
                {
                    client.ReceiveChatMessage("Sistema", message);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"No se pudo enviar mensaje de sistema a un jugador: {ex.Message}");
                }
            });
        }

        public void MarkPosition(int playerId, int position)
        {
            var player = Players.FirstOrDefault(p => p.UserId == playerId);
            if (player == null) return;

            if (!player.MarkedPositions.Contains(position))
            {
                player.MarkedPositions.Add(position);
            }
        }

        private void BroadcastToAll(Action<ILotteryCallback> action)
        {
            List<PlayerClient> playersCopy = new List<PlayerClient>(Players);

            foreach (var player in playersCopy)
            {
                try
                {
                    action(player.CallbackChannel);
                }
                catch (Exception exception)
                {
                    _logger.Warn(string.Format("No se pudo enviar el mensaje al jugador {0}. Error: {1}",
                        player.UserId, exception.Message), exception);
                }
            }
        }
    }

    public static class PlayerClientExtensions
    {
        public static UserDto ToUserDto(this PlayerClient player, bool isHost)
        {
            return new UserDto
            {
                UserId = player.UserId,
                Nickname = player.Nickname,
                AvatarId = player.AvatarId,
                SelectedBoardId = player.SelectedBoardId,
                IsHost = isHost
            };
        }
    }
}