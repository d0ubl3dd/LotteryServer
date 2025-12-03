using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.DAOs
{
    public class SocialMediaDao : ISocialMediaDao
    {
        private readonly lottery_databaseEntities _context;

        public SocialMediaDao()
        {
            _context = new lottery_databaseEntities();
        }

        public async Task<SocialMedia> GetSocialMediaByUserIdAsync(int userId)
        {
            return await _context.SocialMedia
                .FirstOrDefaultAsync(sm => sm.id_user == userId);
        }

        public async Task AddSocialMediaAsync(SocialMedia socialMedia)
        {
            _context.SocialMedia.Add(socialMedia);
            await Task.CompletedTask;
        }

        public async Task UpdateSocialMediaAsync(SocialMedia socialMedia)
        {
            _context.Entry(socialMedia).State = EntityState.Modified;
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsTwitterUsernameExcludingUserAsync(int currentUserId, string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            return await _context.SocialMedia.AnyAsync(sm =>
                sm.twitter == username &&
                sm.id_user != currentUserId
            );
        }

        public async Task<bool> ExistsInstagramUsernameExcludingUserAsync(int currentUserId, string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            return await _context.SocialMedia.AnyAsync(sm =>
                sm.instagram == username &&
                sm.id_user != currentUserId
            );
        }

        public async Task<bool> ExistsTikTokUsernameExcludingUserAsync(int currentUserId, string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            return await _context.SocialMedia.AnyAsync(sm =>
                sm.tiktok == username &&
                sm.id_user != currentUserId
            );
        }
    }
}