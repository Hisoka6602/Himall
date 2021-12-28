﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Himall.Core;
using Himall.DTO;
using Himall.Web.Framework;
using Himall.CommonModel;
using Himall.Application;
using Himall.DTO.QueryModel;
using Himall.Web.Areas.Admin.Models.Distribution;
using Himall.DTO.Distribution;
using Himall.Web.Areas.Admin.Models;
using Himall.Entities;

namespace Himall.Web.Areas.Admin.Controllers
{
    //[H5Authorization]
    /// <summary>
    /// 销售员管理
    /// </summary>
    public class DistributorGradeController : BaseAdminController
    {
        #region 列表
        public ActionResult Management()
        {
            var result = DistributionApplication.GetDistributorGrades(true);
            return View(result);
        }
        /// <summary>
        /// 更改上级
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult Delete(long id)
        {
            Result result = new Result { success = false, msg = "未知错误" };
            if (id == 0)
            {
                throw new HimallException("错误的参数");
            }
            DistributionApplication.DeleteDistributorGrade(id);
            result.success = true;
            result.msg = "操作成功";
            return Json(result);
        }
        #endregion

        public ActionResult Add()
        {
            DistributorGradeModel result = new DistributorGradeModel();
            return View("_Grade", result);
        }

        public ActionResult Edit(long id)
        {
            var data = DistributionApplication.GetDistributorGrade(id);
            if (data == null)
            {
                throw new HimallException("错误的编号");
            }
            DistributorGradeModel result = new DistributorGradeModel
            {
                Id = data.Id,
                GradeName = data.GradeName,
                Quota = (int)data.Quota
            };
            return View("_Grade", result);
        }

        public JsonResult Save(DistributorGradeModel model)
        {
            bool isAdd = true;
            DistributorGradeInfo data = new DistributorGradeInfo();
            if (model.Id > 0)
            {
                data = DistributionApplication.GetDistributorGrade(model.Id);
                if (data == null)
                {
                    throw new HimallException("错误的编号");
                }
                isAdd = false;
            }
            else
            {
                var grades= DistributionApplication.GetDistributorGrades();
                if (grades.Count >= 10)
                {
                    throw new HimallException("最多只能新增10个等级");
                }
            }
            if (DistributionApplication.ExistDistributorGradesName(model.GradeName, model.Id))
            {
                throw new HimallException("保存失败，有重复的销售员等级名称");
            }
            if (DistributionApplication.ExistDistributorGradesQuota(model.Quota, model.Id))
            {
                throw new HimallException("保存失败，有重复的佣金门槛金额");
            }
            if (!ModelState.IsValid)
            {
                throw new HimallException("保存失败，数据未通过验证");
            }
                data.GradeName = model.GradeName;
                data.Quota = model.Quota;
                if (isAdd)
                {
                    DistributionApplication.AddDistributorGrade(data);
                }
                else
                {
                    DistributionApplication.UpdateDistributorGrade(data);
                }
            return Json(new Result { success = true });
        }
    }
}