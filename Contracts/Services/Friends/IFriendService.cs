using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Services.Friends
{
    [ServiceContract]
    public interface IFriendService
    {
        [OperationContract]
        Task SendRequestFriendship(int targetUserId);

        [OperationContract]
        Task AddFriend(int friendshipRequestId);

        [OperationContract]
        Task RemoveFriend(int friendUserId);
    }
}
