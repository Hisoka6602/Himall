using Himall.Service;
using Himall.Web.Framework;
using Himall.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class ShopGradeController : BaseAdminController
    {
        private ShopService _ShopService;
        public ShopGradeController(ShopService ShopService)
        {
            _ShopService = ShopService;
        }
        // GET: Admin/ShopGrade
        public ActionResult Management()
        {
            return View();
        }

        [HttpPost]
        public JsonResult List()
        {
            var shopG = _ShopService.GetShopGrades();
            IEnumerable<ShopGradeModel> shopGs = shopG.ToArray().Select(item => new ShopGradeModel()
            {
                Id = item.Id,
                ChargeStandard = item.ChargeStandard,
                ImageLimit = item.ImageLimit,
                ProductLimit = item.ProductLimit,
                Name = item.Name,

            });

            DataGridModel<ShopGradeModel> dataGrid = new DataGridModel<ShopGradeModel>() { rows = shopGs, total = shopG.Count() };
            return Json(dataGrid);
        }

        [UnAuthorize]
        [HttpPost]
        public ActionResult Edit(ShopGradeModel shopG)
        {
            if (ModelState.IsValid)
            {
                _ShopService.UpdateShopGrade(shopG);
                return RedirectToAction("Management");
            }
            return View(shopG);
        }

        [HttpGet]

        public ActionResult Edit(long id)
        {
            return View(new ShopGradeModel(_ShopService.GetShopGrade(id)));
        }

        [HttpGet]
        public ActionResult Add()
        {
            return View();
        }


        [HttpPost]
        [UnAuthorize]
        public ActionResult Add(ShopGradeModel shopG)
        {
           if (ModelState.IsValid)
            {
                _ShopService.AddShopGrade(shopG);
                return RedirectToAction("Management");
            }
            return View(shopG);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult DeleteShopGrade(long id)
        {
            _ShopService.DeleteShopGrade(id);
            return Json(new { success = true });
        }
    }
}