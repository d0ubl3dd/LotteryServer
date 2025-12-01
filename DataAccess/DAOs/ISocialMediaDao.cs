using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.DAOs
{
    public interface ISocialMediaDao
    {
        Task<SocialMedia> GetSocialMediaByUserIdAsync(int userId);
        Task AddSocialMediaAsync(SocialMedia socialMedia);
        Task<int> SaveChangesAsync();
        Task UpdateSocialMediaAsync(SocialMedia socialMedia);        
        Task<bool> ExistsTwitterUsernameExcludingUserAsync(int currentUserId, string username);
        Task<bool> ExistsInstagramUsernameExcludingUserAsync(int currentUserId, string username);
        Task<bool> ExistsTikTokUsernameExcludingUserAsync(int currentUserId, string username);
    }
}