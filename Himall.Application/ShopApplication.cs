using System;
using System.Collections.Generic;
using Himall.Service;
using AutoMapper;
using Himall.Core;
using Himall.DTO;
using Himall.Core.Plugins.Message;
using Himall.Entities;
using Himall.DTO.QueryModel;
using Himall.CommonModel;
using System.Linq;
using Himall.DTO.CacheData;

namespace Himall.Application
{
    public class ShopApplication : BaseApplicaion<ShopService>
    {
        private static AppMessageService _appMessageService = ObjectContainer.Current.Resolve<AppMessageService>();

        #region 商家入驻设置
        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="mSettled"></param>
        public static void Settled(Settled mSettled)
        {
            Mapper.CreateMap<Settled, Himall.Entities.SettledInfo>();
            var model = Mapper.Map<Settled, Himall.Entities.SettledInfo>(mSettled);
            if (model.ID > 0)
            {
                SettledApplication.UpdateSettled(model);
            }
            else
            {
                SettledApplication.AddSettled(model);
            }
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <returns></returns>
        public static Settled GetSettled()
        {
            Settled mSettled = new Settled();
            Himall.Entities.SettledInfo mSettledInfo = SettledApplication.GetSettled();
            if (mSettledInfo != null)
            {
                Mapper.CreateMap<Himall.Entities.SettledInfo, Settled>();
                mSettled = Mapper.Map<Himall.Entities.SettledInfo, Settled>(mSettledInfo);
            }
            return mSettled;
        }
        #endregion

        #region 商家入驻流程

        /// <summary>
        /// 添加商家管理员
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static Manager AddSellerManager(string username, string password, string salt)
        {
            var model = ManagerApplication.AddSellerManager(username, password, salt);
            Manager mManagerInfo = new Manager()
            {
                Id = model.Id,
                ShopId = model.ShopId
            };
            return mManagerInfo;
        }

        /// <summary>
        /// 获取店铺信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="businessCategoryOn"></param>
        /// <returns></returns>
        public static Shop GetShop(long id, bool businessCategoryOn = false)
        {
            var shop = Cache.Get<Shop>(CacheKeyCollection.CACHE_SHOPDTO(id, businessCategoryOn));
            if (shop != null) return shop;

            var model = Service.GetShop(id, businessCategoryOn);
            if (model == null) return new Shop();

            shop = Mapper.Map<ShopInfo, Shop>(model);
            Cache.Insert(CacheKeyCollection.CACHE_SHOPDTO(id, businessCategoryOn), shop, 600);
            return shop;
        }

        /// <summary>
        /// 设置商家开启客服平台
        /// </summary>
        /// <param name="id"></param>
        public static void SetShopHiChat(long id)
        {
            Service.SetShopHiChat(id);
        }

        public static ShopInfo GetSelfShop()
        {
            if (Cache.Exists(CacheKeyCollection.CACHE_SELFSHOP))
                return Cache.Get<ShopInfo>(CacheKeyCollection.CACHE_SELFSHOP);

            var model = Service.GetSelfShop();
            Cache.Insert<ShopInfo>(CacheKeyCollection.CACHE_SELFSHOP, model, 600);
            return model;
        }

        /// <summary>
        /// 根据id获取门店
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<Shop> GetShops(IEnumerable<long> ids)
        {
            var list = Service.GetShops(ids);
            return Mapper.Map<List<Shop>>(list);
        }

        /// <summary>
        /// 根据id获取门店
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<ShopInfo> GetShopsByIds(IEnumerable<long> ids)
        {
            var list = Service.GetShops(ids);
            return list;
        }

        /// <summary>
        /// 获取店铺信息（以分页的形式展示）
        /// </summary>
        /// <param name="shopQueryModel">ShopQuery对象</param>
        /// <returns></returns>
        public static QueryPageModel<Shop> GetShops(ShopQuery shopQueryModel)
        {
            var data = Service.GetShops(shopQueryModel);
            return new QueryPageModel<Shop>()
            {
                Models = data.Models.Map<List<Shop>>(),
                Total = data.Total
            };
        }

        public static int GetShopCount(ShopQuery query)
        {
            return BaseApplicaion<ShopService>.Service.GetShopCount(query);
        }

        /// <summary>
        /// 获取商家名称
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static Dictionary<long, string> GetShopNames(List<long> ids)
        {
            var shops = Service.GetShops(ids);
            return shops.ToDictionary(key => key.Id, value => value.ShopName);
        }

        public static Entities.ShopInfo GetShopInfo(long id, bool businessCategoryOn = false)
        {
            var model = Service.GetShop(id, businessCategoryOn);
            return model;
        }
        /// <summary>
        /// 商家入驻第二部
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ShopProfileStep1 GetShopProfileStep1(long id, out long CompanyRegionId, out long BusinessLicenceRegionId, out string RefuseReason)
        {
            var shop = Service.GetShop(id);

            var step1 = new ShopProfileStep1();
            step1.Address = shop.CompanyAddress;


            step1.BusinessLicenceArea = shop.BusinessLicenceRegionId;
            step1.BusinessLicenceNumber = shop.BusinessLicenceNumber;
            step1.BusinessLicenceNumberPhoto = shop.BusinessLicenceNumberPhoto;
            if (shop.BusinessLicenceEnd.HasValue)
                step1.BusinessLicenceValidEnd = shop.BusinessLicenceEnd.Value;

            if (shop.BusinessLicenceStart.HasValue)
                step1.BusinessLicenceValidStart = shop.BusinessLicenceStart.Value;
            string BusinessLicenseCert = string.Empty;
            for (int i = 1; i < 4; i++)
            {
                if (HimallIO.ExistFile(shop.BusinessLicenseCert + string.Format("{0}.png", i)))
                {
                    BusinessLicenseCert += shop.BusinessLicenseCert + string.Format("{0}.png", i) + ",";
                }
            }
            step1.BusinessLicenseCert = BusinessLicenseCert.TrimEnd(',');
            step1.BusinessSphere = shop.BusinessSphere;
            step1.CityRegionId = shop.CompanyRegionId;
            if (shop.CompanyFoundingDate.HasValue)
                step1.CompanyFoundingDate = shop.CompanyFoundingDate.Value;
            step1.CompanyName = shop.CompanyName;
            step1.ContactName = shop.ContactsName;
            step1.ContactPhone = shop.ContactsPhone;
            step1.Email = shop.ContactsEmail;
            step1.EmployeeCount = shop.CompanyEmployeeCount;
            step1.GeneralTaxpayerPhoto = shop.GeneralTaxpayerPhot;
            step1.legalPerson = shop.legalPerson;
            step1.OrganizationCode = shop.OrganizationCode;
            step1.OrganizationCodePhoto = shop.OrganizationCodePhoto;
            step1.BusinessType = shop.BusinessType;

            string OtherCert = string.Empty;
            for (int i = 1; i < 4; i++)
            {
                if (HimallIO.ExistFile(shop.OtherCert + string.Format("{0}.png", i)))
                {
                    OtherCert += shop.OtherCert + string.Format("{0}.png", i) + ",";
                }
            }
            step1.OtherCert = OtherCert.TrimEnd(',');
            step1.Phone = shop.CompanyPhone;

            string ProductCert = string.Empty;
            for (int i = 1; i < 4; i++)
            {
                if (HimallIO.ExistFile(shop.ProductCert + string.Format("{0}.png", i)))
                {
                    ProductCert += shop.ProductCert + string.Format("{0}.png", i) + ",";
                }
            }
            step1.ProductCert = ProductCert.TrimEnd(',');
            step1.RegisterMoney = shop.CompanyRegisteredCapital;
            step1.taxRegistrationCert = shop.TaxRegistrationCertificate;
            step1.Settled = GetSettled();

            CompanyRegionId = shop.CompanyRegionId;
            BusinessLicenceRegionId = shop.BusinessLicenceRegionId;
            RefuseReason = null;
            if (shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Refuse) RefuseReason = shop.RefuseReason;

            return step1;
        }

        /// <summary>
        /// 个人入驻第二部信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="CompanyRegionId"></param>
        /// <param name="BusinessLicenceRegionId"></param>
        /// <param name="RefuseReason"></param>
        /// <returns></returns>
        public static ShopProfileSteps1 GetShopProfileSteps1(long id, out long CompanyRegionId, out long BusinessLicenceRegionId, out string RefuseReason)
        {
            var shop = Service.GetShop(id);

            var step1 = new ShopProfileSteps1();
            step1.Address = shop.CompanyAddress;

            step1.CityRegionId = shop.CompanyRegionId;
            step1.CompanyName = shop.CompanyName;

            step1.IDCard = shop.IDCard;
            step1.IDCardUrl = shop.IDCardUrl;
            step1.IDCardUrl2 = shop.IDCardUrl2;
            step1.BusinessType = shop.BusinessType;
            step1.Settled = GetSettled();

            CompanyRegionId = shop.CompanyRegionId;
            BusinessLicenceRegionId = shop.BusinessLicenceRegionId;
            RefuseReason = null;
            if (shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Refuse) RefuseReason = shop.RefuseReason;

            return step1;
        }
        /// <summary>
        /// 获取商家入驻第三部信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ShopProfileStep2 GetShopProfileStep2(long id, out Entities.ShopInfo.ShopStage Stage)
        {
            var shop = Service.GetShop(id);
            var model = new ShopProfileStep2()
            {
                BankAccountName = shop.BankAccountName == null ? "" : shop.BankAccountName,
                BankAccountNumber = shop.BankAccountNumber == null ? "" : shop.BankAccountNumber,
                BankCode = shop.BankCode == null ? "" : shop.BankCode,
                BankName = shop.BankName == null ? "" : shop.BankName,
                BankPhoto = shop.BankPhoto == null ? "" : shop.BankPhoto,
                BankRegionId = shop.BankRegionId,
                TaxpayerId = shop.TaxpayerId == null ? "" : shop.TaxpayerId,
                TaxRegistrationCertificate = shop.TaxRegistrationCertificate == null ? "" : shop.TaxRegistrationCertificate,
                TaxRegistrationCertificatePhoto = shop.TaxRegistrationCertificatePhoto == null ? "" : shop.TaxRegistrationCertificatePhoto,
                WeiXinAddress = shop.WeiXinAddress == null ? "" : shop.WeiXinAddress,
                WeiXinNickName = shop.WeiXinNickName == null ? "" : shop.WeiXinNickName,
                WeiXinOpenId = shop.WeiXinOpenId == null ? "" : shop.WeiXinOpenId,
                WeiXinSex = shop.WeiXinSex == null ? 0 : shop.WeiXinSex.Value,
                WeiXinTrueName = shop.WeiXinTrueName == null ? "" : shop.WeiXinTrueName,
                BusinessType = shop.BusinessType,
                Settled = GetSettled()
            };
            Stage = shop.Stage;
            return model;
        }

        /// <summary>
        /// 商家入驻协议
        /// </summary>
        /// <returns></returns>
        public static string GetSellerAgreement()
        {
            var model = SystemAgreementApplication.GetAgreement(Entities.AgreementInfo.AgreementTypes.Seller);
            if (model != null)
                return model.AgreementContent;
            else
                return "";
        }


        /// <summary>
        /// 商家入驻店铺信息更新
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ShopId"></param>
        /// <returns>0、失败；1、成功；-1、店铺名称已经存在</returns>
        public static int UpdateShop(ShopProfileStep3 model, long ShopId)
        {
            int result = 0;
            if (Service.ExistShop(model.ShopName, ShopId))
            {
                result = -1;
            }
            else
            {
                Entities.ShopInfo shopInfo = Service.GetShop(ShopId);
                shopInfo.Id = ShopId;
                shopInfo.ShopName = model.ShopName;
                shopInfo.GradeId = model.ShopGrade;
                shopInfo.Stage = Entities.ShopInfo.ShopStage.UploadPayOrder;
                var shopCategories = model.Categories;
                Service.UpdateShop(shopInfo, model.Categories.ToList());
                ClearCacheShop(ShopId);
                result = 1;
            }
            return result;
        }
        /// <summary>
        /// 清除门店缓存
        /// </summary>
        /// <param name="shop"></param>
        public static void ClearCacheShop(long shop)
        {
            //TODO:FG 缓存策略需要统一至 应用层
            Service.ClearShopCache(shop);
        }

        #endregion

        #region 店铺信息
        /// <summary>
        /// 商店信息更新
        /// </summary>
        /// <param name="model"></param>
        public static void EditProfile2(long shopId, ShopProfileStep2 shopProfileStep2)
        {
            Himall.DTO.Shop shopInfo = ShopApplication.GetShop(shopId);
            shopInfo.Id = shopId;
            shopInfo.BankAccountName = shopProfileStep2.BankAccountName;
            shopInfo.BankAccountNumber = shopProfileStep2.BankAccountNumber;
            shopInfo.BankCode = shopProfileStep2.BankCode;
            shopInfo.BankName = shopProfileStep2.BankName;
            shopInfo.BankPhoto = shopProfileStep2.BankPhoto;
            shopInfo.BankRegionId = shopProfileStep2.BankRegionId;
            shopInfo.TaxpayerId = shopProfileStep2.TaxpayerId;
            shopInfo.TaxRegistrationCertificate = shopProfileStep2.TaxRegistrationCertificate;
            shopInfo.TaxRegistrationCertificatePhoto = shopProfileStep2.TaxRegistrationCertificatePhoto;
            shopInfo.Stage = Entities.ShopInfo.ShopStage.ShopInfo;

            UpdateShop(shopInfo);
        }

        /// <summary>
        /// 商店信息更新
        /// </summary>
        /// <param name="model"></param>
        public static void UpdateShop(Shop model)
        {
            var mShop = Mapper.Map<Shop, ShopInfo>(model);
            Service.UpdateShop(mShop);
            ClearCacheShop(model.Id);
        }





        /// <summary>
        /// 判断公司名称是否存在
        /// </summary>
        /// <param name="companyName">公司名字</param>
        /// <param name="shopId"></param>
        public static bool ExistCompanyName(string companyName, long shopId = 0)
        {
            return Service.ExistCompanyName(companyName, shopId);
        }

        /// <summary>
        /// 检测营业执照号是否重复
        /// </summary>
        /// <param name="BusinessLicenceNumber">营业执照号</param>
        /// <param name="shopId"></param>
        public static bool ExistBusinessLicenceNumber(string BusinessLicenceNumber, long shopId = 0)
        {
            return Service.ExistBusinessLicenceNumber(BusinessLicenceNumber, shopId);
        }

        /// <summary>
        /// 获取店铺等级列表
        /// </summary>
        /// <returns></returns>
        public static List<ShopGrade> GetShopGrades()
        {
            List<ShopGrade> lmShopGrade = new List<ShopGrade>();
            var model = Service.GetShopGrades();
            foreach (var item in model)
            {
                Mapper.CreateMap<ShopGradeInfo, ShopGrade>();
                lmShopGrade.Add(Mapper.Map<ShopGradeInfo, ShopGrade>(item));
            }
            return lmShopGrade;
        }

        /// <summary>
        /// 获取店铺账户信息
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<ShopAccount> GetShopAccounts(List<long> ids)
        {
            return Service.GetShopAccounts(ids).Map<List<ShopAccount>>();
        }

        /// <summary>
        /// 获取单个店铺账户信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ShopAccount GetShopAccount(long id)
        {
            return Service.GetShopAccount(id).Map<ShopAccount>();
        }

        /// <summary>
        /// 获取店铺经营项目
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<BusinessCategory> GetBusinessCategory(long id)
        {
            List<BusinessCategory> lvBusinessCategory = new List<BusinessCategory>();
            var model = Service.GetBusinessCategory(id);
            foreach (var item in model)
            {
                lvBusinessCategory.Add(new BusinessCategory()
                {
                    Id = item.Id,
                    CategoryId = item.CategoryId,
                    CategoryName = item.CategoryName,
                    ShopId = item.ShopId
                });
            }
            return lvBusinessCategory;
        }

        /// <summary>
        /// 获取店铺宝贝数
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static int GetShopProductCount(long shopId)
        {
            return Service.GetShopProductCount(shopId);
        }

        /// <summary>
        /// 获取单个入驻缴费记录
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static SettledPayment GetSettledPaymentRecord(long id)
        {
            var item = Service.GetShopRenewRecord(id);
            var shopName = Service.GetShop(item.ShopId).ShopName;
            SettledPayment model = new SettledPayment();
            model.Id = item.Id;
            model.OperateType = item.OperateType.ToDescription();
            model.OperateDate = item.OperateDate.ToString("yyyy-MM-dd HH:mm");
            model.Operate = item.Operator;
            model.Content = item.OperateContent;
            model.Amount = item.Amount;
            model.ShopName = shopName;
            return model;
        }
        /// <summary>
        /// 获取关注店铺数量
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static int GetUserConcernShopsCount(long userid)
        {
            var result = BaseApplicaion<ShopService>.Service.GetUserConcernShops(userid, 1, int.MaxValue);
            return result.Total;
        }



        #endregion


        #region 管理员认证
        /// <summary>
        /// 获取店铺管理员ID
        /// </summary>
        /// <param name="ShopId">店铺ID</param>
        /// <returns></returns>
        public static long GetShopManagers(long ShopId)
        {
            long UserId = Service.GetShopManagers(ShopId);
            return UserId;
        }

        /// <summary>
        /// <summary>
        /// 是否官方自营店
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static bool IsSelfShop(long shopId)
        {
            return Service.IsSelfShop(shopId);
        }



        #endregion
      
        
        /// <summary>
        /// 获取指定店铺等级信息
        /// </summary>
        /// <param name="id">店铺等级Id</param>
        /// <returns></returns>
        public static ShopGrade GetShopGrade(long id)
        {
            Mapper.CreateMap<ShopGradeInfo, ShopGrade>();
            return Service.GetShopGrade(id).Map<DTO.ShopGrade>();
        }

        /// <summary>
        /// 是否已过期
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static bool IsExpiredShop(long shopId)
        {
            return Service.IsExpiredShop(shopId);
        }

        /// <summary>
        /// 是否冻结
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static bool IsFreezeShop(long shopId)
        {
            return Service.IsFreezeShop(shopId);
        }



        /// <summary>
        /// 根据网店管家uCode获取对应店铺
        /// </summary>
        /// <param name="uCode"></param>
        /// <returns></returns>
        public static ShopWdgjSettingInfo GetshopWdgjInfoByCode(string uCode)
        {
            return Service.GetshopInfoByCode(uCode);
        }
        
        /// <summary>
        /// 更新商家入驻类型
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        public static void SetBusinessType(long id, ShopBusinessType type)
        {
            Service.SetBusinessType(id, type);
            ClearCacheShop(id);
        }

        #region 门店设置
        public static void SetProvideInvoice(ShopInvoiceConfigInfo info)
        {
            Service.SetProvideInvoice(info);
            ClearCacheShop(info.ShopId);
        }

        public static void SetShopFreight(long id, decimal freight, decimal freeFreight)
        {
            Service.SetShopFreight(id, freight, freeFreight);
            ClearCacheShop(id);
        }

        public static void SetAutoAllotOrder(long id, bool enable)
        {
            Service.SetAllotOrder(id, enable);
            ClearCacheShop(id);
        }
        /// <summary>
        /// 设置店铺公司信息
        /// </summary>
        /// <param name="info"></param>
        public static void SetCompanyInfo(ShopCompanyInfo info)
        {
            Service.SetCompnayInfo(info);
            ClearCacheShop(info.ShopId);
        }
        /// <summary>
        /// 设置店铺银行帐户
        /// </summary>
        /// <param name="bankAccount"></param>
        public static void SetBankAccount(BankAccount bankAccount)
        {
            Service.SetBankAccount(bankAccount);
            ClearCacheShop(bankAccount.ShopId);
        }

        /// <summary>
        /// 设置店铺银行帐户
        /// </summary>
        /// <param name="bankAccount"></param>
        public static void UpdateBankAccount(BankAccount bankAccount)
        {
            Service.UpdateBankAccount(bankAccount);
            ClearCacheShop(bankAccount.ShopId);
        }

        /// <summary>
        /// 设置店铺微信帐户
        /// </summary>
        /// <param name="account"></param>
        public static void SetWeChatAccount(WeChatAccount account)
        {
            Service.SetWeChatAccount(account);
            ClearCacheShop(account.ShopId);
        }

        public static void SetLicenseCert(ShopLicenseCert model)
        {
            Service.SetLicenseCert(model);
            ClearCacheShop(model.ShopId);
        }

        public static void SetAutoPrint(long id, bool enable)
        {
            Service.SetAutoPrint(id, enable);
            ClearCacheShop(id);
        }
        public static void SetPrintCount(long id, int count)
        {
            Service.SetPrintCount(id, count);
            ClearCacheShop(id);
        }

        #endregion

        #region 商家缴费回调
        /// <summary>
        /// 商家缴费回调
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="userName"></param>
        /// <param name="balance"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public static void ShopReNewPayNotify(string tradeNo, long shopId, string userName, decimal balance, int type, int value, bool isShopAccount = false)
        {
            Entities.ShopRenewRecordInfo model = new Entities.ShopRenewRecordInfo();
            model.TradeNo = tradeNo;
            //添加店铺续费记录
            model.ShopId = shopId;
            model.OperateDate = DateTime.Now;
            model.Operator = userName;
            model.Amount = balance;
            //续费操作
            if (type == 1)
            {
                model.OperateType = Entities.ShopRenewRecordInfo.EnumOperateType.ReNew;
                var shopInfo = Service.GetShop(shopId);
                DateTime beginTime = shopInfo.EndDate;
                if (beginTime < DateTime.Now)
                    beginTime = DateTime.Now;
                string strNewEndTime = beginTime.AddYears(value).ToString("yyyy-MM-dd");
                model.OperateContent = "续费 " + value + " 年至 " + strNewEndTime;
                Service.AddShopRenewRecord(model, isShopAccount);
                //店铺续费
                Service.ShopReNew(shopId, value);
            }
            //升级操作
            else
            {
                model.ShopId = shopId;
                model.OperateType = Entities.ShopRenewRecordInfo.EnumOperateType.Upgrade;
                var shopInfo = Service.GetShop(shopId);
                var shopGrade = Service.GetShopGrades().Where(c => c.Id == shopInfo.GradeId).FirstOrDefault();
                var newshopGrade = Service.GetShopGrades().Where(c => c.Id == (long)value).FirstOrDefault();
                model.OperateContent = "将套餐‘" + shopGrade.Name + "'升级为套餐‘" + newshopGrade.Name + "'";
                Service.AddShopRenewRecord(model, isShopAccount);
                //店铺升级
                Service.ShopUpGrade(shopId, (long)value);
            }
            ClearCacheShop(shopId);//清除当前商家缓存
        }
        #endregion
        public static bool UpdateOpenTopImageAd(long shopId, bool isOpenTopImageAd)
        {
            return Service.UpdateOpenTopImageAd(shopId, isOpenTopImageAd);
        }

        public static long GetShopDisplaySales(long shop)
        {
            var sale = Service.GetSales(shop);
            var pro = GetService<ProductService>();
            var virtualSale = pro.GetProductVirtualSale(shop);
            return sale + virtualSale;
        }
        public static ShopInfo GetShopBasicInfo(long id)
        {
            return Service.GetShopBasicInfo(id);
        }

        public static List<ShopBrand> GetShopBrands(List<long> shops)
        {
            return Service.GetShopBrands(shops);
        }

        public static bool IsOpenHichat(long id)
        {
            return Service.IsOpenHichat(id);
        }

        public static bool HasProvideInvoice(List<long> shops)
        {
            return Service.HasProvideInvoice(shops);
        }

        /// <summary>
        /// 设置申请商家步骤
        /// </summary>
        /// <param name="shopStage">第几步</param>
        /// <param name="id">店铺Id</param>
        public static void SetShopStage(ShopInfo.ShopStage shopStage, long id)
        {
            Service.SetShopStage(shopStage, id);
            ClearCacheShop(id);
        }

        public static StatisticOrderComment GetStatisticOrderComment(long shop)
        {
            return CacheManager.GetStatisticOrderComment(shop, () => {
                var data = Service.GetShopStatisticOrderComments(shop);
                return new StatisticOrderComment
                {
                    ShopId = shop,
                    ProductAndDescription = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.ProductAndDescription),
                    ProductAndDescriptionMax = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.ProductAndDescriptionMax),
                    ProductAndDescriptionMin = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.ProductAndDescriptionMin),
                    ProductAndDescriptionPeer = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.ProductAndDescriptionPeer),
                    SellerDeliverySpeed = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.SellerDeliverySpeed),
                    SellerDeliverySpeedMax = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.SellerDeliverySpeedMax),
                    SellerDeliverySpeedMin = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.SellerDeliverySpeedMin),
                    SellerDeliverySpeedPeer = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.SellerDeliverySpeedPeer),
                    SellerServiceAttitude = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.SellerServiceAttitude),
                    SellerServiceAttitudeMax = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.SellerServiceAttitudeMax),
                    SellerServiceAttitudeMin = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.SellerServiceAttitudeMin),
                    SellerServiceAttitudePeer = GetStatisticOrderCommentValue(data, StatisticOrderCommentInfo.EnumCommentKey.SellerServiceAttitudePeer),
                };
            });

        }
        public static List<StatisticOrderComment> GetStatisticOrderComment(List<long> shops) =>
            shops.Select(i => GetStatisticOrderComment(i)).ToList();

        public static ShopMarksData GetMarks(long id) =>
            Service.GetMarks(id);

        private static decimal GetStatisticOrderCommentValue(List<StatisticOrderCommentInfo> data, StatisticOrderCommentInfo.EnumCommentKey key)
        {
            return data.FirstOrDefault(c => c.CommentKey == key)?.CommentValue ?? 5;
        }

        #region TDO:ZYF Invoice
        /// <summary>
        /// 获取商家发票管理配置
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static ShopInvoiceConfigInfo GetShopInvoiceConfig(long shopId)
        {
            return Service.GetShopInvoiceConfig(shopId);
        }

        /// <summary>
        /// 获取商家发票类型列表
        /// </summary>
        /// <param name="shopid"></param>
        /// <returns></returns>
        public static List<InvoiceTypes> GetInvoiceTypes(long shopid)
        {
            List<long> shops = new List<long>();
            shops.Add(shopid);
            var isInvoice = Service.HasProvideInvoice(shops);
            if (!isInvoice)
                return null;
            else
            {
                var config = GetShopInvoiceConfig(shopid);
                List<InvoiceTypes> types = new List<InvoiceTypes>();

                if (config.IsPlainInvoice)
                {
                    var type = new InvoiceTypes()
                    {
                        Id = InvoiceType.OrdinaryInvoices.GetHashCode(),
                        Name = InvoiceType.OrdinaryInvoices.ToDescription(),
                        Rate = config.PlainInvoiceRate
                    };
                    types.Add(type);
                }
                if (config.IsElectronicInvoice)
                {
                    var type = new InvoiceTypes()
                    {
                        Id = InvoiceType.ElectronicInvoice.GetHashCode(),
                        Name = InvoiceType.ElectronicInvoice.ToDescription(),
                        Rate = config.PlainInvoiceRate
                    };
                    types.Add(type);
                }
                if (config.IsVatInvoice)
                {
                    var type = new InvoiceTypes()
                    {
                        Id = InvoiceType.VATInvoice.GetHashCode(),
                        Name = InvoiceType.VATInvoice.ToDescription(),
                        Rate = config.VatInvoiceRate
                    };
                    types.Add(type);
                }
                return types;
            }
        }

        /// <summary>
        /// 获取用户默认的发票信息
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static InvoiceTitleInfo GetInvoiceTitleInfo(long userid, InvoiceType type)
        {
            return Service.GetInvoiceTitleInfo(userid, type);
        }
        #endregion

        /// <summary>
        /// 获取经营类目
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<BusinessCategoryInfo> GetBusinessCategoryList(BusinessCategoryQuery query)
        {
            return Service.GetBusinessCategoryList(query);
        }

        /// <summary>
        /// 获取经营类目
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<BusinessCategoryApplyDetailInfo> GetBusinessCategoriesApplyDetails(long applyid)
        {
            var categories = CategoryApplication.GetCategories();//有效商品
            var details = Service.GetBusinessCategoriesApplyDetails(applyid);

            if (categories == null && details == null)
                return new List<BusinessCategoryApplyDetailInfo>();

            details = details.Where(t => categories.Select(p => p.Id).Contains(t.CategoryId)).ToList();//只读取有效经验类目(已删除的不读取)
            if (details == null)
                return new List<BusinessCategoryApplyDetailInfo>();

            foreach (var item in details)
            {
                var path = CategoryApplication.GetCategoryPath(categories, item.CategoryId);
                item.CatePath = string.Join(">", path.Select(p => p.Name));
            }
            return details;
        }

        public static void AutoExpire()
        {
            Service.AutoExpire();
        }
    }
}
