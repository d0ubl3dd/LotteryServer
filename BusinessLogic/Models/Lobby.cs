using Contracts.Callbacks;
using Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogic.Models
{
    public class Lobby
    {
        public string LobbyCode { get; }
        public PlayerClient Host { get; }
        public List<PlayerClient> Players { get; } = new List<PlayerClient>();
        public const int MAX_PLAYERS = 4;

        public bool IsGameInProgress { get; private set; }
        private Deck _gameDeck;
        private Task _gameLoopTask;
        private CancellationTokenSource _gameCts;

        public Lobby(string code, PlayerClient host)
        {
            LobbyCode = code;
            Host = host;
            AddPlayer(host);
        }

        public bool AddPlayer(PlayerClient player)
        {
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

        public void BroadcastChatMessage(string nickname, string message)
        {
            BroadcastToAll(client => client.ReceiveChatMessage(nickname, message));
        }

        public void BroadcastLobbyClosed()
        {
            BroadcastToAll(client => client.LobbyClosed());
        }

        public void BroadcastGameStarted(GameSettingsDto settings)
        {
            BroadcastToAll(client => client.OnGameStarted(settings));
        }

        public void BroadcastCardDrawn(CardDto card)
        {
            BroadcastToAll(client => client.OnCardDrawn(card));
        }

        public void BroadcastGameFinished()
        {
            BroadcastToAll(client => client.OnGameFinished());
        }

        public void StartLobbyGame(GameSettingsDto settings)
        {
            if (IsGameInProgress) return;

            IsGameInProgress = true;
            _gameDeck = new Deck();
            _gameCts = new CancellationTokenSource();

            BroadcastGameStarted(settings);

            _gameLoopTask = Task.Run(() => RunGameLoop(settings, _gameCts.Token));
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
            int cardDrawDelayMs = (settings?.CardDrawSpeedSeconds ?? 4) * 1000;

            try
            {
                while (IsGameInProgress && _gameDeck.CardsRemaining > 0)
                {
                    await Task.Delay(cardDrawDelayMs, token);

                    Card card = _gameDeck.DrawCard();
                    if (card == null) break;

                    var cardDto = new CardDto { Id = card.Id };

                    BroadcastCardDrawn(cardDto);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"Juego del lobby {LobbyCode} cancelado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en el bucle del juego {LobbyCode}: {ex.Message}");
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

        // --- Helpers ---
        public List<UserDto> GetPlayerDTOs()
        {
            return Players.Select(p => p.ToUserDto(p.UserId == Host.UserId)).ToList();
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
                catch (Exception)
                {
                    
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
                IsHost = isHost
            };
        }
    }
}