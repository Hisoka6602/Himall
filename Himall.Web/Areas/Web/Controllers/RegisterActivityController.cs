using Himall.Application;
using Himall.Web.Framework;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class RegisterActivityController : BaseController
    {
      
        /// <summary>
        /// 注册有礼
        /// </summary>
        /// <returns></returns>
        public ActionResult Gift()
        {
            if (IsMobileTerminal)
            {
                Response.Redirect("/m-Wap/RegisterActivity/Gift");
            }
            var model = CouponApplication.GetCouponSendByRegister();
            ViewBag.Keyword = SiteSettings.Keyword;
            ViewBag.QRCode = SiteSettings.WXLogo;
            ViewBag.SiteName = SiteSettings.SiteName;
            return View(model);
        }
    }
}