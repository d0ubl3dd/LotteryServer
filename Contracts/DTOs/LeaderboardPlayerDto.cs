using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    [DataContract]
    public class LeaderboardPlayerDto
    {
        [DataMember]
        public int UserId { get; set; }
        [DataMember]
        public string Nickname { get; set; }
        [DataMember]
        public int? Score { get; set; }
    }
}
