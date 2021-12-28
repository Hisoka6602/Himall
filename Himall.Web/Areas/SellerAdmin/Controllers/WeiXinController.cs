using Himall.Core;
using Himall.Service;

using Himall.Web.Areas.SellerAdmin.Models;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    [H5AuthorizationAttribute]
    public class WeiXinController : BaseSellerController
    {
        private VShopService _VShopService;
        private WeixinMenuService _WeixinMenuService;
        private MobileHomeTopicService _iMobileHomeTopicService;
        private TopicService _iTopicService;
        public WeiXinController(VShopService VShopService,
            WeixinMenuService WeixinMenuService,
            MobileHomeTopicService MobileHomeTopicService,
            TopicService TopicService)
          
    {
        _VShopService = VShopService;
        _WeixinMenuService = WeixinMenuService;
        _iMobileHomeTopicService = MobileHomeTopicService;
        _iTopicService = TopicService;

    }
            

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult BasicVShopSettings()
        {
            var vshopSetting = _VShopService.GetVShopSetting(CurrentSellerManager.ShopId);
            if (vshopSetting == null)
                vshopSetting = new Entities.WXshopInfo();
            if (string.IsNullOrEmpty(vshopSetting.Token))
            {
                vshopSetting.Token = CreateKey(8);

            }
            ViewBag.Url = String.Format("{0}/m-Weixin/WXApi/{1}",CurrentUrlHelper.CurrentUrlNoPort(),CurrentSellerManager.ShopId);
            ViewBag.VShop = _VShopService.GetVShopByShopId(CurrentSellerManager.ShopId);
            ViewBag.ShopId = CurrentSellerManager.ShopId;
            return View(vshopSetting);
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
        public JsonResult SaveVShopSettings(string weixinAppId, string weixinAppSecret, string weixinfollowUrl=null,string weixiToken=null)
        {
            Result result = new Result();
            var vshopSetting = new Entities.WXshopInfo();
            vshopSetting.ShopId = CurrentSellerManager.ShopId;
            vshopSetting.AppId = weixinAppId;
            vshopSetting.AppSecret = weixinAppSecret;
            vshopSetting.FollowUrl = weixinfollowUrl;
            vshopSetting.Token = weixiToken;
            _VShopService.SaveVShopSetting(vshopSetting);
            result.success = true;
            return Json(result);
        }

        public ActionResult MenuManage()
        {
            List<MenuManageModel> listMenuManage = new List<MenuManageModel>();
            var menuManage = _WeixinMenuService.GetMainMenu(CurrentSellerManager.ShopId);
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
            ViewBag.ShopId = CurrentSellerManager.ShopId;
            ViewBag.VShop = _VShopService.GetVShopByShopId(CurrentSellerManager.ShopId);
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
        public JsonResult GetMenu(long? menuId)
        {
            var menu = new Entities.MenuInfo();
            if(menuId.HasValue)
                menu= _WeixinMenuService.GetMenu(menuId.Value);
            return Json(new { success = true, title = menu.Title, urlType = menu.UrlType, url = menu.Url, parentId=menu.ParentId });
        }

        [HttpPost]
        public JsonResult RequestToWeixin()
        {
            Result result = new Result();
            _WeixinMenuService.ConsistentToWeixin(CurrentSellerManager.ShopId);
            result.success = true;
            return Json(result);
        }

        //[SubmitParameter(Order=0)]
        //[ValidateInput(false)]
        [HttpPost]
        public JsonResult AddMenu(string title, string url, string parentId, int urlType, long? menuId)
        {
            var vshopInfo = _VShopService.GetVShopByShopId(CurrentSellerManager.ShopId);
            short depth;
            if (parentId == "0")
                depth = 1;
            else
                depth = 2;
            var curUrl = CurrentUrlHelper.CurrentUrlNoPort();
            switch (urlType)
            {
                case 0:
                    url = string.Empty;
                    break;
                case 1:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/vshop/detail/"+vshopInfo.Id+"?shop="+CurrentSellerManager.ShopId;
                    break;
                case 2:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/vshop/category?vshopid="+vshopInfo.Id+"&shop=" + CurrentSellerManager.ShopId;
                    break;
                case 3:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/cart/cart?shop=" + CurrentSellerManager.ShopId;
                    break;
                case 4:
                    url = curUrl + "/m-" + PlatformType.WeiXin.ToString() + "/member/center?shop=" + CurrentSellerManager.ShopId;
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
                ShopId = CurrentSellerManager.ShopId,
                FullIdPath = "1",
                Sequence = 1,
                UrlType = (Entities.MenuInfo.UrlTypes)urlType
            };
            if (menuId.HasValue)
            {
                menu.Id = menuId.Value;
                _WeixinMenuService.UpdateMenu(menu);
            }
            else
                _WeixinMenuService.AddMenu(menu);
            result.success = true;
            return Json(result);
        }
    }
}