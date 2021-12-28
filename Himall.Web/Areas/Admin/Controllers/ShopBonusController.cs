using Himall.CommonModel;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;
using Himall.Application;

namespace Himall.Web.Areas.Admin.Controllers
{
    [MarketingAuthorization]
    public class ShopBonusController : BaseAdminController
    {
        private MarketService _MarketService;

        public ShopBonusController(MarketService MarketService)
        {
            _MarketService = MarketService;
        }
      
        public ActionResult Management()
        {
            return View();
        }

        public ActionResult ServiceSetting()
        {
            Entities.MarketSettingInfo model = _MarketService.GetServiceSetting( MarketType.RandomlyBonus );
            return View( model );
        }

        [HttpPost]
        [UnAuthorize]
        public ActionResult SaveServiceSetting( decimal Price )
        {
            Result result = new Result();
            var model = new Entities.MarketSettingInfo { Price = Price , TypeId = MarketType.RandomlyBonus };
            _MarketService.AddOrUpdateServiceSetting( model );
            result.success = true;
            result.msg = "保存成功！";
            return Json( result );
        }

        [UnAuthorize]
        public ActionResult List(MarketBoughtQuery query)
        {
            query.MarketType = MarketType.RandomlyBonus;
            var marketEntities = _MarketService.GetBoughtShopList(query);
            var market = marketEntities.Models.Select(item => {
                var obj = MarketApplication.GetMarketService(item.MarketServiceId);
                return new
                {
                    Id = item.Id,
                    StartDate = item.StartTime.ToString("yyyy-MM-dd"),
                    EndDate = item.EndTime.ToString("yyyy-MM-dd"),
                    ShopName = obj.ShopName
                };
            }).ToList();
            return Json(new { rows = market, total = marketEntities.Total });
        }
	}
}