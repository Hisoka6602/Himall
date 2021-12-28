using AutoMapper;

namespace Himall.Application.Mappers.Profiles
{
    public class ShopProfile : Profile
	{
		protected override void Configure()
		{
			base.Configure();

			CreateMap<Entities.ShopInfo, DTO.Shop>();
			CreateMap<DTO.Shop, Entities.ShopInfo>();

			CreateMap<Entities.ShopBranchInfo, DTO.ShopBranch>();
			CreateMap<DTO.ShopBranch, Entities.ShopBranchInfo>();

			CreateMap<Entities.ShopBranchSkuInfo, DTO.ShopBranchSku>();
			CreateMap<DTO.ShopBranchSku, Entities.ShopBranchSkuInfo>();

			CreateMap<Entities.VShopInfo, DTO.VShop>();
			CreateMap<DTO.VShop, Entities.VShopInfo>();

			CreateMap<Entities.CustomerServiceInfo, DTO.CustomerService>();
			CreateMap<DTO.CustomerService, Entities.CustomerServiceInfo>();

			CreateMap<Entities.ShopAccountItemInfo, Himall.DTO.ShopAccountItem>().ForMember(p => p.ShopAccountType, options => options.MapFrom(p => p.TradeType));
			CreateMap<Himall.DTO.ShopAccountItem, Entities.ShopAccountItemInfo>().ForMember(p => p.TradeType, options => options.MapFrom(p => p.ShopAccountType));

			CreateMap<Entities.ShopAccountInfo, DTO.ShopAccount>();
			CreateMap<DTO.ShopAccount, Entities.ShopAccountInfo>();

			CreateMap<Entities.ShopBranchManagerInfo, DTO.ShopBranchManager>();
			CreateMap<DTO.ShopBranchManager, Entities.ShopBranchManagerInfo>();
		}
	}
}
