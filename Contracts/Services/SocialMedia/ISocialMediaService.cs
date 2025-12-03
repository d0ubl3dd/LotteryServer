using Contracts.DTOs;
using Contracts.Faults;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Contracts.Services.SocialMedia
{
    [ServiceContract]
    public interface ISocialMediaService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<SocialMediaDto> GetSocialMediaAsync(int currentUserId);

        [OperationContract]
        [FaultContract(typeof(ServiceFault))]
        Task<bool> SaveOrUpdateSocialMediaAsync(SocialMediaDto media);
    }
}