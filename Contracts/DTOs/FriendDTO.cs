using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class FriendDto
    {
        [DataMember]
        public int UserId { get; set; }
        [DataMember]
        public int FriendId { get; set; }
        [DataMember]
        public string Nickname { get; set; }
        [DataMember]
        public string Status { get; set; }
    }
}