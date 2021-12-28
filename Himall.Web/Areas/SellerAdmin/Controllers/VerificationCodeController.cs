using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Entities;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Himall.Web.Areas.SellerAdmin.Models;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class VerificationCodeController : BaseSellerController
    {
        // GET: SellerAdmin/VerificationCode
        public ActionResult Management()
        {
            bool isOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            if (isOpenStore)
            {
                #region 商家下所有门店
                var data = ShopBranchApplication.GetShopBranchsAll(new ShopBranchQuery()
                {
                    ShopId = CurrentSellerManager.ShopId
                });
                ViewBag.StoreList = data.Models;
                #endregion
            }
            ViewBag.IsOpenStore = isOpenStore;
            ViewBag.ShopName = this.CurrentShop.ShopName;
            return View();
        }

        [HttpPost]
        public JsonResult List(VerificationRecordQuery query, int page, int rows)
        {
            query.IsAll = query.ShopBranchId == null;
            query.IsShop = query.ShopBranchId.HasValue && query.ShopBranchId.Value == 0;

            query.ShopId = CurrentSellerManager.ShopId;
            query.PageNo = page;
            query.PageSize = rows;


            var orderVerificationCode = OrderApplication.GetOrderVerificationCodeInfos(query);
            DataGridModel<OrderVerificationCodeModel> dataGrid = new DataGridModel<OrderVerificationCodeModel>() { rows = orderVerificationCode.Models, total = orderVerificationCode.Total };
            return Json(dataGrid);
        }
    }
}