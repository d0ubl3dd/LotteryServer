using BusinessLogic.Exceptions;
using Contracts.Faults;
using DataAccess;
using log4net;
using System;
using System.ServiceModel;

namespace BusinessLogic.Logic
{
    public class ChatHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChatHandler));
        private readonly ISessionManager _sessionManager;

        public ChatHandler(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        public void SendMessage(User currentUser, string message)
        {
            ExecuteFaultSafe(() =>
            {
                if (currentUser == null)
                    throw new ArgumentNullException(nameof(currentUser), "El usuario actual no puede ser nulo.");

                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.Info($"[SendMessage] Mensaje vacío ignorado para usuario {currentUser.nickname}.");
                    return;
                }

                _logger.Info($"[SendMessage] Usuario {currentUser.nickname} intenta enviar mensaje.");

                var client = _sessionManager.GetClient(currentUser.id_user);

                if (client == null)
                    throw new UserNotOnlineException("No se encontró sesión activa para este usuario.");

                if (client.CurrentLobby == null)
                    throw new UserNotInLobbyException("No estás dentro de un lobby, no puedes enviar mensajes.");

                _logger.Info($"[SendMessage] Enviando mensaje en lobby '{client.CurrentLobby.LobbyCode}' desde {client.Nickname}.");

                client.CurrentLobby.BroadcastChatMessage(client.Nickname, message);

            }, "SendMessage");
        }

        private void ExecuteFaultSafe(Action action, string operationName)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw HandleException(ex, operationName);
            }
        }

        private FaultException<ServiceFault> HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault> fault)
                return fault;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case UserNotInLobbyException _:
                    errorCode = "CHAT_USER_NOT_IN_LOBBY";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Intento de chat sin lobby.");
                    break;

                case UserNotOnlineException _:
                    errorCode = "CHAT_USER_OFFLINE";
                    clientMessage = "Tu sesión parece haber expirado o no existe.";
                    _logger.Warn($"[{operationName}] Usuario no encontrado en SessionManager.");
                    break;

                case ArgumentNullException _:
                    errorCode = "CHAT_BAD_REQUEST";
                    clientMessage = "Datos de envío inválidos.";
                    _logger.Error($"[{operationName}] Argumento nulo detectado: {ex.Message}");
                    break;

                case ForbiddenWordException _:
                    errorCode = "CHAT_FORBIDDEN_WORD";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Palabra prohibida detectada en mensaje.");
                    break;

                default:
                    errorCode = "CHAT_INTERNAL_ERROR";
                    clientMessage = "Error al enviar el mensaje.";
                    _logger.Fatal($"[{operationName}] Error inesperado: {ex}", ex);
                    break;
            }

            return new FaultException<ServiceFault>(
                new ServiceFault
                {
                    ErrorCode = errorCode,
                    Message = clientMessage
                },
                new FaultReason(clientMessage)
            );
        }
    }
}