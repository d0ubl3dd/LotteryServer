using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using Contracts.Faults;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;

namespace BusinessLogic.Utilities
{
    public static class ExceptionMapper
    {
        private static readonly Dictionary<Type, ErrorStrategy> _strategies = new Dictionary<Type, ErrorStrategy>();
        private static readonly ILog _logger = LogManager.GetLogger("GlobalExceptionHandler");

        static ExceptionMapper()
        {
            Register<ArgumentNullException>("GLOBAL_BAD_REQUEST", "Datos de solicitud incompletos.", logAsError: true);
            Register<ArgumentException>("GLOBAL_BAD_REQUEST", "Datos inválidos.", logAsError: true);
            Register<TimeoutException>("GLOBAL_TIMEOUT", "La operación tardó demasiado.", logAsError: true);
            Register<InvalidOperationException>("GLOBAL_INVALID_OP", "Operación no válida en el estado actual.", logAsError: true);

            Register<EntityException>("DB_ERROR", "Error de conexión con la base de datos.", logAsError: true, isFatal: true);
            Register<SqlException>("DB_ERROR", "Error de base de datos.", logAsError: true, isFatal: true);

            Register<UserNotFoundException>("AUTH_USER_NOT_FOUND", "Usuario no encontrado.");
            Register<IncorrectPasswordException>("AUTH_INVALID_CREDENTIALS", "Credenciales inválidas.");
            Register<AccountLockedException>("AUTH_ACCOUNT_LOCKED", "Cuenta bloqueada.", useExMsg: true);
            Register<UserAlreadyExistsException>("USER_DUPLICATE", "El usuario o correo ya existe.", useExMsg: true);

            Register<GuestActionException>("GUEST_RESTRICTED", "Acción no permitida para invitados.", useExMsg: true);
            Register<EmptyNicknameException>("AUTH_EMPTY_NICKNAME", "El nickname es obligatorio.", useExMsg: true);
            Register<InvalidNicknameLengthException>("AUTH_INVALID_LENGTH", "Longitud de nickname incorrecta.", useExMsg: true);
            Register<InvalidNicknameFormatException>("AUTH_INVALID_FORMAT", "Formato de nickname inválido.", useExMsg: true);

            Register<EmailDeliveryException>("VERIFY_EMAIL_SEND_FAILED", "No pudimos enviar el correo de verificación.", 
                logAsError: true);
            Register<VerificationException>("VERIFY_ERROR", "Error en el proceso de verificación.", useExMsg: true);

            Register<LobbyNotFoundException>("LOBBY_NOT_FOUND", "El lobby no existe o ha cerrado.");
            Register<LobbyFullException>("LOBBY_FULL", "El lobby está lleno.");
            Register<UserAlreadyInLobbyException>("LOBBY_USER_ALREADY_IN", "El usuario ya está en un lobby.", useExMsg: true);
            Register<LobbyActionNotAllowedException>("LOBBY_ACTION_DENIED", "Acción de lobby denegada.", useExMsg: true);
            Register<PlayerBannedException>("LOBBY_PLAYER_BANNED", "Has sido expulsado de este lobby.", useExMsg: true);
            Register<GameAlreadyRunningException>("GAME_ALREADY_ACTIVE", "El juego ya está en curso.", useExMsg: true);
            Register<InvalidGameActionException>("GAME_ACTION_INVALID", "Movimiento inválido.", useExMsg: true);
            Register<LobbyException>("LOBBY_ERROR", "Error de lobby.", useExMsg: true);

            Register<GameException>("GAME_ERROR", "Error en la partida.", useExMsg: true);
            Register<NotEnoughPlayersException>("GAME_NOT_ENOUGH_PLAYERS", "Se necesitan al menos 2 jugadores.", useExMsg: true);

            Register<UserNotInLobbyException>("CHAT_USER_NOT_IN_LOBBY", "Debes estar en un lobby para chatear.", useExMsg: true);
            Register<ForbiddenWordException>("CHAT_FORBIDDEN_WORD", "Uso de lenguaje prohibido.", useExMsg: true);

            Register<FriendshipNotFoundException>("FRIEND_NOT_FOUND", "Solicitud o amistad no encontrada.", useExMsg: true);
            Register<InvalidFriendshipRequestException>("FRIEND_INVALID", "Solicitud de amistad inválida.", useExMsg: true);
            Register<FriendshipDuplicateException>("FRIEND_DUPLICATE", "Ya existe una relación con este usuario.", useExMsg: true);

            Register<UserNotConnectedException>("USER_OFFLINE", "El usuario no tiene una sesión activa.", useExMsg: true);
            Register<UserNotOnlineException>("USER_OFFLINE", "Usuario desconectado.", useExMsg: true);
            Register<ClientNotFoundException>("SESSION_CLIENT_NOT_FOUND", "Sesión de cliente no encontrada.");
            Register<SessionContextException>("SESSION_CONTEXT_ERROR", "Error de contexto de sesión WCF.", logAsError: true);
        }

        /* JUSTIFICATION: 
           This method acts as an internal helper for the static constructor. 
           It exceeds the 3-parameter limit to allow flexible boolean configuration (flags) 
           without the verbosity of creating a separate configuration object for each registration. 
           Refactoring this would hurt the readability of the static constructor. */
        private static void Register<T>(
            string code, string message, bool useExMsg = false, bool logAsError = false, bool isFatal = false) where T : Exception
        {
            Action<string> logAction;

            if (isFatal)
            {
                logAction = _logger.Fatal;
            }
            else
            {
                if (logAsError)
                {
                    logAction = _logger.Error;
                }
                else
                {
                    logAction = _logger.Warn;
                }
            }

            if (!_strategies.ContainsKey(typeof(T)))
            {
                _strategies.Add(typeof(T), new ErrorStrategy
                {
                    ErrorCode = code,
                    ClientMessage = message,
                    UseExceptionMessage = useExMsg,
                    LogAction = logAction
                });
            }
        }

        public static (ServiceFault Fault, Action<string> Logger) GetFaultAndLogAction(Exception ex)
        {
            if (_strategies.TryGetValue(ex.GetType(), out ErrorStrategy strategy))
            {
                string msg = strategy.UseExceptionMessage ? ex.Message : strategy.ClientMessage;
                return (
                    new ServiceFault { ErrorCode = strategy.ErrorCode, Message = msg },
                    strategy.LogAction
                );
            }

            return (
                new ServiceFault { ErrorCode = "INTERNAL_SERVER_ERROR", Message = "Ocurrió un error inesperado en el servidor." },
                _logger.Fatal
            );
        }

        private class ErrorStrategy
        {
            public string ErrorCode { get; set; }
            public string ClientMessage { get; set; }
            public bool UseExceptionMessage { get; set; }
            public Action<string> LogAction { get; set; }
        }
    }
}