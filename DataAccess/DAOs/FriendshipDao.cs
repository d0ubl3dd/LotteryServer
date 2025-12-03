using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.DAOs
{
    public class FriendshipDao : IFriendshipDao
    {
        private const string StatusPending = "Pending";
        private const string StatusAccepted = "Accepted";

        public async Task<bool> FriendshipExistsAsync(int userId1, int userId2)
        {
            using (var context = new lottery_databaseEntities())
            {
                return await context.Friendship.AnyAsync(f =>
                    (f.id_user_sender == userId1 && f.id_user_receiver == userId2) ||
                    (f.id_user_sender == userId2 && f.id_user_receiver == userId1));
            }
        }

        public async Task RequestFriendshipAsync(int senderId, int receiverId)
        {
            using (var context = new lottery_databaseEntities())
            {
                context.Friendship.Add(new Friendship
                {
                    id_user_sender = senderId,
                    id_user_receiver = receiverId,
                    status = StatusPending
                });
                await context.SaveChangesAsync();
            }
        }

        public async Task<Friendship> GetPendingRequestAsync(int senderId, int receiverId)
        {
            using (var context = new lottery_databaseEntities())
            {
                return await context.Friendship.FirstOrDefaultAsync(f =>
                    f.id_user_sender == senderId &&
                    f.id_user_receiver == receiverId &&
                    f.status == StatusPending);
            }
        }

        public async Task<Friendship> GetAcceptedFriendshipAsync(int userId1, int userId2)
        {
            using (var context = new lottery_databaseEntities())
            {
                return await context.Friendship.FirstOrDefaultAsync(f =>
                    ((f.id_user_sender == userId1 && f.id_user_receiver == userId2) ||
                     (f.id_user_sender == userId2 && f.id_user_receiver == userId1)) &&
                    f.status == StatusAccepted);
            }
        }

        public async Task AcceptRequestAsync(Friendship friendship)
        {
            using (var context = new lottery_databaseEntities())
            {
                context.Friendship.Attach(friendship);
                friendship.status = StatusAccepted;
                context.Entry(friendship).Property(x => x.status).IsModified = true;
                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveFriendshipAsync(Friendship friendship)
        {
            using (var context = new lottery_databaseEntities())
            {
                var entity = await context.Friendship.FindAsync(friendship.id_friendship);
                if (entity != null)
                {
                    context.Friendship.Remove(entity);
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task<List<User>> GetAcceptedFriendsAsync(int userId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var friendIds = await context.Friendship
                    .Where(f => (f.id_user_sender == userId || f.id_user_receiver == userId)
                                && f.status == StatusAccepted)
                    .Select(f => f.id_user_sender == userId ? f.id_user_receiver : f.id_user_sender)
                    .ToListAsync();

                return await context.User
                    .Where(u => friendIds.Contains(u.id_user))
                    .ToListAsync();
            }
        }

        public async Task<List<User>> GetPendingRequestsAsync(int userId)
        {
            using (var context = new lottery_databaseEntities())
            {
                return await context.Friendship
                    .Where(f => f.id_user_receiver == userId && f.status == StatusPending)
                    .Join(context.User,
                        f => f.id_user_sender,
                        u => u.id_user,
                        (f, u) => u)
                    .ToListAsync();
            }
        }

        public async Task<List<User>> GetSentRequestsAsync(int userId)
        {
            using (var context = new lottery_databaseEntities())
            {
                return await context.Friendship
                    .Where(f => f.id_user_sender == userId && f.status == StatusPending)
                    .Join(context.User,
                        f => f.id_user_receiver,
                        u => u.id_user,
                        (f, u) => u)
                    .ToListAsync();
            }
        }
    }
}