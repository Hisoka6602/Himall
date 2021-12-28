using Himall.Service;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
namespace Himall.Web.Areas.Web.Controllers
{
    public class CustomerServicesController : BaseWebController
    {
        public ActionResult HiChat()
        {
            return View();
        }

        public ActionResult Sessions()
        {
            return View();
        }
    }
}