using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class VerificationCodeController : BaseAdminController
    {
        // GET: SellerAdmin/VerificationCode
        public ActionResult Management()
        {
            bool isOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            ViewBag.IsOpenStore = isOpenStore;
            return View();
        }

        public ActionResult ManagementShopBranch()
        {
            bool isOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            ViewBag.IsOpenStore = isOpenStore;
            return View();
        }

        [HttpPost]
        public JsonResult List(VerificationRecordQuery query, int page, int rows)
        {
            query.PageNo = page;
            query.PageSize = rows;
            var orderVerificationCode = OrderApplication.GetOrderVerificationCodeInfos(query);
            DataGridModel<OrderVerificationCodeModel> dataGrid = new DataGridModel<OrderVerificationCodeModel>() { rows = orderVerificationCode.Models, total = orderVerificationCode.Total };
            return Json(dataGrid);
        }

        public JsonResult Shopbranchlist(OrderQuery query, int page, int rows)
        {
            query.PageNo = page;
            query.PageSize = rows;
            query.IsVirtual = false;
            query.Status = OrderInfo.OrderOperateStatus.Finish;
            query.IgnoreSelfPickUp = false;
            var data = OrderApplication.GetShopBranchOrders(query);

            var shopbrancorder = data.Models.Select(item =>
            {
                return new
                {
                    OrderId = item.Id,
                    PayDatestr = item.PayDate.HasValue ? item.PayDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    item.ShopBranchName,
                    FinishDatestr = item.FinishDate.HasValue ? item.FinishDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    item.PickupCode,
                    item.SellerRemark
                };
            });
            return Json(new { rows = shopbrancorder, total = data.Total });
        }


        public JsonResult GetShopAndShopBranch(string keyWords)
        {
            var result = OrderApplication.GetShopOrShopBranch(keyWords);
            var values = result.Select(item => new { type = item.Type, value = item.Name, id = item.SearchId });
            return Json(values);
        }
    }
}