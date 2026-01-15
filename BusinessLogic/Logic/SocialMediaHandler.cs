using BusinessLogic.Exceptions;
using BusinessLogic.Logic.Base;
using Contracts.DTOs;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class SocialMediaHandler : BaseHandler
    {
        private readonly ISocialMediaDao _socialMediaRepository;
        private readonly IUserDao _userRepository;

        public SocialMediaHandler(ISocialMediaDao socialDao, IUserDao userDao) : base(typeof(SocialMediaHandler))
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
                {
                    throw new UserNotFoundException(string.Format("El usuario {0} no existe.", userId));
                }

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
            if (media == null)
            {
                throw new ArgumentNullException(nameof(media));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[UpdateSocialMedia] User {0}", media.IdUser);

                var user = await _userRepository.GetUserByIdAsync(media.IdUser);
                if (user == null)
                {
                    throw new UserNotFoundException(string.Format("No existe el usuario con ID {0}", media.IdUser));
                }

                await ValidateSocialMediaAvailability(media);

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

        private async Task ValidateSocialMediaAvailability(SocialMediaDto media)
        {
            if (!string.IsNullOrWhiteSpace(media.Twitter) &&
                await _socialMediaRepository.ExistsTwitterUsernameExcludingUserAsync(media.IdUser, media.Twitter))
            {
                throw new UserAlreadyExistsException(string.Format(
                    "El nombre de usuario de Twitter/X '{0}' ya está en uso por otra cuenta.", media.Twitter));
            }

            if (!string.IsNullOrWhiteSpace(media.Instagram) &&
                await _socialMediaRepository.ExistsInstagramUsernameExcludingUserAsync(media.IdUser, media.Instagram))
            {
                throw new UserAlreadyExistsException(string.Format(
                    "El nombre de usuario de Instagram '{0}' ya está en uso por otra cuenta.", media.Instagram));
            }

            if (!string.IsNullOrWhiteSpace(media.TikTok) &&
                await _socialMediaRepository.ExistsTikTokUsernameExcludingUserAsync(media.IdUser, media.TikTok))
            {
                throw new UserAlreadyExistsException(string.Format(
                    "El nombre de usuario de TikTok '{0}' ya está en uso por otra cuenta.", media.TikTok));
            }
        }
    }
}