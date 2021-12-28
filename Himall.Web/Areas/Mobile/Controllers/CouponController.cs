using Himall.Application;
using Himall.Core.Extends;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class CouponController : BaseMobileMemberController
    {
        CouponService _CouponService;
        VShopService _VShopService;
        ShopService _ShopService;
        ShopBonusService _ShopBonusService;
        MemberService _MemberService;
        public CouponController(CouponService CouponService,
            VShopService VShopService,
            ShopService ShopService,
            ShopBonusService ShopBonusService,
            MemberService MemberService
            )
        {
            _CouponService = CouponService;
            _ShopService = ShopService;
            _VShopService = VShopService;
            _ShopBonusService = ShopBonusService;
            _MemberService = MemberService;

        }

        // GET: Mobile/Coupon
        public ActionResult Get()
        {
            return View();
        }

        public ActionResult Management()
        {
            var service = _CouponService;
            var vshop = _VShopService;
            var userCouponList = CouponApplication.GetUserCouponList(CurrentUser.Id);
            var shopBonus = ShopBonusApplication.GetShopBounsByUser(CurrentUser.Id);
            var couponlist = userCouponList.Select(a => new UserCouponInfo
            {

                UserId = a.UserId,
                ShopId = a.ShopId,
                CouponId = a.CouponId,
                Price = a.Price,
                PerMax = a.PerMax,
                OrderAmount = a.OrderAmount,
                Num = a.Num,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                CreateTime = a.CreateTime,
                CouponName = a.CouponName,
                UseStatus = a.UseStatus,
                UseTime = a.UseTime,
                VshopNames = string.Join(",", VshopApplication.GetVshopsByCouponInfo(a.ShopId, a.CouponId, a.UseArea).ToList().Select(v => v.Name)),
                VShop = VshopApplication.GetVShopByShopId(a.ShopId),
                Remark = a.Remark,
                UseArea = a.UseArea
            }); 

            int NoUseCount = couponlist.Count(item => (item.EndTime > DateTime.Now && item.UseStatus == Entities.CouponRecordInfo.CounponStatuses.Unuse));
            int bonusNoUseCount = shopBonus.Count(p => p.Receive.State == Entities.ShopBonusReceiveInfo.ReceiveState.NotUse && p.Bonus.BonusDateEnd > DateTime.Now);
            ViewBag.NoUseCount = NoUseCount + bonusNoUseCount;
            ViewBag.UserCount = (userCouponList.Count() + shopBonus.Count()) - ViewBag.NoUseCount;
            ViewBag.ShopBonus = shopBonus;
            return View(couponlist);

        }

        [HttpPost]
        public JsonResult AcceptCoupon(long vshopid, long couponid)
        {
            var couponService = _CouponService;
            var couponInfo = couponService.GetCouponInfo(couponid);
            if (couponInfo.EndTime < DateTime.Now)
            {//已经失效
                return ErrorResult("优惠券已经过期.", 2, true);
            }
            CouponRecordQuery crQuery = new CouponRecordQuery();
            crQuery.CouponId = couponid;
            crQuery.UserId = CurrentUser.Id;
            var pageModel = couponService.GetCouponRecordList(crQuery);
            if (couponInfo.PerMax != 0 && pageModel.Total >= couponInfo.PerMax)
            {//达到个人领取最大张数
                return Json(new { code = 3, success = false, msg = "达到个人领取最大张数，不能再领取." });
            }
            crQuery = new CouponRecordQuery()
            {
                CouponId = couponid
            };
            pageModel = couponService.GetCouponRecordList(crQuery);
            if (pageModel.Total >= couponInfo.Num)
            {//达到领取最大张数
                return Json(new { code = 4, success = false, msg = "此优惠券已经领完了." });
            }
            int MemberAvailableIntegrals = 0;
            if (couponInfo.ReceiveType == Entities.CouponInfo.CouponReceiveType.IntegralExchange)
            {
                var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
                if (userInte.AvailableIntegrals < couponInfo.NeedIntegral)
                {
                    //积分不足
                    return Json(new { code = 5, success = false, msg = "积分不足 " + couponInfo.NeedIntegral.ToString() });
                }
                MemberAvailableIntegrals = userInte.AvailableIntegrals;
            }
            Entities.CouponRecordInfo couponRecordInfo = new Entities.CouponRecordInfo()
            {
                CouponId = couponid,
                UserId = CurrentUser.Id,
                UserName = CurrentUser.UserName,
                ShopId = couponInfo.ShopId
            };
            couponService.AddCouponRecord(couponRecordInfo);

            return Json(new { code = 0, success = true, msg = "领取成功", crid = couponRecordInfo.Id, Integral_balance = MemberAvailableIntegrals });//执行成功
        }
        /// <summary>
        /// 获取店铺优惠券包括平台券
        /// </summary>
        /// <param name="shopid"></param>
        /// <returns></returns>
        private IEnumerable<Entities.CouponInfo> GetCouponList(long shopid)
        {
            var service = _CouponService;
            var result = service.GetCouponList(shopid);
            var couponSetList = _VShopService.GetVShopCouponSetting(shopid).Where(a => a.PlatForm == Core.PlatformType.Wap).Select(item => item.CouponID);
            if (result.Count() > 0 && couponSetList.Count() > 0)
            {
                var couponList = result.Where(item => couponSetList.Contains(item.Id));//取设置的优惠券
                var platCoupon = CouponApplication.GetPaltCouponList(shopid);

                return couponList.Concat(platCoupon).OrderByDescending(p => p.Price);
            }
            else
            {
                return CouponApplication.GetPaltCouponList(shopid);
            }
        }

        public ActionResult ShopCouponList(long shopid)
        {
            var coupons = GetCouponList(shopid).OrderByDescending(c => c.Price);
            var vshop = _VShopService.GetVShopByShopId(shopid);
            if (coupons != null)
            {
                ViewBag.CouponList = coupons.ToArray().Select(a => new UserCouponInfo
                {
                    ShopId = a.ShopId,
                    CouponId = a.Id,
                    Price = a.Price,
                    PerMax = a.PerMax,
                    OrderAmount = a.OrderAmount,
                    Num = a.Num,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    CreateTime = a.CreateTime,
                    CouponName = a.CouponName,
                    VShop = vshop,
                    ReceiveStatus = Receive(a.Id),
                    Remark = a.Remark,
                    UseArea = a.UseArea
                }).Where(p => p.ReceiveStatus != 2 && p.ReceiveStatus != 4);//优惠券已经过期、优惠券已领完，则不显示在店铺优惠券列表中
            }
            ViewBag.Shopid = shopid;
            ViewBag.VShopid = vshop != null ? vshop.Id : 0;
            var isFav = _ShopService.IsFavoriteShop(CurrentUser.Id, shopid);
            string favText;
            if (isFav)
            {
                favText = "已收藏";
            }
            else
            {
                favText = "收藏店铺";
            }
            ViewBag.FavText = favText;

            //var wxinfo = _VShopService.GetVShopSetting(shopid) ?? new Entities.WXshopInfo() { FollowUrl = string.Empty };
            //ViewBag.FollowUrl = wxinfo.FollowUrl;
            return View();
        }
        private int Receive(long couponId)
        {
            if (CurrentUser != null && CurrentUser.Id > 0)//未登录不可领取
            {
                var couponService = _CouponService;
                var couponInfo = couponService.GetCouponInfo(couponId);
                if (couponInfo.EndTime < DateTime.Now) return 2;//已经失效

                CouponRecordQuery crQuery = new CouponRecordQuery();
                crQuery.CouponId = couponId;
                crQuery.UserId = CurrentUser.Id;
                var pageModel = couponService.GetCouponRecordList(crQuery);
                if (couponInfo.PerMax != 0 && pageModel.Total >= couponInfo.PerMax) return 3;//达到个人领取最大张数

                crQuery = new CouponRecordQuery()
                {
                    CouponId = couponId
                };
                pageModel = couponService.GetCouponRecordList(crQuery);
                if (pageModel.Total >= couponInfo.Num) return 4;//达到领取最大张数

                if (couponInfo.ReceiveType == Entities.CouponInfo.CouponReceiveType.IntegralExchange)
                {
                    var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
                    if (userInte.AvailableIntegrals < couponInfo.NeedIntegral) return 5;//积分不足
                }

                return 1;//可正常领取
            }
            return 0;
        }
    }
}