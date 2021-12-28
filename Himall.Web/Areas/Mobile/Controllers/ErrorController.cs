using Himall.Web.Framework;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class ErrorController : BaseMobileTemplatesController
    {
        // GET: Mobile/Error
        public ActionResult Error()
        {
            return View();
        }
    }
}