using Himall.Service;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;
using Himall.Core;
using Himall.Application;
using Himall.CommonModel;
using System.Drawing;
using System.IO;
using System;

namespace Himall.Web.Areas.Admin.Controllers
{
    [H5AuthorizationAttribute]
    /// <summary>
    /// 可视化模板管理
    /// </summary>
    public class VTemplateController : BaseAdminController
    {
        private TemplateSettingsService _iTemplateSettingsService;
        public VTemplateController(TemplateSettingsService TemplateSettingsService)
        {
            _iTemplateSettingsService = TemplateSettingsService;

        }
        /// <summary>
        /// 设置微商城当前模板
        /// </summary>
        /// <param name="tName"></param>
        /// <returns></returns>
        public JsonResult UpdateCurrentTemplate(string tName)
        {

            if (string.IsNullOrWhiteSpace(tName)) return Json(new { success = false, msg = "模板名称不能为空" });
            _iTemplateSettingsService.SetCurrentTemplate(tName, 0);
            return Json(new { success = true, msg = "模板启用成功" });
        }
        /// <summary>
        /// 微商城模板管理
        /// </summary>
        /// <returns></returns>
        public ActionResult VHomepage()
        {
            string crrentTemplateName = "t1";
            var curr = _iTemplateSettingsService.GetCurrentTemplate(0);
            if (null != curr)
            {
                crrentTemplateName = curr.CurrentTemplateName;
            }

            var helper = new GalleryHelper();
            var themes = helper.LoadThemes();
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
            string qrCodeImagePath = string.Empty;
            string url = CurrentUrlHelper.CurrentUrl();
            Bitmap map;
            map = Core.Helper.QRCodeHelper.Create(url + "/m-wap");
            MemoryStream ms = new MemoryStream();
            map.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            qrCodeImagePath = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray()); // 将图片内存流转成base64,图片以DataURI形式显示  
            ms.Dispose();

            ViewBag.CurUrl = url;
            ViewBag.QrCodeImagePath = qrCodeImagePath;
            #endregion
            return View(themes.Where(t => t.ThemeName != crrentTemplateName.ToLower()).ToList());
        }

        public ActionResult NewEditeTemplate(int client = 1, string tName = "t1") {
            VTemplateEditModel model = new VTemplateEditModel();
            model.Name = tName;
            model.ClientType = (VTemplateClientTypes)client;
            model.IsShowPrvPage = false;
            string templateName = "NewEditeTemplate";
            switch (model.ClientType)
            {
                case VTemplateClientTypes.WapSpecial:
                case VTemplateClientTypes.SellerWapSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    break;
                case VTemplateClientTypes.AppSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    templateName = "EditTemplate-AppTopic";
                    break;
                case VTemplateClientTypes.WXSmallProgramSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    templateName = "EditTemplate-AppletTopic";
                    break;
            }
            //门店授权
            ViewBag.IsOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            VTemplateHelper.DownloadTemplate(model.Name, model.ClientType);
            return View(templateName, model);
        }

        /// <summary>
        /// 模板编辑
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tName"></param>
        /// <returns></returns>
        public ActionResult EditTemplate(int client = 1, string tName = "t1")
        {
            VTemplateEditModel model = new VTemplateEditModel();
            model.Name = tName;
            model.ClientType = (VTemplateClientTypes)client;
            model.IsShowPrvPage = true;
            string templateName = "EditTemplate";
            switch (model.ClientType)
            {
                case VTemplateClientTypes.WapSpecial:
                case VTemplateClientTypes.SellerWapSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    break;
                case VTemplateClientTypes.AppSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    templateName = "EditTemplate-AppTopic";
                    break;
                case VTemplateClientTypes.WXSmallProgramSpecial:
                    model.IsShowTitle = true;
                    model.IsShowTags = true;
                    model.IsShowPrvPage = false;
                    model.IsShowIcon = true;
                    templateName = "EditTemplate-AppletTopic";
                    break;
            }
            //门店授权
            ViewBag.IsOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            VTemplateHelper.DownloadTemplate(model.Name, model.ClientType);
            return View(templateName, model);
        }
    }
}