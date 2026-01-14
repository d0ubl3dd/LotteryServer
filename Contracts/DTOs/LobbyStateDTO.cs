using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class LobbyStateDto
    {
        [DataMember]
        public string LobbyCode { get; set; }
        [DataMember]
        public string HostNickname { get; set; }
        [DataMember]
        public List<UserDto> Players { get; set; }
        
        [DataMember]
        public List<string> ChatHistory { get; set; }
    }
}