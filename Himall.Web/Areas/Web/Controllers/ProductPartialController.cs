using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class ProductPartialController : BaseWebController
    {
        private MemberIntegralService _iMemberIntegralService;
        private ProductService _ProductService;
        private CouponService _CouponService;
        private ShopBonusService _ShopBonusService;
        private ArticleService _iArticleService;
        private ArticleCategoryService _iArticleCategoryService;
        private NavigationService _iNavigationService;
        private HomeCategoryService _iHomeCategoryService;
        private BrandService _iBrandService;
        private CategoryService _iCategoryService;
        private CustomerCustomerService _CustomerCustomerService;
        private SlideAdsService _iSlideAdsService;
        const string _templateHeadHtmlFileFullName = "~/Areas/Web/Views/Shared/head.html";
        const string _templateFootHtmlFileFullName = "~/Areas/Web/Views/Shared/foot.html";
        const string _themesettings = "~/Areas/Admin/Views/PageSettings/themesetting.json";
        const string _templatesettings = "~/Areas/Admin/Views/PageSettings/templatesettings.json";

        public ProductPartialController(
            MemberIntegralService MemberIntegralService,
            ProductService ProductService,
            CouponService CouponService,
            ShopBonusService ShopBonusService,
            ArticleService ArticleService,
            ArticleCategoryService ArticleCategoryService,
            NavigationService NavigationService,
            HomeCategoryService HomeCategoryService,
            BrandService BrandService,
            CategoryService CategoryService,
            CustomerCustomerService CustomerCustomerService,
            SlideAdsService SlideAdsService

            )
        {
            _iMemberIntegralService = MemberIntegralService;
            _ProductService = ProductService;
            _CouponService = CouponService;
            _ShopBonusService = ShopBonusService;
            _iArticleService = ArticleService;
            _iArticleCategoryService = ArticleCategoryService;
            _iNavigationService = NavigationService;
            _iHomeCategoryService = HomeCategoryService;
            _iBrandService = BrandService;
            _iCategoryService = CategoryService;
            _CustomerCustomerService = CustomerCustomerService;
            _iSlideAdsService = SlideAdsService;

        }
        /// <summary>
        /// 页面缓存时间
        /// </summary>


        // GET: Web/ProductPartial
        //[OutputCache(Duration = ConstValues.PAGE_CACHE_DURATION)]
        public ActionResult Header()
        {
            ViewBag.Now = DateTime.Now;
            bool isLogin = CurrentUser != null;
            var model = new ProductPartialHeaderModel();
            var customerlist = CustomerServiceApplication.GetPlatformCustomerService(true, false,true,CurrentUser);
            model.PlatformCustomerServices = customerlist;
            model.isLogin = isLogin ? "true" : "false";
            //用户积分
            model.MemberIntegral = isLogin ? MemberIntegralApplication.GetAvailableIntegral(CurrentUser.Id) : 0;

            //关注商品
            var user = CurrentUser?.Id ?? 0;
            var baseCoupons = new List<DisplayCoupon>();
            //优惠卷
            var usercoupons = _CouponService.GetAllUserCoupon(user);//优惠卷
            var coupons = CouponApplication.GetCouponInfo(usercoupons.Select(p => p.CouponId));
            var shops = ShopApplication.GetShops(coupons.Select(p => p.ShopId));
            var shopBonus = ShopBonusApplication.GetShopBounsByUser(user);//红包
            baseCoupons.AddRange(usercoupons.Select(item =>
            {
                var coupon = coupons.FirstOrDefault(p => p.Id == item.CouponId);
                var shop = shops.FirstOrDefault(p => p.Id == coupon.ShopId);
                return new DisplayCoupon
                {
                    Type = CommonModel.CouponType.Coupon,
                    Limit = coupon.OrderAmount,
                    Price = item.Price,
                    ShopId = shop != null ? shop.Id : 0,
                    ShopName = shop != null ? shop.ShopName : "平台",
                    EndTime = coupon.EndTime,
                };
            }));

            baseCoupons.AddRange(shopBonus.Select(p => new DisplayCoupon
            {
                Type = CommonModel.CouponType.ShopBonus,
                EndTime = p.Bonus.DateEnd,
                Limit = p.Bonus.UsrStatePrice,
                Price = p.Receive.Price,
                ShopId = p.Shop.Id,
                ShopName = p.Shop.ShopName,
                UseState = p.Bonus.UseState
            }));
            model.BaseCoupon = baseCoupons;
            //广告
            var imageAds = _iSlideAdsService.GetImageAds(0).Where(p => p.TypeId == Himall.CommonModel.ImageAdsType.HeadRightAds).ToList();
            if (imageAds.Count > 0)
            {
                ViewBag.HeadAds = imageAds;
            }
            else
            {
                ViewBag.HeadAds = _iSlideAdsService.GetImageAds(0).Take(1).ToList();
            }
            //浏览的商品
            //var browsingPro = isLogin ? BrowseHistrory.GetBrowsingProducts(10, CurrentUser == null ? 0 : CurrentUser.Id) : new List<ProductBrowsedHistoryModel>();
            //model.BrowsingProducts = browsingPro;

            InitHeaderData();

            string html = System.IO.File.ReadAllText(this.Server.MapPath(_templateHeadHtmlFileFullName));//读取模板头部html静态文件内容
            ViewBag.Html = html;
            return PartialView("~/Areas/Web/Views/Shared/Header.cshtml", model);
        }



        [OutputCache(VaryByParam = "id", Duration = ConstValues.PAGE_CACHE_DURATION)]

        public ActionResult ShopHeader(long id)
        {
            InitHeaderData();
            #region 获取店铺的评价统计
            var statistic = ShopApplication.GetStatisticOrderComment(id);
            ViewBag.ProductAndDescription = statistic.ProductAndDescription;
            ViewBag.ProductAndDescriptionPeer = statistic.ProductAndDescriptionPeer;
            ViewBag.ProductAndDescriptionMin = statistic.ProductAndDescriptionMin;
            ViewBag.ProductAndDescriptionMax = statistic.ProductAndDescriptionMax;

            ViewBag.SellerServiceAttitude = statistic.SellerServiceAttitude;
            ViewBag.SellerServiceAttitudePeer = statistic.SellerServiceAttitudePeer;
            ViewBag.SellerServiceAttitudeMax = statistic.SellerServiceAttitudeMax;
            ViewBag.SellerServiceAttitudeMin = statistic.SellerServiceAttitudeMin;

            ViewBag.SellerDeliverySpeed = statistic.SellerDeliverySpeed;
            ViewBag.SellerDeliverySpeedPeer = statistic.SellerDeliverySpeedPeer;
            ViewBag.SellerDeliverySpeedMax = statistic.SellerDeliverySpeedMax;
            ViewBag.sellerDeliverySpeedMin = statistic.SellerDeliverySpeedMin;
            #endregion

            #region 微店二维码
            var vshop = ObjectContainer.Current.Resolve<VShopService>().GetVShopByShopId(id);
            string vshopUrl = "";
            var curUrl = CurrentUrlHelper.CurrentUrlNoPort();
            if (vshop != null)
            {
                vshopUrl = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/vshop/detail/" + vshop.Id;
                ViewBag.VShopQR = CreateQR(vshop.StrLogo, vshopUrl);
            }
            else
            {
                vshopUrl = curUrl + "/m-" + PlatformType.WeiXin.ToString();
                ViewBag.VShopQR = CreateQR(null, vshopUrl);
            }
            #endregion

            var shopName = "平台";
            if (id > 0)
            {
                var shop = ObjectContainer.Current.Resolve<ShopService>().GetShop(id);
                if (shop != null)
                {
                    shopName = shop.ShopName;
                }
            }
            ViewBag.ShopName = shopName;

            var user = CurrentUser?.Id ?? 0;
            bool isLogin = CurrentUser != null;
            var model = new ProductPartialHeaderModel();
            model.ShopId = id;
            model.isLogin = isLogin ? "true" : "false";
            //用户积分
            model.MemberIntegral = isLogin ? MemberIntegralApplication.GetAvailableIntegral(CurrentUser.Id) : 0;


            var baseCoupons = new List<DisplayCoupon>();
            //优惠卷
            var usercoupons = _CouponService.GetAllUserCoupon(user);//优惠卷
            var coupons = CouponApplication.GetCouponInfo(usercoupons.Select(p => p.CouponId));
            var shops = ShopApplication.GetShops(coupons.Select(p => p.ShopId));
            var shopBonus = ShopBonusApplication.GetShopBounsByUser(user);//红包
            baseCoupons.AddRange(usercoupons.Select(item =>
            {
                var coupon = coupons.FirstOrDefault(p => p.Id == item.CouponId);

                var shop = shops.FirstOrDefault(p => p.Id == coupon.ShopId);
                var shopname = "平台";
                long shopId = 0;
                if (shop != null)
                {
                    shopname = shop.ShopName;
                    shopId = shop.Id;
                }
                return new DisplayCoupon
                {
                    Type = CommonModel.CouponType.Coupon,
                    Limit = coupon.OrderAmount,
                    Price = item.Price,
                    ShopId = shopId,
                    ShopName = shopname,
                    EndTime = coupon.EndTime,
                };
            }));

            baseCoupons.AddRange(shopBonus.Select(p => new DisplayCoupon
            {
                Type = CommonModel.CouponType.ShopBonus,
                EndTime = p.Bonus.DateEnd,
                Limit = p.Bonus.UsrStatePrice,
                Price = p.Receive.Price,
                ShopId = p.Shop.Id,
                ShopName = p.Shop.ShopName,
                UseState = p.Bonus.UseState
            }));

            model.BaseCoupon = baseCoupons;

            //浏览的商品
            //var browsingPro = isLogin ? BrowseHistrory.GetBrowsingProducts(10, CurrentUser == null ? 0 : CurrentUser.Id) : new List<ProductBrowsedHistoryModel>();
            //model.BrowsingProducts = browsingPro;
            InitHeaderData();
            setTheme();//主题设置
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return PartialView("~/Areas/Web/Views/Shared/ShopHeader.cshtml", model);
        }

        private string GetPageName(string url)
        {
            string pagename = "";
            if (url.Contains("Product/Detail"))
            {
                pagename = "product";
            }
            else if (url.Contains("LimitTimeBuy/Detail"))
            {
                pagename = "limitbuy";
            }
            return pagename;
        }

        private string CreateQR(string shopLogo, string vshopUrl)
        {
            Image qrcode;
            //string logoFullPath = Server.MapPath( vshop.Logo );
            if (string.IsNullOrWhiteSpace(shopLogo) || !HimallIO.ExistFile(shopLogo))// || !System.IO.File.Exists( logoFullPath ) 
                qrcode = Core.Helper.QRCodeHelper.Create(vshopUrl);
            else
                qrcode = Core.Helper.QRCodeHelper.Create(vshopUrl, Core.HimallIO.GetRomoteImagePath(shopLogo));

            Bitmap bmp = new Bitmap(qrcode);
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] arr = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(arr, 0, (int)ms.Length);
            ms.Close();

            qrcode.Dispose();
            return Convert.ToBase64String(arr);

        }

        //public ActionResult UserInfo()
        //{
        //    return PartialView("~/Areas/Web/Views/Shared/_UserCenterLeft.cshtml", CurrentUser);
        //}

        [ChildActionOnly]
        public ActionResult TopInfo()
        {

            ViewBag.SiteName = SiteSettings.SiteName;
            //  ViewBag.IsSeller = false;

            var model = CurrentUser;

            ViewBag.IsOpenH5 = SiteSettings.IsOpenH5;
            ViewBag.APPCanDownload = SiteSettings.CanDownload;
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
                ViewBag.APPQR = strUrl;
            }
            ViewBag.QR = SiteSettings.QRCode;
            return PartialView("~/Areas/Web/Views/Shared/_TopInfo.cshtml", model);
        }

        //[OutputCache(Duration = ConstValues.PAGE_CACHE_DURATION)]
        public ActionResult Foot()
        {
            //页脚
            var articleCategoryService = _iArticleCategoryService;
            var articleService = _iArticleService;
            //服务文章
            var pageFootServiceCategory = articleCategoryService.GetSpecialArticleCategory(SpecialCategory.PageFootService);
            if (pageFootServiceCategory == null) { return PartialView("~/Areas/Web/Views/Shared/Foot.cshtml"); }
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
            ViewBag.PageFootService = resultList;

            //页脚
            ViewBag.PageFoot = SiteSettings.PageFoot;
            ViewBag.QRCode = SiteSettings.QRCode;
            ViewBag.SiteName = SiteSettings.SiteName;

            //string html = System.IO.File.ReadAllText(this.Server.MapPath(_templateFootHtmlFileFullName));//读取模板尾部html静态文件内容
            //ViewBag.Html = html;

            return PartialView("~/Areas/Web/Views/Shared/Foot.cshtml");
        }

        public ActionResult GetBrowedProductList()
        {
            var list = BrowseHistrory.GetBrowsingProducts(5, CurrentUser == null ? 0 : CurrentUser.Id);
            return PartialView("_ProductBrowsedHistory", list);
        }

        public ActionResult OrderTopBarCss()
        {
            Himall.DTO.Theme vmTheme = new Theme();
            var currentSetting = GetCurrentThemeSetting();
            if (currentSetting != null)
            {
                vmTheme.ClassifiedsColor = currentSetting.classifiedsColor;
                vmTheme.FrameColor = currentSetting.frameColor;
                vmTheme.MainColor = currentSetting.mainColor;
                vmTheme.SecondaryColor = currentSetting.secondaryColor;
                vmTheme.ThemeId = currentSetting.themeId;
                vmTheme.TypeId = currentSetting.typeId;
                vmTheme.WritingColor = currentSetting.writingColor;
            }
            return PartialView("_OrderTopBarCss", vmTheme);
        }

        public ActionResult UserCenterCss()
        {
            Himall.DTO.Theme vmTheme = new Theme();
            var currentSetting = GetCurrentThemeSetting();
            if (currentSetting != null)
            {
                vmTheme.ClassifiedsColor = currentSetting.classifiedsColor;
                vmTheme.FrameColor = currentSetting.frameColor;
                vmTheme.MainColor = currentSetting.mainColor;
                vmTheme.SecondaryColor = currentSetting.secondaryColor;
                vmTheme.ThemeId = currentSetting.themeId;
                vmTheme.TypeId = currentSetting.typeId;
                vmTheme.WritingColor = currentSetting.writingColor;
            }
            return PartialView("_UserCenterCss", vmTheme);
        }

        [HttpGet]
        public JsonResult GetBrowedProduct()
        {
            var p = BrowseHistrory.GetBrowsingProducts(5, CurrentUser == null ? 0 : CurrentUser.Id);
            return Json(p, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 初始化页面头部  此方法在使用redis缓存时有可能比较慢，所以增加调试信息输出
        /// </summary>
        void InitHeaderData()
        {
            //SiteName
            ViewBag.SiteName = SiteSettings.SiteName;

            //Logo
            ViewBag.Logo = SiteSettings.Logo;

            //搜索输入框关键字
            ViewBag.Keyword = SiteSettings.Keyword;

            //热门关键字
            ViewBag.HotKeyWords = !string.IsNullOrWhiteSpace(SiteSettings.Hotkeywords) ? SiteSettings.Hotkeywords.Split(',') : new string[] { };

            //导航
            ViewBag.Navigators = _iNavigationService.GetPlatNavigations().ToArray();
            //分类

            var categories = _iHomeCategoryService.GetHomeCategorySets().ToList();
            ViewBag.Categories = categories;

            var categoryService = _iCategoryService;
            ViewBag.AllSecondCategoies = categoryService.GetFirstAndSecondLevelCategories().Where(item => item.Depth == 2 && item.IsDeleted == false).ToList();

            var service = _iBrandService;
            var brands = new Dictionary<int, IEnumerable<BrandInfo>>();

            //页脚
            ViewBag.PageFoot = SiteSettings.PageFoot;

            //分类品牌
            ViewBag.CategoryBrands = brands;

            //会员信息
            ViewBag.Member = CurrentUser;

            ViewBag.IsOpenH5 = SiteSettings.IsOpenH5;
            ViewBag.APPCanDownload = SiteSettings.CanDownload;
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
                ViewBag.APPQR = strUrl;
            }

            setTheme();//主题设置
        }

        /// <summary>
        /// 获取当前模板的主题配色
        /// </summary>
        /// <returns></returns>
        private ThemeSetting GetCurrentThemeSetting()
        {
            ThemeSetting currentSetting = new ThemeSetting();
            //内页面跟随首页配置的主题配色方案变化
            string currentTempdate = System.IO.File.ReadAllText(this.Server.MapPath(_templatesettings));//读取当前应用的模板
            TemplateSetting curTemplateObj = ParseFormJson<TemplateSetting>(currentTempdate);
            if (curTemplateObj != null)
            {
                if (System.IO.File.Exists(this.Server.MapPath(_themesettings)))
                {
                    string currentTheme = System.IO.File.ReadAllText(this.Server.MapPath(_themesettings));//读取模板应用的主题配色列表
                    List<ThemeSetting> curThemeObjs = ParseFormJson<List<ThemeSetting>>(currentTheme);
                    if (curThemeObjs != null)
                    {
                        var info = curThemeObjs.FirstOrDefault(a => a.templateId == curTemplateObj.Id);//取当前启用的模板对应的主题配色方案
                        if (null != info)
                        {
                            currentSetting = info;
                            //ViewBag.WritingColor = info.writingColor;
                            //ViewBag.SecondaryColor = info.secondaryColor;
                            //ViewBag.MainColor = info.mainColor;
                            //ViewBag.FrameColor = info.frameColor;
                            //ViewBag.ClassifiedsColor = info.classifiedsColor;
                            //ViewBag.TypeId = (CommonModel.ThemeType)info.typeId;
                        }
                    }
                }
            }
            return currentSetting;
        }

        /// <summary>
        /// 主题设置
        /// </summary>
        private void setTheme()
        {
            //内页面跟随首页配置的主题配色方案变化
            //string currentTempdate = System.IO.File.ReadAllText(this.Server.MapPath(_templatesettings));//读取当前应用的模板
            //TemplateSetting curTemplateObj = ParseFormJson<TemplateSetting>(currentTempdate);
            //if (curTemplateObj != null)
            //{
            //    if (System.IO.File.Exists(this.Server.MapPath(_themesettings)))
            //    {
            //        string currentTheme = System.IO.File.ReadAllText(this.Server.MapPath(_themesettings));//读取模板应用的主题配色列表
            //        List<ThemeSetting> curThemeObjs = ParseFormJson<List<ThemeSetting>>(currentTheme);
            //        if (curThemeObjs != null)
            //        {
            //            var info = curThemeObjs.FirstOrDefault(a => a.templateId == curTemplateObj.Id);//取当前启用的模板对应的主题配色方案
            var info = GetCurrentThemeSetting();
            if (null != info)
            {
                ViewBag.WritingColor = info.writingColor;
                ViewBag.SecondaryColor = info.secondaryColor;
                ViewBag.MainColor = info.mainColor;
                ViewBag.FrameColor = info.frameColor;
                ViewBag.ClassifiedsColor = info.classifiedsColor;
                ViewBag.TypeId = (CommonModel.ThemeType)info.typeId;
            }
            //}
            //}
            //}
            //if (System.IO.File.Exists(this.Server.MapPath(_themesettings)))
            //{
            //    //_templatesettings
            //    string currentTheme = System.IO.File.ReadAllText(this.Server.MapPath(_themesettings));//读取当前模板应用的主题配色
            //    ThemeSetting curThemeObj = ParseFormJson<ThemeSetting>(currentTheme);
            //    if (null != curThemeObj)
            //    {
            //        ViewBag.WritingColor = curThemeObj.writingColor;
            //        ViewBag.SecondaryColor = curThemeObj.secondaryColor;
            //        ViewBag.MainColor = curThemeObj.mainColor;
            //        ViewBag.FrameColor = curThemeObj.frameColor;
            //        ViewBag.ClassifiedsColor = curThemeObj.classifiedsColor; 
            //        ViewBag.TypeId = (CommonModel.ThemeType)curThemeObj.typeId;
            //    }
            //}
        }

        public ActionResult Logo()
        {
            return Content(HimallIO.GetImagePath(SiteSettings.Logo));
        }

        public ActionResult GetShopCoupon(long shopId)
        {
            var model = _CouponService.GetTopCoupon(shopId);
            return PartialView("_ShopCoupon", model);
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
    }
}