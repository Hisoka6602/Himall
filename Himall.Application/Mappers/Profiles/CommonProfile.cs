using AutoMapper;
using Himall.Entities;

namespace Himall.Application.Mappers.Profiles
{
    public class CommonProfile : Profile
    {
        protected override void Configure()
        {
            base.Configure();

            CreateMap<CategoryInfo, DTO.Category>();
            CreateMap<DTO.Category, CategoryInfo>();

            CreateMap<ManagerInfo, DTO.Manager>();
            CreateMap<DTO.Manager, ManagerInfo>();

            CreateMap<DTO.ExpressCompany, Himall.Entities.ExpressInfoInfo>();
            CreateMap<Himall.Entities.ExpressInfoInfo, DTO.ExpressCompany>();
        }
    }
}
