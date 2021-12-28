using AutoMapper;
using Himall.DTO;
using Himall.Entities;

namespace Himall.Application.Mappers.Profiles
{
    public class DistributionProfile : Profile
	{
		protected override void Configure()
		{
			base.Configure();

			CreateMap<DistributorInfo, DTO.Distribution.DistributorListDTO>();
            CreateMap<DistributorInfo, Distributor>();
            CreateMap<DistributionWithdrawInfo, DistributionWithdraw>();
        }
	}
}
