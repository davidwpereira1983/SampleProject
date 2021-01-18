using AutoMapper;

namespace Company.TestProject.WebApi
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            this.CreateMap<Shared.DTO.Product, WebApiClient.DTO.Product>();
        }
    }
}
