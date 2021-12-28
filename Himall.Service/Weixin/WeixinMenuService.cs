using Himall.Core;
using Himall.DTO;
using Himall.DTO.WeiXin;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using Newtonsoft.Json;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using Senparc.Weixin.MP.Entities.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using static Himall.Entities.MenuInfo;

namespace Himall.Service
{
    public class WeixinMenuService : ServiceBase
    {
        public List<MenuInfo> GetMainMenu(long shopId)
        {
            return DbFactory.Default.Get<MenuInfo>().Where(a => a.ParentId == 0 && a.Platform == PlatformType.WeiXin && a.ShopId == shopId).ToList();
        }

        public List<MenuInfo> GetMenuByParentId(long id)
        {
            return DbFactory.Default.Get<MenuInfo>().Where(a => a.ParentId == id && a.Platform == PlatformType.WeiXin).ToList();
        }

        public MenuInfo GetMenu(long id)
        {
            return DbFactory.Default.Get<MenuInfo>().Where(a => a.Id == id && a.Platform == PlatformType.WeiXin).FirstOrDefault();
        }

        public List<MenuInfo> GetAllMenu(long shopId)
        {
            return DbFactory.Default.Get<MenuInfo>().Where(a => a.ShopId == shopId && a.Platform == PlatformType.WeiXin).ToList();
        }

        public void AddMenu(MenuInfo model)
        {
            if (model == null)
                throw new ApplicationException("微信自定义菜单的Model不能为空");
            if (model.ParentId < 0)
                throw new Himall.Core.HimallException("微信自定义菜单的上级菜单不能为负值");
            if (model.Title.Length == 0 || (model.Title.Length > 5 && model.ParentId == 0))
                throw new Himall.Core.HimallException("一级菜单的名称不能为空且在5个字符以内");
            if (model.Title.Length == 0 || (model.Title.Length > 7 && model.ParentId != 0))
                throw new Himall.Core.HimallException("二级菜单的名称不能为空且在5个字符以内");
            if ((DbFactory.Default.Get<MenuInfo>().Where(item => item.ParentId == 0 && item.ShopId == model.ShopId).Count() >= 3 && model.ParentId == 0) || (GetMenuByParentId(model.ParentId).Count() >= 5 && model.ParentId != 0))
                throw new Himall.Core.HimallException("微信自定义菜单最多允许三个一级菜单，一级菜单下最多运行5个二级菜单");
            if (model.UrlType == UrlTypes.SmallProg)
            {
                SiteSettings siteSetting = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings;
                if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppletId))
                {
                    throw new Himall.Core.HimallException("你还未配置微信小程序AppId");
                }
            }
            model.Platform = PlatformType.WeiXin;
            DbFactory.Default.Add(model);
        }

        public void UpdateMenu(MenuInfo model)
        {
            if (model.Id < 0)
                throw new Himall.Core.HimallException("微信自定义菜单的ID有误");
            if (model.ParentId < 0)
                throw new Himall.Core.HimallException("微信自定义菜单二级菜单必须指定一个一级菜单");
            if (model.Title.Length == 0 || (model.Title.Length > 5 && model.ParentId == 0))
                throw new Himall.Core.HimallException("一级菜单的名称不能为空且在5个字符以内");
            if (model.Title.Length == 0 || (model.Title.Length > 7 && model.ParentId != 0))
                throw new Himall.Core.HimallException("二级菜单的名称不能为空且在5个字符以内");
            var menu = DbFactory.Default.Get<MenuInfo>().Where(p => p.Id == model.Id).FirstOrDefault();
            if (model.ParentId == 0 && GetMenuByParentId(model.Id).Count() > 0 && model.UrlType != MenuInfo.UrlTypes.Nothing)
                throw new Himall.Core.HimallException("一级菜单下有二级菜单，不允许绑定链接");
            menu.ParentId = model.ParentId;
            menu.Title = model.Title;
            menu.Url = model.Url;
            menu.UrlType = model.UrlType;
            menu.Platform = PlatformType.WeiXin;
            DbFactory.Default.Update(menu);
        }

        public void DeleteMenu(long id)
        {
            DbFactory.Default.Del<MenuInfo>().Where(a => a.Id == id || a.ParentId == id).Succeed();
        }

        public void ConsistentToWeixin(long shopId)
        {
            string appId = string.Empty;
            string appSecret = string.Empty;
            var siteSetting = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings;
            if (shopId == 0)
            {
                if (String.IsNullOrEmpty(siteSetting.WeixinAppId) || String.IsNullOrEmpty(siteSetting.WeixinAppSecret))
                    throw new Himall.Core.HimallException("您的服务号配置存在问题，请您先检查配置！");
                appId = siteSetting.WeixinAppId;
                appSecret = siteSetting.WeixinAppSecret;
            }
            if (shopId > 0)
            {
                var vshopSetting = ServiceProvider.Instance<VShopService>.Create.GetVShopSetting(shopId);
                if (String.IsNullOrEmpty(vshopSetting.AppId) || String.IsNullOrEmpty(vshopSetting.AppSecret))
                    throw new Himall.Core.HimallException("您的服务号配置存在问题，请您先检查配置！");
                appId = vshopSetting.AppId;
                appSecret = vshopSetting.AppSecret;
            }
            //TODO：统一方式取Token
            string access_token = "";
            try
            {
                access_token = new WXApiService().TryGetToken(appId, appSecret);
            }
            catch (Exception ex)
            {
                Log.Error("[WXACT]appId=" + appId + ";appSecret=" + appSecret + ";" + ex.Message);
                access_token = "";
            }
            if (string.IsNullOrWhiteSpace(access_token))
            {
                //强制获取一次
                access_token = new WXApiService().TryGetToken(appId, appSecret, true);
                Log.Error("[WXACT]强制-appId=" + appId + ";appSecret=" + appSecret + ";");
            }
            if (string.IsNullOrWhiteSpace(access_token))
            {
                throw new HimallException("获取Access Token失败！");
            }
            var menus = GetAllMenu(shopId);
            if (menus == null)
                throw new HimallException("你还没有添加菜单！");
            var mainMenus = menus.Where(item => item.ParentId == 0).ToList();
            foreach (var menu in mainMenus)
            {
                if (GetMenuByParentId(menu.Id).Count() == 0 && menu.UrlType == MenuInfo.UrlTypes.Nothing)
                    throw new HimallException("你有一级菜单下没有二级菜单并且也没有绑定链接");
            }
            ButtonGroup group = new ButtonGroup();
            foreach (var top in mainMenus)
            {
                if (GetMenuByParentId(top.Id).Count() == 0)
                {
                    if (top.UrlType == UrlTypes.SmallProg)
                    {
                        if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppletId))
                        {
                            throw new HimallException("你还未配置微信小程序AppId");
                        }
                    }
                    group.button.Add(BuildMenu(top));
                }
                else
                {
                    var subButton = new SubButton()
                    {
                        name = top.Title
                    };
                    foreach (var sub in GetMenuByParentId(top.Id))
                    {
                        if (sub.UrlType == UrlTypes.SmallProg)
                        {
                            if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppletId))
                            {
                                throw new HimallException("你还未配置微信小程序AppId");
                            }
                        }
                        subButton.sub_button.Add(BuildMenu(sub));
                    }
                    group.button.Add(subButton);
                }

            }

            var resp = CommonApi.CreateMenu(access_token, group);
            if (resp.errcode != Senparc.Weixin.ReturnCode.请求成功)
            {
                Log.Debug("微信菜单同步错误,返回内容：" + resp.errmsg);
                throw new HimallException("服务号配置信息错误或没有微信自定义菜单权限，请检查配置信息以及菜单的长度。");
            }
        }

        private SingleButton BuildMenu(MenuInfo menu)
        {
            if (menu.UrlType == UrlTypes.SmallProg)
            {
                return new Himall.DTO.WeiXin.SingleMiniProgramButton
                {
                    name = menu.Title,
                    url = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteUrl,
                    type = "miniprogram",
                    appid = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.WeixinAppletId,
                    pagepath = "pages/index/index"//小程序首页
                };
            }
            return new SingleViewButton
            {
                name = (string.IsNullOrEmpty(menu.Title) ? "" : menu.Title.Trim()),
                url = (string.IsNullOrEmpty(menu.Url) ? Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteUrl : menu.Url.Trim())
            };
        }

        public List<MobileFootMenuInfo> GetFootMenus(MenuInfo.MenuType type = MenuType.WeiXin, long shopid = 0)
        {
            return DbFactory.Default.Get<MobileFootMenuInfo>().Where(t => t.ShopId == shopid && t.Type == type).ToList();
        }

        public MobileFootMenuInfo GetFootMenusById(long id)
        {
            return DbFactory.Default.Get<MobileFootMenuInfo>().Where(s => s.Id == id).FirstOrDefault();
        }
        /// <summary>
        /// 修改导航栏
        /// </summary>
        /// <param name="footmenu"></param>
        public void UpdateMobileFootMenu(MobileFootMenuInfo footmenu)
        {
            DbFactory.Default
                .Set<MobileFootMenuInfo>()
                .Set(n => n.Name, footmenu.Name)
                .Set(n => n.Url, !string.IsNullOrEmpty(footmenu.Url) ? footmenu.Url : string.Empty)
                .Set(n => n.MenuIcon, !string.IsNullOrEmpty(footmenu.MenuIcon) ? footmenu.MenuIcon : string.Empty)
                .Set(n => n.MenuIconSel, !string.IsNullOrEmpty(footmenu.MenuIconSel) ? footmenu.MenuIconSel : string.Empty)
                .Where(n => n.Id == footmenu.Id)
                .Succeed();
        }
        /// <summary>
        /// 增加导航栏
        /// </summary>
        /// <param name="footmenu"></param>
        public void AddMobileFootMenu(MobileFootMenuInfo footmenu)
        {
            DbFactory.Default.Add(footmenu);
        }

        public void DeleteFootMenu(long id, long shopid = 0)
        {
            DbFactory.Default.Del<MobileFootMenuInfo>().Where(n => n.Id == id && n.ShopId == shopid).Succeed();
        }
    }
}
