﻿using Himall.Service;
using Himall.Web.Framework;
using System.EnterpriseServices;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class MemberGradeController : BaseAdminController
    {
        MemberGradeService _iMemberGradeService;
        public MemberGradeController(MemberGradeService MemberGradeService)
        {
            _iMemberGradeService = MemberGradeService;
        }
        public ActionResult Management()
        {
            var list = _iMemberGradeService.GetMemberGradeList();
            return View(list);
        }

        [HttpPost]
        [Description("删除会员等级")]
        public JsonResult Delete(int id)
        {
            _iMemberGradeService.DeleteMemberGrade(id);
            return Json(new Result() { success = true, msg = "删除成功！" });
        }

        public ActionResult Edit(long id)
        {
            var model = _iMemberGradeService.GetMemberGrade(id);
            return View(model);
        }

        private bool CheckMemberGrade(Himall.Entities.MemberGradeInfo model, out string erroMsg)
        {
            bool flag = true;
            erroMsg = "";
            if (string.IsNullOrWhiteSpace(model.GradeName))
            {
                erroMsg = "会员等级名称不能为空";
                flag = false;
            }
            if (model.Integral < 0)
            {
                erroMsg = "积分不能小于0";
                flag = false;
            }
            return flag;
        }

        public ActionResult Add()
        {
            return View();
        }
        [HttpPost]
        public JsonResult Add(Himall.Entities.MemberGradeInfo model)
        {
            if (!(1 <= model.Discount && model.Discount <= 10))
            {
                return Json(new Result() { success = false, msg = "可享受折扣率允许范围1-10折！" });
            }
            string erroMsg;
            if (!CheckMemberGrade(model, out erroMsg))
            {
                return Json(new Result() { success = false, msg = erroMsg });
            }

            _iMemberGradeService.AddMemberGrade(model);
            return Json(new Result() { success = true, msg = "添加成功！" });
        }
        [HttpPost]
        public JsonResult Edit(Himall.Entities.MemberGradeInfo model)
        {
            if (!(1 <= model.Discount && model.Discount <= 10))
            {
                return Json(new Result() { success = false, msg = "可享受折扣率允许范围1-10折！" });
            }
            string erroMsg;
            if (!CheckMemberGrade(model, out erroMsg))
            {
                return Json(new Result() { success = false, msg = erroMsg });
            }
            _iMemberGradeService.UpdateMemberGrade(model);
            return Json(new Result() { success = true, msg = "修改成功！" });
        }
    }
}