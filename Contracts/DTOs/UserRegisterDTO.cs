using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class UserRegisterDTO
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string PaternalLastName { get; set; }

        [DataMember]
        public string MaternalLastName { get; set; }
    }
}