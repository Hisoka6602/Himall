using Himall.Core;
using Himall.Service;
using Himall.Web.Framework;
using System.Web.Mvc;
using System.Collections.Generic;
using System;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class MobileHomeProductsController : BaseAdminController
    {

        MobileHomeProductsService _iMobileHomeProductsService;
        BrandService _iBrandService;
        CategoryService _iCategoryService;
        ShopCategoryService _iShopCategoryService;
        MobileHomeProducts mobileHomeproduct;
        public MobileHomeProductsController(
             MobileHomeProductsService MobileHomeProductsService,
            BrandService BrandService,
            CategoryService CategoryService,
            ShopCategoryService ShopCategoryService

            )
        {
            _iBrandService = BrandService;
            _iCategoryService = CategoryService;
            _iMobileHomeProductsService = MobileHomeProductsService;
            _iShopCategoryService = ShopCategoryService;
            mobileHomeproduct = new MobileHomeProducts();
        }


        [HttpPost]
        public JsonResult GetMobileHomeProducts(PlatformType platformType, int page, int rows, string keyWords,string shopName, long? categoryId = null)
        {
            object model = mobileHomeproduct.GetMobileHomeProducts(CurrentManager.ShopId, platformType, page, rows, keyWords,shopName, categoryId);

            return Json(model);
        }

        [HttpPost]
        public JsonResult AddHomeProducts(string productIds, PlatformType platformType)
        {
            if (string.IsNullOrEmpty(productIds)) return Json(new { success = true });
            mobileHomeproduct.AddHomeProducts(CurrentManager.ShopId, productIds, platformType);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateSequence(long id, short sequence)
        {
            mobileHomeproduct.UpdateSequence(CurrentManager.ShopId, id, sequence);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult Delete(long id)
        {
            mobileHomeproduct.Delete(CurrentManager.ShopId, id);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult DeleteList(string ids)
        {
            var strArr = ids.Split(',');
            List<long> listid = new List<long>();
            foreach (var arr in strArr)
            {
                listid.Add(Convert.ToInt64(arr));
            }
            mobileHomeproduct.DeleteList(listid.ToArray());
            return Json(new Result() { success = true, msg = "批量删除成功！" });
        }

        [HttpPost]
        public JsonResult GetAllHomeProductIds(PlatformType platformType)
        {
            var homeProductIds = mobileHomeproduct.GetAllHomeProductIds(CurrentManager.ShopId, platformType);
            return Json(homeProductIds);
        }

    }
}