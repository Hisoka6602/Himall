using System.Linq;
using System.Web.Mvc;
using Himall.Web.Framework;
using Himall.Application;
using Himall.DTO.QueryModel;

namespace Himall.Web.Areas.Admin.Controllers
{
    [MarketingAuthorization]
    /// <summary>
    /// 满减
    /// </summary>
    public class FullDiscountController : BaseAdminController
    {
        #region 购买服务列表

        public ActionResult BoughtList()
        {
            return View();
        }

        [UnAuthorize]
        public JsonResult GetBoughtList(MarketBoughtQuery query)
        {
            query.MarketType = CommonModel.MarketType.FullDiscount;
            var data = FullDiscountApplication.GetMarketServiceBuyList(query);
            return Json(data);
        }
        #endregion

        #region 服务费用设置

        public ActionResult ServiceSetting()
        {
            decimal model = FullDiscountApplication.GetMarketServicePrice();
            return View(model);
        }

        [HttpPost]
        public JsonResult ServiceSetting(decimal Price)
        {
            Result result = new Result();
            if(Price<0)
            {
                result.success = false;
                result.msg = "错误的服务价格！";
                return Json(result);
            }
            FullDiscountApplication.SetMarketServicePrice(Price);
            result.success = true;
            result.msg = "保存成功！";
            return Json(result);
        }
        #endregion
    }
}