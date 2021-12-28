using Himall.Core;
using Himall.Service;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;
using Himall.Web.Areas.SellerAdmin.Models;
using Himall.CommonModel;
using Himall.Application;
using System.Net;
using System.Text;
using System.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using Himall.Entities;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    [H5AuthorizationAttribute]
    public class VTemplateController : BaseSellerController
    {
        private VShopService _VShopService;
        private SlideAdsService _iSlideAdsService;
        private NavigationService _iNavigationService;
        private CouponService _CouponService;
        private TemplateSettingsService _iTemplateSettingsService;

        public VTemplateController(VShopService VShopService,
            SlideAdsService SlideAdsService,
            NavigationService NavigationService,
            CouponService CouponService,
            TemplateSettingsService TemplateSettingsService
            )
        {
            _VShopService = VShopService;
            _iSlideAdsService = SlideAdsService;
            _iNavigationService = NavigationService;
            _CouponService = CouponService;
            _iTemplateSettingsService = TemplateSettingsService;
        }

        /// <summary>
        /// 模板编辑
        /// </summary>
        /// <param name="tName"></param>
        /// <returns></returns>
        public ActionResult EditTemplate(int client = 2, string tName = "t1")
        {
            VTemplateEditModel model = new VTemplateEditModel();
            model.Name = tName;
            model.ClientType = (VTemplateClientTypes)client;
            model.IsShowPrvPage = false;
            var templateName = "EditTemplate";
            switch (model.ClientType)
            {
                case VTemplateClientTypes.SellerWapSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    break;
                case VTemplateClientTypes.SellerWxSmallProgramSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    templateName = "EditTemplate-AppletTopic";
                    break;
            }
            long shopid = CurrentSellerManager.ShopId;
            model.ShopId = shopid;
            var tmpobj = _VShopService.GetVShopByShopId(shopid);
            //if (tmpobj == null)
            //{
            //    throw new Himall.Core.HimallException("未开通微店");
            //}
            long vshopid = tmpobj == null ? 0 : tmpobj.Id;
            model.VShopId = vshopid;
            VTemplateHelper.DownloadTemplate(model.Name, model.ClientType);
            return View(templateName, model);
        }

        /// <summary>
        /// 微商城微信首页模板
        /// </summary>
        /// <returns></returns>
        public ActionResult VHomepage()
        {
            var vshop = _VShopService.GetVShopByShopId(CurrentSellerManager.ShopId);
            ViewBag.IsOpenVShop = vshop != null;
            ViewBag.VShopId = vshop == null ? 0 : vshop.Id;
            ViewBag.ShopId = CurrentSellerManager.ShopId;
            string crrentTemplateName = "t1";
            var curr = _iTemplateSettingsService.GetCurrentTemplate(CurrentSellerManager.ShopId);
            if (null != curr)
            {
                crrentTemplateName = curr.CurrentTemplateName;
            }

            var helper = new GalleryHelper();
            var themes = helper.LoadThemes(CurrentSellerManager.ShopId);
            var CurTemplateObj = themes.FirstOrDefault(t => t.ThemeName.Equals(crrentTemplateName.ToLower()));
            if (CurTemplateObj == null)
            {
                CurTemplateObj = themes.FirstOrDefault(t => t.ThemeName.Equals("t1"));
            }
            if (CurTemplateObj == null)
            {
                throw new HimallException("错误的模板：" + crrentTemplateName);
            }
            ViewBag.CurrentTemplate = CurTemplateObj;


            #region 二维码图片
            if (vshop != null)
            {
                string qrCodeImagePath = string.Empty;
                string url = CurrentUrlHelper.CurrentUrl() + "/m-wap/vshop/detail/" + vshop.Id;
                Bitmap map;
                map = Core.Helper.QRCodeHelper.Create(url);
                MemoryStream ms = new MemoryStream();
                map.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                qrCodeImagePath = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray()); // 将图片内存流转成base64,图片以DataURI形式显示  
                ms.Dispose();

                ViewBag.CurUrl = url;
                ViewBag.QrCodeImagePath = qrCodeImagePath;

            }

            #endregion
            return View(themes.Where(t => t.ThemeName != crrentTemplateName.ToLower()).ToList());
        }


        /// <summary>
        /// 设置微商城首页模板
        /// </summary>
        /// <param name="tName"></param>
        /// <returns></returns>
        public JsonResult UpdateCurrentTemplate(string tName)
        {
            if (string.IsNullOrWhiteSpace(tName))
                return Json(new { success = false, msg = "模板名称不能为空" });
            _iTemplateSettingsService.SetCurrentTemplate(tName, CurrentSellerManager.ShopId);
            return Json(new { success = true, msg = "模板启用成功" });
        }

        public JsonResult UpdateTemplateName(string tName, string newName)
        {
            if (string.IsNullOrWhiteSpace(tName))
                return Json(new { success = false, msg = "模板名称不能为空" });
            new GalleryHelper().UpdateTemplateName(tName, newName, CurrentSellerManager.ShopId);
            return Json(new { success = true, msg = "模板名称修改成功" });
        }

        /// <summary>
        /// 小程序微店首页模板
        /// </summary>
        /// <returns></returns>
        public ActionResult SmallProgVHomepage()
        {
            var vshop = _VShopService.GetVShopByShopId(CurrentSellerManager.ShopId);
            if (vshop == null)
            {
                //throw new Himall.Core.HimallException("未开通微店");
            }
            ViewBag.IsOpenVShop = vshop != null;
            ViewBag.VShopId = vshop == null ? 0 : vshop.Id;
            ViewBag.ShopId = CurrentSellerManager.ShopId;
            string crrentTemplateName = "t1";

            var helper = new GalleryHelper();
            var themes = helper.LoadThemes(CurrentSellerManager.ShopId);
            var CurTemplateObj = themes.FirstOrDefault(t => t.ThemeName.Equals(crrentTemplateName.ToLower()));
            if (CurTemplateObj == null)
            {
                CurTemplateObj = themes.FirstOrDefault(t => t.ThemeName.Equals("t1"));
            }
            if (CurTemplateObj == null)
            {
                throw new HimallException("错误的模板：" + crrentTemplateName);
            }
            ViewBag.CurrentTemplate = CurTemplateObj;
            ViewBag.CurUrl = SiteSettingApplication.GetCurDomainUrl();
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (!string.IsNullOrWhiteSpace(siteSetting.WeixinAppletId) && !string.IsNullOrWhiteSpace(siteSetting.WeixinAppletSecret))
            {
                ViewBag.IsHaveApplet = true;
            }

            //获取指定页面小程序码
            ViewBag.QrCodeImagePath = WXSmallProgramApplication.GetWxAppletCodeVshopOwnLogo(vshop);

            return View(themes.Where(t => t.ThemeName != crrentTemplateName.ToLower()).ToList());
        }

        /// <summary>
        /// 小程序模板编辑
        /// </summary>
        /// <param name="tName"></param>
        /// <returns></returns>
        public ActionResult EditSmallProgTemplate(int client = 17, string tName = "t1")
        {
            VTemplateEditModel model = new VTemplateEditModel();
            model.Name = tName;
            model.ClientType = (VTemplateClientTypes)client;
            model.IsShowPrvPage = true;
            switch (model.ClientType)
            {
                case VTemplateClientTypes.SellerWapSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    break;
            }
            long shopid = CurrentSellerManager.ShopId;
            model.ShopId = shopid;
            var tmpobj = _VShopService.GetVShopByShopId(shopid);
            //if (tmpobj == null)
            //{
            //    throw new Himall.Core.HimallException("未开通微店");
            //}
            long vshopid = tmpobj == null ? 0 : tmpobj.Id;
            model.VShopId = vshopid;
            return View(model);
        }

    }
}