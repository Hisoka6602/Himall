using Himall.Service;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class CategoryController : BaseWebController
    {
        private CategoryService _iCategoryService;
        private ShopCategoryService _iShopCategoryService;
        public CategoryController(CategoryService CategoryService, ShopCategoryService ShopCategoryService)
        {
            _iCategoryService = CategoryService;
            _iShopCategoryService = ShopCategoryService;
        }

        [HttpPost]
        public JsonResult GetCategory(long? key = null, int? level = -1)
        {
            if (level == -1)
                key = 0;

            if (key.HasValue)
            {
                var categories = _iCategoryService.GetCategoryByParentId(key.Value);
                var cateoriesPair = categories.Select(item => new KeyValuePair<long, string>(item.Id, item.Name));
                return Json(cateoriesPair);
            }
            else
                return Json(new object[] { });
        }

        /// <summary>
        /// 获取店铺授权的分类
        /// </summary>
        /// <returns></returns>
        public JsonResult GetAuthorizationCategory(long shopId,long? key = null, int? level = -1)
        {
            if (level == -1)
                key = 0;
            if (key.HasValue)
            {
                var categories = _iShopCategoryService.GetBusinessCategory(shopId).Where(r => r.ParentCategoryId == key.Value).ToArray();
                var cateoriesPair = categories.Select(item => new KeyValuePair<long, string>(item.Id, item.Name));
                return Json(cateoriesPair);
            }
            else
                return Json(new object[] { });
        }
    }
}