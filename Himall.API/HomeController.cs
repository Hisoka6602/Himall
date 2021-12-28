using Himall.Application;
using Himall.Core;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Results;

namespace Himall.API
{
    public class HomeController : BaseApiController
    {
        //APP首页配置共用于安卓和IOS，这里的平台类型写的为IOS，安卓调用首页接口数据时平台类型也选IOS
        public APPHome Get()
        {
            var services = CustomerServiceApplication.GetPlatformCustomerService(true, true,true,CurrentUser,PlatformType.Android);

            //获取首页弹窗广告信息
            AdvanceInfo advance = new AdvanceInfo();
            var advanceset = AdvanceApplication.GetAdvanceInfo();
            if (advanceset != null)
            {
                advance = advanceset;
                GetAppPageByType(advanceset);
            }

            APPHome appHome = new APPHome();
            appHome.success = true;
            appHome.CustomerServices = services;
            appHome.PopuActive = advance;
            return appHome;
        }

        private void GetAppPageByType(AdvanceInfo advance)
        {
            string resultlink = "";
            var poupadvance = JObject.Parse(advance.Link);
            var link=((JValue)poupadvance.Root["link"]).Value.ToString();
            var type = ((JValue)poupadvance.Root["linkType"]).Value.ToString();
            switch (type)
            {
                case "5"://限时购
                    int startidx = link.IndexOf("?productId=");
                    string id=link.Substring(startidx+11);
                    resultlink = "/m-wap/limittimebuy/detail/" + id;
                    break;
                default:
                    resultlink = link;
                    break;
            };
            advance.Link= advance.Link.Replace(link,resultlink);
        }


        /// <summary>
        /// 获取商品更新的接口
        /// </summary>
        /// <param name="productIds"></param>
        /// <returns></returns>
        public object GetViewProductsById(string productIds)
        {
            List<long> pidlist = new List<long>(Array.ConvertAll<string, long>(productIds.Split(','), s => long.Parse(s)));


            if (pidlist.Count <= 0)
            {
                throw new HimallException("请传入查询的商品编号！");
            }

            var prolist = ProductManagerApplication.GetViewProductsByIds(pidlist);

            var result = SuccessResult<dynamic>(data: prolist);
            return result;
        }


        /// <summary>
        /// 限时购列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public object GetLimitBuyViewByIds(string ids)
        {
            List<long> pidlist = new List<long>(Array.ConvertAll<string, long>(ids.Split(','), s => long.Parse(s)));


            if (pidlist.Count <= 0)
            {
                throw new HimallException("请传入查询的活动编号！");
            }

            var prolist = ProductManagerApplication.GetLimitBuyViewByIds(pidlist);

            var result = SuccessResult<dynamic>(data: prolist);
            return result;
        }


        /// <summary>
        /// 火拼团列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public object GetFightGroupViewByIds(string ids)
        {
            List<long> fids = new List<long>(Array.ConvertAll<string, long>(ids.Split(','), s => long.Parse(s)));


            if (fids.Count <= 0)
            {
                throw new HimallException("请传入查询的活动编号！");
            }

            var prolist = ProductManagerApplication.GetFightGroupViewByIds(fids);

            var result = SuccessResult<dynamic>(data: prolist);
            return result;
        }


        public JsonResult<Result<dynamic>> GetHiChatSetting(long shopId)
        {
            var shop = ShopApplication.GetShop(shopId);
            var data = ShopOpenApiApplication.Get(shopId);
            return JsonResult<dynamic>(new
            {
                AppKey = data.AppKey,
                IsOpenHiChat = shop.IsOpenHiChat
            });
        }

        public object GetUpdateApp(string appVersion, int type)
        {
            var siteSetting = SiteSettingApplication.SiteSettings;

            if (string.IsNullOrWhiteSpace(appVersion) || (3 < type && type < 2))
            {
                return ErrorResult("版本号不能为空或者平台类型错误", 10006);
            }
            Version ver = null;
            try
            {
                ver = new Version(appVersion);
            }
            catch (Exception)
            {
                return ErrorResult("错误的版本号", 10005);
            }
            if (string.IsNullOrWhiteSpace(siteSetting.AppVersion))
            {
                siteSetting.AppVersion = "0.0.0";
            }
            var downLoadUrl = "";
            Version v1 = new Version(siteSetting.AppVersion), v2 = new Version(appVersion);
            if (v1 > v2)
            {
                if (type == (int)PlatformType.IOS)
                {
                    if (string.IsNullOrWhiteSpace(siteSetting.IOSDownLoad))
                    {
                        return ErrorResult("站点未设置IOS下载地址", 10004);
                    }
                    downLoadUrl = siteSetting.IOSDownLoad;
                }
                if (type == (int)PlatformType.Android)
                {
                    if (string.IsNullOrWhiteSpace(siteSetting.AndriodDownLoad))
                    {
                        return ErrorResult("站点未设置Andriod下载地址", 10003);
                    }
                    string str = siteSetting.AndriodDownLoad.Substring(siteSetting.AndriodDownLoad.LastIndexOf("/"), siteSetting.AndriodDownLoad.Length - siteSetting.AndriodDownLoad.LastIndexOf("/"));
                    var curProjRootPath = System.Web.Hosting.HostingEnvironment.MapPath("~/app") + str;
                    if (!File.Exists(curProjRootPath))
                    {
                        return ErrorResult("站点未上传app安装包", 10002);
                    }
                    downLoadUrl = siteSetting.AndriodDownLoad;
                }
            }
            else
            {
                return ErrorResult("当前为最新版本", 10001);
            }
            dynamic result = SuccessResult();
            result.code = 10000;
            result.DownLoadUrl = downLoadUrl;
            result.Description = siteSetting.AppUpdateDescription;

            return result;
        }

        /// <summary>
        /// 获取App引导页图片
        /// </summary>
        /// <returns></returns>
        public List<Himall.DTO.SlideAdModel> GetAppGuidePages()
        {
            var result = SlideApplication.GetGuidePages();
            foreach (var item in result)
            {
                item.ImageUrl = HimallIO.GetRomoteImagePath(item.ImageUrl);
            }
            if (result == null)
            {
                result = new List<DTO.SlideAdModel>();
            }
            return result;
        }

        /// <summary>
        /// app关于我们
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object GetAboutUs(int type = 2)
        {
            var agreementTypes = (Entities.AgreementInfo.AgreementTypes)type;
            var appModel = SystemAgreementApplication.GetAgreement(agreementTypes);
            var content = string.Empty;
            if (appModel != null)
                content = appModel.AgreementContent.Replace("src=\"/Storage/", "src=\"" + Core.HimallIO.GetRomoteImagePath("/Storage/") + "/");
            return SuccessResult(content);
        }
    }
}
