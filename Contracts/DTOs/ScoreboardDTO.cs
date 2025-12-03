using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class ScoreboardDto
    {
        [DataMember]
        public List<PlayerScoreDto> TopScores { get; set; }
    }

    [DataContract]
    public class PlayerScoreDto
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int Score { get; set; }
    }
}