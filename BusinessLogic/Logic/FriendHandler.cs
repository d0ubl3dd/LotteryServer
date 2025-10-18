using DataAccess;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class FriendHandler
    {
        /// <summary>
        /// Creates a friendship request by adding a new row with 'Pending' status.
        /// </summary>
        public async Task SendRequestFriendship(int currentUserId, int targetUserId)
        {
            // A friendship request is a row with "Pending" status.
            var newFriendship = new Friendship
            {
                id_user1 = currentUserId,
                id_user2 = targetUserId,
                status = "Pending"
            };

            using (var context = new base_pruebaEntities())
            {
                // Prevents sending duplicate requests or requests to existing friends.
                bool friendshipExists = await context.Friendship
                    .AnyAsync(f => (f.id_user1 == currentUserId && f.id_user2 == targetUserId) ||
                                   (f.id_user1 == targetUserId && f.id_user2 == currentUserId));

                if (!friendshipExists)
                {
                    context.Friendship.Add(newFriendship);
                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Accepts a friend request, changing its status and creating the reverse relationship.
        /// </summary>
        /// <param name="currentUser">The user who is accepting the request.</param>
        /// <param name="requesterId">The ID of the user who sent the request.</param>
        public async Task<bool> AddFriend(User currentUser, int requesterId)
        {
            using (var context = new base_pruebaEntities())
            {
                // Find the pending request where the current user is the receiver.
                var pendingRequest = await context.Friendship
                    .FirstOrDefaultAsync(f => f.id_user1 == requesterId && f.id_user2 == currentUser.id_user && f.status == "Pending");

                if (pendingRequest == null)
                {
                    // No pending request found.
                    return false;
                }

                // 1. Update the original request's status to "Accepted".
                pendingRequest.status = "Accepted";

                // 2. Create the reverse relationship to make the friendship bidirectional.
                var reverseFriendship = new Friendship
                {
                    id_user1 = currentUser.id_user,
                    id_user2 = requesterId,
                    status = "Accepted"
                };
                context.Friendship.Add(reverseFriendship);

                await context.SaveChangesAsync();
                return true;
            }
        }

        /// <summary>
        /// Removes a friendship by deleting both rows that represent the relationship.
        /// </summary>
        public async Task<bool> RemoveFriend(int currentUserId, int friendUserId)
        {
            using (var context = new base_pruebaEntities())
            {
                // Find both sides of the friendship.
                var friendship1 = await context.Friendship
                    .FirstOrDefaultAsync(f => f.id_user1 == currentUserId && f.id_user2 == friendUserId);

                var friendship2 = await context.Friendship
                    .FirstOrDefaultAsync(f => f.id_user1 == friendUserId && f.id_user2 == currentUserId);

                if (friendship1 != null)
                {
                    context.Friendship.Remove(friendship1);
                }
                if (friendship2 != null)
                {
                    context.Friendship.Remove(friendship2);
                }

                int changes = await context.SaveChangesAsync();
                return changes > 0;
            }
        }
    }
}