using Himall.Web.Framework;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class ErrorController : BaseController
    {
        // GET: Web/Common
        public ActionResult Error404()
        {
            Response.StatusCode = 404;
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }

        public ActionResult DefaultError()
        {
            Response.StatusCode = 500;
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }
    }
}