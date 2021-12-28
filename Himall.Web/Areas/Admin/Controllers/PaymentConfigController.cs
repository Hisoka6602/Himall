using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class PaymentConfigController : BaseAdminController
    {
        private RegionService _RegionService;
        private PaymentConfigService _iPaymentConfigService;

        public PaymentConfigController(RegionService RegionService, PaymentConfigService PaymentConfigService)
        {
            _RegionService = RegionService;
            _iPaymentConfigService = PaymentConfigService;
        } 

        public ActionResult Index()
        {
            var p = _RegionService.GetAllRegions().Where(a => a.Level == CommonModel.Region.RegionLevel.Province && a.Sub != null).ToList();
            ViewBag.Address = _iPaymentConfigService.GetAddressId();
            ViewBag.AddressCity = _iPaymentConfigService.GetAddressIdCity();
            return View(p);
        }
        [HttpPost]
        public ActionResult GetRegion(long id)
        {
            return Json(_RegionService.GetRegion(id));
        }

        [HttpPost]
        public ActionResult Save(string addressIds, string addressIds_city)
        {
            _iPaymentConfigService.Save(addressIds, addressIds_city);
            return Json(new Result() { success = true, msg = "保存成功！" });
        }
    }
}