﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class SellerPublicController : BaseSellerController
    {
        [ChildActionOnly]
        public ActionResult Top()
        {
            var t = ConfigurationManager.AppSettings["IsInstalled"];
            if (!(null == t || bool.Parse(t)))
            {
                return RedirectToAction("Agreement", "Installer", new { area = "Web" });
            }
            var setting = SiteSettingApplication.SiteSettings;
            if (CurrentSellerManager != null)
            {
                var shopInfo = ShopApplication.GetShopInfo(CurrentSellerManager.ShopId);
                ViewBag.IsSellerAdmin = shopInfo.IsSelf;
                ViewBag.ShopId = CurrentSellerManager.ShopId;
                ViewBag.Name = CurrentSellerManager.UserName;
                ViewBag.SiteName = setting.SiteName;
                ViewBag.IsOpenPC = setting.IsOpenPC;
                ViewBag.Logo = HimallIO.GetImagePath(SiteSettingApplication.SiteSettings.MemberLogo);
                ViewBag.EndDate = shopInfo.EndDate.ToString("yyyy-MM-dd");
                var cache = CacheKeyCollection.isPromptKey(CurrentSellerManager.ShopId);
                var cacheCode = Core.Cache.Get<string>(cache);
                if (string.IsNullOrEmpty(cacheCode))
                {
                    Core.Cache.Insert(cache, "0", DateTime.Parse(DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")));//一天只提醒一次
                    ViewBag.isPrompt = shopInfo.EndDate < DateTime.Now.AddDays(15) ? 1 : 0;//到期前15天提示
                }
                else
                {
                    ViewBag.isPrompt = 0;
                }
            }
            return View(CurrentSellerManager);
        }

        [ChildActionOnly]
        public ActionResult Bottom()
        {

            ViewBag.Rights = string.Join(",", CurrentSellerManager.SellerPrivileges.Select(a => (int)a).OrderBy(a => a));

            return View();
        }
    }
}