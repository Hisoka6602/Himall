using Himall.Application;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;


namespace Himall.Web.Areas.Web.Controllers
{
    public class UserCouponController : BaseMemberController
    {
        private ShopBonusService _ShopBonusService;
        private CouponService _CouponService;
        public UserCouponController(CouponService CouponService, ShopBonusService ShopBonusService)
        {
            _CouponService = CouponService;
            _ShopBonusService = ShopBonusService;
        }
        public ActionResult Index(int? status, int pageSize = 10, int pageNo = 1)
        {
            if (!status.HasValue)
            {
                status = 0;
            }

            CouponRecordQuery query = new CouponRecordQuery();
            query.UserId = CurrentUser.Id;
            query.PageNo = pageNo;
            query.PageSize = pageSize;
            query.Status = status;
            var model = _CouponService.GetCouponRecordList(query);
            var coupons = _CouponService.GetCouponInfo(model.Models.Select(p => p.CouponId).ToArray());
            var shopBonus = ShopBonusApplication.GetShopBouns(query);


            #region 分页控制
            PagingInfo info = new PagingInfo
            {
                CurrentPage = pageNo,
                ItemsPerPage = pageSize,
                TotalItems = model.Total > shopBonus.Total ? model.Total : shopBonus.Total
            };
            ViewBag.pageInfo = info;
            ViewBag.Bonus = shopBonus.Models;
            ViewBag.State = query.Status;
            ViewBag.Coupons = coupons;
            #endregion
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View(model.Models.ToList());
        }
    }
}