using Himall.CommonModel;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Application
{
    public class GiftApplication:BaseApplicaion<GiftService>
    {
        public static GiftInfo GetGift(long id)
        {
            return Service.GetById(id);
        }

        public static IntegralMallAdInfo GetAdInfo(IntegralMallAdInfo.AdActivityType adtype, IntegralMallAdInfo.AdShowPlatform adplatform)
        {
            return Service.GetAdInfo(adtype, adplatform);
        }
        public static QueryPageModel<GiftModel> GetGifts(GiftQuery query)
        {
            return Service.GetGifts(query);
        }
        public static GiftInfo GetById(long id)
        {
            return Service.GetById(id);
        }
    }
}
