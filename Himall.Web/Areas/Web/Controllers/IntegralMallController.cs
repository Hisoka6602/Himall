using Himall.Application;
using Himall.CommonModel;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class IntegralMallController : BaseWebController
    {
        private CouponService _CouponService;
        private GiftService _iGiftService;
        public IntegralMallController(CouponService CouponService, GiftService GiftService)
        {
            _CouponService = CouponService;
            _iGiftService = GiftService;
        }
        /// <summary>
        /// 积分商城
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            IntegralMallPageModel result = new IntegralMallPageModel();
            //Logo
            ViewBag.Logo = SiteSettings.Logo;

            //优惠券数据
            result.CouponPageSize = 6;
            QueryPageModel<Entities.CouponInfo> coupons = _CouponService.GetIntegralCoupons(1, result.CouponPageSize);
            result.CouponList = coupons.Models.ToList();
            result.CouponTotal = coupons.Total;
            result.CouponMaxPage = GetMaxPage(result.CouponTotal, result.CouponPageSize);

            //礼品数据
            result.GiftPageSize = 12;
            GiftQuery query = new GiftQuery();
            query.skey = "";
            query.status = GiftInfo.GiftSalesStatus.Normal;
            query.PageSize = result.GiftPageSize;
            query.PageNo = 1;
            QueryPageModel<GiftModel> gifts = _iGiftService.GetGifts(query);
            result.GiftList = gifts.Models.ToList();
            result.GiftTotal = gifts.Total;
            result.GiftMaxPage = GetMaxPage(result.GiftTotal, result.GiftPageSize);

            if (CurrentUser != null)
            {
                //登录后处理会员积分
                var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
                var userGrade = MemberGradeApplication.GetMemberGradeByUserIntegral(userInte.HistoryIntegrals);
                result.MemberAvailableIntegrals = userInte.AvailableIntegrals;
                result.MemberGradeName = userGrade.GradeName;
            }
            ViewBag.Keyword = SiteSettings.Keyword;
            
            return View(result);
        }

        public ActionResult Coupon(int page = 1)
        {
            if (CurrentUser == null)
            {
                string url = Request.RawUrl.ToString();
                string returnurl = System.Web.HttpUtility.HtmlEncode(url);
                return RedirectToAction("", "Login", new { area = "Web", returnUrl = returnurl });
            }
            int pagesize = 12;
            QueryPageModel<Entities.CouponInfo> coupons = _CouponService.GetIntegralCoupons(page, pagesize);
            List<Entities.CouponInfo> datalist = coupons.Models.ToList();

            #region 分页控制
            PagingInfo info = new PagingInfo
            {
                CurrentPage = page,
                ItemsPerPage = pagesize,
                TotalItems = coupons.Total
            };
            ViewBag.pageInfo = info;

            #endregion

            int MemberAvailableIntegrals = 0;
            var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
            if (userInte != null)
            {
                MemberAvailableIntegrals = userInte.AvailableIntegrals;
            }
            ViewBag.MemberAvailableIntegrals = MemberAvailableIntegrals;
            ViewBag.Keyword = SiteSettings.Keyword;
            return View(datalist);
        }
        /// <summary>
        /// 获取红包
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CouponList(int page, int rows = 6)
        {
            QueryPageModel<Entities.CouponInfo> coupons = _CouponService.GetIntegralCoupons(page, rows);
            List<Entities.CouponInfo> result = coupons.Models.ToList();
            return Json(result);
        }
        /// <summary>
        /// 获取礼品列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GiftList(int page, int rows = 12)
        {
            GiftQuery query = new GiftQuery();
            query.skey = "";
            query.status = GiftInfo.GiftSalesStatus.Normal;
            query.PageSize = rows;
            query.PageNo = page;
            QueryPageModel<GiftModel> gifts = _iGiftService.GetGifts(query);
            List<GiftModel> datalist = gifts.Models.ToList();
            var result = datalist.Select(d => new
            {
                Id = d.Id,
                GiftName = d.GiftName,
                NeedIntegral = d.NeedIntegral,
                LimtQuantity = d.LimtQuantity,
                StockQuantity = d.StockQuantity,
                EndDate = d.EndDate,
                NeedGrade = d.NeedGrade,
                SumSales = d.SumSales,
                SalesStatus = d.SalesStatus,
                ImagePath = d.ImagePath,
                GiftValue = d.GiftValue,
                ShowImagePath = Core.HimallIO.GetProductSizeImage(d.ShowImagePath, 1, (int)ImageSize.Size_350),
                NeedGradeName = d.NeedGradeName
            }).ToList();
            return Json(result);
        }
        /// <summary>
        /// 计算最大页数
        /// </summary>
        /// <param name="total"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        private int GetMaxPage(int total, int pagesize)
        {
            int result = 1;
            if (total > 0 && pagesize > 0)
            {
                result = (int)Math.Ceiling((double)total / (double)pagesize);
            }
            return result;
        }
    }
}