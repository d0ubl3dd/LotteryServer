using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    [DataContract]
    public class UserRegisterDTO
    {
        [DataMember]
        public int IdUser { get; set; }
        [DataMember]
        public int IdAvatar { get; set; }
        [DataMember]
        public string Nickname { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public DateTime RegistrationDate { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string PaternalLastName { get; set; }
        [DataMember]
        public string MaternalLastName { get; set; }
        [DataMember]
        public int? Score { get; set; }       
    }
}