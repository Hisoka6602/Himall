using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Himall.Entities;

namespace Himall.Application.Mappers.Profiles
{
	public class FullDiscountProfile : Profile
	{
		protected override void Configure()
		{
			base.Configure();

			CreateMap<ActiveProductInfo, DTO.FullDiscountActiveProduct>();
			CreateMap<DTO.FullDiscountActiveProduct, ActiveProductInfo>();

            CreateMap<FullDiscountRuleInfo, DTO.FullDiscountRules>();
            CreateMap<DTO.FullDiscountRules, FullDiscountRuleInfo>();

            CreateMap<ActiveInfo, DTO.FullDiscountActive>();
            CreateMap<DTO.FullDiscountActive, ActiveInfo>();
            CreateMap<ActiveInfo, DTO.FullDiscountActiveBase>();
            CreateMap<DTO.FullDiscountActiveBase, ActiveInfo>();
            CreateMap<ActiveInfo, DTO.FullDiscountActiveList>();
            CreateMap<DTO.FullDiscountActiveList, ActiveInfo>();

            //CreateMap<ShopAccountItemInfo, Himall.DTO.ShopAccountItem>().ForMember(p => p.ShopAccountType, options => options.MapFrom(p => p.TradeType));
        }
	}
}
