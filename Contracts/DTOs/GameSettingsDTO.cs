using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    [DataContract]
    public class GameSettingsDto
    {
        [DataMember]
        public int MaxPlayers { get; set; }
        [DataMember]
        public bool IsPrivate { get; set; }
        [DataMember]
        public int CardDrawSpeedSeconds { get; set; }
    }
}
