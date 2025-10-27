using System.Runtime.Serialization;

namespace Contracts.Faults
{
    [DataContract]
    public class ServiceFault
    {
        [DataMember]
        public string ErrorCode { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}