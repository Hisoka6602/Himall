using AutoMapper;
using Himall.Core.Helper;
using Himall.Service;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.IO;
using System.Web.Mvc;
using System.Linq;
using System.Configuration;
using System.Web.Configuration;
using Himall.CommonModel;
using Himall.Entities;
using Himall.Application;
using Himall.DTO;
using Himall.Core;
using Himall.Core.Plugins.Message;
using System.Text.RegularExpressions;
using System.Text;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class SiteSettingController : BaseAdminController
    { /// <summary>
      /// 上传文件的扩展名集合
      /// </summary>
        string[] AllowApkFileExtensions = new string[] { ".apk" };

        // GET: Admin/SiteSetting
        public ActionResult Edit()
        {
            Mapper.CreateMap<SiteSettings, SiteSettingModel>().ForMember(a => a.SiteIsOpen, b => b.MapFrom(s => s.SiteIsClose));
            var settings = Mapper.Map<SiteSettings, SiteSettingModel>(SiteSettingApplication.SiteSettings);
            settings.Logo = Core.HimallIO.GetImagePath(settings.Logo);
            settings.MemberLogo = Core.HimallIO.GetImagePath(settings.MemberLogo);
            settings.PCLoginPic = Core.HimallIO.GetImagePath(settings.PCLoginPic);
            settings.QRCode = Core.HimallIO.GetImagePath(settings.QRCode);
            settings.WXLogo = Core.HimallIO.GetImagePath(settings.WXLogo);

            #region 强制绑定手机参数
            var sms = PluginsManagement.GetPlugins<ISMSPlugin>().FirstOrDefault();
            if (sms != null)
            {
                ViewBag.ShowSMS = true;
                ViewBag.LoginLink = sms.Biz.GetLoginLink();
                ViewBag.BuyLink = sms.Biz.GetBuyLink();
            }
            #endregion
            return View(settings);
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult Edit(SiteSettingModel siteSettingModel)
        {
            if (string.IsNullOrWhiteSpace(siteSettingModel.WXLogo))
            {
                return Json(new Result() { success = false, msg = "请上传微信Logo", status = -1 });
            }
            if (string.IsNullOrWhiteSpace(siteSettingModel.Logo))
            {
                return Json(new Result() { success = false, msg = "请上传Logo", status = -2 });
            }
            if (string.IsNullOrWhiteSpace(siteSettingModel.MemberLogo))
            {
                return Json(new Result() { success = false, msg = "请上传卖家中心Logo", status = -2 });
            }


            if (StringHelper.GetStringLength(siteSettingModel.SiteName) > CommonModel.CommonConst.SITENAME_LENGTH)
            {
                var unicodeChar = CommonModel.CommonConst.SITENAME_LENGTH / 2;
                return Json(new Result() { success = false, msg = "网站名字最长" + CommonModel.CommonConst.SITENAME_LENGTH + "位," + unicodeChar + "个中文字符", status = -2 });
            }
            if (siteSettingModel.OpenErp)
            { //开启erp
                if (string.IsNullOrEmpty(siteSettingModel.ErpUrl.Trim()))
                {
                    return Json(new Result() { success = false, msg = "请填写ErpUrl", status = -2 });
                }
                if (string.IsNullOrEmpty(siteSettingModel.ErpAppkey.Trim()))
                {
                    return Json(new Result() { success = false, msg = "请填写ErpAppkey", status = -2 });
                }
                if (string.IsNullOrEmpty(siteSettingModel.ErpAppsecret.Trim()))
                {
                    return Json(new Result() { success = false, msg = "请填写ErpAppsecret", status = -2 });
                }
                if (string.IsNullOrEmpty(siteSettingModel.ErpSid.Trim()))
                {
                    return Json(new Result() { success = false, msg = "请填写ErpSid", status = -2 });
                }
                if (string.IsNullOrEmpty(siteSettingModel.ErpStoreNumber.Trim()))
                {
                    return Json(new Result() { success = false, msg = "请填写ErpStoreNumber", status = -2 });
                }
            }

            if (string.IsNullOrEmpty(siteSettingModel.SitePhone))
            {
                return Json(new Result() { success = false, msg = "请填写客服电话", status = -2 });
            }
            string logoName = "logo.png";
            string memberLogoName = "memberLogo.png";
            string qrCodeName = "qrCode.png";
            string PCLoginPicName = "pcloginpic.png";

            string relativeDir = "/Storage/Plat/Site/";
            string imageDir = relativeDir;

            //if (!Directory.Exists(imageDir))
            //{
            //    Directory.CreateDirectory(imageDir);
            //}

            if (!string.IsNullOrWhiteSpace(siteSettingModel.Logo))
            {

                if (siteSettingModel.Logo.Contains("/temp/"))
                {
                    string Logo = siteSettingModel.Logo.Substring(siteSettingModel.Logo.LastIndexOf("/temp"));
                    Core.HimallIO.CopyFile(Logo, imageDir + logoName, true);
                }
            }
            if (!string.IsNullOrWhiteSpace(siteSettingModel.MemberLogo))
            {
                if (siteSettingModel.MemberLogo.Contains("/temp/"))
                {
                    string memberLogo = siteSettingModel.MemberLogo.Substring(siteSettingModel.MemberLogo.LastIndexOf("/temp"));

                    Core.HimallIO.CopyFile(memberLogo, imageDir + memberLogoName, true);
                    //  Core.Helper.IOHelper.CopyFile(memberLogo, imageDir, false, memberLogoName);
                }
            }
            if (!string.IsNullOrWhiteSpace(siteSettingModel.QRCode))
            {
                if (siteSettingModel.QRCode.Contains("/temp/"))
                {
                    string qrCode = siteSettingModel.QRCode.Substring(siteSettingModel.QRCode.LastIndexOf("/temp"));
                    Core.HimallIO.CopyFile(qrCode, imageDir + qrCodeName, true);
                    //   Core.Helper.IOHelper.CopyFile(qrCode, imageDir, false, qrCodeName);
                }
            }

            if (!string.IsNullOrWhiteSpace(siteSettingModel.PCLoginPic))
            {
                if (siteSettingModel.PCLoginPic.Contains("/temp/"))
                {
                    string PCLoginPic = siteSettingModel.PCLoginPic.Substring(siteSettingModel.PCLoginPic.LastIndexOf("/temp"));
                    //    Core.Helper.IOHelper.CopyFile(PCLoginPic, imageDir, true, PCLoginPicName);
                    Core.HimallIO.CopyFile(PCLoginPic, imageDir + PCLoginPicName, true);


                }
            }

            if (!string.IsNullOrWhiteSpace(siteSettingModel.WXLogo))
            {
                if (siteSettingModel.WXLogo.Contains("/temp/"))
                {
                    string newFile = relativeDir + "wxlogo.png";
                    // string oriFullPath = Core.Helper.IOHelper.GetMapPath(siteSettingModel.WXLogo);
                    // string newFullPath = Core.Helper.IOHelper.GetMapPath(newFile);
                    string wxlogoPic = siteSettingModel.WXLogo.Substring(siteSettingModel.WXLogo.LastIndexOf("/temp"));
                    Core.HimallIO.CopyFile(wxlogoPic, newFile, true);
                    Core.HimallIO.CreateThumbnail(wxlogoPic, newFile, (int)ImageSize.Size_100, (int)ImageSize.Size_100);
                    //using (Image image = Image.FromFile(oriFullPath))
                    //{
                    //    image.Save(oriFullPath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    //    if (System.IO.File.Exists(newFullPath))
                    //    {
                    //        System.IO.File.Delete(newFullPath);
                    //    }
                    //    ImageHelper.CreateThumbnail(oriFullPath + ".png", newFullPath, 100, 100);
                    //}
                    siteSettingModel.WXLogo = newFile;
                }
            }



            var settings = SiteSettingApplication.SiteSettings;

            #region 是否修改了SEO信息(如修改了更新首页静态页SEO内容)
            if (settings.Site_SEOTitle != siteSettingModel.Site_SEOTitle || settings.Site_SEOKeywords != siteSettingModel.Site_SEOKeywords || settings.Site_SEODescription != siteSettingModel.Site_SEODescription)
            {
                const string _homeIndexFileFullName = "~/Areas/Web/Views/Home/index1.html";
                string html = System.IO.File.ReadAllText(this.Server.MapPath(_homeIndexFileFullName));//读取模板html文件内容

                html = Regex.Replace(html, "<title((.|\n)*?)</title>", "<title>" + siteSettingModel.Site_SEOTitle + "</title>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                html = Regex.Replace(html, "<meta name=\"keywords\" ((.|\n)*?)/>", string.Format("<meta name=\"keywords\" content=\"{0}\" />", siteSettingModel.Site_SEOKeywords), RegexOptions.IgnoreCase | RegexOptions.Compiled);
                html = Regex.Replace(html, "<meta name=\"description\" ((.|\n)*?)/>", string.Format("<meta name=\"description\" content=\"{0}\" />", siteSettingModel.Site_SEODescription), RegexOptions.IgnoreCase | RegexOptions.Compiled);

                string fullName = this.Server.MapPath(_homeIndexFileFullName);
                using (var fs = new FileStream(fullName, FileMode.Create, FileAccess.Write))
                {
                    var buffer = Encoding.UTF8.GetBytes(html);
                    fs.Write(buffer, 0, buffer.Length);
                }
            }
            #endregion

            settings.SiteName = siteSettingModel.SiteName;
            settings.SitePhone = siteSettingModel.SitePhone;
            settings.SiteIsClose = siteSettingModel.SiteIsOpen;
            settings.Logo = relativeDir + logoName;
            settings.MemberLogo = string.IsNullOrEmpty(siteSettingModel.MemberLogo) ? "" : (relativeDir + memberLogoName);//如删除了图不显示图
            siteSettingModel.PCLoginPic = string.IsNullOrEmpty(siteSettingModel.PCLoginPic) ? "" : (relativeDir + PCLoginPicName);//如删除了图不显示图
            settings.QRCode = string.IsNullOrEmpty(siteSettingModel.QRCode) ? "" : (relativeDir + qrCodeName);//如删除了图不显示图
            settings.FlowScript = siteSettingModel.FlowScript;
            settings.Site_SEOTitle = siteSettingModel.Site_SEOTitle;
            settings.Site_SEOKeywords = siteSettingModel.Site_SEOKeywords;
            settings.Site_SEODescription = siteSettingModel.Site_SEODescription;

            settings.MobileVerifOpen = siteSettingModel.MobileVerifOpen;

            settings.RegisterType = (int)siteSettingModel.RegisterType;
            settings.MobileVerifOpen = false;
            settings.EmailVerifOpen = false;
            settings.RegisterEmailRequired = false;
            settings.PageFoot = siteSettingModel.PageFoot;
            settings.PCBottomPic = siteSettingModel.PCBottomPic;
            switch (siteSettingModel.RegisterType)
            {
                case RegisterTypes.Mobile:
                    settings.MobileVerifOpen = true;
                    break;
                case RegisterTypes.Normal:
                    if (siteSettingModel.EmailVerifOpen == true)
                    {
                        settings.EmailVerifOpen = true;
                        settings.RegisterEmailRequired = true;
                    }
                    break;
            }

            settings.WXLogo = siteSettingModel.WXLogo;
            settings.PCLoginPic = siteSettingModel.PCLoginPic;

            Version ver = null;
            try
            {
                ver = new Version(siteSettingModel.AppVersion);
                settings.AppVersion = ver.ToString();
            }
            catch (Exception)
            {
                //throw new Himall.Core.HimallException("错误的版本号");
                settings.AppVersion = "";
            }
            try
            {
                ver = new Version(siteSettingModel.ShopAppVersion);
                settings.ShopAppVersion = ver.ToString();
            }
            catch (Exception)
            {
                //throw new Himall.Core.HimallException("错误的版本号");
                settings.AppVersion = "";
            }

            settings.AppUpdateDescription = siteSettingModel.AppUpdateDescription;
            settings.AndriodDownLoad = siteSettingModel.AndriodDownLoad;
            settings.IOSDownLoad = siteSettingModel.IOSDownLoad;

            settings.ShopAppUpdateDescription = siteSettingModel.ShopAppUpdateDescription;
            settings.ShopAndriodDownLoad = siteSettingModel.ShopAndriodDownLoad;
            settings.ShopIOSDownLoad = siteSettingModel.ShopIOSDownLoad;

            settings.CanDownload = siteSettingModel.CanDownload;
            settings.JDRegionAppKey = siteSettingModel.JDRegionAppKey;
            settings.IsConBindCellPhone = siteSettingModel.IsConBindCellPhone;
            settings.QQMapAPIKey = siteSettingModel.QQMapAPIKey;
            settings.SiteUrl = siteSettingModel.SiteUrl;
            settings.OpenErp = siteSettingModel.OpenErp;
            settings.OpenErpStock = siteSettingModel.OpenErpStock;
            settings.ErpAppkey = siteSettingModel.ErpAppkey;
            settings.ErpAppsecret = siteSettingModel.ErpAppsecret;
            settings.ErpSid = siteSettingModel.ErpSid;
            settings.ErpStoreNumber = siteSettingModel.ErpStoreNumber;
            settings.ErpUrl = siteSettingModel.ErpUrl;
            settings.ErpPlateId = siteSettingModel.ErpPlateId;
            SiteSettingApplication.SaveChanges();
            Result result = new Result();
            result.success = true;
            return Json(result);
        }
        [HttpPost]
        public ActionResult UploadApkFile()
        {
            string strResult = "NoFile";
            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];
                if (file.ContentLength == 0)
                {
                    strResult = "文件长度为0,格式异常。";
                }
                else
                {
                    string filename = file.FileName;// + Path.GetExtension(file.FileName);
                    if (!CheckApkFileType(filename))
                    {
                        return Content("上传的文件格式不正确", "text/html");
                    }

                    string DirUrl = Server.MapPath("~/app/");
                    if (!System.IO.Directory.Exists(DirUrl))      //检测文件夹是否存在，不存在则创建
                    {
                        System.IO.Directory.CreateDirectory(DirUrl);
                    }
                    //var curhttp = System.Web.HttpContext.Current;
                    string url = CurrentUrlHelper.CurrentUrlNoPort();
                    string strfile = url + "/app/" + filename;
                    if (Core.HimallIO.GetHimallIO().GetType().FullName.Equals("Himall.Strategy.OSS"))
                    {
                        strfile = HimallIO.GetRomoteImagePath("/app/" + filename);
                    }
                    try
                    {
                        var opcount = Core.Cache.Get<int>(CacheKeyCollection.UserImportOpCount);
                        if (opcount == 0)
                        {
                            Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, 1);
                        }
                        else
                        {
                            Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, opcount + 1);
                        }
                        //file.SaveAs(Path.Combine(DirUrl, filename));
                        string fname = "/app/" + filename;
                        Core.HimallIO.CreateFile(fname, file.InputStream, FileCreateType.Create);
                    }
                    catch (Exception ex)
                    {
                        var opcount = Core.Cache.Get<int>(CacheKeyCollection.UserImportOpCount);
                        if (opcount != 0)
                        {
                            Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, opcount - 1);
                        }
                        strfile = "Error";
                        Log.Error(ex);
                    }
                    strResult = strfile;
                }
            }
            return Content(strResult, "text/html");
        }
        private bool CheckApkFileType(string filename)
        {
            var fileExtension = Path.GetExtension(filename).ToLower();
            return AllowApkFileExtensions.Select(x => x.ToLower()).Contains(fileExtension);
        }

    }
}