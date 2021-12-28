using AutoMapper;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Application.Mappers.Profiles
{
    public class CacheProfile : Profile
    {
        protected override void Configure()
        {
            base.Configure();

            CreateMap<ProductInfo, ProductData>().ReverseMap();
            CreateMap<SKU, SkuData>().ReverseMap();

            CreateMap<MemberInfo, MemberData>().ReverseMap();
            CreateMap<ShippingAddressInfo, ShippingAddressData>().ReverseMap();
        }
    }
}
