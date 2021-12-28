using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.CommonModel;
using Himall.Application;

namespace Himall.Web.Areas.Admin.Controllers
{
    [MarketingAuthorization]
    public class CollocationController : BaseAdminController
    {
        MarketService _MarketService;
        public CollocationController(MarketService MarketService)
        {
            _MarketService = MarketService;
        }

        #region 活动列表
        public ActionResult Management()
        {
           var  model = _MarketService.GetServiceSetting(MarketType.Collocation);
            return View();
        }
        #endregion


        #region 购买服务列表
        public JsonResult List(MarketBoughtQuery query)
        {
            query.MarketType = MarketType.Collocation;
            var data = _MarketService.GetBoughtShopList(query);

            var list = data.Models.Select(item => {
                var market = MarketApplication.GetMarketService(item.MarketServiceId);
                return new
                {
                    Id = item.Id,
                    StartDate = item.StartTime.ToString("yyyy-MM-dd"),
                    EndDate = item.EndTime.ToString("yyyy-MM-dd"),
                    ShopName = market.ShopName
                };
            }).ToList();

            return Json(new { rows = list, total = data.Total });
        }
        #endregion

        #region 服务费用设置

        public ActionResult ServiceSetting()
        {
            Entities.MarketSettingInfo model = _MarketService.GetServiceSetting(MarketType.Collocation);
            return View(model);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult SaveServiceSetting(decimal Price)
        {
            Result result = new Result();
            var model = new Entities.MarketSettingInfo { Price = Price, TypeId = MarketType.Collocation };
            _MarketService.AddOrUpdateServiceSetting(model);
            result.success = true;
            result.msg = "保存成功！";
            return Json(result);
        }
        #endregion


    }
}