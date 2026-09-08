using AutoMapper;
using ProcurementSystem.Core.DTOs.Auth;

namespace ProcurementSystem.Core.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mappings will be configured here as DTOs are created
            // Note: Entity-to-DTO mappings should be registered in the API layer
            // or after Infrastructure reference is available via DI

            // TODO: Add mappings as you create DTOs
            // CreateMap<BidPackageEntity, BidPackageDto>();
            // CreateMap<CreateBidPackageRequest, BidPackageEntity>();
        }
    }
}
