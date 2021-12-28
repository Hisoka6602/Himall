using Himall.Application;
using Himall.Core;
using Himall.Entities;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class PopuActiveController: BaseAdminController
    {
        public ActionResult PopupActive()
        {
            ViewBag.IsOpenStore = SiteSettingApplication.SiteSettings.IsOpenStore;
            var advance = AdvanceApplication.GetAdvanceInfo();
            AdvanceModel model = new AdvanceModel()
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddMonths(1)
            };
            if (advance != null)
            {
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

        [HttpPost]
        public JsonResult SavePopupActive(AdvanceModel model) {
            if (model.IsEnable)//如果是开启需要判断数据
            {
                if (string.IsNullOrWhiteSpace(model.Img))
                {
                    throw new HimallException("请上传广告图片");
                }
                if (string.IsNullOrWhiteSpace(model.Link))
                {
                    throw new HimallException("请配置链接");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.Img))
                {
                    model.Img = "";
                }
                if (string.IsNullOrWhiteSpace(model.Link))
                {
                    model.Link = "";
                }
            }
            AdvanceInfo advance = new AdvanceInfo()
            {
                IsEnable=model.IsEnable,
                IsReplay=model.IsReplay,
                Img=model.Img,
                Link=model.Link,
                StartTime=model.StartTime,
                EndTime=model.EndTime
            };
            AdvanceApplication.AddAdvance(advance);
            return SuccessResult("保存成功");
        }
    }
}