using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Areas.Admin.Models.Product;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.Text;
using System.IO;
using Himall.Entities;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class WXSmallProgramController : BaseAdminController
    {
        private WXSmallProgramService _WXSmallProgramService;
        private WXMsgTemplateService _WXMsgTemplateService;

        public WXSmallProgramController(
            WXSmallProgramService WXSmallProgramService,  WXMsgTemplateService WXMsgTemplateService)
        {
            _WXSmallProgramService = WXSmallProgramService;
            _WXMsgTemplateService = WXMsgTemplateService;
        }
        public ActionResult HomePageSetting()
        {
            var model = new VTemplateEditModel();
            model.ClientType = VTemplateClientTypes.WXSmallProgram;
            model.Name = "smallprog";
            
            //门店授权
         
            ViewBag.IsOpenStore =SiteSettingApplication.SiteSettings.IsOpenStore;
            VTemplateHelper.DownloadTemplate("", model.ClientType, 0);
            return View(model);
        }

        public ActionResult ProductSetting()
        {
            return View();
        }

        /// <summary>
        /// 设置小程序商品
        /// </summary>
        /// <param name="productIds">商品ID，用','号隔开</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AddWXSmallProducts(string productIds)
        {
            WXSmallProgramApplication.SetWXSmallProducts(productIds);
            return Json(new { success = true });
        }

        /// <summary>
        /// 查询已绑定的商品信息
        /// </summary>
        /// <param name="page">分页页码</param>
        /// <param name="rows">每页行数</param>
        /// <param name="keyWords">搜索关键字</param>
        /// <param name="categoryId">3级分类</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetWXSmallProducts(int page, int rows, string keyWords, string shopName, long? categoryId = null)
        {
            ProductQuery productQuery = new ProductQuery()
            {
                CategoryId = categoryId,
                KeyWords = keyWords,
                ShopName = shopName
            };
            var datasql = _WXSmallProgramService.GetWXSmallProducts(page, rows,
                productQuery);

            var products = datasql.Models.ToArray().Select(item => new ProductModel()
            {
                name = item.ProductName,
                brandName = item.BrandName,
                id = item.Id,
                imgUrl = item.GetImage(ImageSize.Size_50),
                price = item.MinSalePrice,
                state = item.ShowProductState,
                productCode = item.ProductCode,
                shopName = item.ShopName
            });
            var dataGrid = new DataGridModel<ProductModel>() { rows = products, total = datasql.Total };
            return Json(dataGrid);
        }

        /// <summary>
        /// 删除对应商品
        /// </summary>
        /// <param name="Id">设置ID</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult DeleteWXSmallProductById(long Id)
        {
            _WXSmallProgramService.DeleteWXSmallProductById(Id);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult DeleteList(string ids)
        {
            var list = ids.Split(',').Select(p => long.Parse(p)).ToList();
            _WXSmallProgramService.DeleteWXSmallProductByIds(list);
            return Json(new Result() { success = true, msg = "批量删除成功！" });
        }
        #region 微信模版
        public ActionResult EditWXMessage()
        {
            var messagePlugins = PluginsManagement.GetPlugins<IMessagePlugin>();
            var data = messagePlugins.Select(item =>
            {
                dynamic model = new ExpandoObject();
                model.name = item.PluginInfo.DisplayName;
                model.pluginId = item.PluginInfo.PluginId;
                model.enable = item.PluginInfo.Enable;
                model.status = item.Biz.GetAllStatus();
                return model;
            }
                );

            var siteSetting = SiteSettingApplication.SiteSettings;
            ViewBag.WeixinAppletId = siteSetting.WeixinAppletId;
            ViewBag.WeixinAppletSecret = siteSetting.WeixinAppletSecret;

            ViewBag.messagePlugins = data;

            List<Entities.WeiXinMsgTemplateInfo> wxtempllist = new List<Entities.WeiXinMsgTemplateInfo>();
            wxtempllist = _WXMsgTemplateService.GetWeiXinMsgTemplateListByApplet();
            return View(wxtempllist);
        }

        //public ActionResult EditWXO2OMessage()
        //{
        //    var messagePlugins = PluginsManagement.GetPlugins<IMessagePlugin>();
        //    var data = messagePlugins.Select(item =>
        //    {
        //        dynamic model = new ExpandoObject();
        //        model.name = item.PluginInfo.DisplayName;
        //        model.pluginId = item.PluginInfo.PluginId;
        //        model.enable = item.PluginInfo.Enable;
        //        model.status = item.Biz.GetAllStatus();
        //        return model;
        //    });
        //    var siteSetting = SiteSettingApplication.SiteSettings;
        //    ViewBag.WeixinO2OAppletId = siteSetting.WeixinO2OAppletId;
        //    ViewBag.WeixinO2OAppletSecret = siteSetting.WeixinO2OAppletSecret;
        //    ViewBag.messagePlugins = data;
        //    List<Entities.WeiXinMsgTemplateInfo> wxtempllist = new List<Entities.WeiXinMsgTemplateInfo>();
        //    wxtempllist = _WXMsgTemplateService.GetWeiXinMsgTemplateListByApplet(true);
        //    return View(wxtempllist);
        //}

        #endregion

        [HttpPost]
        [UnAuthorize]
        [ValidateInput(false)]
        public JsonResult Save(string values, string weixinAppletId, string WeixinAppletSecret)
        {
            weixinAppletId = (!string.IsNullOrEmpty(weixinAppletId) ? weixinAppletId.Trim() : weixinAppletId);
            WeixinAppletSecret = (!string.IsNullOrEmpty(WeixinAppletSecret) ? WeixinAppletSecret.Trim() : WeixinAppletSecret);
            var settings = SiteSettingApplication.SiteSettings;
            settings.WeixinAppletId = weixinAppletId;
            settings.WeixinAppletSecret = WeixinAppletSecret;
            SiteSettingApplication.SaveChanges();
            var items = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<KeyValuePair<string, string>>>(values);
            _WXMsgTemplateService.UpdateWXsmallMessage(items);

            var fileName = @"/Storage/Applet/Codes/platform.jpg";
            if (Core.HimallIO.ExistFile(fileName)) Core.HimallIO.DeleteFile(fileName);//它appid或secret变了把之前小程序二维码删掉便于生成最新二维码

            return Json(new { success = true });
        }

        [UnAuthorize]
        [HttpPost]
        public JsonResult GetWXSmallCode()
        {
            Result result = new Result();
            result.success = false;
            try
            {
                result.data = GetShopBranchAppletCode() + "?t=" + (new Random().Next());
                result.success = true;
            }
            catch (Exception ex)
            {
                result.msg = ex.Message;
                Log.Error("后台小程序消息设置页, 小程序appid或appsecret配置错误:", ex);
            }
            return Json(result);
        }

        private string GetShopBranchAppletCode()
        {
            var fileName = @"/Storage/Applet/Codes/platform.jpg";
            if (Core.HimallIO.ExistFile(fileName)) return Core.HimallIO.GetImagePath(fileName);
            Himall.Service.Weixin.WXHelper wxhelper = new Himall.Service.Weixin.WXHelper();
            var accessToken = wxhelper.GetAccessToken(SiteSettingApplication.SiteSettings.WeixinAppletId, SiteSettingApplication.SiteSettings.WeixinAppletSecret);
            var data = "{\"path\":\"pages/index/index\",\"width\":600}";
            HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create("https://api.weixin.qq.com/wxa/getwxacode?access_token=" + accessToken);  //创建url
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            byte[] load = Encoding.UTF8.GetBytes(data);
            request.ContentLength = load.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(load, 0, load.Length);
            HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            Stream s = response.GetResponseStream();
            byte[] mg = StreamToBytes(s);
            MemoryStream ms = new MemoryStream(mg);
            if (ms.Length < 1000)
                throw new Himall.Core.HimallException("二维码生成失败，appid或appsecret配置不对！");//它生成图大小肯定是没生成成功，弹出提示
            Core.HimallIO.CreateFile(fileName, ms, Core.FileCreateType.Create);
            ms.Dispose();
            return Core.HimallIO.GetImagePath(fileName);
        }

        private static byte[] StreamToBytes(Stream stream)
        {
            List<byte> bytes = new List<byte>();
            int temp = stream.ReadByte();
            while (temp != -1)
            {
                bytes.Add((byte)temp);
                temp = stream.ReadByte();
            }
            return bytes.ToArray();
        }

        #region  小程序底部导航模块
        public ActionResult SmallProMobileFootMenu() {
            ViewBag.IsOpenStore = SiteSettingApplication.SiteSettings.IsOpenStore;
            return View();
        }
        public JsonResult GetFootMenus()
        {
            var FootMenus = WXSmallProgramApplication.GetMobileFootMenuInfos(MenuInfo.MenuType.SmallProg);
            if (FootMenus!=null && FootMenus.Count > 0)
            {
                var slideModel = FootMenus.Select(item =>
                {
                    return new
                    {
                        menuid = item.Id,
                        childdata = new { },
                        type = "click",
                        name = item.Name,
                        shopmenupic = item.MenuIcon,
                        shopmenupicsel = item.MenuIconSel,
                        content = item.Url
                    };
                });
                return Json(new { status = "0", shopmenustyle = "", enableshopmenu = "True", data = slideModel });
            }
            else
            {
                return Json(new { status = " -1 " });
            }
        }

        public JsonResult GetFootMenuInfoById(string id)
        {
            if (id != "undefined")
            {
                var info = WXSmallProgramApplication.GetFootMenusById(Convert.ToInt64(id));
                if (info != null)
                {
                    var data = new
                    {
                        menuid = info.Id,
                        type = "",
                        name = info.Name,
                        shopmenupic = info.MenuIcon,
                        shopmenupicsel = info.MenuIconSel,
                        content = info.Url
                    };
                    return Json(new { status = "0", data = data });
                }
                else
                {
                    return Json(new { status = "1" });
                }
            }
            else
            {
                return Json(new { status = "1" });
            }
        }

        /// <summary>
        /// 新增导航栏小程序
        /// </summary>
        /// <param name="footMenuInfo"></param>
        /// <returns></returns>
        public JsonResult AddFootMenu(MobileFootMenuInfo footMenuInfo)
        {
            footMenuInfo.Type = MenuInfo.MenuType.SmallProg;
            WXSmallProgramApplication.AddFootMenu(footMenuInfo);
            return Json(new { status = "0" });
        }

        public JsonResult DelFootMenu(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                long mid = Convert.ToInt64(id);
                WXSmallProgramApplication.DeleteFootMenu(mid);
                return Json(new { status = "0" });
             
            }
            else
            {
                return Json(new { status = "1" });
            }
        }
        #endregion

        #region 小程序 订阅消息模块
        /// <summary>
        /// 更新小程序订阅消息模板
        /// </summary>
        /// <returns></returns>
        public JsonResult GetAppletSubscribeTmplate()
        {
            var issucess = WXMsgTemplateApplication.GetAppletSubscribeTmplate();

            return Json(new { success = issucess });
        }


        #endregion

        #region 主题颜色
        public ActionResult ThemeColors()
        {
            var mode = SiteSettings;
            return View(mode);
        }

        [HttpPost]
        public JsonResult SaveSiteSettings(SiteSettingModel siteSettingModel)
        {
            Result result = new Result();
            var settings = SiteSettingApplication.SiteSettings;
            settings.PrimaryColor = siteSettingModel.PrimaryColor;
            settings.PrimaryTxtColor = siteSettingModel.PrimaryTxtColor;
            settings.SecondaryColor = siteSettingModel.SecondaryColor;
            settings.SecondaryTxtColor = siteSettingModel.SecondaryTxtColor;
            SiteSettingApplication.SaveChanges();
            result.success = true;
            return Json(result);
        }
        #endregion
    }
}