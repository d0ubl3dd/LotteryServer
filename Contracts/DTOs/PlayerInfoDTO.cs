using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace Contracts.DTOs
{
    [DataContract]
    public class PlayerInfoDTO
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int Level { get; set; }
    }
}
