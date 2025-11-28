using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace DataAccess.DAOs
{
    public class UserDao : IUserDao
    {
        private readonly lottery_databaseEntities _context;
        public UserDao()
        {
            _context = new lottery_databaseEntities();
        }

        public async Task<bool> NicknameExistsAsync(string nickname)
        {
            return await _context.User.AnyAsync(u => u.nickname == nickname);            
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.User.AnyAsync(u => u.email == email);
        }

        public void AddUser(User user)
        {
            _context.User.Add(user);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<User> GetUserByNicknameAsync(string nickname)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.nickname == nickname);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _context.User.FindAsync(id);
        }
    }
}
