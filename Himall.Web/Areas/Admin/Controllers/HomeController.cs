using Himall.Web.Framework;
using System.Web.Mvc;
using Himall.Service;
using Himall.Core.Helper;
using System;
using Himall.Application;
using Himall.CommonModel;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using System.Collections.Generic;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class HomeController : BaseAdminController
    {
        ShopService _ShopService;
        StatisticsService _iStatisticsService;
        ManagerService _ManagerService;
        public HomeController(ShopService ShopService, StatisticsService StatisticsService, ManagerService ManagerService)
        {
            _ShopService = ShopService;
            _iStatisticsService = StatisticsService;
            _ManagerService = ManagerService;
        }

        [UnAuthorize]
        public ActionResult Index()
        {
            return RedirectToAction("Console");
        }

        [UnAuthorize]
        public ActionResult Test()
        {
            return Content("123");
        }

        [UnAuthorize]
        [HttpPost]
        public JsonResult ChangePassword(string oldpassword, string password)
        {
            if (string.IsNullOrWhiteSpace(oldpassword) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new Result() { success = false, msg = "密码不能为空！" });
            }
            var model = CurrentManager;
            var pwd = SecureHelper.MD5(SecureHelper.MD5(oldpassword) + model.PasswordSalt);
            if (pwd == model.Password)
            {
                _ManagerService.ChangePlatformManagerPassword(model.Id, password, 0);
                return Json(new Result() { success = true, msg = "修改成功" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "旧密码错误" });
            }
        }


        [UnAuthorize]
        public JsonResult CheckOldPassword(string password)
        {
            var model = CurrentManager;
            var pwd = SecureHelper.MD5(SecureHelper.MD5(password) + model.PasswordSalt);
            if (model.Password == pwd)
            {
                return Json(new Result() { success = true });
            }
            return Json(new Result() { success = false });
        }

        [UnAuthorize]
        public ActionResult Copyright()
        {
            return View();
        }

        [UnAuthorize]
        public ActionResult About()
        {
            return View();
        }
        [UnAuthorize]
        public ActionResult Console()
        {
            var model = StatisticApplication.GetPlatformConsole();
            return View(model);
        }

        [HttpGet]
        [UnAuthorize]
        public ActionResult ProductRecentMonthSaleRank()
        {
            var end = DateTime.Now.Date;
            var begin = end.AddMonths(-1);
            var model = StatisticApplication.GetProductSaleRankingChart(0, begin, end, SaleDimension.Count, 15); 
            return Json(new { success = true, chart = model }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [UnAuthorize]
        public ActionResult GetRecentMonthShopSaleRankChart()
        {
            var end = DateTime.Now.Date;
            var begin = end.AddMonths(-1);
            var model = StatisticApplication.GetShopRankingChart(begin, end, SaleDimension.Count, 15);
            return Json(new { success = true, chart = model }, JsonRequestBehavior.AllowGet);
        }
    }
}