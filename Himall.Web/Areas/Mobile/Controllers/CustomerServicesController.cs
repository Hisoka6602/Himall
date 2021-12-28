using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Entities;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class CustomerServicesController : BaseMobileTemplatesController
    {
        // GET: Mobile/CustomerServices
        public ActionResult PlatCustomerServices()
        {
            var services = CustomerServiceApplication.GetPlatformCustomerService(true, true, true, CurrentUser);
            return View(services);
        }

        public ActionResult HiChat()
        {
            return View();
        }

        public ActionResult Sessions()
        {
            ViewBag.UserId = CurrentUser.Id;
            ViewBag.Nick = CurrentUser.Nick;
            ViewBag.Photo = Himall.Core.HimallIO.GetRomoteImagePath(CurrentUser.Photo);
            return View();
        }
        public ActionResult ShopCustomerServices(long shopId, long productId = 0)
        {
            var product = productId > 0 ? ProductManagerApplication.GetProduct(productId) : null;
            var customerServices = CustomerServiceApplication.GetMobileCustomerServiceAndMQ(shopId, true, CurrentUser, product);
            return View("PlatCustomerServices", customerServices);
        }
    }
}