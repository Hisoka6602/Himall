using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins.OAuth;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class HomeController : BaseWebController
    {
        private MemberService _MemberService;
        private SlideAdsService _iSlideAdsService;
        private FloorService _iFloorService;
        private ArticleCategoryService _iArticleCategoryService;
        private ArticleService _iArticleService;
        private BrandService _iBrandService;
        private LimitTimeBuyService _LimitTimeBuyService;
        private ShopBonusService _ShopBonusService;
        const string _themesettings = "~/Areas/Admin/Views/PageSettings/themesetting.json";
        const string _templatesettings = "~/Areas/Admin/Views/PageSettings/templatesettings.json";

        public HomeController(
            MemberService MemberService,
            SlideAdsService SlideAdsService,
            FloorService FloorService,
            ArticleCategoryService ArticleCategoryService,
            ArticleService ArticleService,
            BrandService BrandService,
            LimitTimeBuyService LimitTimeBuyService,
            ShopBonusService ShopBonusService)
        {
            _MemberService = MemberService;
            _iSlideAdsService = SlideAdsService;
            _iFloorService = FloorService;
            _iArticleCategoryService = ArticleCategoryService;
            _iArticleService = ArticleService;
            _iBrandService = BrandService;
            _LimitTimeBuyService = LimitTimeBuyService;
            _ShopBonusService = ShopBonusService;
        }
        private bool IsInstalled()
        {
            var t = ConfigurationManager.AppSettings["IsInstalled"];
            return null == t || bool.Parse(t);
        }

        //#if !DEBUG
        //               [OutputCache(Duration = ConstValues.PAGE_CACHE_DURATION)]
        //#endif
        //[OutputCache(Duration = ConstValues.PAGE_CACHE_DURATION)]
        [HttpGet]
        public ActionResult Index()
        {
            if (!IsInstalled())
            {
                return RedirectToAction("Agreement", "Installer");
            }
            return File(Server.MapPath("~/Areas/Web/Views/Home/index1.html"), "text/html");
        }

        public ActionResult Index1()
        {
            return File(Server.MapPath("~/Areas/Web/Views/Home/index1.html"), "text/html");
        }

        public ActionResult Test()
        {
            return View();
        }

        /// <summary>n
        /// 用于响应SLB，直接返回
        /// </summary>
        /// <returns></returns>
        [HttpHead]
        public ContentResult Index(string s)
        {
            return Content("");
        }


        IEnumerable<string> GetOAuthValidateContents()
        {
            var oauthPlugins = Core.PluginsManagement.GetPlugins<IOAuthPlugin>(true);
            return oauthPlugins.Select(item => item.Biz.GetValidateContent());
        }

        [HttpPost]
        public JsonResult GetProducts(long[] ids)
        {
            var products = ProductManagerApplication.GetProductByIds(ids).ToList().Select(item => new
            {
                item.Id,
                item.ProductName,
                item.MarketPrice,
                item.MinSalePrice,
                item.SaleStatus,
                ImagePath = HimallIO.GetProductSizeImage(item.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_350)
            });

            return Json(products, true);
        }

        /// <summary>
        /// 获取限时购商品
        /// </summary>
        /// <param name="ids">商品ID集合</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetLimitedProducts(List<long> ids)
        {
            var result = LimitTimeApplication.GetPriceByProducrIds(ids).ToList();
            var productIds = result.Select(p => p.ProductId);
            var skuInfos = ProductManagerApplication.GetSKUByProducts(productIds);//商品规格
            var flashSaleIds = result.Select(p => p.Id);
            var flashSaleDetails = LimitTimeApplication.GetFlashSaleDetailByFlashSaleIds(flashSaleIds).ToList();//限时购明细
            var products = result.Select(item => new
            {
                ProductName = item.ProductName,
                MinPrice = item.MinPrice,
                ProductId = item.ProductId,
                Count = (skuInfos == null || flashSaleDetails == null) ? 0 :
                (Math.Min(skuInfos.Where(a => a.ProductId == item.ProductId).Sum(b => b.Stock)
                , flashSaleDetails.Where(t => t.ProductId == item.ProductId).Sum(t => t.TotalCount)))//取活动=限时购活动库存和规格库存最少的一个
            });

            return Json(products, true);
        }

        /// <summary>
        /// 首页动态获取我的积分、优惠券、已领取的优惠券
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult Coupon()
        {
            int memberIntegral = 0; List<Coupon> baseCoupons = new List<Coupon>();
            long shopId = CurrentSellerManager != null ? CurrentSellerManager.ShopId : 0;
            if (CurrentUser != null)
            {
                memberIntegral = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id).AvailableIntegrals;

                //优惠卷
                var coupons = CouponApplication.GetAllUserCoupon(CurrentUser.Id).ToList();
                coupons = coupons == null ? new List<UserCouponInfo>() : coupons;
                if (coupons != null)
                {
                    baseCoupons.AddRange(coupons.Select(p => new Coupon()
                    {
                        BasePrice = p.Price,
                        BaseShopId = p.ShopId,
                        BaseShopName = p.ShopName,
                        BaseType = p.BaseType,
                        OrderAmount = p.OrderAmount
                    }));
                }

                //红包
                var shopBonus = ShopBonusApplication.GetCanUseDetailByUserId(CurrentUser.Id);
                shopBonus = shopBonus == null ? new List<ShopBonusReceiveInfo>() : shopBonus;
                if (shopBonus != null)
                {
                    baseCoupons.AddRange(shopBonus.Select(p => {
                        var grant = _ShopBonusService.GetGrant(p.BonusGrantId);
                        var bonus = _ShopBonusService.GetShopBonus(grant.ShopBonusId);
                        var shop = ShopApplication.GetShop(bonus.ShopId);

                        return new Coupon()
                        {
                            BasePrice = p.Price,
                            BaseShopId = shop.Id,
                            BaseShopName = shop.ShopName,
                            BaseType = p.BaseType,
                            UseState = bonus.UseState,
                            UsrStatePrice = bonus.UsrStatePrice
                        };
                    }));
                }
            }
            return Json(new
            {
                memberIntegral = memberIntegral,
                baseCoupons = baseCoupons,
                shopId = shopId
            }, true);
        }

        // GET: Web/Home
        public ActionResult Index2()
        {
            BranchShopDayFeatsQuery query = new BranchShopDayFeatsQuery();
            query.StartDate = DateTime.Now.Date.AddDays(-10);
            query.EndDate = DateTime.Now.Date;
            query.ShopId = 288;
            query.BranchShopId = 21;
            var model = Himall.Application.OrderAndSaleStatisticsApplication.GetDayAmountSale(query);
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }

        [HttpGet]
        public JsonResult GetFoot()
        {
            var articleCategoryService = _iArticleCategoryService;
            var articleService = _iArticleService;
            //服务文章
            var pageFootServiceCategory = articleCategoryService.GetSpecialArticleCategory(SpecialCategory.PageFootService);
            if (pageFootServiceCategory == null)
            {
                return Json(new List<PageFootServiceModel>(), JsonRequestBehavior.AllowGet);
            }
            var pageFootServiceSubCategies = articleCategoryService.GetArticleCategoriesByParentId(pageFootServiceCategory.Id);
            var pageFootService = pageFootServiceSubCategies.ToArray().Select(item =>
                 new PageFootServiceModel()
                 {
                     CateogryName = item.Name,
                     Articles = articleService.GetArticleByArticleCategoryId(item.Id).Where(t => t.IsRelease)
                 }
                );
            var PageFootService = pageFootService;
            return Json(PageFootService, JsonRequestBehavior.AllowGet);
        }

        public ActionResult TestLogin()
        {
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }


        /// <summary>
        /// 获取主题配色json
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetThemeSettingJson()
        {
            string currentTempdate = System.IO.File.ReadAllText(this.Server.MapPath(_templatesettings));//读取当前应用的模板
            TemplateSetting curTemplateObj = ParseFormJson<TemplateSetting>(currentTempdate);
            if (curTemplateObj != null)
            {
                if (System.IO.File.Exists(this.Server.MapPath(_themesettings)))
                {
                    string currentTheme = System.IO.File.ReadAllText(this.Server.MapPath(_themesettings));//读取当前模板应用的主题配色
                    List<ThemeSetting> curThemeObjs = ParseFormJson<List<ThemeSetting>>(currentTheme);
                    if (curThemeObjs != null && curThemeObjs.Count > 0)
                    {
                        var info = curThemeObjs.FirstOrDefault(a => a.templateId == curTemplateObj.Id);
                        if (null != info)
                        {
                            return Json(info, true);
                        }
                    }
                }
            }
            return Json(null, true);
        }


        [HttpPost]
        public JsonResult GetFootNew()
        {
            //页脚
            var articleCategoryService = _iArticleCategoryService;
            var articleService = _iArticleService;
            //服务文章
            var pageFootServiceCategory = articleCategoryService.GetSpecialArticleCategory(SpecialCategory.PageFootService);
            if (pageFootServiceCategory == null) { return Json(null); }
            var pageFootServiceSubCategies = articleCategoryService.GetArticleCategoriesByParentId(pageFootServiceCategory.Id).ToList();
            dynamic noticeInfo = new System.Dynamic.ExpandoObject();
            var allArticle = articleService.GetArticleByArticleCategoryIds(pageFootServiceSubCategies.Select(a => a.Id).ToList()).Where(p => p.IsRelease).ToList();
            FootNoticeModel info = null;
            List<FootNoticeModel> resultList = new List<FootNoticeModel>();
            pageFootServiceSubCategies.ForEach(p =>
            {
                info = new FootNoticeModel()
                {
                    CateogryName = p.Name,
                    List = allArticle.Where(x => x.CategoryId == p.Id).Select(y => new ArticleInfo
                    {
                        Id = y.Id,
                        Title = y.Title
                    }).ToList()
                };
                resultList.Add(info);
            });
            noticeInfo.PageFootService = resultList;
            //页脚
            noticeInfo.PageFoot = SiteSettings.PageFoot;
            noticeInfo.QRCode = Himall.Core.HimallIO.GetImagePath(SiteSettings.QRCode);
            noticeInfo.SiteName = SiteSettings.SiteName;
            noticeInfo.Logo = SiteSettings.Logo;
            noticeInfo.PCBottomPic = SiteSettings.PCBottomPic;
            noticeInfo.Site_SEOTitle = SiteSettings.Site_SEOTitle;
            noticeInfo.APPCanDownload = SiteSettings.CanDownload;
            noticeInfo.IsOpenH5 = SiteSettings.IsOpenH5;
            //noticeInfo.IsOSS = Core.HimallIO.GetHimallIO().GetType().FullName.Equals("Himall.Strategy.OSS");//是否开通了OSS
            if (SiteSettings.CanDownload)
            {
                string host = CurrentUrlHelper.CurrentUrlNoPort();
                var link = String.Format("{0}/m-wap/home/downloadApp", host);
                var map = Core.Helper.QRCodeHelper.Create(link);
                MemoryStream ms = new MemoryStream();
                map.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                //  将图片内存流转成base64,图片以DataURI形式显示  
                string strUrl = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray());
                ms.Dispose();
                noticeInfo.appqr = strUrl;
            }
            return Json(noticeInfo, true);
        }


        [HttpPost]
        public JsonResult GetNotice()
        {
            var specialArticleInfo = ObjectContainer.Current.Resolve<ArticleCategoryService>().GetSpecialArticleCategory(SpecialCategory.InfoCenter);
            if (specialArticleInfo != null)
            {
                var result = ObjectContainer.Current.Resolve<ArticleService>().GetTopNArticle<ArticleInfo>(7, specialArticleInfo.Id);
                var notice = result.Select(p => new
                {
                    url = "/Article/Details/" + p.Id,
                    title = p.Title,
                    cid = p.CategoryId
                });
                return Json(notice, true);
            }
            return Json(null);
        }

        public static T ParseFormJson<T>(string szJson)
        {
            T obj = Activator.CreateInstance<T>();
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(szJson)))
            {
                DataContractJsonSerializer dcj = new DataContractJsonSerializer(typeof(T));
                return (T)dcj.ReadObject(ms);
            }
        }


        [HttpPost]
        public JsonResult GetBrands()
        {
            var result = BrandApplication.GetBrands("", 1, int.MaxValue).Models;
            var brands = result.Select(item => new
            {
                BrandName = item.Name,
                BrandLogo = Core.HimallIO.GetImagePath(item.Logo),
                Id = item.Id
            });
            return Json(brands, true);
        }

        [HttpPost]
        /// <summary>
        /// 获取平台客服信息
        /// </summary>
        /// <returns></returns>
        public JsonResult GetPlatformCustomerService()
        {
            var result = CustomerServiceApplication.GetPlatformCustomerService(true, false, true, CurrentUser).OrderByDescending(t => t.Tool);
            return Json(result, true);
        }

        [HttpPost]
        public JsonResult GetUserInfo() {
            dynamic result =new { photo="", nick="",success=false };
            if (CurrentUser!=null) {
                result.photo = CurrentUser.PhotoUrl;
                result.nick = CurrentUser.ShowNick;
                result.success = true;
            }
            return Json(result, true);
        }
    }
}