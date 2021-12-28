using Himall.Application;
using Himall.Core;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class MarketingController : BaseAdminController
    {

        // GET: Admin/sale
        public ActionResult Management()
        {
            ViewBag.Rights = string.Join(",", CurrentManager.AdminPrivileges.Select(a => (int)a).OrderBy(a => a));
            return View();
        }

        #region 充值赠送配置
        public ActionResult RechargePresentRule()
        {
            RechargePresentRuleModel model = new RechargePresentRuleModel();
            model.IsEnable = SiteSettings.IsOpenRechargePresent;
            model.Rules = RechargePresentRuleApplication.GetRules();
            model.RulesJson = JsonConvert.SerializeObject(model.Rules);
            return View(model);
        }
        [HttpPost]
        public JsonResult SaveRechargePresentRule(RechargePresentRuleModel model)
        {
            Result result = new Result { success = false, msg = "未知错误" };
            if (ModelState.IsValid)
            {
                model.CheckValidation();
                var setting = SiteSettingApplication.SiteSettings;
                setting.IsOpenRechargePresent = model.IsEnable;
                SiteSettingApplication.SaveChanges();
                if (model.IsEnable)
                {
                    RechargePresentRuleApplication.SetRules(model.Rules);
                }
                result.success = true;
                result.msg = "配置充值赠送规则成功";
            }
            else
            {
                result.success = false;
                result.msg = "数据错误";
            }
            return Json(result);
        }
        #endregion


        public ActionResult AdvanceList() {
            
            var advance=AdvanceApplication.GetAdvanceInfo();
            AdvanceModel model = new AdvanceModel() {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddMonths(1)
            };
            if (advance != null) {
                model.IsEnable = advance.IsEnable;
                model.IsReplay = advance.IsReplay;
                model.Link = advance.Link;
                model.StartTime = advance.StartTime;
                model.EndTime = advance.EndTime;

                if (!string.IsNullOrEmpty(advance.Img))
                {
                    model.Img = HimallIO.GetRomoteImagePath(advance.Img);
                }

            }


            return View(model);
        }
    }
}