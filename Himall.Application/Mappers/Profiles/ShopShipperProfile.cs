using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Himall.Entities;

namespace Himall.Application.Mappers.Profiles
{
    public class ShopShipperProfile : Profile
    {
        protected override void Configure()
        {
            base.Configure();

            CreateMap<ShopShipperInfo, DTO.ShopShipper>();
            CreateMap<DTO.ShopShipper, ShopShipperInfo>();

        }
    }
}
