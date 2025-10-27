using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    [DataContract]
    public class UserProfileDTO
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string Email { get; set; }
    }
}
