using BusinessLogic.Exceptions;
using Contracts.Callbacks;
using Contracts.DTOs;
using Contracts.GameData;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogic.Models
{
    public class Lobby
    {
        public const int MaxPlayers = 4;
        private const int SpamLimit = 10;
        private const int ChatHistoryLimit = 50;
        private const int WinScore = 1000;
        private const int PenaltyScore = 500;
        private const int DrawDelayFactor = 1000;

        private const string SystemName = "Sistema";
        private const string DbErrorMsg = "DB_ERROR";
        private const string ForbiddenWordsFileName = "forbidden_words.txt";

        public string LobbyCode { get; }
        public PlayerClient Host { get; }
        public int HostUserId => Host.UserId;
        public virtual bool IsGameInProgress { get; private set; }
        public List<PlayerClient> Players { get; } = new List<PlayerClient>();
        public IReadOnlyList<int> DrawnCards => _drawnCards.AsReadOnly();

        private readonly IUserDao _userDao;
        private readonly HashSet<int> _bannedPlayers = new HashSet<int>();
        private readonly List<string> _recentChatMessages = new List<string>();
        private readonly List<int> _drawnCards = new List<int>();
        private readonly HashSet<string> _forbiddenWords;
        private readonly Dictionary<int, Dictionary<string, int>> _messageHistory = new Dictionary<int, Dictionary<string, int>>();

        private Deck _gameDeck;
        private CancellationTokenSource _gameCts;
        private readonly SemaphoreSlim _pauseSemaphore = new SemaphoreSlim(0, 1);
        private bool _isPaused = false;

        private PlayerClient _lastDeclarer;
        private List<int> _lastMarkedPositions;

        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(Lobby));

        public Lobby(string code, PlayerClient host, IUserDao userDao)
        {
            LobbyCode = code;
            Host = host;
            _userDao = userDao ?? throw new ArgumentNullException(nameof(userDao));

            AddPlayer(host);

            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", ForbiddenWordsFileName);
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

            lock (Players)
            {
                if (Players.Count >= MaxPlayers || Players.Any(p => p.UserId == player.UserId))
                {
                    return false;
                }
                Players.Add(player);
            }

            player.CurrentLobby = this;

            if (player.CallbackChannel is ICommunicationObject channel)
            {
                channel.Closed += (s, e) => RemovePlayer(player);
                channel.Faulted += (s, e) => RemovePlayer(player);
            }
            return true;
        }

        public void RemovePlayer(PlayerClient player)
        {
            lock (Players)
            {
                Players.Remove(player);
            }
            player.CurrentLobby = null;

            if (player.UserId == Host.UserId)
            {
                StopLobbyGame();
                return;
            }
            if (IsGameInProgress && Players.Count < 2)
            {
                StopLobbyGame();
            }
        }

        public List<string> GetChatHistory()
        {
            lock (_recentChatMessages)
            {
                return new List<string>(_recentChatMessages);
            }
        }

        private static HashSet<string> LoadForbiddenWords(string path)
        {
            try
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
            catch (Exception ex)
            {
                _logger.Error("Error cargando palabras prohibidas", ex);
                return new HashSet<string>();
            }
        }

        public virtual bool BroadcastChatMessage(string nickname, string message)
        {
            var sender = Players.FirstOrDefault(p => p.Nickname == nickname);
            if (sender == null)
            {
                return false;
            }

            if (ContainsForbiddenWords(message))
            {
                HandleForbiddenWord(sender);
                return false;
            }

            if (IsSpam(sender.UserId, message))
            {
                throw new ChatException("Spam detectado.");
            }

            string formattedMessage = $"{nickname}: {message}";
            AddToChatHistory(formattedMessage);

            BroadcastToAll(client => client.ReceiveChatMessage(nickname, message));
            return true;
        }

        private bool ContainsForbiddenWords(string message)
        {
            return _forbiddenWords.Any(word => message.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void HandleForbiddenWord(PlayerClient sender)
        {
            if (sender.UserId == Host.UserId)
            {
                Host.CallbackChannel.ReceiveChatMessage(SystemName, "No puedes decir groserías. Modera tu lenguaje.");
            }
            else
            {
                throw new ForbiddenWordException("Uso de lenguaje prohibido detectado.");
            }
        }

        private bool IsSpam(int userId, string message)
        {
            if (!_messageHistory.ContainsKey(userId))
            {
                _messageHistory[userId] = new Dictionary<string, int>();
            }

            var history = _messageHistory[userId];
            if (!history.ContainsKey(message))
            {
                history[message] = 0;
            }

            history[message]++;
            return history[message] > SpamLimit;
        }

        private void AddToChatHistory(string message)
        {
            lock (_recentChatMessages)
            {
                _recentChatMessages.Add(message);
                if (_recentChatMessages.Count > ChatHistoryLimit)
                {
                    _recentChatMessages.RemoveAt(0);
                }
            }
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

        public void BroadcastPlayerLeft(int playerId) => BroadcastToAll(client => client.PlayerLeft(playerId));
        public void BroadcastKicked(int playerId) => BroadcastToAll(client => client.PlayerKicked(playerId));
        public void BroadcastLobbyClosed() => BroadcastToAll(client => client.LobbyClosed());
        public void BroadcastCardDrawn(CardDto card) => BroadcastToAll(client => client.OnCardDrawn(card));
        public void BroadcastGameFinished() => BroadcastToAll(client => client.OnGameFinished());
        public void BroadcastGameResumed() => BroadcastToAll(client => client.OnGameResumed());

        public void BroadcastGameStarted(GameSettingsDto settings)
        {
            BroadcastToAll(client => client.OnGameStarted(settings));
        }

        public async Task NotifyGameWinAsync(int winnerId)
        {
            var winnerClient = Players.FirstOrDefault(p => p.UserId == winnerId);
            if (winnerClient == null) return;

            StopLobbyGame();

            try
            {
                string messageCode = await ProcessWinScoreAsync(winnerClient);
                BroadcastSystemMessage(messageCode);

                var markedPositions = winnerClient.MarkedPositions ?? new List<int>();
                BroadcastToAll(client => client.NotifyWinner(
                    winnerClient.Nickname,
                    winnerClient.UserId,
                    winnerClient.SelectedBoardId,
                    markedPositions));
            }
            catch (Exception ex)
            {
                _logger.Error("Error en NotifyGameWinAsync", ex);
                throw new FaultException(DbErrorMsg);
            }
        }

        private async Task<string> ProcessWinScoreAsync(PlayerClient winnerClient)
        {
            if (winnerClient.UserId <= 0) return $"WIN_GST|{winnerClient.Nickname}";

            var user = await _userDao.GetUserByIdAsync(winnerClient.UserId);
            if (user != null)
            {
                user.score += WinScore;
                await _userDao.SaveChangesAsync();
                return $"WIN_REG|{winnerClient.Nickname}|{WinScore}";
            }

            return $"WIN_SIMPLE|{winnerClient.Nickname}";
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
            _isPaused = false;

            if (_pauseSemaphore.CurrentCount == 0)
            {
                try
                {
                    _pauseSemaphore.Release();
                }
                catch (SemaphoreFullException)
                {
                }
            }

            if (_gameCts != null)
            {
                _gameCts.Cancel();
                _gameCts.Dispose();
                _gameCts = null;
            }

            ResetGameState();
            BroadcastGameFinished();
        }

        private void ResetGameState()
        {
            _lastDeclarer = null;
            _lastMarkedPositions = null;
            _drawnCards.Clear();
        }

        private async Task RunGameLoop(GameSettingsDto settings, CancellationToken token)
        {
            int cardDrawDelayMs = (settings?.CardDrawSpeedSeconds ?? 1) * DrawDelayFactor;

            try
            {
                while (IsGameInProgress && _gameDeck.CardsRemaining > 0)
                {
                    if (_isPaused)
                    {
                        _logger.Info("[RunGameLoop] Juego pausado...");
                        await _pauseSemaphore.WaitAsync(token);
                    }

                    await Task.Delay(cardDrawDelayMs, token);

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    Card card = _gameDeck.DrawCard();
                    if (card == null)
                    {
                        break;
                    }

                    _drawnCards.Add(card.Id);
                    BroadcastCardDrawn(new CardDto { Id = card.Id });
                }
            }
            catch (TaskCanceledException)
            {
                _logger.InfoFormat("Juego lobby {0} cancelado.", LobbyCode);
            }
            catch (Exception exception)
            {
                _logger.Error($"Error crítico en loop {LobbyCode}", exception);
            }
            finally
            {
                if (IsGameInProgress)
                {
                    StopLobbyGame();
                }
            }
        }

        public Task DeclareWinAsync(PlayerBoardDto playerBoard)
        {
            var playerId = playerBoard.PlayerId;
            _lastDeclarer = Players.FirstOrDefault(p => p.UserId == playerId);
            _lastMarkedPositions = new List<int>(playerBoard.MarkedPositions);

            PauseGame();

            BroadcastToAll(client => client.NotifyWinner(
                _lastDeclarer.Nickname,
                _lastDeclarer.UserId,
                _lastDeclarer.SelectedBoardId,
                playerBoard.MarkedPositions));

            return Task.CompletedTask;
        }

        public async Task<bool> ValidateFalseLoteriaAsync(int challengerUserId)
        {
            if (_lastDeclarer == null || _lastMarkedPositions == null || !_lastMarkedPositions.Any() || !IsGameInProgress)
            {
                return false;
            }

            var boardConfig = BoardConfigurations.GetBoardById(_lastDeclarer.SelectedBoardId);
            if (boardConfig == null)
            {
                return false;
            }

            bool declarerWasCorrect = CheckIfDeclarerWon(boardConfig);

            var challenger = Players.FirstOrDefault(p => p.UserId == challengerUserId);
            BroadcastToAll(c => c.OnFalseLoteriaResult(_lastDeclarer.Nickname, challenger?.Nickname, declarerWasCorrect));

            try
            {
                await HandleFalseLoteriaOutcome(declarerWasCorrect, challenger);
            }
            catch (Exception ex)
            {
                _logger.Error("Error en ValidateFalseLoteriaAsync", ex);
                throw new FaultException(DbErrorMsg);
            }

            _lastDeclarer = null;
            _lastMarkedPositions = null;

            return !declarerWasCorrect;
        }

        private bool CheckIfDeclarerWon(List<int> boardConfig)
        {
            return _lastMarkedPositions.All(pos =>
            {
                if (pos < 0 || pos >= boardConfig.Count)
                {
                    return false;
                }
                int cardNumber = boardConfig[pos];
                return _drawnCards.Contains(cardNumber);
            });
        }

        private async Task HandleFalseLoteriaOutcome(bool declarerWasCorrect, PlayerClient challenger)
        {
            var declarerUser = await _userDao.GetUserByIdAsync(_lastDeclarer.UserId);

            User challengerUser = null;
            if (challenger != null)
            {
                challengerUser = await _userDao.GetUserByIdAsync(challenger.UserId);
            }

            if (declarerWasCorrect)
            {
                if (declarerUser != null)
                {
                    declarerUser.score = (sbyte)Math.Min((declarerUser.score ?? 0) + WinScore, sbyte.MaxValue);
                }

                if (challengerUser != null)
                {
                    challengerUser.score = (sbyte)Math.Max(0, (challengerUser.score ?? 0) - PenaltyScore);
                }

                if (challenger != null)
                {
                    BroadcastSystemMessage($"FL_FAIL|{challenger.Nickname}");
                }

                await _userDao.SaveChangesAsync();
                StopLobbyGame();
            }
            else
            {
                if (declarerUser != null)
                {
                    declarerUser.score = (sbyte)Math.Max(0, (declarerUser.score ?? 0)
                    - PenaltyScore);
                }

                if (challengerUser != null)
                {
                    challengerUser.score = (sbyte)Math.Min((challengerUser.score ?? 0)
                    + PenaltyScore, sbyte.MaxValue);
                }

                BroadcastSystemMessage($"FL_LIE|{_lastDeclarer.Nickname}");

                await _userDao.SaveChangesAsync();

                ResumeGame();
                BroadcastGameResumed();
            }
        }

        public void BanPlayer(int userId)
        {
            _bannedPlayers.Add(userId);
        }

        public List<UserDto> GetPlayerDTOs()
        {
            lock (Players)
            {
                return Players.Select(p => p.ToUserDto(p.UserId == Host.UserId)).ToList();
            }
        }

        public void MarkPosition(int playerId, int position)
        {
            var player = Players.FirstOrDefault(p => p.UserId == playerId);
            if (player != null && !player.MarkedPositions.Contains(position))
            {
                player.MarkedPositions.Add(position);
            }
        }

        private void BroadcastSystemMessage(string message)
        {
            string formattedMessage = $"{SystemName}: {message}";
            AddToChatHistory(formattedMessage);
            BroadcastToAll(client => client.ReceiveChatMessage(SystemName, message));
        }

        private void PauseGame()
        {
            if (!_isPaused && IsGameInProgress)
            {
                _isPaused = true;
                _logger.InfoFormat("[PauseGame] Juego pausado en lobby {0}", LobbyCode);
            }
        }

        private void ResumeGame()
        {
            if (_isPaused && IsGameInProgress)
            {
                _isPaused = false;
                if (_pauseSemaphore.CurrentCount == 0)
                {
                    try
                    {
                        _pauseSemaphore.Release();
                    }
                    catch (SemaphoreFullException)
                    {
                    }
                }
                _logger.InfoFormat("[ResumeGame] Juego reanudado en lobby {0}", LobbyCode);
            }
        }

        private void BroadcastToAll(Action<ILotteryCallback> action)
        {
            List<PlayerClient> playersSnapshot;
            lock (Players)
            {
                playersSnapshot = new List<PlayerClient>(Players);
            }

            foreach (var player in playersSnapshot)
            {
                SafeNotifyClient(player, action);
            }
        }

        private void SafeNotifyClient(PlayerClient player, Action<ILotteryCallback> action)
        {
            try
            {
                if (player.CallbackChannel is ICommunicationObject commObject && commObject.State == CommunicationState.Opened)
                {
                    action(player.CallbackChannel);
                }
            }
            catch (Exception ex)
            {
                _logger.WarnFormat("Fallo al notificar jugador {0}", player.UserId);
                _logger.Warn("Detalle del fallo:", ex);
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