﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    /// <summary>
    /// 商家入驻
    /// </summary>
    public class ShopRegisterController : BaseMobileMemberController
    {
        CategoryService _iCategoryService;
        public ShopRegisterController(CategoryService CategoryService)
        {
            _iCategoryService = CategoryService;
        }

        #region 页面加载时判断申请进度
        /// <summary>
        /// 页面加载时处理
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.Logo = SiteSettings.MemberLogo;
            string controllerName = filterContext.RouteData.Values["controller"].ToString().ToLower();
            string actionName = filterContext.RouteData.Values["action"].ToString().ToLower();
            string areaName = filterContext.RouteData.DataTokens["area"].ToString().ToLower();
            if (CurrentSellerManager == null && CurrentUser == null)
            {
                var result = RedirectToAction("Entrance", "Login", new { returnUrl = WebHelper.GetAbsoluteUri() });
                filterContext.Result = result;
                return;
            }
            if (CurrentSellerManager == null && actionName.IndexOf("step") != 0 && filterContext.RequestContext.HttpContext.Request.HttpMethod.ToUpper() != "POST")
            {
                if (actionName != ("step1").ToLower())
                {
                    var result = RedirectToAction("step1", "shopregister", new { area = "Mobile" });
                    filterContext.Result = result;
                    return;
                }
            }
            else if (CurrentSellerManager != null)
            {
                var shop = ShopApplication.GetShop(CurrentSellerManager.ShopId);
                int stage = (int)shop.Stage;
                if (shop.Stage == Entities.ShopInfo.ShopStage.Finish && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Open || shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.WaitAudit)//完成且审核通过，跳首页
                {
                    if (actionName != ("step1").ToLower() && actionName != ("step6").ToLower())
                    {
                        var result = RedirectToAction("step1", "shopregister", new { area = "Mobile" });
                        filterContext.Result = result;
                        return;
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// 商家入驻协议
        /// </summary>
        /// <returns></returns>
        public ActionResult Step1()
        {
            ViewBag.SellerAdminAgreement = ShopApplication.GetSellerAgreement();
            var model = ShopApplication.GetSettled();

            #region 这里主要检测app嵌套进来，用户商家之前切换，取正确的商家信息
            var sellerManager = CurrentSellerManager;
            var newmanager = ManagerApplication.GetSellerManager(CurrentUser.UserName);//实时读取下(避免app嵌套该网页读取不同商家)
            if (newmanager == null || newmanager.ShopId <= 0)
            {
                WebHelper.DeleteCookie(CookieKeysCollection.SELLER_MANAGER);//说明当前用户不是商家，之前商家已记录 清除下之前商家记录；
                if (sellerManager != null)
                    Core.Cache.Remove(CacheKeyCollection.Seller(sellerManager.Id));//清除原来的缓存
            }
            else if (newmanager != null && sellerManager == null)
            {
                base.SetSellerAdminLoginCookie(newmanager.Id, DateTime.Now.AddDays(7));//之前商家还没记录，则这里商家cookie保存下
            }

            if (newmanager != null && sellerManager != null && newmanager.ShopId != sellerManager.ShopId)
            {
                WebHelper.DeleteCookie(CookieKeysCollection.SELLER_MANAGER);//说明当前用户不是商家，之前商家已记录清除；
                Core.Cache.Remove(CacheKeyCollection.Seller(sellerManager.Id));//清除原来的缓存

                base.SetSellerAdminLoginCookie(newmanager.Id, DateTime.Now.AddDays(7));//当前的商家信息保存下
            }
            #endregion

            if (newmanager != null)
            {
                var shop = ShopApplication.GetShop(newmanager.ShopId);
                if (null != shop && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.HasExpired)
                    throw new HimallException("抱歉，您的店铺已过期");
                if (null != shop && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze)
                    throw new HimallException("抱歉，您的店铺已冻结");
                if (shop != null)
                {
                    if (shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Refuse)
                        return RedirectToAction("step3");
                    ViewBag.IsSeller = shop.Stage == Entities.ShopInfo.ShopStage.Finish && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Open;
                    ViewBag.WaitAudit = shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.WaitAudit;
                }
            }
            return View(model);
        }

        /// <summary>
        /// 选择企业/个人入驻
        /// </summary>
        /// <returns></returns>
        public ActionResult Step2()
        {
            return View();
        }

        /// <summary>
        /// 个人或企业账户信息
        /// </summary>
        /// <returns></returns>
        public ActionResult Step3()
        {
            var modelShop = ShopApplication.GetShop(CurrentSellerManager.ShopId);
            #region 个人/企业信息
            long companyRegionId = 0;
            long businessLicenceRegionId = 0;
            string refuseReason = "";
            if (modelShop.BusinessType.Equals(Himall.CommonModel.ShopBusinessType.Personal))
            {
                var step1 = ShopApplication.GetShopProfileSteps1(CurrentSellerManager.ShopId, out companyRegionId, out businessLicenceRegionId, out refuseReason);
                ViewBag.CompanyRegionIds = RegionApplication.GetRegionPath((int)companyRegionId);
                ViewBag.RefuseReason = refuseReason;
                ViewBag.fullName = RegionApplication.GetFullName((int)companyRegionId);

                long uid = ShopApplication.GetShopManagers(CurrentSellerManager.ShopId);
                var model = MemberApplication.GetMembers(uid);
                step1.RealName = model.RealName;
                Himall.DTO.MemberAccountSafety mMemberAccountSafety = MemberApplication.GetMemberAccountSafety(uid);
                
                step1.MemberEmail = mMemberAccountSafety.Email;
                step1.MemberPhone = mMemberAccountSafety.Phone;

                ModelState.AddModelError("Phone", "多个联系方式用,号分隔");
                ModelState.AddModelError("ContactPhone", "多个联系方式用,号分隔");
                return View("Steps3", step1);
            }
            else
            {
                var step1 = ShopApplication.GetShopProfileStep1(CurrentSellerManager.ShopId, out companyRegionId, out businessLicenceRegionId, out refuseReason);
                ViewBag.CompanyRegionIds = RegionApplication.GetRegionPath((int)companyRegionId);
                ViewBag.RefuseReason = refuseReason;
                ViewBag.fullName = RegionApplication.GetFullName((int)companyRegionId);

                long uid = ShopApplication.GetShopManagers(CurrentSellerManager.ShopId);
                var model = MemberApplication.GetMembers(uid);
                step1.RealName = model.RealName;
                Himall.DTO.MemberAccountSafety mMemberAccountSafety = MemberApplication.GetMemberAccountSafety(uid);
                step1.MemberEmail = mMemberAccountSafety.Email;
                step1.MemberPhone = mMemberAccountSafety.Phone;
                
                ModelState.AddModelError("Phone", "多个联系方式用,号分隔");
                ModelState.AddModelError("ContactPhone", "多个联系方式用,号分隔");
                return View(step1);
            }
            #endregion
        }

        /// <summary>
        /// 银行账户信息
        /// </summary>
        /// <returns></returns>
        public ActionResult Step4()
        {
            ShopApplication.ClearCacheShop(CurrentSellerManager.ShopId);
            var Stage = Entities.ShopInfo.ShopStage.Agreement;
            var shop = ShopApplication.GetShopProfileStep2(CurrentSellerManager.ShopId, out Stage);

            if (Stage == Entities.ShopInfo.ShopStage.CompanyInfo) return RedirectToAction("step3");

            ViewBag.BankRegionIds = RegionApplication.GetRegionPath(shop.BankRegionId);
            ViewBag.fullName = RegionApplication.GetFullName(shop.BankRegionId);

            return View(shop);
        }

        /// <summary>
        /// 店铺信息
        /// </summary>
        /// <param name="ids">经营类目ID集</param>
        /// <returns></returns>
        public ActionResult Step5(string ids)
        {
            ShopApplication.ClearCacheShop(CurrentSellerManager.ShopId);
            var shop = ShopApplication.GetShop(CurrentSellerManager.ShopId, true);
            if (shop.Stage == Entities.ShopInfo.ShopStage.CompanyInfo) return RedirectToAction("step3");
            if (shop.Stage == Entities.ShopInfo.ShopStage.FinancialInfo) return RedirectToAction("step4");

            var step3 = new ShopProfileStep3();
            step3.ShopName = shop.ShopName;
            step3.ShopGrade = shop.GradeId;
            var gradeInfo = ShopApplication.GetShopGrade(shop.GradeId);
            ViewBag.GradeName = gradeInfo != null ? gradeInfo.Name : "未选择";//获取等级名称
            ViewBag.ShopBusinessCategory = "未选择";
            ViewBag.BusinessCategoryIds = string.Empty;
            if (!string.IsNullOrWhiteSpace(ids))
            {
                var idArr = ids.Split(',');
                if (idArr.Length > 0)
                {
                    List<long> idList = idArr.Select(p => long.Parse(p)).ToList();
                    var categoriesList = _iCategoryService.GetCategories().Where(p => idList.Contains(p.Id));
                    if (categoriesList.Count() == idArr.Length)
                    {
                        string businessCategory = string.Join("、", categoriesList.Select(x => x.Name).ToArray());
                        ViewBag.ShopBusinessCategory = businessCategory.Length > 0 ? businessCategory : "未选择";
                        ViewBag.BusinessCategoryIds = ids;
                    }
                }
            }
            else
            {
                var businessCategoryInfo = ShopApplication.GetBusinessCategory(CurrentSellerManager.ShopId);
                if (businessCategoryInfo != null && businessCategoryInfo.Count > 0)
                {
                    List<long> categoryIds = businessCategoryInfo.Select(p => p.CategoryId).ToList();
                    ViewBag.BusinessCategoryIds = string.Join(",", categoryIds.ToArray());
                    string businessCategory = string.Join("、", _iCategoryService.GetCategories().Where(p => categoryIds.Contains(p.Id)).Select(x => x.Name).ToArray());
                    ViewBag.ShopBusinessCategory = businessCategory.Length > 0 ? businessCategory : "未选择";
                }
            }

            return View(step3);
        }

        public ActionResult Step6()
        {
            ShopApplication.ClearCacheShop(CurrentSellerManager.ShopId);
            var shop = ShopApplication.GetShop(CurrentSellerManager.ShopId, true);
            if (shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Refuse)
            {
                ViewBag.SellerAdminAgreement = ShopApplication.GetSellerAgreement();
                var model = ShopApplication.GetSettled();
                return View("step1", model);
            }
            return View();
        }

        /// <summary>
        /// 跳转记录,记录当前管理员
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Agreement(string agree, int businessType)
        {
            if (agree.Equals("on"))
            {
                var seller = ShopApplication.AddSellerManager(CurrentUser.UserName, CurrentUser.Password, CurrentUser.PasswordSalt);
                base.SetSellerAdminLoginCookie(seller.Id, DateTime.Now.AddDays(7));
                var model = ShopApplication.GetShop(CurrentSellerManager.ShopId);

                if ((model.Stage != Entities.ShopInfo.ShopStage.Finish || model.ShopStatus == Entities.ShopInfo.ShopAuditStatus.WaitConfirm))
                {
                    if (model.Stage == 0 || model.Stage == Entities.ShopInfo.ShopStage.CompanyInfo)
                    {
                        model.BusinessType = businessType == 0 ? ShopBusinessType.Enterprise : ShopBusinessType.Personal;
                        model.Stage = Entities.ShopInfo.ShopStage.CompanyInfo;
                        ShopApplication.UpdateShop(model);
                    }
                    return RedirectToAction("step3");
                }
                else
                {
                    throw new HimallException("注册流程异常，请联系管理员");
                }                
            }
            else return RedirectToAction("step2");
        }
        /// <summary>
        /// 发送验证码
        /// </summary>
        /// <param name="pluginId"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SendCode(string pluginId, string destination)
        {
            long uid = ShopApplication.GetShopManagers(CurrentSellerManager.ShopId);
            var member = MemberApplication.GetMembers(uid);
            var status = MemberApplication.SendMemberCode(pluginId, destination, member.UserName, SiteSettings.SiteName);
            bool result = status == SendMemberCodeReturn.success;
            return Json(new Result() { success = result, msg = status.ToDescription() });
        }

        /// <summary>
        /// 商家入驻第一步信息保存
        /// </summary>
        /// <param name="shopProfileStep1"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult EditProfile1(ShopProfileStep1 shopProfileStep1)
        {
            if (ShopApplication.ExistCompanyName(shopProfileStep1.CompanyName, CurrentSellerManager.ShopId))
                return Json(new { success = false, msg = "该公司名已存在！" });
            if (ShopApplication.ExistBusinessLicenceNumber(shopProfileStep1.BusinessLicenceNumber, CurrentSellerManager.ShopId))
                return Json(new { success = false, msg = "该营业执照号已存在！" });

            //公司信息
            Himall.DTO.Shop shopInfo = ShopApplication.GetShop(CurrentSellerManager.ShopId);
            shopInfo.Id = CurrentSellerManager.ShopId;
            shopInfo.CompanyName = shopProfileStep1.CompanyName;
            shopInfo.CompanyAddress = shopProfileStep1.Address;
            shopInfo.CompanyRegionId = shopProfileStep1.CityRegionId;
            shopInfo.CompanyRegionAddress = shopProfileStep1.Address;
            shopInfo.CompanyPhone = shopProfileStep1.Phone;
            shopInfo.CompanyEmployeeCount = shopProfileStep1.EmployeeCount;
            shopInfo.CompanyRegisteredCapital = shopProfileStep1.RegisterMoney;
            shopInfo.ContactsName = shopProfileStep1.ContactName;
            shopInfo.ContactsPhone = shopProfileStep1.ContactPhone;
            shopInfo.ContactsEmail = shopProfileStep1.Email;
            shopInfo.BusinessLicenceNumber = shopProfileStep1.BusinessLicenceNumber;
            shopInfo.BusinessLicenceRegionId = shopProfileStep1.BusinessLicenceArea;
            shopInfo.BusinessLicenceStart = shopProfileStep1.BusinessLicenceValidStart;
            shopInfo.BusinessLicenceEnd = shopProfileStep1.BusinessLicenceValidEnd;
            shopInfo.BusinessSphere = shopProfileStep1.BusinessSphere;
            shopInfo.BusinessLicenceNumberPhoto = shopProfileStep1.BusinessLicenceNumberPhoto;
            shopInfo.OrganizationCode = shopProfileStep1.OrganizationCode;
            shopInfo.OrganizationCodePhoto = shopProfileStep1.OrganizationCodePhoto;
            shopInfo.GeneralTaxpayerPhot = shopProfileStep1.GeneralTaxpayerPhoto;
            shopInfo.Stage = Entities.ShopInfo.ShopStage.FinancialInfo;
            shopInfo.BusinessLicenseCert = Request.Form["BusinessLicenseCert"];
            shopInfo.ProductCert = Request.Form["ProductCert"];
            shopInfo.OtherCert = Request.Form["OtherCert"];
            shopInfo.legalPerson = shopProfileStep1.legalPerson;
            shopInfo.CompanyFoundingDate = shopProfileStep1.CompanyFoundingDate;
            ShopApplication.UpdateShop(shopInfo);

            //管理员信息
            long uid = ShopApplication.GetShopManagers(CurrentSellerManager.ShopId);
            var model = MemberApplication.GetMemberAccountSafety(uid);
            if (shopProfileStep1.MemberPhone.Equals("")) return Json(new { success = false, msg = "必须认证手机！" });

            //修改真实姓名
            var member = MemberApplication.GetMembers(uid);
            member.RealName = shopProfileStep1.RealName;
            MemberApplication.UpdateMember(member);

            //手机认证
            if (!shopProfileStep1.MemberPhone.Equals(model.Phone))
            {
                string pluginId = "Himall.Plugin.Message.SMS";
                int result = MemberApplication.CheckMemberCode(pluginId, shopProfileStep1.PhoneCode, shopProfileStep1.MemberPhone, uid);
                string strMsg = "";
                switch (result)
                {
                    case 0: strMsg = "手机验证码错误！"; break;
                    case -1: strMsg = "此手机号已绑定！"; break;
                }
                if (!strMsg.Equals("")) return Json(new { success = false, msg = strMsg });
            }
            return Json(new { success = true, msg = "成功！" });
        }

        /// <summary>
        /// 个人信息
        /// </summary>
        /// <param name="shopProfileStep1"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult EditProfiles1(ShopProfileSteps1 shopProfileStep1)
        {
            ShopApplication.ClearCacheShop(CurrentSellerManager.ShopId);
            var shopInfo = ShopApplication.GetShop(CurrentSellerManager.ShopId);
            shopInfo.Id = CurrentSellerManager.ShopId;
            shopInfo.CompanyName = shopProfileStep1.CompanyName;
            shopInfo.CompanyAddress = shopProfileStep1.Address;
            shopInfo.CompanyRegionId = shopProfileStep1.CityRegionId;
            shopInfo.CompanyRegionAddress = shopProfileStep1.Address;
            shopInfo.Stage = Entities.ShopInfo.ShopStage.FinancialInfo;
            shopInfo.BusinessLicenseCert = Request.Form["BusinessLicenseCert"];
            shopInfo.ProductCert = Request.Form["ProductCert"];
            shopInfo.OtherCert = Request.Form["OtherCert"];
            shopInfo.IDCard = shopProfileStep1.IDCard;
            shopInfo.IDCardUrl = shopProfileStep1.IDCardUrl;
            shopInfo.IDCardUrl2 = shopProfileStep1.IDCardUrl2;
            ShopApplication.UpdateShop(shopInfo);
            var mTheme = ShopApplication.GetSettled();
            //管理员信息
            long uid = ShopApplication.GetShopManagers(CurrentSellerManager.ShopId);
            var model = MemberApplication.GetMemberAccountSafety(uid);
            if (!mTheme.CompanyVerificationType.Equals(VerificationType.VerifyEmail) && shopProfileStep1.MemberPhone.Equals(""))
            {
                return Json(new { success = false, msg = "必须认证手机！" });
            }
            if (!mTheme.CompanyVerificationType.Equals(Himall.CommonModel.VerificationType.VerifyPhone) && shopProfileStep1.MemberEmail.Equals(""))
            {
                return Json(new { success = false, msg = "必须认证邮箱！" });
            }


            //修改真实姓名
            var member = MemberApplication.GetMembers(uid);
            member.RealName = shopProfileStep1.RealName;
            MemberApplication.UpdateMember(member);

            //手机认证
            if (!shopProfileStep1.MemberPhone.Equals(model.Phone))
            {
                string pluginId = "Himall.Plugin.Message.SMS";
                int result = MemberApplication.CheckMemberCode(pluginId, shopProfileStep1.PhoneCode, shopProfileStep1.MemberPhone, uid);
                string strMsg = "";
                switch (result)
                {
                    case 0: strMsg = "手机验证码错误！"; break;
                    case -1: strMsg = "此手机号已绑定！"; break;
                }
                if (!strMsg.Equals(""))
                {
                    return Json(new { success = false, msg = strMsg });
                }
            }

            //邮箱认证
            if (!shopProfileStep1.MemberEmail.Equals(model.Email))
            {
                string pluginId = "Himall.Plugin.Message.Email";
                int result = MemberApplication.CheckMemberCode(pluginId, shopProfileStep1.EmailCode, shopProfileStep1.MemberEmail, uid);
                string strMsg = "";
                switch (result)
                {
                    case 0: strMsg = "邮箱验证码错误！"; break;
                    case -1: strMsg = "此邮箱已绑定！"; break;
                }
                if (!strMsg.Equals(""))
                {
                    return Json(new { success = false, msg = strMsg });
                }
            }
            return Json(new { success = true, msg = "成功！" });
        }

        /// <summary>
        /// 保存账户信息
        /// </summary>
        /// <param name="shopProfileStep2"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult EditProfile2(ShopProfileStep2 shopProfileStep2)
        {
            ShopApplication.ClearCacheShop(CurrentSellerManager.ShopId);
            ShopApplication.EditProfile2(CurrentSellerManager.ShopId, shopProfileStep2);

            return Json(new { success = true });
        }

        /// <summary>
        /// 第三步店铺信息提交
        /// </summary>
        /// <param name="shopProfileStep3"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult EditProfile3(string shopProfileStep3)
        {
            ShopProfileStep3 model = Newtonsoft.Json.JsonConvert.DeserializeObject<ShopProfileStep3>(shopProfileStep3);
            int result = ShopApplication.UpdateShop(model, CurrentSellerManager.ShopId);
            if (result.Equals(-1))
            {
                var msg = string.Format("{0} 店铺名称已经存在", model.ShopName);
                return Json(new { success = false, msg = msg });
            }
            return Json(new { success = true });
        }

        /// <summary>
        /// 获取店铺等级
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetShopGrades()
        {
            var shopGrades = ShopApplication.GetShopGrades();
            return SuccessResult<dynamic>(data: shopGrades);
        }

        /// <summary>
        /// 获取一级分类
        /// </summary>
        /// <returns></returns>
        public ActionResult Categories()
        {
            ViewBag.Categories = _iCategoryService.GetValidBusinessCategoryByParentId(0).ToList();
            return View();
        }

        /// <summary>
        /// 获取可以做为经营类目子集分类
        /// </summary>
        /// <param name="key">一级分类</param>
        /// <returns>一级分类下的二级分类和三级分类</returns>
        [HttpGet]
        public JsonResult GetCategories(long? key = null)
        {
            var list = _iCategoryService.GetValidBusinessCategoryByParentId(key.Value);
            var data = list.Select(item =>
            {
                return new Himall.DTO.Category()
                {
                    Id = item.Id,
                    Name = item.Name,
                    SubCategories = _iCategoryService.GetValidBusinessCategoryByParentId(item.Id).Map<List<Category>>()
                };
            });
            return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
        }
    }
}