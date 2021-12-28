using Himall.Application;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Drawing;
using System.IO;
using System.Web.Mvc;
using Himall.Core;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class AgreementController : BaseAdminController
    {
        // GET: Admin/Agreement
        public ActionResult Management()
        {
            int type = 0;
            Int32.TryParse(Request.QueryString["type"], out type);
            var agreementTypes = (Entities.AgreementInfo.AgreementTypes)type;

            ViewBag.Type = type;
            //初始化默认返回买家注册协议
            return View(GetManagementModel(agreementTypes));
        }
        /// <summary>
        /// 入驻链接
        /// </summary>
        /// <returns></returns>
        public ActionResult SettledLink()
        {
            #region 商家入驻链接和二维码
            string LinkUrl = String.Format("{0}/m-weixin/shopregister/step1", CurrentUrlHelper.CurrentUrlNoPort());
            ViewBag.LinkUrl = LinkUrl;
            string qrCodeImagePath = string.Empty;
            if (!string.IsNullOrWhiteSpace(LinkUrl))
            {
                Bitmap map;
                map = Core.Helper.QRCodeHelper.Create(LinkUrl);
                MemoryStream ms = new MemoryStream();
                map.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                qrCodeImagePath = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray()); // 将图片内存流转成base64,图片以DataURI形式显示  
                ms.Dispose();
            }
            ViewBag.Imgsrc = qrCodeImagePath;
            #endregion
            return View();
        }

        public ActionResult EnterSet()
        {
            //入驻参数设置
            return View();
        }


        [HttpPost]
        public JsonResult GetManagement(int agreementType)
        {
            return Json(GetManagementModel((Entities.AgreementInfo.AgreementTypes)agreementType));
        }

        public AgreementModel GetManagementModel(Entities.AgreementInfo.AgreementTypes type)
        {
            AgreementModel model = new AgreementModel();
            model.AgreementType = type;
            var iAgreement = SystemAgreementApplication.GetAgreement(type);
            if (iAgreement != null)
            {
                model.AgreementContent = iAgreement.AgreementContent;
            }
            return model;

        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveAgreement(int agreementType, string agreementContent)
        {
            var agreementTypeEnum = (Entities.AgreementInfo.AgreementTypes)agreementType;
            var model = SystemAgreementApplication.GetAgreement(agreementTypeEnum);
            if (model == null)
                model = new Entities.AgreementInfo();

            model.AgreementType = agreementTypeEnum;
            model.AgreementContent = agreementContent;
            if (SystemAgreementApplication.SaveAgreement(model))
                return Json(new Result() { success = true, msg = "更新协议成功！" });
            else
                return Json(new Result() { success = false, msg = "更新协议失败！" });
        }

        #region 入驻设置
        public ActionResult Settled()
        {
            var model = ShopApplication.GetSettled();
            return View(model);
        }


        /// <summary>
        /// 商家入驻设置
        /// </summary>
        /// <param name="mSettled"></param>
        /// <returns></returns>
        public JsonResult setSettled(Himall.DTO.Settled mSettled)
        {
            ShopApplication.Settled(mSettled);
            return Json(new Result() { success = true, msg = "设置成功！" });
        }

        #endregion
    }
}