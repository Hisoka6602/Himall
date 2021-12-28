using Himall.Application;
using Himall.CommonModel;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class SideController : BaseWebController
    {
        private MemberIntegralService _iMemberIntegralService;
        private ProductService _ProductService;
        private ShopBonusService _ShopBonusService;
        private CouponService _CouponService;
        public SideController(MemberIntegralService MemberIntegralService,
            ProductService ProductService,
            ShopBonusService ShopBonusService,
            CouponService CouponService
            )
        {

            _iMemberIntegralService = MemberIntegralService;
            _ProductService = ProductService;
            _ShopBonusService = ShopBonusService;
            _CouponService = CouponService;
        }

        /// <summary>
        /// 侧边我的资产
        /// </summary>
        /// <returns></returns>
        public ActionResult MyAsset()
        {
            MyAssetViewModel result = new Models.MyAssetViewModel();
            result.MyCouponCount = 0;
            result.isLogin = CurrentUser != null;
            ViewBag.isLogin = result.isLogin ? "true" : "false";
            //用户积分
            result.MyMemberIntegral = result.isLogin ? MemberIntegralApplication.GetAvailableIntegral(CurrentUser.Id) : 0;
            //关注商品
            result.MyConcernsProducts = result.isLogin ? _ProductService.GetUserAllConcern(CurrentUser.Id,10) : new List<Entities.FavoriteInfo>();
            //优惠卷
            var coupons = result.isLogin ? _CouponService.GetAllUserCoupon(CurrentUser.Id).ToList() : new List<UserCouponInfo>();
            coupons = coupons == null ? new List<UserCouponInfo>() : coupons;
            result.MyCoupons = coupons;
            result.MyCouponCount += result.MyCoupons.Count();

            //红包
            result.MyShopBonus = ShopBonusApplication.GetShopBounsByUser(CurrentUser.Id); 
            result.MyCouponCount += result.MyShopBonus.Count();

            //浏览的商品
            var browsingPro = result.isLogin ? BrowseHistrory.GetBrowsingProducts(10, CurrentUser == null ? 0 : CurrentUser.Id) : new List<ProductBrowsedHistoryModel>();
            result.MyBrowsingProducts = browsingPro;
            ViewBag.Keyword = SiteSettings.Keyword;
            return View(result);
        }
    }
}