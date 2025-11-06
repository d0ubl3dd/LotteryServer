using System;
using System.Runtime.Serialization;

[DataContract]
public class UserDto
{
    [DataMember]
    public int UserId { get; set; }
    [DataMember]
    public int? AvatarId { get; set; }
    [DataMember]
    public string AvatarUrl { get; set; }
    [DataMember]
    public string Nickname { get; set; }
    [DataMember]
    public string Email { get; set; }
    [DataMember]
    public string Password { get; set; }
    [DataMember]
    public DateTime? RegistrationDate { get; set; }
    [DataMember]
    public string FirstName { get; set; }
    [DataMember]
    public string PaternalLastName { get; set; }
    [DataMember]
    public string MaternalLastName { get; set; }
    [DataMember]
    public int? Score { get; set; }
    [DataMember]
    public bool IsHost { get; set; }
}
