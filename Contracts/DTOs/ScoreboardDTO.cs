using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    [DataContract]
    public class ScoreboardDTO
    {
        [DataMember]
        public List<PlayerScoreDTO> TopScores { get; set; }
    }

    [DataContract]
    public class PlayerScoreDTO
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int Score { get; set; }
    }
}
