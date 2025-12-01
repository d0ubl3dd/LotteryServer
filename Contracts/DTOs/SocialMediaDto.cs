using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    [DataContract]
    public class SocialMediaDto
    {
        [DataMember]
        public int IdSocialMedia { get; set; }
        [DataMember]
        public int IdUser { get; set; }
        [DataMember]
        public string Facebook { get; set; }
        [DataMember]
        public string Instagram { get; set; }
        [DataMember]
        public string TikTok { get; set; }
        [DataMember]
        public string Twitter { get; set; }        
    }
}
