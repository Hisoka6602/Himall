using Himall.Service;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using Himall.Core;
using System.IO;
using Himall.Application;
using Himall.Entities;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class WeiXinController : BaseAdminController
    {
        private SlideAdsService _iSlideAdsService;
        private WeixinMenuService _WeixinMenuService;
        private MobileHomeTopicService _iMobileHomeTopicService;
        private TopicService _iTopicService;
        private TemplateSettingsService _iTemplateSettingsService;
        private WXMsgTemplateService _WXMsgTemplateService;
        public WeiXinController(SlideAdsService SlideAdsService,
            WeixinMenuService WeixinMenuService,
            MobileHomeTopicService MobileHomeTopicService,
            TopicService TopicService,
            TemplateSettingsService TemplateSettingsService,
            WXMsgTemplateService WXMsgTemplateService
            )
        {
            _WeixinMenuService = WeixinMenuService;
            _iMobileHomeTopicService = MobileHomeTopicService;
            _iTopicService = TopicService;
            _iSlideAdsService = SlideAdsService;
            _iTemplateSettingsService = TemplateSettingsService;
            _WXMsgTemplateService = WXMsgTemplateService;
        }

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult AutoReplay()
        {
            return View();
        }
        [HttpPost]
        public JsonResult PostAutoReplyList(int page, int rows)
        {
            var data = WeixinAutoReplyApplication.GetPage(page, rows);
            return Json(new { rows = data.Models.ToList(), total = data.Total });
        }
        [HttpPost]
        public JsonResult GetAutoReplayById(int Id)
        {
            var result = new Result();
            var data = WeixinAutoReplyApplication.GetAutoReplyById(Id);
            result.success = true;
            result.data = data;
            return Json(result);
        }
        public JsonResult ModAutoReplay(Entities.AutoReplyInfo item)
        {
            var result = new Result();
            WeixinAutoReplyApplication.ModAutoReply(item);
            result.success = true;
            result.msg = "规则保存成功！";
            return Json(result);
        }
        public JsonResult DelAutoReplay(Entities.AutoReplyInfo item)
        {
            var result = new Result();
            WeixinAutoReplyApplication.DeleteAutoReply(item);
            result.success = true;
            result.msg = "规则删除成功！";
            return Json(result);
        }
        // GET: Admin/WeiXin

        public ActionResult FocusReplay()
        {
            var item = WeixinAutoReplyApplication.GetAutoReplyByKey(CommonModel.ReplyType.Follow);
            if (item == null)
            {
                item = new Entities.AutoReplyInfo();
                item.TextReply = "欢迎关注";
            }
            return View(item);
        }
        public ActionResult NewsReplay()
        {
            var item = WeixinAutoReplyApplication.GetAutoReplyByKey(CommonModel.ReplyType.Msg);
            if (item == null)
            {
                item = new Entities.AutoReplyInfo();
                item.TextReply = "欢迎关注";
            }
            return View(item);
        }

        [H5Authorization]
        public ActionResult BasicSettings()
        {
            var settings = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrEmpty(settings.WeixinToken))
            {
                settings.WeixinToken = CreateKey(8);
                SiteSettingApplication.SaveChanges();
            }
            var siteSettingMode = new SiteSettingModel()
            {
                WeixinAppId = string.IsNullOrEmpty(settings.WeixinAppId) ? string.Empty : settings.WeixinAppId.Trim(),
                WeixinAppSecret = string.IsNullOrEmpty(settings.WeixinAppSecret) ? string.Empty : settings.WeixinAppSecret.Trim(),
                WeixinToKen = settings.WeixinToken.Trim()
            };
            ViewBag.Url = String.Format("{0}/m-Weixin/WXApi", CurrentUrlHelper.CurrentUrlNoPort());
            //TODO:演示站处理
            //如果是演示站，支付方式参数做特别处理
            if (DemoAuthorityHelper.IsDemo())
            {
                siteSettingMode.WeixinAppId = "*".PadRight(siteSettingMode.WeixinAppId.Length, '*');
                siteSettingMode.WeixinAppSecret = "*".PadRight(siteSettingMode.WeixinAppSecret.Length, '*');
                ViewBag.isDemo = true;
            }
            return View(siteSettingMode);
        }

        [UnAuthorize]
        [HttpPost]
        public JsonResult GetQrcode()
        {
            Result result = new Result();
            result.success = false;
            try
            {
                Himall.Service.Weixin.WXHelper wxhelper = new Himall.Service.Weixin.WXHelper();
                //var ticket = Service.Weixin.WXHelper.GetQRCodeTicket(SiteSettings.WeixinAppId, SiteSettings.WeixinAppSecret);
                var ticket = wxhelper.GetQRCodeTicket(SiteSettings.WeixinAppId, SiteSettings.WeixinAppSecret);
                result.data = "https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + Url.Encode(ticket);
                result.success = true;
            }
            catch (Exception ex)
            {
                result.msg = ex.Message;
                Log.Error("后台微信端设置页, 微信公众号配置错误:", ex);
            }
            return Json(result);
        }

        private string CreateKey(int len)
        {
            byte[] bytes = new byte[len];
            new RNGCryptoServiceProvider().GetBytes(bytes);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(string.Format("{0:X2}", bytes[i]));
            }
            return sb.ToString();
        }

        [UnAuthorize]
        [HttpPost]
        public JsonResult SaveWeiXinSettings(string weixinAppId, string WeixinAppSecret)
        {
            //TODO:演示站处理
            if (DemoAuthorityHelper.IsDemo())
            {
                return Json(new { success = false, msg = "演示站点自动隐藏此参数，且不能保存！" });
            }
            Result result = new Result();
            var settings = SiteSettingApplication.SiteSettings;
            settings.WeixinAppId = weixinAppId.Trim();
            settings.WeixinAppSecret = WeixinAppSecret.Trim();
            SiteSettingApplication.SaveChanges();
            result.success = true;
            return Json(result);
        }

        [H5AuthorizationAttribute]
        public ActionResult MenuManage()
        {
            List<MenuManageModel> listMenuManage = new List<MenuManageModel>();
            var menuManage = _WeixinMenuService.GetMainMenu(CurrentManager.ShopId);
            foreach (var item in menuManage)
            {
                MenuManageModel model = new MenuManageModel()
                {
                    ID = item.Id,
                    TopMenuName = item.Title,
                    SubMenu = _WeixinMenuService.GetMenuByParentId(item.Id),
                    URL = item.Url,
                    LinkType = item.UrlType
                };
                listMenuManage.Add(model);
            }
            ViewBag.IsOpenMallSmallProg = SiteSettingApplication.SiteSettings.IsOpenMallSmallProg;//是否开通小程序
            return View(listMenuManage);
        }

        [HttpPost]
        public JsonResult DeleteMenu(int menuId)
        {
            Result result = new Result();
            _WeixinMenuService.DeleteMenu(menuId);
            result.success = true;
            return Json(result);
        }

        [HttpPost]
        public JsonResult RequestToWeixin()
        {
            Result result = new Result();
            _WeixinMenuService.ConsistentToWeixin(CurrentManager.ShopId);
            result.success = true;
            return Json(result);
        }

        [HttpPost]
        public JsonResult AddMenu(string title, string url, string parentId, int urlType)
        {
            short depth;
            if (parentId == "0")
                depth = 1;
            else
                depth = 2;
            var curUrl = CurrentUrlHelper.CurrentUrlNoPort();
            switch (urlType)
            {
                case 1:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/";
                    break;
                case 2:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/vshop";
                    break;
                case 3:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/category/Index";
                    break;
                case 4:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/member/center";
                    break;
                case 5:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/cart/cart";
                    break;
                default:
                    break;
            }
            if ((!string.IsNullOrEmpty(url)) && (!(url.ToLower().Contains("http://") || url.ToLower().Contains("https://"))))
                throw new Himall.Core.HimallException("链接必须以http://开头");
            Result result = new Result();
            var menu = new Entities.MenuInfo()
            {
                Title = title,
                Url = url,
                ParentId = Convert.ToInt64(parentId),
                Platform = PlatformType.WeiXin,
                Depth = depth,
                ShopId = CurrentManager.ShopId,
                FullIdPath = "1",
                Sequence = 1,
                UrlType = (Entities.MenuInfo.UrlTypes)urlType
            };
            _WeixinMenuService.AddMenu(menu);
            result.success = true;
            return Json(result);
        }

        public ActionResult EditMenu(long menuId)
        {

            var menu = _WeixinMenuService.GetMenu(menuId);
            var menuMode = new MenuManageModel()
            {
                ID = menu.Id,
                TopMenuName = menu.Title,
                URL = menu.Url,
                LinkType = menu.UrlType
            };
            if (menu.ParentId != 0)
            {
                ViewBag.parentName = _WeixinMenuService.GetMenu(menu.ParentId).Title;
                ViewBag.parentId = menu.ParentId;
            }
            else
                ViewBag.parentId = 0;
            ViewBag.IsOpenMallSmallProg = SiteSettingApplication.SiteSettings.IsOpenMallSmallProg;//是否开通小程序
            return View(menuMode);
        }

        [HttpPost]


        public ActionResult WeiXinReplay1()
        {
            return View();
        }
        public JsonResult UpdateMenu(string menuId, string menuName, int urlType, string url, string parentId)
        {
            var curUrl = CurrentUrlHelper.CurrentUrlNoPort();
            switch (urlType)
            {
                case 1:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/";
                    break;
                case 2:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/vshop";
                    break;
                case 3:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/category/Index";
                    break;
                case 4:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/member/center";
                    break;
                case 5:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/cart/cart";
                    break;
                default:
                    break;
            }
            if ((!string.IsNullOrEmpty(url)) && (!(url.ToLower().Contains("http://") || url.ToLower().Contains("https://"))))
                throw new Himall.Core.HimallException("链接必须以http://开头");

            Result result = new Result();
            var menuInfo = new Entities.MenuInfo();
            menuInfo.Id = Convert.ToInt64(menuId);
            menuInfo.Title = menuName;
            menuInfo.UrlType = (Entities.MenuInfo.UrlTypes)urlType;
            menuInfo.Url = url;
            menuInfo.ParentId = Convert.ToInt64(parentId);
            _WeixinMenuService.UpdateMenu(menuInfo);
            result.success = true;
            return Json(result);
        }

        public ActionResult TopicSettings()
        {
            var homeTopicInfos = _iMobileHomeTopicService.GetMobileHomeTopicInfos(PlatformType.WeiXin).ToArray();
            var topicService = _iTopicService;
            var models = homeTopicInfos.Select(item =>
            {
                var topic = TopicApplication.GetTopic(item.TopicId);
                return new TopicModel()
                {
                    FrontCoverImage = topic.FrontCoverImage,
                    Id = item.Id,
                    Name = topic.Name,
                    Tags = topic.Tags,
                    Sequence = item.Sequence
                };
            });
            return View(models);
        }

        [HttpPost]
        public JsonResult ChooseTopic(string frontCoverImage, long topicId)
        {
            var topicService = _iTopicService;
            var topic = TopicApplication.GetTopic(topicId);
            topic.FrontCoverImage = frontCoverImage;
            topicService.UpdateTopicInfo(topic);
            _iMobileHomeTopicService.AddMobileHomeTopic(topicId, 0, PlatformType.WeiXin, frontCoverImage);
            return Json(new { success = true });
        }


        [HttpPost]
        public JsonResult RemoveChoseTopic(long id)
        {
            _iMobileHomeTopicService.Delete(id);
            return Json(new { success = true });
        }


        [HttpPost]
        public JsonResult UpdateSequence(long id, int sequence)
        {
            _iMobileHomeTopicService.SetSequence(id, sequence);
            return Json(new { success = true });
        }


        public ActionResult SlideImageSettings()
        {
            var slideImageSettings = _iSlideAdsService.GetSlidAds(0, Entities.SlideAdInfo.SlideAdType.WeixinHome).ToArray();
            var slideImageService = _iSlideAdsService;
            var models = slideImageSettings.Select(item =>
            {
                var slideImage = slideImageService.GetSlidAd(0, item.Id);
                return new SlideAdModel()
                {
                    ID = item.Id,
                    imgUrl = item.ImageUrl,
                    DisplaySequence = item.DisplaySequence,
                    Url = item.Url,
                    Description = item.Description
                };
            });
            return View(models);
        }

        public JsonResult AddSlideImage(string id, string description, string imageUrl, string url)
        {
            Result result = new Result();
            var slideAdInfo = new Entities.SlideAdInfo();
            slideAdInfo.Id = Convert.ToInt64(id);
            slideAdInfo.ImageUrl = imageUrl;
            slideAdInfo.TypeId = Entities.SlideAdInfo.SlideAdType.WeixinHome;
            slideAdInfo.Url = url;
            slideAdInfo.Description = description;
            slideAdInfo.ShopId = 0;
            if (slideAdInfo.Id > 0)
                _iSlideAdsService.UpdateSlidAd(slideAdInfo);
            else
                _iSlideAdsService.AddSlidAd(slideAdInfo);
            result.success = true;
            return Json(result);
        }

        [HttpPost]
        public JsonResult DeleteSlideImage(string id)
        {
            Result result = new Result();
            _iSlideAdsService.DeleteSlidAd(0, Convert.ToInt64(id));
            result.success = true;
            return Json(result);
        }

        public ActionResult SaveSlideImage(long id = 0)
        {
            Entities.SlideAdInfo slideImageIfo;
            if (id > 0)
                slideImageIfo = _iSlideAdsService.GetSlidAd(0, id);
            else
                slideImageIfo = new Entities.SlideAdInfo();
            SlideAdModel model = new SlideAdModel()
            {
                Description = slideImageIfo.Description,
                imgUrl = Core.HimallIO.GetImagePath(slideImageIfo.ImageUrl),
                Url = slideImageIfo.Url,
                ID = id
            };
            return View(model);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult SlideImageChangeSequence(int oriRowNumber, int newRowNumber)
        {
            _iSlideAdsService.UpdateWeixinSlideSequence(0, oriRowNumber, newRowNumber, Entities.SlideAdInfo.SlideAdType.WeixinHome);
            return Json(new { success = true });
        }
        public ActionResult ProductSettings()
        {
            return View();
        }

        public JsonResult GetSlideImages()
        {
            //轮播图
            var slideImageSettings = _iSlideAdsService.GetSlidAds(0, Entities.SlideAdInfo.SlideAdType.WeixinHome).ToArray();
            var slideImageService = _iSlideAdsService;
            var slideModel = slideImageSettings.Select(item =>
            {
                var slideImage = slideImageService.GetSlidAd(0, item.Id);
                return new
                {
                    id = item.Id,
                    imgUrl = Core.HimallIO.GetImagePath(item.ImageUrl),
                    displaySequence = item.DisplaySequence,
                    url = item.Url,
                    description = item.Description
                };
            });
            return Json(new { rows = slideModel, total = 100 }, JsonRequestBehavior.AllowGet);
        }

        [H5AuthorizationAttribute]
        public ActionResult WXMsgTemplateManage(string mediaid)
        {
            IEnumerable<Entities.WXMaterialInfo> result = new List<Entities.WXMaterialInfo>() {
                new Entities.WXMaterialInfo{}
            };
            if (!string.IsNullOrWhiteSpace(mediaid))
            {
                result = _WXMsgTemplateService.GetMedia(mediaid, this.SiteSettings.WeixinAppId, SiteSettings.WeixinAppSecret);
            }
            return View(result);
        }
        [ValidateInput(false)]
        public JsonResult AddWXMsgTemplate(string mediaid, string data)
        {
            IEnumerable<Entities.WXMaterialInfo> template = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<Entities.WXMaterialInfo>>(data);
            if (string.IsNullOrWhiteSpace(mediaid))
            {
                var result = _WXMsgTemplateService.Add(template, SiteSettings.WeixinAppId, SiteSettings.WeixinAppSecret);
                if (string.IsNullOrEmpty(result.media_id))
                {
                    return Json(new { success = false, msg = result.errmsg });
                }
            }
            else
            {
                var updateResult = _WXMsgTemplateService.UpdateMedia(mediaid, template, SiteSettings.WeixinAppId, SiteSettings.WeixinAppSecret);
                var result = updateResult.Where(e => !string.IsNullOrWhiteSpace(e.errmsg)).FirstOrDefault();
                if (result != null)
                {
                    if (result.errmsg == "ok")
                    {
                        return Json(new { success = true, msg = result.errmsg });
                    }
                    else
                    {
                        return Json(new { success = false, msg = result.errmsg });
                    }
                }
            }

            return Json(new { success = true });
        }
        public JsonResult AddWXImageMsg(string name)
        {
            var filename = Server.MapPath(name);
            var mediaid = _WXMsgTemplateService.AddImage(filename, this.SiteSettings.WeixinAppId, this.SiteSettings.WeixinAppSecret);
            if (string.IsNullOrWhiteSpace(mediaid))
            {
                return Json(new { success = false, msg = "上传图片失败！" });
            }
            else
            {
                return Json(new { success = true, media = mediaid });
            }
        }
        public JsonResult GetWXMaterialList(int pageIdx, int pageSize)
        {
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppId))
            {
                throw new HimallException("未配置微信公众号");
            }
            var offset = (pageIdx - 1) * pageSize;
            var list = _WXMsgTemplateService.GetMediaMsgTemplateList(siteSetting.WeixinAppId, siteSetting.WeixinAppSecret, offset, pageSize);
            return Json(list);
        }
        public ActionResult GetMedia(string mediaid)
        {
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppId))
            {
                throw new HimallException("未配置微信公众号");
            }
            MemoryStream stream = new MemoryStream();
            _WXMsgTemplateService.GetMedia(mediaid, siteSetting.WeixinAppId, siteSetting.WeixinAppSecret, stream);
            return File(stream.ToArray(), "Image/png");
        }
        public ActionResult WXMsgTemplate()
        {
            return View();
        }
        public JsonResult GetMediaInfo(string mediaid)
        {
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppId))
            {
                throw new HimallException("未配置微信公众号");
            }
            if (string.IsNullOrEmpty(mediaid))
            {
                throw new HimallException("素材ID不能为空");
            }
            var result = _WXMsgTemplateService.GetMedia(mediaid, this.SiteSettings.WeixinAppId, this.SiteSettings.WeixinAppSecret);
            return Json(new { success = true, data = result });
        }
        public JsonResult DeleteMedia(string mediaid)
        {
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppId))
            {
                throw new HimallException("未配置微信公众号");
            }
            if (string.IsNullOrEmpty(mediaid))
            {
                throw new HimallException("素材ID不能为空");
            }
            var result = _WXMsgTemplateService.DeleteMedia(mediaid, this.SiteSettings.WeixinAppId, this.SiteSettings.WeixinAppSecret);
            if (string.IsNullOrEmpty(result.errmsg))
            {
                return Json(new { success = false, msg = result.errmsg });
            }
            else
            {
                return Json(new { success = true });
            }
        }

        public ActionResult NearShopBranchSetting()
        {
            var shopBranchTagInfos = ShopBranchApplication.GetAllShopBranchTagInfos();
            List<SelectListItem> tagList = new List<SelectListItem>{new SelectListItem
            {
                Selected = true,
                Value = 0.ToString(),
                Text = "请选择..."
            }};
            foreach (var item in shopBranchTagInfos)
            {
                tagList.Add(new SelectListItem
                {
                    Selected = false,
                    Value = item.Id.ToString(),
                    Text = item.Title
                });
            }
            ViewBag.ShopBranchTags = tagList;
            //轮播图
            ViewBag.imageAds = _iSlideAdsService.GetSlidAdsOrInit(0, Entities.SlideAdInfo.SlideAdType.NearShopBranchSpecial).ToList();
            //门店授权
            ViewBag.IsOpenStore = SiteSettingApplication.SiteSettings.IsOpenStore;
            return View(SiteSettingApplication.SiteSettings);
        }

        #region 导航栏设置
        [H5AuthorizationAttribute]
        /// <summary>
        /// 页面初始
        /// </summary>
        /// <returns></returns>
        public ActionResult WXMobileFootMenu()
        {
            ViewBag.IsOpenStore = SiteSettingApplication.SiteSettings.IsOpenStore;
            return View();
        }

        public JsonResult GetFootMenus()
        {
            var FootMenus = WXSmallProgramApplication.GetMobileFootMenuInfos(MenuInfo.MenuType.WeiXin).ToArray();
            if (FootMenus.Length > 0)
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
        /// 新增导航栏微信
        /// </summary>
        /// <param name="footMenuInfo"></param>
        /// <returns></returns>
        public JsonResult AddFootMenu(MobileFootMenuInfo footMenuInfo)
        {
            footMenuInfo.Type = MenuInfo.MenuType.WeiXin;
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

    }
}