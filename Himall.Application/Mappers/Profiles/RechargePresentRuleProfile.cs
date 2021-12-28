using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Himall.Entities;

namespace Himall.Application.Mappers.Profiles
{
    public class RechargePresentRuleProfile : Profile
    {
        protected override void Configure()
        {
            base.Configure();

            CreateMap<RechargePresentRuleInfo, DTO.RechargePresentRule>();
            CreateMap<DTO.RechargePresentRule, RechargePresentRuleInfo>();
        }
    }
}
