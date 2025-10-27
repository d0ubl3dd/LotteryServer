using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class UserSessionDTO
    {
        [DataMember]
        public int UserId { get; set; }
        [DataMember]
        public string Nickname { get; set; }
        [DataMember]
        public int AvatarId { get; set; }
    }
}