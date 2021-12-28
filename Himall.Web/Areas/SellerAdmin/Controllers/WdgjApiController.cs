
using Himall.Service;
using Himall.Web.Areas.SellerAdmin.Models;
using Himall.Web.Framework;
using System.Web.Mvc;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class WdgjApiController : BaseSellerController
    {
        private ShopService _ShopService;
        private long CurShopId;

        public WdgjApiController(ShopService ShopService)
        {
            _ShopService = ShopService;
            if (CurrentSellerManager != null)
            {//退出登录后，直接进入controller异常处理
                CurShopId = CurrentSellerManager.ShopId;
            }
        }

        public ActionResult Index()
        {
            var data = _ShopService.GetshopWdgjInfoById(CurShopId);
            var models = new WdgjApiModel()
            {
                Id = data != null ? data.Id : 0,
                uCode = data != null ? data.uCode : "",
                uSign = data != null ? data.uSign : ""
            };
            return View(models);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult Add(WdgjApiModel wdgj)
        {
            var service = _ShopService;
            Entities.ShopWdgjSettingInfo shopwdgjInfo = new Entities.ShopWdgjSettingInfo()
            {
                Id = wdgj.Id,
                ShopId = CurShopId,
                uCode = wdgj.uCode,
                uSign = wdgj.uSign
            };
            if (shopwdgjInfo.Id > 0)
                service.UpdateShopWdgj(shopwdgjInfo);
            else
                service.AddShopWdgj(shopwdgjInfo);
            return Json(new { success = true });
        }
    }
}