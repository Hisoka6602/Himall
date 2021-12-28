using Himall.Service;

using Himall.Web.Areas.SellerAdmin.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class CategoryController : BaseSellerController
    {
        private CategoryService _iCategoryService;
        private ShopCategoryService _iShopCategoryService;
        public CategoryController(CategoryService CategoryService, ShopCategoryService ShopCategoryService)
        {
            _iCategoryService = CategoryService;
            _iShopCategoryService = ShopCategoryService;
        }

        // GET: SellerAdmin/Category
        public ActionResult Management(int? isAll=1)
        {
            var ICategory = _iShopCategoryService;
            IEnumerable<Entities.ShopCategoryInfo> category = null;
            if (isAll.HasValue && isAll.Value == 1)
            {
                category = ICategory.GetShopCategory(CurrentSellerManager.ShopId);//多个分类
            }
            else
            {
                category = ICategory.GetMainCategory(CurrentSellerManager.ShopId);//只有一级分类
            }
            List<ShopCategoryModel> list = new List<ShopCategoryModel>();
            foreach (var item in category)
            {
                list.Add(new ShopCategoryModel(item));
            }
            return View(list);
        }

        [UnAuthorize]
        [HttpPost]
        public JsonResult GetEffectCategory(long categoryId)
        {
            var cate = _iCategoryService.GetCategory(categoryId);
            string names = _iCategoryService.GetEffectCategoryName(CurrentSellerManager.ShopId, cate.TypeId);
            return Json(new { json = names }, JsonRequestBehavior.AllowGet);
        }


        [UnAuthorize]
        public JsonResult GetCategoryDrop(long id = 0)
        {
            List<SelectListItem> cateList = new List<SelectListItem>{ new SelectListItem
                {
                    Selected = id==0,
                    Text ="请选择...",
                    Value = "0"
                }
            };
            var cateMain = _iShopCategoryService.GetMainCategory(CurrentSellerManager.ShopId);
            foreach (var item in cateMain)
            {
                cateList.Add(new SelectListItem
                {
                    Selected = id == item.Id,
                    Text = item.Name,
                    Value = item.Id.ToString()
                });
            }
            return Json(new { success = true, category = cateList }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ShopOperationLog("创建店铺分类", "pid,name")]
        public JsonResult CreateCategory(string name, long pId)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 12)
                throw new Exception("分类名称长度不能多于12位");



            var cate = new Entities.ShopCategoryInfo
            {
                Name = name,
                ParentCategoryId = pId,
                IsShow = true,
                DisplaySequence = _iShopCategoryService.GetCategoryByParentId(pId).Count() + 1,
                ShopId = CurrentSellerManager.ShopId
            };
            _iShopCategoryService.AddCategory(cate);
           // ObjectContainer.Current.Resolve<OperationLogService>().AddSellerOperationLog(
           //new LogInfo
           //{
           //    Date = DateTime.Now,
           //    Description = "创建店铺分类，父Id=" + pId,
           //    IPAddress = Request.UserHostAddress,
           //    PageUrl = "/Category/CreateCategory",
           //    UserName = CurrentSellerManager.UserName,
           //    ShopId = CurrentSellerManager.ShopId
           //});
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [ShopOperationLog("修改店铺分类名称", "id,name")]
        public JsonResult UpdateName(string name, long id)
        {
            _iShopCategoryService.UpdateCategoryName(id, name);

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [UnAuthorize]
        public JsonResult UpdateOrder(long order, long id)
        {
            _iShopCategoryService.UpdateCategoryDisplaySequence(id, order);
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
        [UnAuthorize]
        public JsonResult UpdateCategoryShow(bool isShow, long id)
        {
            //_iShopCategoryService.UpdateCategoryShow(id, !isShow);
            var ids = _iShopCategoryService.GetSecondAndThirdLevelCategories(new long[] { id}).Select(a=>a.Id).ToList();
            var pids = _iShopCategoryService.GetParentCategoryById(id, isShow).Select(a => a.Id).ToList();
            if (ids != null)
            {
                ids.Add(id);
                _iShopCategoryService.UpdateCategorysShow(!isShow,ids);
                _iShopCategoryService.UpdateCategorysShow(!isShow, pids);
            }
            HttpResponse.RemoveOutputCacheItem(string.Format("/Shop/Home/{0}", this.CurrentShop.Id)); //移除页面缓存
            HttpResponse.RemoveOutputCacheItem(string.Format("/shop/home/{0}", this.CurrentShop.Id));
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [UnAuthorize]
        public ActionResult GetCategoryByParentId(int id)
        {
            List<ShopCategoryModel> list = new List<ShopCategoryModel>();
            var categoryList = _iShopCategoryService.GetCategoryByParentId(id);
            foreach (var item in categoryList)
            {
                list.Add(new ShopCategoryModel(item));
            }
            return Json(new { success = true, Category = list }, JsonRequestBehavior.AllowGet);
        }

        [UnAuthorize]
        [HttpPost]
        public JsonResult GetAllShopCategory()
        {
            var categories = _iShopCategoryService.GetShopCategory(CurrentSellerManager.ShopId);
            return SuccessResult<dynamic>(data: categories);
        }

        [UnAuthorize]
        [HttpPost]
        public JsonResult BatchMoveProductCategory(long cid1,long cid2)
        {
            if (cid1 <= 0 || cid2 <= 0) {
                return Json(new { success = false, msg = "请选择商品分类" });
            }
            if (cid1 == cid2) {
                return Json(new { success=false,msg="请选择不同的商品分类进行替换"});
            }

            if (_iShopCategoryService.BatchMoveProductCategory(cid1, cid2))
            {
                return Json(new { success = true, msg = "批量转移商品成功" });
            }
            else {
                return Json(new { success = false, msg = "批量转移商品失败" });
            }
            
        }

        [UnAuthorize]
        [HttpPost]
        public JsonResult GetCategory(long? key = null, int? level = -1)
        {
            if (level == -1)
                key = 0;

            if (key.HasValue)
            {
                var categories = _iShopCategoryService.GetCategoryByParentId(key.Value, CurrentSellerManager.ShopId);
                var cateoriesPair = categories.Select(item => new KeyValuePair<long, string>(item.Id, item.Name));
                return Json(cateoriesPair);
            }
            else
                return Json(new object[] { });
        }


        [UnAuthorize]
        [HttpPost]
        public JsonResult GetSystemCategory(long? key = null, int? level = -1)
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

        [HttpPost]
        [ShopOperationLog("删除店铺分类", "id")]
        public JsonResult DeleteCategoryById(long id)
        {
            _iShopCategoryService.DeleteCategory(id, CurrentSellerManager.ShopId);
           // ObjectContainer.Current.Resolve<OperationLogService>().AddSellerOperationLog(
           //new LogInfo
           //{
           //    Date = DateTime.Now,
           //    Description = "删除店铺分类，Id=" + id ,
           //    IPAddress = Request.UserHostAddress,
           //    PageUrl = "/Category/DeleteCategoryById",
           //    UserName = CurrentSellerManager.UserName,
           //    ShopId = CurrentSellerManager.ShopId
           //});
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult BatchDeleteCategory(string Ids)
        {
            int id;
            foreach (var idStr in Ids.Split('|'))
            {
                if (string.IsNullOrWhiteSpace(idStr)) continue;
                if (int.TryParse(idStr, out id))
                {
                    _iShopCategoryService.DeleteCategory(id, CurrentSellerManager.ShopId);
                }
            }
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
    }
}