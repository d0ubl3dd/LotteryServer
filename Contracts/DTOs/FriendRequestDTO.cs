using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class FriendRequestDTO
    {
        [DataMember]
        public int RequesterId { get; set; }

        [DataMember]
        public string Nickname { get; set; }
    }
}