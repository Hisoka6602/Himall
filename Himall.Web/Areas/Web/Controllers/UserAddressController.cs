using Himall.Service;
using Himall.Web.Framework;
using System.Web.Mvc;
using Himall.Application;
using Himall.CommonModel;

namespace Himall.Web.Areas.Web.Controllers
{
    public class UserAddressController : BaseMemberController
    {
        private ShippingAddressService _ShippingAddressService;
        public UserAddressController(ShippingAddressService ShippingAddressService)
        {
            _ShippingAddressService = ShippingAddressService;
        }
        // GET: Web/UserAddress
        public ActionResult Index()
        {
            var userId = CurrentUser.Id;
            var m = _ShippingAddressService.GetByUser(userId);
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            ViewBag.QQMapKey = SiteSettingApplication.SiteSettings.QQMapAPIKey;
            #region 是否开启门店授权
            ViewBag.IsOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            #endregion
            return View(m);
        }

        [HttpPost]
        public JsonResult AddShippingAddress(Entities.ShippingAddressInfo info)
        {
            info.UserId = CurrentUser.Id;
            _ShippingAddressService.Create(info);
            return Json(new { success = true, msg = "添加成功", id = info.Id });
        }

        [HttpPost]
        public JsonResult DeleteShippingAddress(long id)
        {
            var userId = CurrentUser.Id;
            _ShippingAddressService.Remove(id, userId);
            return Json(new Result() { success = true, msg = "删除成功" });
        }

        [HttpPost]
        public JsonResult EditShippingAddress(Entities.ShippingAddressInfo info)
        {
            info.UserId = CurrentUser.Id;
            _ShippingAddressService.Save(info);
            return Json(new { success = true, msg = "修改成功", id = info.Id });
        }

        [HttpPost]
        public JsonResult SetQuickShippingAddress(long id)
        {
            var userId = CurrentUser.Id;
            _ShippingAddressService.SetQuick(id, userId);
            return Json(new Result() { success = true, msg = "设置成功" });
        }

        [HttpPost]
        public JsonResult SetDefaultShippingAddress(long id)
        {
            var userId = CurrentUser.Id;
            _ShippingAddressService.SetDefault(id, userId);
            return Json(new Result() { success = true, msg = "设置成功" });
        }

        [HttpPost]
        public JsonResult GetShippingAddress(long id)
        {
            var address = _ShippingAddressService.Get(id);
            var json = new
            {

                id = address.Id,
                fullRegionName = address.RegionFullName,
                address = address.Address,
                addressDetail = address.AddressDetail,
                phone = address.Phone,
                shipTo = address.ShipTo,
                fullRegionIdPath = address.RegionIdPath

            };
            return Json(json);
        }

        [HttpGet]
        public ActionResult InitRegion(string fromLatLng)
        {
            string address = string.Empty, province = string.Empty, city = string.Empty, district = string.Empty, street = string.Empty, newStreet = string.Empty;
            ShopbranchHelper.GetAddressByLatLng(fromLatLng, ref address, ref province, ref city, ref district, ref street);
            if (district == "" && street != "")
            {
                district = street;
                street = "";
            }
            string fullPath = Himall.Application.RegionApplication.GetAddress_Components(city, district, street, out newStreet);
            return Json(new { fullPath = fullPath, showCity = string.Format("{0} {1} {2}", province, city, district), street = street }, JsonRequestBehavior.AllowGet);
        }
    }
}