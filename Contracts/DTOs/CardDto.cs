using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class CardDto
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Name { get; set; }
    }
}