using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    [DataContract]
    public class GameSettingsDTO
    {
        [DataMember]
        public int MaxPlayers { get; set; }

        [DataMember]
        public bool IsPrivate { get; set; }
    }
}
