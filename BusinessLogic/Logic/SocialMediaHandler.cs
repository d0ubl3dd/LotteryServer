using BusinessLogic.Exceptions;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using log4net;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class SocialMediaHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SocialMediaHandler));
        
        private readonly ISocialMediaDao _socialMediaRepository;
        private readonly IUserDao _userRepository;

        public SocialMediaHandler(ISocialMediaDao socialDao, IUserDao userDao)
        {
            _socialMediaRepository = socialDao ?? throw new ArgumentNullException(nameof(socialDao));
            _userRepository = userDao ?? throw new ArgumentNullException(nameof(userDao));
        }

        public async Task<SocialMediaDto> GetSocialMedia(int userId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                    throw new UserNotFoundException($"El usuario {userId} no existe.");
                
                var social = await _socialMediaRepository.GetSocialMediaByUserIdAsync(userId);
                if (social == null)
                {
                    return new SocialMediaDto
                    {
                        IdUser = userId,
                        Facebook = null,
                        Instagram = null,
                        TikTok = null,
                        Twitter = null
                    };
                }

                return new SocialMediaDto
                {
                    IdUser = social.id_user,
                    Facebook = social.facebook,
                    Instagram = social.instagram,
                    TikTok = social.tiktok,
                    Twitter = social.twitter
                };

            }, "GetSocialMedia");
        }

        public async Task<bool> UpdateSocialMedia(SocialMediaDto media)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[UpdateSocialMedia] User {media.IdUser}");

                var user = await _userRepository.GetUserByIdAsync(media.IdUser);
                if (user == null)
                    throw new UserNotFoundException($"No existe el usuario con ID {media.IdUser}");
                
                if (!string.IsNullOrWhiteSpace(media.Twitter) &&
                    await _socialMediaRepository.ExistsTwitterUsernameExcludingUserAsync(media.IdUser, media.Twitter))
                {
                    throw new UserAlreadyExistsException($"El nombre de usuario de Twitter/X '{media.Twitter}' ya está en uso por otra cuenta.");
                }

                if (!string.IsNullOrWhiteSpace(media.Instagram) &&
                    await _socialMediaRepository.ExistsInstagramUsernameExcludingUserAsync(media.IdUser, media.Instagram))
                {
                    throw new UserAlreadyExistsException($"El nombre de usuario de Instagram '{media.Instagram}' ya está en uso por otra cuenta.");
                }

                if (!string.IsNullOrWhiteSpace(media.TikTok) &&
                    await _socialMediaRepository.ExistsTikTokUsernameExcludingUserAsync(media.IdUser, media.TikTok))
                {
                    throw new UserAlreadyExistsException($"El nombre de usuario de TikTok '{media.TikTok}' ya está en uso por otra cuenta.");
                }

                var existing = await _socialMediaRepository.GetSocialMediaByUserIdAsync(media.IdUser);

                if (existing == null)
                {                
                    await _socialMediaRepository.AddSocialMediaAsync(new SocialMedia
                    {
                        id_user = media.IdUser,
                        facebook = media.Facebook,
                        instagram = media.Instagram,
                        tiktok = media.TikTok,
                        twitter = media.Twitter
                    });
                }
                else
                {
                    existing.facebook = media.Facebook;
                    existing.instagram = media.Instagram;
                    existing.tiktok = media.TikTok;
                    existing.twitter = media.Twitter;
                    
                    await _socialMediaRepository.UpdateSocialMediaAsync(existing);
                }

                await _socialMediaRepository.SaveChangesAsync();
                return true;

            }, "UpdateSocialMedia");
        }

        private async Task<T> ExecuteFaultSafeAsync<T>(Func<Task<T>> action, string method)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, method);
                return default;
            }
        }

        private void HandleException(Exception ex, string method)
        {
            if (ex is FaultException<ServiceFault>)
                throw ex;

            string error = "SOCIAL_ERROR";
            string msg = "Error en redes sociales.";

            switch (ex)
            {
                case UserNotFoundException _:
                    error = "USER_NOT_FOUND";
                    msg = ex.Message;
                    _logger.Warn($"[{method}] {msg}");
                    break;

                case UserAlreadyExistsException _:
                    error = "USER_DUPLICATE";
                    msg = ex.Message;
                    _logger.Warn($"[{method}] Conflicto de unicidad: {msg}");
                    break;

                case ArgumentException _:
                    error = "INVALID_DATA";
                    msg = ex.Message;
                    _logger.Warn($"[{method}] {msg}");
                    break;

                default:
                    _logger.Error($"[{method}] Error inesperado → {ex}", ex);
                    break;
            }

            throw new FaultException<ServiceFault>(
                new ServiceFault { ErrorCode = error, Message = msg },
                new FaultReason(msg)
            );
        }
    }
}