using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class ExpressTemplateController : BaseAdminController
    {
        private ExpressService _ExpressService;

        public ExpressTemplateController(ExpressService ExpressService)
        {
            _ExpressService = ExpressService;
        }

        public ActionResult Management()
        {
            var result = ExpressApplication.GetAllExpress();
            return View(result);
        }
        [HttpPost]
        public JsonResult Express(ExpressCompany model)
        {
            if (model.Id > 0)
            {
                ExpressApplication.UpdateExpressCode(model);
            }
            else
            {
                ExpressApplication.AddExpress(model);
            }
            return Json(new Result { success = true });
        }

        public JsonResult DeleteExpress(long id)
        {
            ExpressApplication.DeleteExpress(id);
            return Json(new Result { success = true, msg = "删除成功" }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ClearData(long id)
        {
            ExpressApplication.ClearData(id);
            return Json(new Result { success = true, msg = "清除成功" }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Setting()
        {
            ViewBag.SurplusQuantity = ExpressApplication.GetSurplusQuantity();
            ViewBag.CurUrl = SiteSettingApplication.GetCurDomainUrl();
            return View(SiteSettingApplication.SiteSettings);
        }
        public JsonResult SaveExpressSetting(string KuaidiApp_key, string KuaidiAppSecret)
        {
            var setting = SiteSettingApplication.SiteSettings;
            setting.KuaidiApp_key = KuaidiApp_key;
            setting.KuaidiAppSecret = KuaidiAppSecret;
            SiteSettingApplication.SaveChanges();
            return Json(new Result() { success = true, msg = "保存成功" });
        }

        public ActionResult Edit(string name)
        {
            var template = ExpressApplication.GetExpress(name);
            return View(template);
        }

        public JsonResult ChangeStatus(long id, ExpressStatus status)
        {
            ExpressApplication.ChangeExpressStatus(id, status);
            return Json(new Result { success = true, msg = "操作成功" });
        }
        [HttpPost]
        [UnAuthorize]
        public JsonResult GetConfig(string name)
        {
            var template = ExpressApplication.GetExpress(name);
            var elementTypes = Enum.GetValues(typeof(ExpressElementType));
            var allElements = new List<Element>();
            foreach(var item in elementTypes)
            {
                Element el = new Element() {
                     key=((int)item).ToString(),
                     value=((ExpressElementType)item).ToDescription()
                };
                allElements.Add(el);
            }
            ExpressTemplateConfig config = new ExpressTemplateConfig()
            {
                width = template.Width,
                height = template.Height,
                data = allElements.ToArray(),
                
            };
            if (template.Elements != null)
            {
                int i = 0;
                foreach (var element in template.Elements)
                {
                    var item = config.data.FirstOrDefault(t => t.key == ((int)element.ElementType).ToString());
                    item.a =element.a;
                    item.b =element.b;
                    item.selected = true;
                    i++;
                }
                config.selectedCount = i;
            }
            return Json(config, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult Save(string elements, string name,int width,int height,string backimage)//前台返回的的元素点的X、Y与宽、高的比例
        {
            elements = elements.Replace("\"[", "[").Replace("]\"", "]");
            var expressElements = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<ExpressElement>>(elements);

            ExpressCompany express = new ExpressCompany();
            express.Name = name;
            express.Height = height;
            express.Width = width;
            express.BackGroundImage = backimage;
            express.Elements = expressElements.Select(e => new ExpressElement
            {
                a = e.a,
                b = e.b,
                ElementType = (ExpressElementType)e.name,
            }).ToList();
            ExpressApplication.UpdateExpressAndElement(express);
            return Json(new Result { success = true });
        }
    }
}