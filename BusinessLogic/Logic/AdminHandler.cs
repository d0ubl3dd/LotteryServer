using System;
using System.Threading.Tasks;
using Contracts.Faults;
using BusinessLogic.Exceptions;
using System.ServiceModel;
using DataAccess.DAOs;
using DataAccess;
using log4net;

namespace BusinessLogic.Logic
{
    public class AdminHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AdminHandler));
        private readonly IUserDao _userDAO;

        public AdminHandler(IUserDao userDAO)
        {
            _userDAO = userDAO ?? throw new ArgumentNullException(nameof(userDAO));
        }

        public async Task BanUser(int adminId, int targetUserId, string reason)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                var admin = await _userDAO.GetUserByIdAsync(adminId);
                if (admin == null)
                {
                    throw new UserNotFoundException("El administrador no existe.");
                }

                var targetUser = await _userDAO.GetUserByIdAsync(targetUserId);
                if (targetUser == null)
                {
                    throw new UserNotFoundException("El usuario a banear no existe.");
                }

                bool isAlreadyBanned = await _userDAO.IsUserBannedAsync(targetUserId);
                if (isAlreadyBanned)
                {
                    throw new InvalidOperationException("El usuario ya se encuentra baneado.");
                }

                var banInfo = new Banned
                {
                    id_user = targetUserId,
                    //moderator = adminId,
                    reason = reason,
                    banned_at = DateTime.UtcNow,
                    unbanned_at = null
                };

                await _userDAO.BanUserAsync(banInfo);

                targetUser.status = "Offline";
                await _userDAO.SaveChangesAsync();

                _logger.Warn($"[BanUser] El admin {admin.nickname} baneó a {targetUser.nickname}. Razón: {reason}");

            }, "BanUser");
        }

        private async Task ExecuteFaultSafeAsync(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
            }
        }

        private void HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault>)
                throw ex;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case UserNotFoundException _:
                    errorCode = "ADMIN_USER_NOT_FOUND";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Usuario no encontrado: {ex.Message}");
                    break;

                case InvalidOperationException _:
                    errorCode = "ADMIN_INVALID_OPERATION";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Operación inválida: {ex.Message}");
                    break;

                case ArgumentNullException _:
                    errorCode = "ADMIN_BAD_REQUEST";
                    clientMessage = "Datos incompletos.";
                    _logger.Error($"[{operationName}] Argumento nulo: {ex.Message}");
                    break;

                case System.Data.Entity.Core.EntityException _:
                case System.Data.SqlClient.SqlException _:
                    errorCode = "ADMIN_DB_ERROR";
                    clientMessage = "Error de conexión con la base de datos.";
                    _logger.Fatal($"[{operationName}] Error de BD: {ex}", ex);
                    break;

                default:
                    errorCode = "ADMIN_INTERNAL_ERROR";
                    clientMessage = "Error inesperado al procesar la solicitud de administración.";
                    _logger.Fatal($"[{operationName}] Error no controlado: {ex}", ex);
                    break;
            }

            throw new FaultException<ServiceFault>(
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