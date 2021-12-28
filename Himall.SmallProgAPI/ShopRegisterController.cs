using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.SmallProgAPI.Model;
using Himall.SmallProgAPI.Model.ParamsModel;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    public class ShopRegisterController : BaseApiController
    {
        private static string _encryptKey = Guid.NewGuid().ToString("N");
        /// <summary>
        /// 检测是否存在商家数据，如没数据
        /// </summary>
        /// <returns></returns>
        private ManagerInfo CheckSellerManager()
        {
            var sellerManager = ManagerApplication.GetSellerManager(CurrentUser.UserName);//检测之前是否已是商家
            if (sellerManager == null)
                throw new HimallApiException(ApiErrorCode.Invalid_User_Key_Info, "不存在初始入驻商家");

            return sellerManager;
        }

        /// <summary>
        /// 商家入驻商家信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private dynamic GetShopDynamic(ShopInfo model, bool isbuss = false)
        {
            dynamic shop = new System.Dynamic.ExpandoObject();
            shop.Id = model.Id;//名称
            shop.ShopName = model.ShopName ?? "";//名称
            shop.GradeId = model.GradeId;//名称
            shop.Stage = model.Stage;//店铺进度
            shop.ShopStatus = model.ShopStatus;//店铺状态
            shop.RefuseReason = model.RefuseReason ?? "";//拒绝原因
            shop.BusinessType = model.BusinessType;//入驻类型

            shop.RealName = CurrentUser.RealName ?? "";//管理员(既会员真实姓名)
            Himall.DTO.MemberAccountSafety mMemberAccountSafety = MemberApplication.GetMemberAccountSafety(CurrentUser.Id);
            if (mMemberAccountSafety != null)
            {
                shop.MemberEmail = mMemberAccountSafety.Email ?? "";//邮箱号码
                shop.MemberPhone = mMemberAccountSafety.Phone ?? "";//手机号码
            }

            #region 经营类目
            //shop.BusinessCategory = model.BusinessCategory;//经营项目
            //Dictionary<long, string> keys = new Dictionary<long, string>();
            List<long> catelist = new List<long>();
            if (isbuss)
            {
                var businesss = ShopApplication.GetBusinessCategory(model.Id);
                if (businesss != null && businesss.Count > 0)
                {
                    dynamic businessCategorys = new System.Dynamic.ExpandoObject();
                    foreach (var item in businesss)
                    {
                        if (!catelist.Contains(item.CategoryId))
                        {
                            catelist.Add(item.CategoryId);
                        }
                    }
                }
            }
            shop.BusinessCategory = catelist;//经营项目
            #endregion

            //--下面是企业入驻时一些字段
            //dynamic company = new System.Dynamic.ExpandoObject();
            shop.CompanyName = model.CompanyName ?? "";//公司名称(和个人入驻同用字段)
            shop.CompanyAddress = model.CompanyAddress ?? "";//公司地址(和个人入驻同用字段)
            shop.CompanyRegionId = model.CompanyRegionId;//公司省市区(和个人入驻同用字段)
            shop.CompanyRegionFullName = RegionApplication.GetFullName(model.CompanyRegionId);//省市区
            shop.BusinessLicenceRegionId = model.BusinessLicenceRegionId;//应有执照公司省市区
            shop.BusinessLicenceRegionFullName = RegionApplication.GetFullName(model.BusinessLicenceRegionId) ?? "";//应有执照公司省市区
            shop.CompanyEmployeeCount = model.CompanyEmployeeCount;//员工数
            shop.BusinessLicenceNumber = model.BusinessLicenceNumber ?? "";//营业执照号
            shop.BusinessLicenceNumberPhoto = model.BusinessLicenceNumberPhoto ?? "";//营业执照照片
            shop.BusinessSphere = model.BusinessSphere ?? "";//法定经营范围
            shop.OrganizationCode = model.OrganizationCode ?? "";//组织机构代码
            shop.OrganizationCodePhoto = model.OrganizationCodePhoto ?? "";//组织机构代码图片
            shop.GeneralTaxpayerPhot = model.GeneralTaxpayerPhot ?? "";//一般纳税人证明图片
            shop.BusinessLicenseCert = model.BusinessLicenseCert ?? "";//营业执照证书(经营许可类证书)
            shop.ProductCert = model.ProductCert ?? "";//商品证书
            shop.OtherCert = model.OtherCert ?? "";//其他证书

            //--下面是个人入驻一些字段
            //dynamic personal = new System.Dynamic.ExpandoObject();
            shop.IDCard = model.IDCard ?? "";//
            shop.IDCardUrl = model.IDCardUrl ?? "";//正面照
            shop.IDCardUrl2 = model.IDCardUrl2 ?? "";//背面照

            shop.BankAccountName = model.BankAccountName ?? "";//银行开户名
            shop.BankAccountNumber = model.BankAccountNumber ?? "";//银行账号
            shop.BankCode = model.BankCode ?? "";//支行号
            shop.BankName = model.BankName ?? "";//银行名称
            shop.BankPhoto = model.BankPhoto ?? "";//开户证明
            shop.BankRegionId = model.BankRegionId;//开户银行所在地
            shop.BankRegionFullName = RegionApplication.GetFullName(model.BankRegionId) ?? "";//省市区

            return shop;
        }

        /// <summary>
        /// 入驻设置配置
        /// </summary>
        /// <returns></returns>
        public object GetSettleConfig()
        {
            CheckUserLogin();

            var settled = ShopApplication.GetSettled();//入驻设置配置
            if (settled == null)
                settled = new Settled();

            //--下面是企业入驻必填配置字段
            dynamic company = new System.Dynamic.ExpandoObject();
            company.IsCity = settled.IsCity;
            company.IsPeopleNumber = settled.IsPeopleNumber;
            company.IsAddress = settled.IsAddress;
            company.IsBusinessLicenseCode = settled.IsBusinessLicenseCode;
            company.IsBusinessScope = settled.IsBusinessScope;
            company.IsBusinessLicense = settled.IsBusinessLicense;
            company.IsAgencyCode = settled.IsAgencyCode;
            company.IsAgencyCodeLicense = settled.IsAgencyCodeLicense;
            company.IsTaxpayerToProve = settled.IsTaxpayerToProve;
            company.CompanyVerificationType = settled.CompanyVerificationType;

            //--下面是个人入驻必填配置
            dynamic personal = new System.Dynamic.ExpandoObject();
            personal.IsSName = settled.IsSName;
            personal.IsSCity = settled.IsSCity;
            personal.IsSAddress = settled.IsSAddress;
            personal.IsSIDCard = settled.IsSIDCard;
            personal.IsSIdCardUrl = settled.IsSIdCardUrl;
            personal.SelfVerificationType = settled.SelfVerificationType;


            return new { success = true, msg = "ok", BusinessType = settled.BusinessType, Company = company, Personal = personal };
        }

        /// <summary>
        /// 入驻协议
        /// </summary>
        /// <returns></returns>
        public object GetSeller()
        {
            CheckUserLogin();

            var sellerManager = ManagerApplication.GetSellerManager(CurrentUser.UserName);//检测之前是否已是商家

            if (sellerManager != null)
            {
                var shop = ShopApplication.GetShop(sellerManager.ShopId);
                if (null != shop && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.HasExpired)
                    return new Result() { success = false, msg = "抱歉，您的店铺已过期！" };
                if (null != shop && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze)
                    return new Result() { success = false, msg = "抱歉，您的店铺已冻结！" };

                if (shop != null)
                {
                    var IsSeller = shop.Stage == Entities.ShopInfo.ShopStage.Finish && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Open;
                    if (IsSeller)
                        return new Result() { success = false, msg = "您已是入驻商家！" };

                    var WaitAudit = shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.WaitAudit;
                    if (WaitAudit)
                        return new Result() { success = false, msg = "您入驻商家待审核中！" };
                }
            }

            string SellerAdminAgreement = ShopApplication.GetSellerAgreement();//商家入驻协议内容
            var settled = ShopApplication.GetSettled();//入驻设置配置

            return new { success = true, msg = "ok", SellerAdminAgreement = SellerAdminAgreement, BusinessType = (settled != null ? settled.BusinessType : 0), StartVShop = SiteSettingApplication.SiteSettings.StartVShop };
        }

        /// <summary>
        /// 同意协议
        /// </summary>
        /// <returns></returns>
        public object GetAgreement(string agree, int businessType)
        {
            CheckUserLogin();

            if (agree.Equals("on"))
            {
                var seller = ShopApplication.AddSellerManager(CurrentUser.UserName, CurrentUser.Password, CurrentUser.PasswordSalt);
                var model = ShopApplication.GetShopBasicInfo(seller.ShopId);
                if (model == null || model.Id <= 0)
                    return new { success = false, msg = "注册流程异常，请联系管理员" };

                if (model.Stage != Entities.ShopInfo.ShopStage.Finish || model.ShopStatus == Entities.ShopInfo.ShopAuditStatus.WaitConfirm)
                {
                    if (model.Stage == 0 || model.Stage == Entities.ShopInfo.ShopStage.CompanyInfo)
                    {
                        Shop shop = new Shop();
                        shop.Id = model.Id;
                        shop.ShopName = model.ShopName;
                        shop.BusinessType = businessType == 0 ? ShopBusinessType.Enterprise : ShopBusinessType.Personal;
                        shop.Stage = Entities.ShopInfo.ShopStage.CompanyInfo;
                        ShopApplication.UpdateShop(shop);
                    }
                }


                return new { success = true, msg = "ok", shop = GetShopDynamic(model) };
            }

            return new { success = false, msg = "未同意商家协议不能入驻" };
        }

        /// <summary>
        /// 获取自己商家信息
        /// </summary>
        /// <returns></returns>
        public object GetShop()
        {
            CheckUserLogin();
            var sellerManager = CheckSellerManager();

            var model = ShopApplication.GetShopBasicInfo(sellerManager.ShopId);

            return new { success = true, msg = "ok", shop = GetShopDynamic(model, true) };
        }

        /// <summary>
        /// 商家入驻企业入驻第一步信息保存
        /// </summary>
        /// <param name="shopProfileStep1"></param>
        /// <returns></returns>
        [HttpPost]
        public object PostEditProfile1(ShopProfileStep1 shopProfileStep1)
        {
            CheckUserLogin();
            var sellerManager = CheckSellerManager();

            if (ShopApplication.ExistCompanyName(shopProfileStep1.CompanyName, sellerManager.ShopId))
                return Json(new { success = false, msg = "该公司名已存在！" });
            if (ShopApplication.ExistBusinessLicenceNumber(shopProfileStep1.BusinessLicenceNumber, sellerManager.ShopId))
                return Json(new { success = false, msg = "该营业执照号已存在！" });

            //公司信息
            Himall.DTO.Shop shopInfo = ShopApplication.GetShop(sellerManager.ShopId);
            shopInfo.Id = sellerManager.ShopId;
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
            shopInfo.BusinessLicenceNumberPhoto = shopProfileStep1.BusinessLicenceNumberPhoto;//营业执照号电子版
            shopInfo.OrganizationCode = shopProfileStep1.OrganizationCode;
            shopInfo.OrganizationCodePhoto = shopProfileStep1.OrganizationCodePhoto;//组织机构代码证电子版
            shopInfo.GeneralTaxpayerPhot = shopProfileStep1.GeneralTaxpayerPhoto;//一般纳税人证明
            shopInfo.Stage = Entities.ShopInfo.ShopStage.FinancialInfo;
            shopInfo.BusinessLicenseCert = shopProfileStep1.BusinessLicenseCert;//经营许可类证书
            shopInfo.ProductCert = shopProfileStep1.ProductCert;//产品类证书
            shopInfo.OtherCert = shopProfileStep1.OtherCert;//其它证书
            shopInfo.legalPerson = shopProfileStep1.legalPerson;
            shopInfo.CompanyFoundingDate = shopProfileStep1.CompanyFoundingDate;

            #region 验证必填
            var settled = ShopApplication.GetSettled();//入驻设置配置
            if (settled != null)
            {
                if (settled.IsCity.GetHashCode() == 1 && shopInfo.CompanyRegionId<=0)
                    return Json(new { success = false, msg = "公司所在地必填！" });

                if (settled.IsPeopleNumber.GetHashCode() == 1 && shopInfo.CompanyEmployeeCount <= 0)
                    return Json(new { success = false, msg = "员工总数必填！" });

                if (settled.IsAddress.GetHashCode() == 1 && string.IsNullOrEmpty(shopInfo.CompanyAddress))
                    return Json(new { success = false, msg = "详细地址必填！" });

                if (settled.IsBusinessLicenseCode.GetHashCode() == 1 && string.IsNullOrEmpty(shopInfo.BusinessLicenceNumber))
                    return Json(new { success = false, msg = "营业执照号必填！" });

                if (settled.IsBusinessLicense.GetHashCode() == 1 && string.IsNullOrEmpty(shopInfo.BusinessLicenceNumberPhoto))
                    return Json(new { success = false, msg = "营业执照必填！" });
            }
            #endregion
            ShopApplication.UpdateShop(shopInfo);

            long uid = ShopApplication.GetShopManagers(sellerManager.ShopId);
            if (!string.IsNullOrEmpty(shopProfileStep1.RealName))
            {
                var member = MemberApplication.GetMembers(uid);//修改真实姓名
                member.RealName = shopProfileStep1.RealName;
                MemberApplication.UpdateMember(member);
            }

            if (shopInfo.ShopStatus == Himall.Entities.ShopInfo.ShopAuditStatus.Refuse)
            {
                return Json(new { success = true, msg = "成功！" });//它前面被拒绝审核说明之前已绑定了手机，这里不行再验证手机验证码
            }

            //管理员信息
            var model = MemberApplication.GetMemberAccountSafety(uid);
            if (shopProfileStep1.MemberPhone.Equals("")) return Json(new { success = false, msg = "必须认证手机！" });


            //手机认证
            if (!shopProfileStep1.MemberPhone.Equals(model.Phone))
            {
                string pluginId = "Himall.Plugin.Message.SMS";
                //int result = MemberApplication.CheckMemberCode(pluginId, shopProfileStep1.PhoneCode, shopProfileStep1.MemberPhone, uid);
                int result = MemberApplication.CheckSmallMemberCode(pluginId, shopProfileStep1.PhoneCode, shopProfileStep1.MemberPhone, uid);
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
        /// 商家入驻个人入驻个人信息
        /// </summary>
        /// <param name="shopProfileStep1"></param>
        /// <returns></returns>
        [HttpPost]
        public object PostEditProfiles1(ShopProfileSteps1 shopProfileStep1)
        {
            CheckUserLogin();
            var sellerManager = CheckSellerManager();

            ShopApplication.ClearCacheShop(sellerManager.ShopId);
            var shopInfo = ShopApplication.GetShop(sellerManager.ShopId);
            shopInfo.Id = sellerManager.ShopId;
            shopInfo.CompanyName = shopProfileStep1.CompanyName;
            shopInfo.CompanyAddress = shopProfileStep1.Address;
            shopInfo.CompanyRegionId = shopProfileStep1.CityRegionId;
            shopInfo.CompanyRegionAddress = shopProfileStep1.Address;
            shopInfo.Stage = Entities.ShopInfo.ShopStage.FinancialInfo;
            //shopInfo.BusinessLicenseCert = shopProfileStep1.BusinessLicenseCert;
            //shopInfo.ProductCert = shopProfileStep1.ProductCert;
            //shopInfo.OtherCert = shopProfileStep1.OtherCert;
            shopInfo.IDCard = shopProfileStep1.IDCard;
            shopInfo.IDCardUrl = shopProfileStep1.IDCardUrl;
            shopInfo.IDCardUrl2 = shopProfileStep1.IDCardUrl2;
            #region 验证必填
            var settled = ShopApplication.GetSettled();//入驻设置配置
            if (settled != null)
            {
                if (settled.IsSName.GetHashCode() == 1 && string.IsNullOrEmpty(shopInfo.CompanyName))
                    return Json(new { success = false, msg = "姓名必填！" });

                if (settled.IsSCity.GetHashCode() == 1 && shopInfo.CompanyRegionId <= 0)
                    return Json(new { success = false, msg = "住址必填！" });

                if (settled.IsSAddress.GetHashCode() == 1 && string.IsNullOrEmpty(shopInfo.CompanyAddress))
                    return Json(new { success = false, msg = "详细地址必填！" });

                if (settled.IsSIDCard.GetHashCode() == 1 && string.IsNullOrEmpty(shopInfo.IDCard))
                    return Json(new { success = false, msg = "身份证必填！" });

                if (settled.IsSIdCardUrl.GetHashCode() == 1 && (string.IsNullOrEmpty(shopInfo.IDCardUrl) || string.IsNullOrEmpty(shopInfo.IDCardUrl2)))
                    return Json(new { success = false, msg = "营业执照必填！" });
            }
            #endregion

            ShopApplication.UpdateShop(shopInfo);

            long uid = ShopApplication.GetShopManagers(sellerManager.ShopId);
            if (!string.IsNullOrEmpty(shopProfileStep1.RealName))
            {
                var member = MemberApplication.GetMembers(uid);//修改真实姓名
                member.RealName = shopProfileStep1.RealName;
                MemberApplication.UpdateMember(member);
            }

            if (shopInfo.ShopStatus == Himall.Entities.ShopInfo.ShopAuditStatus.Refuse)
            {
                return Json(new { success = true, msg = "成功！" });//它前面被拒绝审核说明之前已绑定了手机，这里不行再验证手机验证码
            }

            //管理员信息
            var model = MemberApplication.GetMemberAccountSafety(uid);
            if (shopProfileStep1.MemberPhone.Equals("")) return Json(new { success = false, msg = "必须认证手机！" });


            if (shopProfileStep1.MemberPhone != null && !shopProfileStep1.MemberPhone.Equals(model.Phone))
            {
                string pluginId = "Himall.Plugin.Message.SMS";
                //int result = MemberApplication.CheckMemberCode(pluginId, shopProfileStep1.PhoneCode, shopProfileStep1.MemberPhone, uid);
                int result = MemberApplication.CheckSmallMemberCode(pluginId, shopProfileStep1.PhoneCode, shopProfileStep1.MemberPhone, uid);
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
        /// 保存账户信息
        /// </summary>
        /// <param name="shopProfileStep2"></param>
        /// <returns></returns>
        [HttpPost]
        public object PostEditProfile2(ShopProfileStep2 shopProfileStep2)
        {
            CheckUserLogin();
            var sellerManager = CheckSellerManager();

            ShopApplication.ClearCacheShop(sellerManager.ShopId);
            ShopApplication.EditProfile2(sellerManager.ShopId, shopProfileStep2);

            return Json(new { success = true });
        }

        /// <summary>
        /// 第三步店铺信息提交
        /// </summary>
        /// <param name="shopProfileStep3"></param>
        /// <returns></returns>
        [HttpPost]
        public object PostEditProfile3(ShopProfileStep3 shopProfileStep3)
        {
            CheckUserLogin();
            var sellerManager = CheckSellerManager();

            if (shopProfileStep3 == null || string.IsNullOrEmpty(shopProfileStep3.ShopName))
                return Json(new { success = false, msg = "店铺名称不能为空！" });

            bool isNoCate = shopProfileStep3.Categories.Count() <= 0;
            if (!isNoCate)
            {
                isNoCate = true;
                foreach (var cate in shopProfileStep3.Categories)
                {
                    if (cate > 0)
                    {
                        isNoCate = false;
                        break;
                    }
                }
            }

            if (isNoCate)
                return Json(new { success = false, msg = "请至少选择一个经营类目！" });

            int result = ShopApplication.UpdateShop(shopProfileStep3, sellerManager.ShopId);
            if (result.Equals(-1))
            {
                var msg = string.Format("{0} 店铺名称已经存在", shopProfileStep3.ShopName);
                return Json(new { success = false, msg = msg });
            }
            return Json(new { success = true });
        }

        /// <summary>
        /// 获取店铺等级
        /// </summary>
        /// <returns></returns>
        public object GetShopGrades()
        {
            CheckUserLogin();

            var shopGrades = ShopApplication.GetShopGrades();
            return SuccessResult<dynamic>(data: shopGrades);
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        /// <returns></returns>
        public object GetCategoriesAll()
        {
            CheckUserLogin();

            var categories = CategoryApplication.GetCategories();

            List<Category> catelist = new List<Category>();
            var firstcate = categories.Where(t => t.Depth == 1).OrderBy(t => t.DisplaySequence);
            foreach (var fitem in firstcate)
            {
                var second = categories.Where(t => t.ParentCategoryId == fitem.Id).OrderBy(t => t.DisplaySequence).ToList();
                if (second == null || second.Count <= 0)
                    continue;//没二级一级不加,直接返回取下一个

                var newsecond = new List<Category>();
                foreach (var seitem in second)
                {
                    var three = categories.Where(t => t.ParentCategoryId == seitem.Id).OrderBy(t => t.DisplaySequence).ToList();
                    if (three == null || three.Count <= 0)
                        continue;//没三级二级不加,直接返回取下一个
                    seitem.SubCategories = three;
                    newsecond.Add(seitem);//添加三级
                }

                if (newsecond.Count <= 0)
                    continue;//二级下面没二级分类，当前级别不加

                fitem.SubCategories = newsecond;
                catelist.Add(fitem);                
            }

            return SuccessResult<dynamic>(data: catelist);
        }

        /// <summary>
        /// 获取一级分类
        /// </summary>
        /// <returns></returns>
        public object GetCategories()
        {
            CheckUserLogin();

            var categories = CategoryApplication.GetValidBusinessCategoryByParentId(0).ToList();
            return SuccessResult<dynamic>(data: categories);
        }

        /// <summary>
        /// 获取可以做为经营类目子集分类
        /// </summary>
        /// <param name="key">一级分类</param>
        /// <returns>一级分类下的二级分类和三级分类</returns>
        public object GetCategoriesByKey(long? key = null)
        {
            if(key==null)
                return Json(new { success = false, msg = "确实操作key值" });

            var list = CategoryApplication.GetValidBusinessCategoryByParentId(key.Value);
            var data = list.Select(item =>
            {
                return new Himall.DTO.Category()
                {
                    Id = item.Id,
                    Name = item.Name,
                    SubCategories = CategoryApplication.GetValidBusinessCategoryByParentId(item.Id).Map<List<Category>>()
                };
            });
            return Json(new { success = true, data = data });
        }



        protected override bool CheckContact(string contact, out string errorMessage)
        {
            CheckUserLogin();

            errorMessage = string.Empty;
            if (!string.IsNullOrWhiteSpace(contact))
            {
                var isMobile = Core.Helper.ValidateHelper.IsMobile(contact);
                var userMenberInfo = Application.MemberApplication.GetMemberByContactInfo(contact);
                if (CurrentUser != null)
                {
                    if (userMenberInfo != null && userMenberInfo.Id != CurrentUser.Id)
                    {
                        errorMessage = (isMobile ? "手机" : "邮箱") + "号已经存在";//,此手机号已其他绑定
                        return false;
                    }
                }

                //Cache.Insert(_encryptKey + contact, string.Format("{0}:{1:yyyyMMddHHmmss}", CurrentUser.Id, CurrentUser.CreateDate), DateTime.Now.AddHours(1));
                return true;
            }

            return false;
        }
        protected override string CreateCertificate(string contact)
        {
            var identity = Cache.Get<string>(_encryptKey + contact);
            identity = SecureHelper.AESEncrypt(identity, _encryptKey);
            return identity;
        }
    }
}
