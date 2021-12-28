using Himall.Application;
using Himall.Core;
using Himall.Service;
using Himall.Web.Areas.SellerAdmin.Models;
using Himall.Web.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class CustomerServiceController : BaseSellerController
    {
        private CustomerCustomerService _CustomerCustomerService;
        private ShopOpenApiService _iShopOpenApiService;
        private static readonly string DefaultUserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36";
        public CustomerServiceController(CustomerCustomerService CustomerCustomerService, ShopOpenApiService ShopOpenApiService)
        {
            _CustomerCustomerService = CustomerCustomerService;
            _iShopOpenApiService = ShopOpenApiService;
        }

        // GET: SellerAdmin/CustomerService
        public ActionResult Management()
        {
            var customerServices = _CustomerCustomerService.GetCustomerService(CurrentSellerManager.ShopId).OrderByDescending(item => item.Id).ToArray();
            var model = new CustomerServiceManagementViewModel();
            model.CustomerServices = customerServices.Select(
                item => new CustomerServiceModel()
                {
                    Id = item.Id,
                    Account = item.AccountCode,
                    Name = item.Name,
                    Tool = item.Tool,
                    Type = item.Type
                }).ToList();

            var mobileService = _CustomerCustomerService.GetCustomerServiceForMobile(CurrentSellerManager.ShopId);

            var hasMobileService = mobileService != null ? true : false;
            model.HasMobileService = hasMobileService;
            model.MobileService = mobileService;
            var sitesetting = SiteSettingApplication.SiteSettings;
            if (sitesetting != null)
            {
                ViewBag.IsOpenPC = sitesetting.IsOpenPC;
                ViewBag.IsOpenH5 = sitesetting.IsOpenH5;
            }
            return View(model);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult Delete(long id)
        {
            _CustomerCustomerService.Remove(CurrentSellerManager.ShopId, id);
            return Json(new { success = true });
        }

        public ActionResult Add(long? id)
        {
            var service = _CustomerCustomerService;
            Entities.CustomerServiceInfo customerServiceInfo;
            if (id.HasValue && id > 0)
                customerServiceInfo = service.GetCustomerService(CurrentSellerManager.ShopId, id.Value);
            else {
                customerServiceInfo = new Entities.CustomerServiceInfo();
     
            }
              

            var customerServiceModels = new CustomerServiceModel()
            {
                Id = customerServiceInfo.Id,
                Account = customerServiceInfo.AccountCode,
                Name = customerServiceInfo.Name,
                Tool = customerServiceInfo.Tool,
                Type = customerServiceInfo.Type
            };
            return View(customerServiceModels);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult Add(CustomerServiceModel customerServiceModel)
        {
            var service = _CustomerCustomerService;
            Entities.CustomerServiceInfo customerServiceInfo = new Entities.CustomerServiceInfo()
            {
                Id = customerServiceModel.Id,
                Type = customerServiceModel.Type.GetValueOrDefault(Entities.CustomerServiceInfo.ServiceType.PreSale),
                Tool = customerServiceModel.Tool,
                Name = customerServiceModel.Name,
                AccountCode = customerServiceModel.Account,
                ShopId = CurrentSellerManager.ShopId,
                TerminalType = Himall.Entities.CustomerServiceInfo.ServiceTerminalType.PC,
                ServerStatus = Himall.Entities.CustomerServiceInfo.ServiceStatusType.Open
            };

            if (customerServiceInfo.Id > 0)
                service.UpdateCustomerService(customerServiceInfo);
            else
                service.Create(customerServiceInfo);

            return Json(new { success = true });

        }

        public ActionResult addMobile()
        {
            var service = _CustomerCustomerService;
            Entities.CustomerServiceInfo customerServiceInfo;
            customerServiceInfo = service.GetCustomerServiceForMobile(CurrentSellerManager.ShopId);
            if (customerServiceInfo == null)
                customerServiceInfo = new Entities.CustomerServiceInfo();
            var customerServiceModels = new CustomerServiceModel()
            {
                Id = customerServiceInfo.Id,
                Account = customerServiceInfo.AccountCode,
                Name = customerServiceInfo.Name,
                Tool = customerServiceInfo.Tool,
                Type = customerServiceInfo.Type
            };
            return View(customerServiceModels);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult addMobile(CustomerServiceModel customerServiceModel)
        {
            var service = _CustomerCustomerService;
            Entities.CustomerServiceInfo customerServiceInfo = new Entities.CustomerServiceInfo()
            {
                Id = customerServiceModel.Id,
                Type = customerServiceModel.Type.GetValueOrDefault(Entities.CustomerServiceInfo.ServiceType.PreSale),
                Tool = Entities.CustomerServiceInfo.ServiceTool.QQ,
                Name = customerServiceModel.Name,
                AccountCode = customerServiceModel.Account,
                ShopId = CurrentSellerManager.ShopId,
                TerminalType = Entities.CustomerServiceInfo.ServiceTerminalType.Mobile,
                ServerStatus = Entities.CustomerServiceInfo.ServiceStatusType.Open
            };

            if (customerServiceInfo.Id > 0)
                service.UpdateCustomerService(customerServiceInfo);
            else
                service.Create(customerServiceInfo);

            return Json(new { success = true });
        }

        public ActionResult AddMeiQia(long? id)
        {
            return Add(id);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult deleteMobile()
        {
            _CustomerCustomerService.RemoveMobile(CurrentSellerManager.ShopId);
            return Json(new { success = true });
        }

        [HttpGet]
        [UnAuthorize]
        public ActionResult LoginHiChat()
        {
            var data = _iShopOpenApiService.Get(CurrentShop.Id);
            if (data == null)
            {
                data = _iShopOpenApiService.MakeOpenApi(CurrentShop.Id);
                _iShopOpenApiService.Add(data);
            }
            var client = new System.Net.Http.HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var isOpenHichat = ShopApplication.IsOpenHichat(CurrentShop.Id);
            if (!isOpenHichat)
            {
                string registerResult = "";
                try
                {
                    // 1、注册
                    var registerData = new Dictionary<string, string>();
                    registerData["appKey"] = data.AppKey;
                    registerData["appSecret"] = data.AppSecreat;
                    registerData["name"] = SiteSettings.SiteName + "-" + CurrentShop.ShopName;
                    registerData["domain"] = SiteSettings.SiteUrl;
                    var postclient = new HttpClient();
                    registerResult = postclient.PostForm("http://hichat.kuaidiantong.cn/merchantApi/OAuth/Register", registerData);
                    var obj = JObject.Parse(registerResult);
                    if (!obj["success"].Value<bool>())
                    {
                        return Content("客服平台注册失败:" + obj["msg"].Value<string>());
                    }
                    else
                    {
                        ShopApplication.SetShopHiChat(CurrentShop.Id);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error("注册HiCall结果：" + registerResult);
                    Log.Error(ex);
                }
            }

            // 2、登录

            var loginData = new Dictionary<string, string>();
            loginData["appKey"] = data.AppKey;
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1);
            loginData["timestamp"] = Convert.ToInt64(ts.TotalSeconds).ToString();
            loginData["workerKey"] = CurrentSellerManager.Id.ToString();
            loginData["name"] = CurrentSellerManager.UserName;
            loginData["isManager"] = (CurrentSellerManager.RoleId == 0).ToString();
            var sign = Core.Helper.ApiSignHelper.GetSign(loginData, data.AppSecreat);
            loginData["sign"] = sign;

           
            var queryParams = string.Join("&", loginData.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
            var jsonResult = client.GetStringAsync("https://hichat.kuaidiantong.cn/merchantApi/OAuth/Allot?" + queryParams).Result;
            var jsonObj = JObject.Parse(jsonResult);
            if (jsonObj["success"].Value<bool>())
            {
                return Redirect("https://hichat.kuaidiantong.cn/#/home?token=" + jsonObj["data"]["identifier"].Value<string>());
            }
            else
            {
                return Content("跳转客服平台失败:" + jsonObj["msg"].Value<string>());
            }

        }
    }
}