using Himall.Application;
using Himall.CommonModel;
using Himall.CommonModel;
using Himall.Core;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class HomeController : BaseMobileTemplatesController
    {
        private TemplateSettingsService _iTemplateSettingsService;
        private CustomerCustomerService _CustomerCustomerService;

        public HomeController(TemplateSettingsService TemplateSettingsService, CustomerCustomerService CustomerCustomerService)
        {
            _iTemplateSettingsService = TemplateSettingsService;
            _CustomerCustomerService = CustomerCustomerService;
        }

        [OutputCache(Duration = ConstValues.PAGE_CACHE_DURATION, VaryByCustom = "Home")]
        // GET: Mobile/Home
        public ActionResult Index(int ispv = 0, string tn = "")
        {
            string crrentTemplateName = "t1";
            //新版本调整可视化只留t1模板，则下面注释
            //var curr = _iTemplateSettingsService.GetCurrentTemplate(0);
            //if (null != curr)
            //{
            //    crrentTemplateName = curr.CurrentTemplateName;
            //}
            //if (ispv == 1)
            //{
            //    if (!string.IsNullOrWhiteSpace(tn))
            //    {
            //        crrentTemplateName = tn;
            //    }
            //}
            ViewBag.Title = SiteSettings.SiteName + "首页";
            ViewBag.FootIndex = 0;

            var services = CustomerServiceApplication.GetPlatformCustomerService(true, true, true, CurrentUser);
            ViewBag.CustomerServices = services;

            VTemplateHelper.DownloadTemplate(crrentTemplateName, VTemplateClientTypes.WapIndex, 0);
            return View(string.Format("~/Areas/Admin/Templates/vshop/{0}/Skin-HomePage.cshtml", crrentTemplateName));
        }
        //前台没看到调用代码
        public JsonResult LoadProducts(int page, int pageSize)
        {
            var homeProducts = ObjectContainer.Current.Resolve<MobileHomeProductsService>().GetMobileHomeProducts(0, Core.PlatformType.WeiXin, page, pageSize);
            var products = ProductManagerApplication.GetProducts(homeProducts.Models.Select(p => p.ProductId));
            var model = products.Select(item => new
            {
                name = item.ProductName,
                id = item.Id,
                image = item.GetImage(ImageSize.Size_350),
                price = item.MinSalePrice,
                marketPrice = item.MarketPrice
            });
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        //前台调用被注释，暂不修改
        public JsonResult LoadProductsFromCache(int page)
        {
            var html = TemplateSettingsApplication.GetGoodTagFromCache(page);
            return Json(new { htmlTag = html }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult About()
        {
            return View();
        }

        public ActionResult DownLoadApp()
        {
            if (PlatformType == Core.PlatformType.WeiXin)
                return RedirectToAction("WeiXinDownLoad");
            if (visitorTerminalInfo.OperaSystem == EnumVisitorOperaSystem.Android)
                return RedirectToAction("AndriodDownLoad");
            if (visitorTerminalInfo.OperaSystem == EnumVisitorOperaSystem.IOS)
                return RedirectToAction("IOSDownLoad");
            return View();
        }


        public ActionResult WeiXinDownLoad(string isbranch)
        {
            if (PlatformType == Core.PlatformType.WeiXin)
                return View();
            if (visitorTerminalInfo.OperaSystem == EnumVisitorOperaSystem.Android)
                return RedirectToAction("AndriodDownLoad",new { isbranch = isbranch });
            if (visitorTerminalInfo.OperaSystem == EnumVisitorOperaSystem.IOS)
                return RedirectToAction("IOSDownLoad");
            return View();
        }


        public ActionResult AndriodDownLoad(string isbranch)
        {
            var DownLoadApk = SiteSettingApplication.SiteSettings.AndriodDownLoad;
            if (isbranch == "1")
                DownLoadApk = SiteSettingApplication.SiteSettings.ShopAndriodDownLoad;
            if (!string.IsNullOrEmpty(DownLoadApk))
            {
                ViewBag.DownLoadApk = DownLoadApk;
                return View();
            }
            return RedirectToAction("DownLoadError");
        }

        public ActionResult IOSDownLoad()
        {
            var DownLoadApk = SiteSettingApplication.SiteSettings.IOSDownLoad;
            if (!string.IsNullOrEmpty(DownLoadApk))
            {
                return Redirect(DownLoadApk);
            }
            return RedirectToAction("DownLoadError");
        }

        public ActionResult DownLoadError()
        {
            return View();
        }

        /// <summary>
        /// 获取分享内容
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetShare(string url)
        {
            var shareArgs = Himall.Application.WXApiApplication.GetWeiXinShareArgs(url);
            var siteSetting = SiteSettingApplication.SiteSettings;
            var shareTitle = string.Empty;
            if (siteSetting != null && !string.IsNullOrEmpty(siteSetting.SiteName))
            {
                shareTitle = siteSetting.SiteName;
            }
            var result = new
            {
                AppId = shareArgs.AppId,
                Timestamp = shareArgs.Timestamp,
                NonceStr = shareArgs.NonceStr,
                Signature = shareArgs.Signature,
                ShareIcon = Core.HimallIO.GetRomoteImagePath(SiteSettingApplication.SiteSettings.WXLogo),
                ShareTitle = shareTitle
            };

            return Json(new { success = true, data = result }, true);
        }
        /// <summary>
        /// 获取模板节点
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetTemplateItem(string id, string tn = "")
        {
            string result = "";
            if (string.IsNullOrWhiteSpace(tn))
            {
                tn = "t1";
                var curr = _iTemplateSettingsService.GetCurrentTemplate(0);
                if (null != curr)
                {
                    tn = curr.CurrentTemplateName;
                }
            }
            result = VTemplateHelper.GetTemplateItemById(id, tn, VTemplateClientTypes.WapIndex);
            return result;
        }
        /// <summary>
        /// 分销头部
        /// </summary>
        /// <returns></returns>
        public ActionResult DistributionHeader()
        {
            Himall.Entities.DistributorInfo result = null;
            if (CurrentSpreadId.HasValue &&CurrentSpreadId > 0)
            {
                result = DistributionApplication.GetDistributor(CurrentSpreadId.Value);
                if (result != null && result.IsNormalDistributor)
                {
                    result.ShopLogo = Himall.Core.HimallIO.GetRomoteImagePath(result.ShopLogo);
                }
                else
                {
                    result = null;
                }
            }
            ViewBag.NeedDistributionWeiXinShare = NeedDistributionWeiXinShare;
            return View(result);
        }

        /// <summary>
        /// 获取是否开启了分销和当前是否为分销
        /// </summary>
        /// <returns></returns>
        public JsonResult GetIsShowDistributionHead()
        {
            var isShowDistributionHead = (SiteSettingApplication.SiteSettings.DistributionIsEnable && CurrentSpreadId > 0);
            var result = DistributionApplication.GetDistributor(CurrentSpreadId.Value);
            isShowDistributionHead = isShowDistributionHead && result.IsShowShopLogo;
            return Json(new { success = true, isShowDistributionHead }, true);
        }


        //获取首页弹窗广告信息
        public JsonResult GetPopuActive()
        {

            AdvanceInfo advance = new AdvanceInfo();
            var advanceset = AdvanceApplication.GetAdvanceInfo();
            if (advanceset != null)
            {
                advanceset.Img = advanceset.Img;
                advance = advanceset;
            }

            return Json(advance,true);
        }

        public JsonResult GetViewProductsById(string productIds,string limitbuyIds="",string fightgroupIds="")
        {
            var productlist = GetUpdateProductView(productIds);//获取指定商品最新数据    


            var result = SuccessResult<dynamic>(data: productlist);
            return Json(result,true);
        }


        /// <summary>
        /// 限时购列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public JsonResult GetLimitBuyViewByIds(string ids)
        {
            List<long> pidlist = new List<long>(Array.ConvertAll<string, long>(ids.Split(','), s => long.Parse(s)));


            if (pidlist.Count <= 0)
            {
                throw new HimallException("请传入查询的活动编号！");
            }

            var prolist = ProductManagerApplication.GetLimitBuyViewByIds(pidlist);

            var result = SuccessResult<dynamic>(data: prolist);
            return Json(result,true);
        }


        /// <summary>
        /// 火拼团列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public JsonResult GetFightGroupViewByIds(string ids)
        {
            List<long> fids = new List<long>(Array.ConvertAll<string, long>(ids.Split(','), s => long.Parse(s)));


            if (fids.Count <= 0)
            {
                throw new HimallException("请传入查询的活动编号！");
            }

            var prolist = ProductManagerApplication.GetFightGroupViewByIds(fids);

            var result = SuccessResult<dynamic>(data: prolist);
            return Json(result,true);
        }

        /// <summary>
        /// 获取限时购最新消息
        /// </summary>
        /// <param name="limitbuyIds"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> GetUpdateLimitBuyView(string limitbuyIds) {
            List<long> limitlist = new List<long>(Array.ConvertAll<string, long>(limitbuyIds.Split(','), s => long.Parse(s)));
            IEnumerable<dynamic> limitnew = null;
            if (limitlist.Count > 0) {
                limitnew = ProductManagerApplication.GetLimitBuyViewByIds(limitlist);
            }
            return limitnew;
        }

        /// <summary>
        /// 获取火拼团最新数据
        /// </summary>
        /// <param name="fightgroupIds"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> GetUpdateFightGroupView(string fightgroupIds)
        {
            List<long> fightlist = new List<long>(Array.ConvertAll<string, long>(fightgroupIds.Split(','), s => long.Parse(s)));
            IEnumerable<dynamic> fightnew = null;
            if (fightlist.Count > 0)
            {
                fightnew = ProductManagerApplication.GetFightGroupViewByIds(fightlist);
            }
            return fightnew;
        }

     

    

        private IEnumerable<dynamic> GetUpdateProductView(string productIds) {
            List<long> pidlist = new List<long>(Array.ConvertAll<string, long>(productIds.Split(','), s => long.Parse(s)));

            IEnumerable<dynamic> prolist = null ;
           
            if (pidlist.Count > 0) {
                prolist = ProductManagerApplication.GetViewProductsByIds(pidlist);
            }
            return prolist;
        }

      
    }
}
