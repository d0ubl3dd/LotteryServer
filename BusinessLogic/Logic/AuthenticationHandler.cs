using BusinessLogic.Exceptions;
using BusinessLogic.Logic.Base;
using BusinessLogic.Validation;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class AuthenticationHandler : BaseHandler
    {
        private readonly IUserDao _userDAO;

        public AuthenticationHandler(IUserDao userDAO) : base(typeof(AuthenticationHandler))
        {
            if (userDAO == null)
            {
                throw new ArgumentNullException(nameof(userDAO));
            }
            _userDAO = userDAO;
        }

        public async Task<User> LoginUser(string userName, string password)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                User userResult = null;

                _logger.InfoFormat("[LoginUser] Login attempt for: {0}", userName);

                User foundUser = await _userDAO.GetUserByNicknameAsync(userName);

                LoginValidationResult validationResult = LoginValidator.ValidateLoginAttempt(userName, password, foundUser);

                if (validationResult == LoginValidationResult.Success)
                {
                    await HandleSuccessfulLogin(foundUser);
                    userResult = foundUser;
                }
                else
                {
                    await HandleFailedLogin(foundUser, validationResult);
                    ThrowLoginException(validationResult);
                }

                return userResult;

            }, "LoginUser");
        }

        public async Task LogoutUser(User userToLogout)
        {
            if (userToLogout == null)
            {
                throw new ArgumentNullException(nameof(userToLogout), "Cannot logout a null user.");
            }

            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[LogoutUser] Logging out for {0}.", userToLogout.nickname);

                if (userToLogout.id_user > 0)
                {
                    User userInDb = await _userDAO.GetUserByIdAsync(userToLogout.id_user);

                    if (userInDb == null)
                    {
                        throw new UserNotFoundException("The user to disconnect was not found in DB.");
                    }

                    userInDb.status = "Offline";
                    await _userDAO.SaveChangesAsync();
                }
                else
                {
                    _logger.InfoFormat("[LogoutUser] Guest user {0} disconnected (memory cleanup).", userToLogout.nickname);
                }

                _logger.Info("[LogoutUser] Session closed successfully.");

            }, "LogoutUser");
        }

        private async Task HandleSuccessfulLogin(User foundUser)
        {
            User userToUpdate = await _userDAO.GetUserByIdAsync(foundUser.id_user);
            if (userToUpdate != null)
            {
                userToUpdate.status = "Online";
                userToUpdate.failedLoginAttempts = 0;
                userToUpdate.lastLoginDate = DateTime.UtcNow;
                await _userDAO.SaveChangesAsync();

                _logger.InfoFormat("[LoginUser] Successful login and status updated for: {0}", foundUser.nickname);
            }
        }

        private async Task HandleFailedLogin(User foundUser, LoginValidationResult reason)
        {
            _logger.InfoFormat("[LoginUser] Failed login for {0}. Reason: {1}",
                foundUser?.nickname ?? "Unknown",
                reason);

            if (foundUser != null)
            {
                User userToUpdate = await _userDAO.GetUserByIdAsync(foundUser.id_user);
                if (userToUpdate != null)
                {
                    userToUpdate.failedLoginAttempts++;

                    if (userToUpdate.failedLoginAttempts >= 5)
                    {
                        userToUpdate.isLocked = true;
                        _logger.WarnFormat("[LoginUser] Account for {0} has been LOCKED.", userToUpdate.nickname);
                    }

                    await _userDAO.SaveChangesAsync();
                }
            }
        }

        private static void ThrowLoginException(LoginValidationResult result)
        {
            Exception exceptionToThrow;

            switch (result)
            {
                case LoginValidationResult.UserNotFound:
                    exceptionToThrow = new UserNotFoundException("The user does not exist.");
                    break;

                case LoginValidationResult.IncorrectPassword:
                    exceptionToThrow = new IncorrectPasswordException("Incorrect password.");
                    break;

                case LoginValidationResult.AccountLocked:
                    exceptionToThrow = new AccountLockedException("Your account is locked due to too many attempts.");
                    break;

                default:
                    exceptionToThrow = new InvalidOperationException("Unknown validation error.");
                    break;
            }

            throw exceptionToThrow;
        }
    }
}