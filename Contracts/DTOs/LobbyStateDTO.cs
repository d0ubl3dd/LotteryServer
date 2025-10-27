using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class LobbyStateDTO
    {
        [DataMember]
        public string LobbyCode { get; set; }
        [DataMember]
        public string HostNickname { get; set; }
        [DataMember]
        public List<PlayerInfoDTO> Players { get; set; }
    }
}