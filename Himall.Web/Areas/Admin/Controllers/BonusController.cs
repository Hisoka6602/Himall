using Himall.Web.Framework;
using System;
using System.Linq;
using System.Web.Mvc;
using Himall.Service;
using Himall.Web.Models;
using Himall.Core;
using Himall.Web.Areas.Admin.Models.Market;
using System.IO;
using Himall.Entities;
using Himall.DTO.QueryModel;
using Himall.Application;

namespace Himall.Web.Areas.Admin.Controllers
{
    [MarketingAuthorization]
    public class BonusController : BaseAdminController
    {
        private BonusService _BonusService;

        public BonusController(BonusService BonusService)
        {
            _BonusService = BonusService;
        }

        public ActionResult Management()
        {
            try
            {
                var siteSetting = SiteSettingApplication.SiteSettings;
                if (string.IsNullOrEmpty(siteSetting.WeixinAppId) || string.IsNullOrEmpty(siteSetting.WeixinAppSecret))
                {
                    return RedirectToAction("UnConfig");
                }
                return View();
            }
            catch (Exception e)
            {
                Exception innerEx = e.InnerException == null ? e : e.InnerException;
                Log.Info("微信红包进入出错：", innerEx);
                throw e;
            }
        }

        public ActionResult Config()
        {
            throw new NotImplementedException("功能转移至系统-消息设置");
        }

        [HttpPost]
        public ActionResult Config(FormCollection form)
        {
            var settings = SiteSettingApplication.SiteSettings;
            settings.WX_MSGGetCouponTemplateId = form["WX_MSGGetCouponTemplateId"];
            SiteSettingApplication.SaveChanges();
            return RedirectToAction("Config");
        }


        public ActionResult UnConfig()
        {
            return View();
        }



        public ActionResult Detail(long id)
        {
            ViewBag.Id = id;
            return View();
        }

        public ActionResult Add()
        {
            return View();
        }

        public ActionResult Edit(long id)
        {
            var obj = _BonusService.Get(id);
            BonusModel model = new BonusModel(obj);
            return View(model);
        }

        public ActionResult Apportion(long id)
        {
            var obj = _BonusService.Get(id);
            BonusModel model = new BonusModel(obj);
            model.QRPath = model.QRPath;
            return View(model);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">1 : 关注，2：活动</param>
        /// <param name="state">1：正在进行，2：无效</param>
        [HttpPost]
        public JsonResult List(BonusQuery query)
        {
            var result = _BonusService.Get(query);

            var datas = result.Models.ToList().Select(m => new BonusModel()
            {
                Id = m.Id,
                Type = m.Type,
                TypeStr = m.Type.ToDescription(),
                Style = m.Style,
                PriceType = (BonusInfo.BonusPriceType)m.PriceType,
                Name = m.Name,
                MerchantsName = m.MerchantsName,
                Remark = m.Remark,
                Blessing = m.Blessing,
                TotalPrice = m.TotalPrice,
                StartTime = m.StartTime,
                EndTime = m.EndTime,
                FixedAmount = (decimal)m.FixedAmount,
                RandomAmountStart = (decimal)m.RandomAmountStart,
                RandomAmountEnd = (decimal)m.RandomAmountEnd,
                ImagePath = m.ImagePath,
                Description = m.Description,
                ReceiveCount = m.ReceiveCount,
                IsInvalid = m.IsInvalid,
                StateStr = GetStateString(m)
            }).ToList();

            var model = new DataGridModel<BonusModel>
            {
                rows = datas,
                total = result.Total
            };
            return Json(model, true);
        }


        private string GetStateString(Entities.BonusInfo m)
        {
            if (m.EndTime < DateTime.Now)
            {
                return "已过期";
            }
            else if (m.IsInvalid)
            {
                return "已失效";
            }
            return "正在进行";

        }

        [HttpPost]
        public JsonResult DetailList(long id, int page = 1, int rows = 20)
        {
            var result = _BonusService.GetDetail(id, page, rows);
            var members = MemberApplication.GetMembers(result.Models.Where(p => p.UserId.HasValue).Select(p => p.UserId.Value).ToList());
            var datas = result.Models.Select(item =>
            {
                var username = string.Empty;
                if (item.UserId.HasValue)
                    username = members.FirstOrDefault(p => p.Id == item.UserId)?.UserName ?? string.Empty;
                return new BonusReceiveModel()
                {
                    OpenId = item.OpenId,
                    Price = item.Price,
                    ReceiveTime = item.ReceiveTime == null ? "" : ((DateTime)item.ReceiveTime).ToString("yyyy-MM-dd"),
                    UserName = username,
                    IsTransformedDeposit = item.IsTransformedDeposit
                };
            }).ToList();
            var model = new DataGridModel<BonusReceiveModel> { rows = datas, total = result.Total };
            return Json(model);
        }

        [HttpPost]
        public JsonResult Invalid(long id)
        {
            _BonusService.Invalid(id);
            return Json(true);
        }

        [HttpPost]
        public ActionResult Add(BonusModel model)
        {
            if (!string.IsNullOrEmpty(model.ImagePath))
            {
                if (!HimallIO.ExistDir("/Storage/Plat/Bonus"))
                {
                    HimallIO.CreateDir("/Storage/Plat/Bonus");
                }
                //转移图片
                if (model.ImagePath.Contains("/temp/"))
                {
                    string source = model.ImagePath.Substring(model.ImagePath.LastIndexOf("/temp"));
                    string dest = @"/Storage/Plat/Bonus/";
                    model.ImagePath = Path.Combine(dest, Path.GetFileName(source));
                    Core.HimallIO.CopyFile(source, model.ImagePath, true);
                }
                else if (model.ImagePath.Contains("/Storage/"))
                {
                    model.ImagePath = model.ImagePath.Substring(model.ImagePath.LastIndexOf("/Storage"));
                }

            }
            else
            {
                model.ImagePath = "";
            }

            model.StartTime = DateTime.Now;
            if (Himall.Entities.BonusInfo.BonusType.Prize == model.Type)
            {
                model.EndTime = DateTime.Now.AddYears(20);
            }
            string url = CurrentUrlHelper.CurrentUrlNoPort() + "/m-weixin/bonus/index/";
            _BonusService.Add(model, url);
            return RedirectToAction("Management");
            throw new HimallException("验证失败");
        }

        [HttpPost]
        public ActionResult CanAdd()
        {
            var result = _BonusService.CanAddBonus();
            return Json(result);
        }

        [HttpPost]
        public ActionResult EditData(BonusModel model)
        {
            if (ModelState.IsValid)
            {
                _BonusService.Update(model);
                return RedirectToAction("Management");
            }
            throw new HimallException("验证失败");
        }
    }
}