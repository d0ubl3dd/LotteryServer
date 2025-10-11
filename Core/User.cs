using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Core
{
    [DataContract]
    public class User
    {
        [DataMember]
        public int id_user { get; set; }
        [DataMember]
        public int id_avatar { get; set; }
        [DataMember]
        public string nickname { get; set; }
        [DataMember]
        public string email { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public System.DateTime registration_date { get; set; }
        [DataMember]
        public string frist_name { get; set; }
        [DataMember]
        public string paternal_last_name { get; set; }
        [DataMember]
        public string maternal_last_name { get; set; }
        [DataMember]
        public int score { get; set; }
    }
}
