using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class PlayerInfoDTO
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public string Nickname { get; set; }

        [DataMember]
        public int AvatarId { get; set; }

        [DataMember]
        public bool IsHost { get; set; }
    }
}