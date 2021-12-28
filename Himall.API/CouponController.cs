using AutoMapper;
using Himall.API.Model;
using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.CommonModel;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.API
{
    public class CouponController : BaseApiController
    {
        public object GetShopCouponList(long shopId)
        {
            var service = ServiceProvider.Instance<CouponService>.Create;
            var couponSetList = ServiceProvider.Instance<VShopService>.Create.GetVShopCouponSetting(shopId).Select(item => item.CouponID);
            
            //取设置的优惠券
            var coupons = service.GetCouponList(shopId)
                        .Where(item => couponSetList.Contains(item.Id));

            //取平台券
            var platCoupons = CouponApplication.GetPaltCouponList(shopId);
            coupons = coupons.Concat(platCoupons).ToList();

            if (coupons.Count()>0)
            {
               
                var vshop = ShopApplication.GetShop(shopId);
                var settings = service.GetSettingsByCoupon(coupons.Select(p => p.Id).ToList());
                var userCoupon = coupons.Where(d => settings.Any(c => c.CouponID==d.Id && c.PlatForm == Core.PlatformType.Wap)).Select(a => new
                {
                    ShopId = a.ShopId,
                    CouponId = a.Id,
                    Price = a.Price,
                    PerMax = a.PerMax,
                    OrderAmount = a.OrderAmount,
                    Num = a.Num,
                    StartTime = a.StartTime.ToString(),
                    EndTime = a.EndTime.ToString(),
                    CreateTime = a.CreateTime.ToString(),
                    CouponName = a.CouponName,
                    VShopLogo = Core.HimallIO.GetRomoteImagePath(vshop.Logo),
                    VShopId = vshop?.Id,
                    ShopName = a.ShopName,
                    Receive = Receive(a.ShopId, a.Id),
                    Remark = a.Remark,
                    UseArea = a.UseArea
                });
                var data = userCoupon.Where(p => p.Receive != 2 && p.Receive != 4);//优惠券已经过期、优惠券已领完，则不显示在店铺优惠券列表中
                dynamic result = SuccessResult();
                result.Coupon = data;
                return result;
            }
            else

                return ErrorResult("该店铺没有可领优惠券");
        }


        public object GetUserCounponList()
        {
            CheckUserLogin();
            var service = ServiceProvider.Instance<CouponService>.Create;
            var vshop = ServiceProvider.Instance<VShopService>.Create;
            var userCouponList = service.GetUserCouponList(CurrentUser.Id);
            var shopBonus = GetBonusList();
            if (userCouponList != null || shopBonus != null)
            {
                //优惠券
                var couponlist = new Object();
                if (userCouponList != null)
                {
                    couponlist = userCouponList.ToArray().Select(a => new
                    {
                        UserId = a.UserId,
                        ShopId = a.ShopId,
                        CouponId = a.CouponId,
                        Price = a.Price,
                        PerMax = a.PerMax,
                        OrderAmount = a.OrderAmount,
                        Num = a.Num,
                        StartTime = a.StartTime.ToString(),
                        EndTime = a.EndTime.ToString(),
                        CreateTime = a.CreateTime.ToString(),
                        CouponName = a.CouponName,
                        UseStatus = a.UseStatus,
                        UseTime = a.UseTime.HasValue ? a.UseTime.ToString() : null,
                        VShop = GetVShop(a.ShopId),
                        ShopName = a.ShopName,
                        Remark = a.Remark,
                        UseArea = a.UseArea
                    });
                }
                else
                    couponlist = null;
                //代金红包
                var userBonus = new List<dynamic>();
                if (shopBonus != null)
                {
                    userBonus = shopBonus.Select(item =>
                   {
                       var Price = item.Price;

                       var bonusService = ServiceProvider.Instance<ShopBonusService>.Create;
                       var grant = bonusService.GetGrant(item.BonusGrantId);
                       var bonus = bonusService.GetShopBonus(grant.ShopBonusId);
                       var shop = ShopApplication.GetShop(bonus.ShopId);
                       var vShop = VshopApplication.GetVShopByShopId(shop.Id);

                       var showOrderAmount = bonus.UsrStatePrice > 0 ? bonus.UsrStatePrice : item.Price;
                       if (bonus.UseState != ShopBonusInfo.UseStateType.FilledSend)
                           showOrderAmount = item.Price;

                       var Logo = string.Empty;
                       long VShopId = 0;
                       if (vShop != null)
                       {
                           Logo = Core.HimallIO.GetRomoteImagePath(vShop.StrLogo);
                           VShopId = vShop.Id;
                       }

                       var State = (int)item.State;
                       if (item.State != ShopBonusReceiveInfo.ReceiveState.Use && bonus.DateEnd < DateTime.Now)
                           State = (int)ShopBonusReceiveInfo.ReceiveState.Expired;
                       var BonusDateEnd = bonus.BonusDateEnd.ToString("yyyy-MM-dd");
                       dynamic obj = new System.Dynamic.ExpandoObject();
                       obj.Price = Price;
                       obj.ShowOrderAmount = showOrderAmount;
                       obj.Logo = Logo;
                       obj.VShopId = VShopId;
                       obj.State = State;
                       obj.DateStart = bonus.DateStart.ToString("yyyy-MM-dd");
                       obj.end = bonus.BonusDateStart.ToString("yyyy-MM-dd");
                       obj.BonusDateEnd = BonusDateEnd;
                       obj.DateEnd = bonus.DateEnd;
                       obj.ShopName = shop.ShopName;
                       return obj;
                   }).ToList();
                }
                else
                    shopBonus = null;
                //优惠券
                int NoUseCouponCount = 0;
                int UseCouponCount = 0;
                if (userCouponList != null)
                {
                    NoUseCouponCount = userCouponList.Count(item => (item.EndTime > DateTime.Now && item.UseStatus == CouponRecordInfo.CounponStatuses.Unuse));
                    UseCouponCount = userCouponList.Count() - NoUseCouponCount;
                }
                //红包
                int NoUseBonusCount = 0;
                int UseBonusCount = 0;
                if (shopBonus != null)
                {
                    NoUseBonusCount = userBonus.Count(r => r.State == (int)ShopBonusReceiveInfo.ReceiveState.NotUse && r.DateEnd > DateTime.Now);
                    UseBonusCount = userBonus.Count() - NoUseBonusCount;
                }

                int UseCount = UseCouponCount + UseBonusCount;
                int NotUseCount = NoUseCouponCount + NoUseBonusCount;

                var result = new
                {
                    success = true,
                    NoUseCount = NotUseCount,
                    UserCount = UseCount,
                    Coupon = couponlist,
                    Bonus = userBonus
                };
                return result;
            }
            else
            {
                throw new Himall.Core.HimallException("没有领取记录!");
            }
        }


        /// <summary>
        /// 领取优惠券
        /// </summary>
        /// <returns></returns>
        public object GetUserCoupon(long couponId)
        {
            CheckUserLogin();
            bool status = true;
            string message = "";
            //long vshopId = vspId;// value.vshopId; 店铺Id
            //long couponId = couponId;// value.couponId; 优惠劵Id
            var couponInfo = CouponApplication.GetCouponInfo(couponId);
            if (couponInfo.EndTime < DateTime.Now)
            {//已经失效
                status = false;
                message = "优惠券已经过期";
            }
            CouponRecordQuery crQuery = new CouponRecordQuery();
            crQuery.CouponId = couponId;
            crQuery.UserId = CurrentUser.Id;
            QueryPageModel<CouponRecordInfo> pageModel = CouponApplication.GetCouponRecordList(crQuery);
            if (couponInfo.PerMax != 0 && pageModel.Total >= couponInfo.PerMax)
            {//达到个人领取最大张数
                status = false;
                message = "达到领取最大张数";
            }
            crQuery = new CouponRecordQuery()
            {
                CouponId = couponId
            };
            pageModel = CouponApplication.GetCouponRecordList(crQuery);
            if (pageModel.Total >= couponInfo.Num)
            {//达到领取最大张数
                status = false;
                message = "此优惠券已经领完了";
            }
            if (couponInfo.ReceiveType == CouponInfo.CouponReceiveType.IntegralExchange)
            {
                var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUserId);
                if (userInte.AvailableIntegrals < couponInfo.NeedIntegral)
                {
                    //积分不足
                    status = false;
                    message = "积分不足 ";
                }
            }
            if (status)
            {
                CouponRecordInfo couponRecordInfo = new CouponRecordInfo()
                {
                    CouponId = couponId,
                    UserId = CurrentUser.Id,
                    UserName = CurrentUser.UserName,
                    ShopId = couponInfo.ShopId
                };
                CouponApplication.AddCouponRecord(couponRecordInfo);
                return JsonResult<int>(msg: "领取成功");//执行成功
            }
            else
            {
                return Json(ErrorResult<int>(message));
            }
        }
        public object GetCouponDetail(int couponId = 0)
        {
            if (couponId <= 0)
            {
                return ErrorResult("参数错误");
            }
            CouponInfo coupon = CouponApplication.GetCouponInfo(couponId);
            if (coupon == null)
            {
                return ErrorResult("错误的优惠券编号");
            }
            else
            {
                return new
                {
                    success = true,
                    CouponId = coupon.Id,
                    CouponName = coupon.CouponName,
                    Price = coupon.Price,
                    SendCount = coupon.Num,
                    UserLimitCount = coupon.PerMax,
                    OrderUseLimit = Math.Round(coupon.OrderAmount, 2),
                    StartTime = coupon.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    ClosingTime = coupon.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    CanUseProducts = "",
                    ObtainWay = coupon.ReceiveType,
                    NeedPoint = coupon.NeedIntegral,
                    UseWithGroup = false,
                    UseWithPanicBuying = false,
                    UseWithFireGroup = false,
                    Remark = coupon.Remark,
                    UseArea = coupon.UseArea
                };
            }
        }

        private object GetVShop(long shopId)
        {
            var vshop = ServiceProvider.Instance<VShopService>.Create.GetVShopByShopId(shopId);
            if (vshop == null)
                return ErrorResult("没有开通微店");
            else
            {
                var result = new
                {
                    success = true,
                    VShopId = vshop.Id,
                    VShopLogo = Core.HimallIO.GetRomoteImagePath(vshop.StrLogo)
                };
                return result;
            }

        }
        //领取优惠券
        public object PostAcceptCoupon(CouponAcceptCouponModel value)
        {
            CheckUserLogin();
            long vshopId = value.vshopId;
            long couponId = value.couponId;
            var couponService = ServiceProvider.Instance<CouponService>.Create;
            var couponInfo = couponService.GetCouponInfo(couponId);
            if (couponInfo.EndTime < DateTime.Now)
            {
                //已经失效
                return ErrorResult("优惠券已经过期.", 2);
            }
            CouponRecordQuery crQuery = new CouponRecordQuery();
            crQuery.CouponId = couponId;
            crQuery.UserId = CurrentUser.Id;
            QueryPageModel<CouponRecordInfo> pageModel = couponService.GetCouponRecordList(crQuery);
            if (couponInfo.PerMax != 0 && pageModel.Total >= couponInfo.PerMax)
            {
                //达到个人领取最大张数
                return ErrorResult("达到个人领取最大张数，不能再领取.", 3);
            }
            crQuery = new CouponRecordQuery()
            {
                CouponId = couponId
            };
            pageModel = couponService.GetCouponRecordList(crQuery);
            if (pageModel.Total >= couponInfo.Num)
            {
                //达到领取最大张数
                return ErrorResult("此优惠券已经领完了.", 4);
            }
            if (couponInfo.ReceiveType == CouponInfo.CouponReceiveType.IntegralExchange)
            {
                var integral = MemberIntegralApplication.GetAvailableIntegral(CurrentUserId);
                if (integral < couponInfo.NeedIntegral)
                    return ErrorResult("积分不足 " + couponInfo.NeedIntegral.ToString(), 5);
            }
            CouponRecordInfo couponRecordInfo = new CouponRecordInfo()
            {
                CouponId = couponId,
                UserId = CurrentUser.Id,
                UserName = CurrentUser.UserName,
                ShopId = couponInfo.ShopId
            };
            couponService.AddCouponRecord(couponRecordInfo);
            return SuccessResult("", 1);
        }


        /// <summary>
        /// 取积分优惠券
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        public object GetIntegralCoupon(int page = 1, int pagesize = 10)
        {
            var _CouponService = ServiceProvider.Instance<CouponService>.Create;
            VShopService _VShopService = ServiceProvider.Instance<VShopService>.Create;
            QueryPageModel<CouponInfo> coupons = _CouponService.GetIntegralCoupons(page, pagesize);
            Mapper.CreateMap<CouponInfo, CouponGetIntegralCouponModel>();
            QueryPageModel<CouponGetIntegralCouponModel> result = new QueryPageModel<CouponGetIntegralCouponModel>();
            result.Total = coupons.Total;
            if (result.Total > 0)
            {
                var datalist = coupons.Models.ToList();
                var objlist = new List<CouponGetIntegralCouponModel>();
                foreach (var item in datalist)
                {
                    var tmp = Mapper.Map<CouponGetIntegralCouponModel>(item);
                    tmp.ShowIntegralCover = Core.HimallIO.GetRomoteImagePath(item.IntegralCover);
                    var vshopobj = _VShopService.GetVShopByShopId(tmp.ShopId);
                    if (vshopobj != null)
                    {
                        tmp.VShopId = vshopobj.Id;
                        //优惠价封面为空时，取微店Logo，微店Logo为空时，取商城微信Logo
                        if (string.IsNullOrWhiteSpace(tmp.ShowIntegralCover))
                        {
                            if (!string.IsNullOrWhiteSpace(vshopobj.WXLogo))
                            {
                                tmp.ShowIntegralCover = Core.HimallIO.GetRomoteImagePath(vshopobj.WXLogo);
                            }
                        }
                    }
                    if (string.IsNullOrWhiteSpace(tmp.ShowIntegralCover))
                    {
                        var siteset = SiteSettingApplication.SiteSettings;
                        tmp.ShowIntegralCover = Core.HimallIO.GetRomoteImagePath(siteset.WXLogo);
                    }
                    objlist.Add(tmp);
                }
                result.Models = objlist.ToList();
            }
            dynamic _result = SuccessResult();
            _result.Models = result.Models;
            _result.total = result.Total;
            return _result;
        }
      

        private List<ShopBonusReceiveInfo> GetBonusList()
        {
            var service = ServiceProvider.Instance<ShopBonusService>.Create;
            return service.GetDetailByUserId(CurrentUser.Id);
        }
        /// <summary>
        /// 是否可领取优惠券
        /// </summary>
        /// <param name="vshopId"></param>
        /// <param name="couponId"></param>
        /// <returns></returns>
        private int Receive(long vshopId, long couponId)
        {
            if (CurrentUser != null && CurrentUser.Id > 0)//未登录不可领取
            {
                var couponService = ServiceProvider.Instance<CouponService>.Create;
                var couponInfo = couponService.GetCouponInfo(couponId);
                if (couponInfo.EndTime < DateTime.Now) return 2;//已经失效

                CouponRecordQuery crQuery = new CouponRecordQuery();
                crQuery.CouponId = couponId;
                crQuery.UserId = CurrentUser.Id;
                QueryPageModel<CouponRecordInfo> pageModel = couponService.GetCouponRecordList(crQuery);
                if (couponInfo.PerMax != 0 && pageModel.Total >= couponInfo.PerMax) return 3;//达到个人领取最大张数

                crQuery = new CouponRecordQuery()
                {
                    CouponId = couponId
                };
                pageModel = couponService.GetCouponRecordList(crQuery);
                if (pageModel.Total >= couponInfo.Num) return 4;//达到领取最大张数

                if (couponInfo.ReceiveType == CouponInfo.CouponReceiveType.IntegralExchange)
                {
                    var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUserId);
                    if (userInte.AvailableIntegrals < couponInfo.NeedIntegral) return 5;//积分不足
                }

                return 1;//可正常领取
            }
            return 0;
        }
    }
}
