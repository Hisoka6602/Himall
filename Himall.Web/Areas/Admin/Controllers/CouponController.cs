using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.CommonModel;
using Himall.Application;
using Himall.Entities;
using System;
using Himall.Core;
using Himall.Web.Models;
using System.IO;

namespace Himall.Web.Areas.Admin.Controllers
{
    [MarketingAuthorization]
    public class CouponController : BaseAdminController
    {
        MarketService _MarketService;
        private CouponService _CouponService;

        public CouponController(MarketService MarketService,CouponService CouponService)
        {
            _MarketService = MarketService;
            _CouponService = CouponService;
        }

        #region 活动列表
        public ActionResult Management()
        {
            return View();
        }
        #endregion


        #region 购买服务列表

        [UnAuthorize]
        public JsonResult List(MarketBoughtQuery query)
        {
            query.MarketType = MarketType.Coupon;
            var result = _MarketService.GetBoughtShopList(query);
            var list = result.Models.Select(item =>
            {
                var market = MarketApplication.GetMarketService(item.MarketServiceId);
                return new
                {
                    Id = item.Id,
                    StartDate = item.StartTime.ToString("yyyy-MM-dd"),
                    EndDate = item.EndTime.ToString("yyyy-MM-dd"),
                    ShopName = market.ShopName
                };
            }).ToList();
            return Json(new { rows = list, total = result.Total });
        }
        #endregion

        #region 服务费用设置

        public ActionResult ServiceSetting()
        {
            Entities.MarketSettingInfo model =  _MarketService.GetServiceSetting(MarketType.Coupon);
            return View(model);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult SaveServiceSetting(decimal Price)
        {
            Result result = new Result();
            var model = new Entities.MarketSettingInfo { Price = Price, TypeId = MarketType.Coupon };
             _MarketService.AddOrUpdateServiceSetting(model);
            result.success = true;
            result.msg = "保存成功！";
            return Json(result);
        }
        #endregion


        #region 添加修改优惠券
        public ActionResult Edit(long id)
        {
            var couponser = _CouponService;
            var model = couponser.GetCouponInfo(0, id);
            if (model == null)
                throw new HimallException("错误的优惠券编号。");
            if (model.IsSyncWeiXin == 1 && model.WXAuditStatus != (int)WXCardLogInfo.AuditStatusEnum.Audited)
                throw new HimallException("同步微信优惠券未审核通过时不可修改。");

            model.FormIsSyncWeiXin = model.IsSyncWeiXin == 1;

            var viewmodel = new CouponViewModel();
            var shops = couponser.GetCouponShopsByCouponId(id);

            viewmodel.Coupon = model;
            viewmodel.CouponShops = shops;
            viewmodel.Shops = ShopApplication.GetShopsByIds(shops.Select(p => p.ShopId));
           
            viewmodel.Settings = couponser.GetSettingsByCoupon(new System.Collections.Generic.List<long> { id });
            viewmodel.CanVshopIndex = true;
            viewmodel.EndTime = model.EndTime;
            viewmodel.CanAddIntegralCoupon = couponser.CanAddIntegralCoupon(0, id);
            return View(viewmodel);
        }
        public ActionResult Add()
        {
            CouponInfo model = new CouponInfo();
            long shopId = 0;
            var couponser = _CouponService;
            model = new CouponInfo();
            model.StartTime = DateTime.Now;
            model.EndTime = model.StartTime.AddMonths(1);
            model.ReceiveType = CouponInfo.CouponReceiveType.ShopIndex;

            model.CanVshopIndex = true;
            var settings = new System.Collections.Generic.List<CouponSettingInfo>();

            if (model.CanVshopIndex)
                settings.Add(new CouponSettingInfo() { Display = 1, PlatForm = PlatformType.Wap });
            settings.Add(new CouponSettingInfo() { Display = 1, PlatForm = PlatformType.PC });
            ViewBag.Settings = settings;
            model.FormIsSyncWeiXin = false;
            model.ShopId = shopId;
           
            ViewBag.EndTime = model.EndTime.ToString("yyyy-MM-dd");
            ViewBag.CanAddIntegralCoupon = couponser.CanAddIntegralCoupon(shopId);
            return View(model);
        }

        [HttpPost]
        public JsonResult Edit(CouponInfo info)
        {
            bool isAdd = false;
            if (info.Id == 0) isAdd = true;
            var couponser = _CouponService;
            var shopId = 0;
            info.ShopId = shopId;
            var shopName = "平台";
            info.ShopName = shopName;
            if (info.UseArea == 0)
            {
                info.CouponProductInfo = null;
                info.CouponShopInfo = null;
                info.Remark = "";
            }
            if (info.OrderAmount > 0 && info.OrderAmount<info.Price)
            {
                return Json(new Result() { success = false, msg = "面值必须小于或者等于达到订单金额满额才可以使用" });
            }
            if (isAdd)
            {
                info.CreateTime = DateTime.Now;
                if (info.StartTime >= info.EndTime)
                {
                    return Json(new Result() { success = false, msg = "开始时间必须小于结束时间" });
                }
                info.IsSyncWeiXin = 0;
                if (info.FormIsSyncWeiXin)
                {
                    info.IsSyncWeiXin = 1;

                    if (string.IsNullOrWhiteSpace(info.FormWXColor))
                    {
                        return Json(new Result() { success = false, msg = "错误的卡券颜色" });
                    }
                    if (string.IsNullOrWhiteSpace(info.FormWXCTit))
                    {
                        return Json(new Result() { success = false, msg = "请填写卡券标题" });
                    }

                    if (!WXCardLogInfo.WXCardColors.Contains(info.FormWXColor))
                    {
                        return Json(new Result() { success = false, msg = "错误的卡券颜色" });
                    }
                    //判断字符长度
                    var enc = System.Text.Encoding.Default;
                    if (enc.GetBytes(info.FormWXCTit).Count() > 18)
                    {
                        return Json(new Result() { success = false, msg = "卡券标题不得超过9个汉字" });
                    }
                    if (!string.IsNullOrWhiteSpace(info.FormWXCSubTit))
                    {
                        if (enc.GetBytes(info.FormWXCSubTit).Count() > 36)
                        {
                            return Json(new Result() { success = false, msg = "卡券副标题不得超过18个汉字" });
                        }
                    }
                }
            }
            if (info.CouponSettingInfo == null)
                info.CouponSettingInfo = new System.Collections.Generic.List<CouponSettingInfo>();

            #region 选择指定商家判断及初始值
            if (info.UseArea == 1)
            {
                if (!string.IsNullOrEmpty(info.CouponShopIds))
                {
                    var CouponShoplist = new System.Collections.Generic.List<CouponShopInfo>();
                    foreach (var item in info.CouponShopIds.Split(','))
                    {
                        long selshopid = 0;
                        long.TryParse(item, out selshopid);
                        if (selshopid > 0)
                        {
                            CouponShopInfo couponshop = new CouponShopInfo();
                            couponshop.ShopId = selshopid;
                            couponshop.CouponId = info.Id;
                            CouponShoplist.Add(couponshop);
                        }
                    }
                    info.CouponShopInfo = CouponShoplist;
                }
                if (info.CouponShopInfo == null || info.CouponShopInfo.Count <= 0)
                {
                    return Json(new Result() { success = false, msg = "请选择指定商家" });
                }
            }
            #endregion

            if (info.UseArea == 1 && string.IsNullOrEmpty(info.Remark))
            {
                return Json(new Result() { success = false, msg = "请输入指定商家的备注信息" });
            }
            var couponsetting = Request.Form["chkShow"];

            info.CanVshopIndex = true;//CurrentSellerManager.VShopId > 0;

            switch (info.ReceiveType)
            {
                case CouponInfo.CouponReceiveType.IntegralExchange:
                    //if (!couponser.CanAddIntegralCoupon(shopId, info.Id))
                    //{
                    //    return Json(new Result() { success = false, msg = "当前已有积分优惠券，只可以推广一张积分优惠券！" });
                    //}
                    info.CouponSettingInfo.Clear();
                    if (info.EndIntegralExchange == null)
                    {
                        return Json(new Result() { success = false, msg = "错误的兑换截止时间" });
                    }
                    if (info.EndIntegralExchange > info.EndTime.AddDays(1).Date)
                    {
                        return Json(new Result() { success = false, msg = "错误的兑换截止时间" });
                    }
                    if (info.NeedIntegral < 10)
                    {
                        return Json(new Result() { success = false, msg = "积分最少10分起兑" });
                    }
                    break;
                case CouponInfo.CouponReceiveType.DirectHair:
                    info.CouponSettingInfo.Clear();
                    break;
                default:
                    if (!string.IsNullOrEmpty(couponsetting))
                    {
                        info.CouponSettingInfo.Clear();
                        var t = couponsetting.Split(',');
                        if (t.Contains("WAP"))
                            info.CouponSettingInfo.Add(new CouponSettingInfo() { Display = 1, PlatForm = Himall.Core.PlatformType.Wap });
                        if (t.Contains("PC"))
                            info.CouponSettingInfo.Add(new CouponSettingInfo() { Display = 1, PlatForm = Himall.Core.PlatformType.PC });
                    }
                    else
                    {
                        return Json(new Result() { success = false, msg = "必须选择一个推广类型" });
                    }
                    break;
            }

            #region 转移图片
            string path = Server.MapPath(string.Format(@"/Storage/Shop/{0}/Coupon/{1}", shopId, info.Id));
            #endregion

            try
            {
                if (isAdd)
                {
                    couponser.AddCoupon(info);
                }
                else
                {
                    couponser.EditCoupon(info);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new Result { msg = ex.Message, success = false });
            }
        }
        #endregion
        /// <summary>
        /// 优惠券失效
        /// </summary>
        /// <param name="couponId"></param>
        /// <returns></returns>
        public JsonResult Cancel(long couponId)
        {
            var shopId = 0;
            _CouponService.CancelCoupon(couponId, shopId);
            return Json(new Result() { success = true, msg = "操作成功！" });
        }

        #region 优惠券列表

        public ActionResult CouponList()
        {
            return View();
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult GetItemList(int page, int rows, string couponName)
        {
            var service = _CouponService;
            var result = service.GetCouponList(new CouponQuery { CouponName = couponName, ShopId = 0, IsShowAll = true, PageSize = rows, PageNo = page });
            var list = result.Models.Select(
                item =>
                {
                    var records = service.GetRecordByCoupon(item.Id);
                    int Status = 0;
                    if (item.StartTime <= DateTime.Now && item.EndTime > DateTime.Now)
                        Status = 2;
                    else
                        if (item.StartTime > DateTime.Now)
                        Status = 1;
                    else
                        Status = 0;

                    return new
                    {
                        Id = item.Id,
                        StartTime = item.StartTime.ToString("yyyy/MM/dd"),
                        EndTime = item.EndTime.ToString("yyyy/MM/dd"),
                        Price = Math.Round(item.Price, 2),
                        CouponName = item.CouponName,
                        PerMax = item.PerMax == 0 ? "不限张" : item.PerMax.ToString() + "张/人",
                        OrderAmount = item.OrderAmount == 0 ? "不限制" : "满" + item.OrderAmount + "使用",
                        Num = item.Num,
                        ReceviceNum = records.Count(),
                        RecevicePeople = records.GroupBy(a => a.UserId).Count(),
                        Used = records.Count(a => a.CounponStatus == Entities.CouponRecordInfo.CounponStatuses.Used),
                        IsSyncWeiXin = item.IsSyncWeiXin,
                        WXAuditStatus = (item.IsSyncWeiXin != 1 ? (int)WXCardLogInfo.AuditStatusEnum.Audited : item.WXAuditStatus),
                        Status = Status,
                        CreateTime = item.CreateTime
                    };
                }
                ).OrderByDescending(r => r.Status).ThenByDescending(r => r.CreateTime);
            var model = new { rows = list, total = result.Total };
            return Json(model);
        }
        #endregion

        public ActionResult Receivers(long Id)
        {
            ViewBag.Id = Id;
            return View();
        }

        [HttpPost]
        public ActionResult GetReceivers(long Id, int page, int rows)
        {
            CouponRecordQuery query = new CouponRecordQuery();
            query.CouponId = Id;
            query.PageNo = page;
            query.PageSize = rows;
            var record = _CouponService.GetCouponRecordList(query);
            var coupons = CouponApplication.GetCouponInfo(record.Models.Select(p => p.CouponId));
            var list = record.Models.Select(item =>
            {
                var coupon = coupons.FirstOrDefault(p => p.Id == item.CouponId);
                return new
                {
                    Id = item.Id,
                    Price = Math.Round(coupon.Price, 2),
                    CreateTime = coupon.CreateTime.ToString("yyyy-MM-dd"),
                    CouponSN = item.CounponSN,
                    UsedTime = item.UsedTime.HasValue ? item.UsedTime.Value.ToString("yyyy-MM-dd") : "",
                    ReceviceTime = item.CounponTime.ToString("yyyy-MM-dd"),
                    Recever = item.UserName,
                    OrderId = item.OrderId,
                    Status = item.CounponStatus == Entities.CouponRecordInfo.CounponStatuses.Unuse ? (coupon.EndTime < DateTime.Now.Date ? "已过期" : item.CounponStatus.ToDescription()) : item.CounponStatus.ToDescription(),
                };
            });
            var model = new { rows = list, total = record.Total };
            return Json(model);
        }


        public ActionResult Detail(long Id)
        {
            var model = _CouponService.GetCouponInfo(0, Id);
            if (model != null)
            {
                if (model.IsSyncWeiXin == 1 && model.WXAuditStatus != (int)WXCardLogInfo.AuditStatusEnum.Audited)
                {
                    throw new HimallException("同步微信优惠券未审核通过时不可修改。");
                }
            }
            string host = CurrentUrlHelper.CurrentUrlNoPort();
            ViewBag.Url = String.Format("{0}/m-wap/vshop/CouponInfo/{1}", host, model.Id);
            var map = Core.Helper.QRCodeHelper.Create(ViewBag.Url);
            MemoryStream ms = new MemoryStream();
            map.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            //  将图片内存流转成base64,图片以DataURI形式显示  
            string strUrl = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray());
            ms.Dispose();
            //  显示  
            ViewBag.Image = strUrl;
            var market = _MarketService.GetMarketService(0, MarketType.Coupon);
            ViewBag.EndTime = MarketApplication.GetServiceEndTime(market.Id).ToString("yyyy-MM-dd");
            return View(model);
        }

    }
}