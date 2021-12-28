using Himall.Core;
using Himall.Service;
using Himall.Web.Areas.Admin.Models.Product;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Himall.Application;
using Himall.CommonModel;
using Himall.Entities;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class ProductTypeController : BaseAdminController
    {
        private TypeService _iTypeService;
        private OperationLogService _iOperationLogService;
        private BrandService _iBrandService;
        public ProductTypeController(TypeService TypeService,
        OperationLogService OperationLogService,
        BrandService BrandService)
        {
            _iTypeService = TypeService;
            _iOperationLogService = OperationLogService;
            _iBrandService = BrandService;
        }
        public ActionResult Management()
        {
            return View();
        }

        [HttpPost]
        [OperationLog("删除平台类目", "id")]
        public JsonResult DeleteType(long Id)
        {
            Result result = new Result();
            try
            {
                TypeApplication.DeleteType(Id);
                // ObjectContainer.Current.Resolve<OperationLogService>().AddPlatformOperationLog(
                //new LogInfo
                //{
                //    Date = DateTime.Now,
                //    Description = "删除平台类目，Id=" + Id,
                //    IPAddress = Request.UserHostAddress,
                //    PageUrl = "/ProductType/DeleteTyp",
                //    UserName = CurrentManager.UserName,
                //    ShopId = 0

                //});
                result.success = true;
            }
            catch (HimallException ex)
            {
                result.msg = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("删除平台类目失败", ex);
                result.msg = "删除平台类目失败";
            }
            return Json(result);
        }

        [HttpPost]
        public JsonResult DataGridJson(string searchKeyWord, int page, int rows)
        {
            if (!string.IsNullOrWhiteSpace(searchKeyWord))
            {
                searchKeyWord = searchKeyWord.Trim();
            }
            var typePage = _iTypeService.GetTypes(searchKeyWord, page, rows);
            var data = typePage.Models.Select(item => new
            {

                Id = item.Id,
                Name = item.Name

            }).ToList();
            DataGridModel<dynamic> dataGrid = new DataGridModel<dynamic> { rows = data, total = typePage.Total };
            return Json(dataGrid, JsonRequestBehavior.AllowGet);
        }

        private void TransformAttrs(TypeInfo model)
        {
            model.AttributeInfo = _iTypeService.GetAttributesByType(model.Id);
            var values = _iTypeService.GetAttributeValues(model.AttributeInfo.Select(p => p.Id).ToList());
            foreach (var item in model.AttributeInfo)
            {
                var vals = values.Where(t => t.AttributeId == item.Id).ToList();
                item.AttrValue = string.Join(",", vals.Select(t=>t.Value));
                item.AttributeValueInfo = vals;
            }
        }

        private void TransformSpec(TypeInfo model)
        {
            var values = _iTypeService.GetValuesByType(model.Id);
            if (model.IsSupportColor && values.Any(p => p.Specification == SpecificationType.Color))
            {
                var vals = values.Where(p => p.Specification == SpecificationType.Color).Select(a=>a.Value);
                model.ColorValue = string.Join(",", vals);
            }
            if (model.IsSupportSize && values.Any(p => p.Specification == SpecificationType.Size))
            {
                var vals = values.Where(p => p.Specification == SpecificationType.Size).Select(a => a.Value);
                model.SizeValue = string.Join(",", vals);
            }
            if (model.IsSupportVersion && values.Any(p => p.Specification == SpecificationType.Version))
            {
                var vals = values.Where(p => p.Specification == SpecificationType.Version).Select(a => a.Value);
                model.VersionValue = string.Join(",", vals);
            }
        }

        public JsonResult GetBrandsAjax(long id)
        {
            var typeBrands = _iTypeService.GetBrandsByType(id);
            var brands = _iBrandService.GetBrands("");
            var data = brands.Select(brand => new BrandViewModel
            {
                id = brand.Id,
                isChecked = typeBrands.Contains(brand.Id),
                value = brand.Name
            });
            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Edit(long id = 0)
        {
            var brands = _iBrandService.GetBrands("");
            ViewBag.Brands = brands.ToList();

            var newInfo = new TypeInfo();
            ViewBag.Attrbutes = new List<AttributeInfo>();
            if (id == 0)
                return View(newInfo);
            else
            {
                var model = _iTypeService.GetType(id);
                if (model == null)
                    return HttpNotFound();
                //ViewBag.Attrbutes = _iTypeService.GetAttributesByType(id);
                if (string.IsNullOrWhiteSpace(model.SizeValue) && string.IsNullOrWhiteSpace(model.ColorValue) && string.IsNullOrWhiteSpace(model.VersionValue))
                {
                    model.SizeValue = newInfo.SizeValue;
                    model.ColorValue = newInfo.ColorValue;
                    model.VersionValue = newInfo.VersionValue;
                }

                TransformAttrs(model);
                TransformSpec(model);
                return View(model);
            }
        }

        [HttpPost]
        [OperationLog("修改平台类目", "id")]
        public ActionResult SaveModel(TypeModel type)
        {
            if (0 != type.Id)
            {
                _iTypeService.UpdateType(type);
            }
            else if (0 == type.Id)
            {
                _iTypeService.AddType(type);
            }
           
            return RedirectToAction("Management");
        }


    }
}