using DataAccess; // Usamos las entidades, no Contracts
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.DAOs
{
    public interface IFriendshipDao
    {
        Task<bool> FriendshipExistsAsync(int userId1, int userId2);
        Task RequestFriendshipAsync(int senderId, int receiverId);
        Task<Friendship> GetPendingRequestAsync(int senderId, int receiverId);
        Task<Friendship> GetAcceptedFriendshipAsync(int userId1, int userId2);
        Task AcceptRequestAsync(Friendship friendship);
        Task RemoveFriendshipAsync(Friendship friendship);
        Task<List<User>> GetAcceptedFriendsAsync(int userId);
        Task<List<User>> GetPendingRequestsAsync(int userId);
        Task<List<User>> GetSentRequestsAsync(int userId);
    }
}