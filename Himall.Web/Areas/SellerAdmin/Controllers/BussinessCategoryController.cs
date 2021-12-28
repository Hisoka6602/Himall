using Himall.Core;
using Himall.Core.Plugins;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.DTO;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Areas.Admin.Models.Product;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Himall.Application;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class BussinessCategoryController : BaseSellerController
    {
        private ShopService _ShopService;
        private CategoryService _iCategoryService;
        public BussinessCategoryController(ShopService ShopService, CategoryService CategoryService)
        {
            _ShopService = ShopService;
            _iCategoryService = CategoryService;
        }
        public ActionResult Management()
        {
            return View();
        }

        public ActionResult ApplyList()
        {
            return View();
        }

        public JsonResult GetApplyList(int page, int rows)
        {
            BussinessCateApplyQuery query = new BussinessCateApplyQuery();
            query.PageNo = page;
            query.PageSize = rows;
            query.shopId = CurrentSellerManager.ShopId;
            var model = _ShopService.GetBusinessCateApplyList(query);
            var cate = model.Models.ToList().Select(a => new { Id = a.Id, ShopName = a.ShopName, ApplyDate = a.ApplyDate.ToString("yyyy-MM-dd HH:mm"), AuditedStatus = a.AuditedStatus.ToDescription() });
            var p = new { rows = cate.ToList(), total = model.Total };
            return Json(p);
        }

        public ActionResult ApplyDetail(long id)
        {
            var model = _ShopService.GetBusinessCategoriesApplyInfo(id);
            ViewBag.Details = ShopApplication.GetBusinessCategoriesApplyDetails(id);
            return View(model);
        }


        public ActionResult List(int page, int rows)
        {
            BusinessCategoryQuery query = new BusinessCategoryQuery();
            query.PageNo = page;
            query.PageSize = rows;
            query.ShopId = CurrentSellerManager.ShopId;
            var data = ShopApplication.GetBusinessCategoryList(query);
            var list = data.Models.Select(a => new { a.Id, a.CommisRate, a.CategoryName }).ToList();
            return Json(new { rows = list, total = data.Total });
        }

        public ActionResult Apply()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetCategories(long? key = null, int? level = -1)
        {
            var categories = _iCategoryService.GetValidBusinessCategoryByParentId(key.GetValueOrDefault());
            var models = categories.Select(item => new KeyValuePair<long, string>(item.Id, item.Name));
            return Json(models);
        }

        public JsonResult GetBussinessCate(long id)
        {
            var categories = _ShopService.GetThirdBusinessCategory(id, CurrentSellerManager.ShopId);
            var t = categories.Select(item => new { id = item.Id, rate = item.Rate, path = item.Path });
            return Json(t, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ApplyBussinessCate(string categoryIds)
        {
            List<long> arr = new List<long>();
            var ids= Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(categoryIds, arr);
            _ShopService.ApplyShopBusinessCate(CurrentSellerManager.ShopId, ids);
            return Json(new Result() { success = true, msg = "申请成功" });
        }
    }
}