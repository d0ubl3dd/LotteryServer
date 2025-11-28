using DataAccess;
using System.Threading.Tasks;

namespace DataAccess.DAOs
{
    public interface IUserDao
    {
        Task<bool> NicknameExistsAsync(string nickname);
        Task<bool> EmailExistsAsync(string email);
        void AddUser(User user);
        Task<int> SaveChangesAsync();
        Task<User> GetUserByNicknameAsync(string nickname);
        Task<User> GetUserByIdAsync(int id);
        Task<bool> IsUserBannedAsync(int userId);
        Task BanUserAsync(Banned banInfo);
    }
}