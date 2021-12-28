using AutoMapper;
using Himall.DTO.Live;
using Himall.Entities;

namespace Himall.Application.Mappers.Profiles
{
    public class LiveProfile : Profile
    {
        protected override void Configure()
        {
            base.Configure();

            CreateMap<LiveRoomInfo, LiveRoom>().ReverseMap();

            CreateMap<LiveProductInfo, LiveProduct>().ReverseMap();
        }
    }
}
