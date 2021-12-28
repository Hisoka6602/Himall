using Himall.Core.Helper;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class DebugController : BaseMobileTemplatesController
    {
        private MemberService _MemberService;
        public DebugController(MemberService MemberService)
        {
            _MemberService = MemberService;
        }
        // GET: Mobile/Debug
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Logout()
        {
            var cacheKey = WebHelper.GetCookie(CookieKeysCollection.HIMALL_USER);
            if (!string.IsNullOrWhiteSpace(cacheKey))
            {

                //_MemberService.DeleteMemberOpenId(userid, string.Empty);
                WebHelper.DeleteCookie(CookieKeysCollection.HIMALL_USER);

                WebHelper.DeleteCookie(CookieKeysCollection.SELLER_MANAGER);
                //记录主动退出符号
                WebHelper.SetCookie(CookieKeysCollection.HIMALL_ACTIVELOGOUT, "1", DateTime.MaxValue);

                ClearDistributionSpreadCookie();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}