using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Himall.Core;
using Himall.CommonModel;
using NetRube.Data;
using Himall.DTO;

namespace Himall.Service
{
    public class CashDepositsService : ServiceBase
    {
        public QueryPageModel<CashDepositInfo> GetCashDeposits(CashDepositQuery query)
        {
            var db = DbFactory.Default.Get<CashDepositInfo>().LeftJoin<ShopInfo>((ci, si) => ci.ShopId == si.Id);
            var scd = DbFactory.Default.Get<CashDepositInfo>().LeftJoin<ShopInfo>((ci, si) => ci.ShopId == si.Id);
            if (!string.IsNullOrWhiteSpace(query.ShopName))
            {
                db.Where<ShopInfo>(item => item.ShopName.Contains(query.ShopName));
                scd.Where<ShopInfo>(item => item.ShopName.Contains(query.ShopName));
            }
            var c = scd.ToList();
            List<long> shopIds = new List<long>();
            foreach (var cashDeposit in c)
            {
                var needPayShop = GetNeedPayCashDepositByShopId(cashDeposit.ShopId);
                if (needPayShop > 0)
                {
                    shopIds.Add(cashDeposit.ShopId);
                }
            }
            if (query.Type.HasValue)
            {
                if (query.Type.Value == false)
                    db.Where(item => item.ShopId.ExIn(shopIds));
                else
                    db.Where(item => item.ShopId.ExNotIn(shopIds));
            }

            switch (query.Sort.ToLower())
            {
                case "totalbalance":
                    if (query.IsAsc) db.OrderBy(p => p.TotalBalance);
                    else db.OrderByDescending(p => p.TotalBalance);
                    break;
                case "currentbalance":
                    if (query.IsAsc) db.OrderBy(p => p.CurrentBalance);
                    else db.OrderByDescending(p => p.CurrentBalance);
                    break;
                case "date":
                    if (query.IsAsc) db.OrderBy(p => p.Date);
                    else db.OrderByDescending(p => p.Date);
                    break;
                //case "needPay":
                //需业务计算未实现
                default:
                    db.OrderByDescending(o => o.Date);
                    break;
            }


            var models = db.Select().ToPagedList(query.PageNo, query.PageSize);
            QueryPageModel<CashDepositInfo> pageModel = new QueryPageModel<CashDepositInfo>() { Models = models, Total = models.TotalRecordCount };
            return pageModel;
        }

        public QueryPageModel<CashDepositDetailInfo> GetCashDepositDetails(CashDepositDetailQuery query)
        {
            //IQueryable<CashDepositDetailInfo> cashDepositDetails = Context.CashDepositDetailInfo.AsQueryable();
            var cashDepositDetails = DbFactory.Default.Get<CashDepositDetailInfo>();
            if (query.StartDate.HasValue)
            {
                cashDepositDetails.Where(item => query.StartDate <= item.AddDate);
            }
            if (query.EndDate.HasValue)
            {
                cashDepositDetails.Where(item => query.EndDate >= item.AddDate);
            }
            if (!string.IsNullOrWhiteSpace(query.Operator))
            {
                cashDepositDetails.Where(item => item.Operator.Contains(query.Operator));
            }
            cashDepositDetails.Where(item => item.CashDepositId == query.CashDepositId);
            //int total = 0;
            //cashDepositDetails = cashDepositDetails.FindBy(item => item.CashDepositId == query.CashDepositId, query.PageNo, query.PageSize, out total, item => item.AddDate, false);
            var models = cashDepositDetails.OrderByDescending(p => p.AddDate).ToPagedList(query.PageNo, query.PageSize);
            QueryPageModel<CashDepositDetailInfo> pageModel = new QueryPageModel<CashDepositDetailInfo>() { Models = models, Total = models.TotalRecordCount };
            return pageModel;
        }

        public CashDepositInfo GetCashDeposit(long id)
        {
            return DbFactory.Default.Get<CashDepositInfo>().Where(p => p.Id == id).FirstOrDefault();
        }

        public bool ShopAccountRecord(long shopId, decimal amount, string remark, string detailId)
        {
            var success= DbFactory.Default.InTransaction(() =>
            {
                var ShopAccount = DbFactory.Default.Get<ShopAccountInfo>().Where(a => a.ShopId == shopId).FirstOrDefault();
                ShopAccountItemInfo info = new ShopAccountItemInfo();
                info.IsIncome = false;
                info.ShopId = ShopAccount.ShopId;
                info.DetailId = detailId;
                info.ShopName = ShopAccount.ShopName;
                info.AccountNo = shopId + info.DetailId + new Random().Next(10000);
                info.ReMark = remark;
                info.TradeType = ShopAccountType.CashDeposit;
                info.CreateTime = DateTime.Now;
                info.Amount = amount;
                info.AccoutID = ShopAccount.Id;
                ShopAccount.Balance -= info.Amount;//总余额减钱
                info.Balance = ShopAccount.Balance;//变动后当前剩余金额

                DbFactory.Default.Add(info);
                DbFactory.Default.Update(ShopAccount);
            });
            CacheManager.ClearShopCashDeposit(shopId);
            return success;
        }

        public void AddCashDepositDetails(CashDepositDetailInfo cashDepositDetail)
        {
            DbFactory.Default.Add(cashDepositDetail);

            CashDepositInfo cashDeposit = DbFactory.Default.Get<CashDepositInfo>().Where(p => p.Id == cashDepositDetail.CashDepositId).FirstOrDefault();
            if (cashDepositDetail.Balance < 0 && cashDeposit.CurrentBalance + cashDepositDetail.Balance < 0)
                new HimallException("扣除金额不能多余店铺可用余额");
            cashDeposit.CurrentBalance = cashDeposit.CurrentBalance + cashDepositDetail.Balance;
            if (cashDepositDetail.Balance > 0)
            {
                cashDeposit.EnableLabels = true;
            }
            if (cashDepositDetail.Balance > 0)
            {
                cashDeposit.TotalBalance = cashDeposit.TotalBalance + cashDepositDetail.Balance;
                cashDeposit.Date = DateTime.Now;
            }
            DbFactory.Default.Update(cashDeposit);
            CacheManager.ClearShopCashDeposit(cashDeposit.ShopId);
        }


        public void AddCashDeposit(CashDepositInfo cashDeposit, List<CashDepositDetailInfo> details)
        {
            DbFactory.Default.Add(cashDeposit);
            if (details != null && details.Count > 0)
            {
                details.ForEach(p => p.CashDepositId = cashDeposit.Id);
                DbFactory.Default.AddRange(details);
            }
            CacheManager.ClearShopCashDeposit(cashDeposit.ShopId);
        }
        public CashDepositInfo GetCashDepositByShopId(long shopId)=>
            CacheManager.GetShopCashDeposit(shopId, () =>
            {
                return DbFactory.Default.Get<CashDepositInfo>()
                   .Where(item => item.ShopId == shopId)
                   .FirstOrDefault();
            });

        public decimal GetNeedPayCashDepositByShopId(long shopId)
        {
            decimal needCashDeposit = 0.00M;
            var shopService = ServiceProvider.Instance<ShopService>.Create;
            var shopCategoryService = ServiceProvider.Instance<ShopCategoryService>.Create;

            var categories = shopCategoryService.GetBusinessCategory(shopId);
            var mainCategories = categories.Where(item => item.ParentCategoryId == 0).Select(item => item.Id).ToList();

            var cashDeposits = DbFactory.Default.Get<CategoryCashDepositInfo>().Where(item => item.CategoryId.ExIn(mainCategories));
            decimal needCashDeposits = -1;
            if (cashDeposits.Count() > 0)
            {
                needCashDeposits = DbFactory.Default.Get<CategoryCashDepositInfo>().Where(item => item.CategoryId.ExIn(mainCategories))
                    .Max<decimal>(item => item.NeedPayCashDeposit);
            }
            var cashDeposit = DbFactory.Default.Get<CashDepositInfo>().Where(item => item.ShopId == shopId).FirstOrDefault();
            if (cashDeposit != null && cashDeposit.CurrentBalance < needCashDeposits)
            {
                needCashDeposit = needCashDeposits - cashDeposit.CurrentBalance;
            }
            if (cashDeposit == null)
            {
                needCashDeposit = needCashDeposits;
            }
            return needCashDeposit;
        }

        public void UpdateEnableLabels(long id, bool enableLabels)
        {
            DbFactory.Default.Set<CashDepositInfo>()
                .Set(n => n.EnableLabels, enableLabels)
                .Where(p => p.Id == id)
                .Succeed();
            var model = DbFactory.Default.Get<CashDepositInfo>(p => p.Id == id).FirstOrDefault();
            CacheManager.ClearShopCashDeposit(model.ShopId);
        }

        #region 类目保证金
        public List<CategoryCashDepositInfo> GetCategoryCashDeposits() =>
            CacheManager.GetCategoryCashDeposit(() => DbFactory.Default.Get<CategoryCashDepositInfo>().ToList());

        public void AddCategoryCashDeposits(CategoryCashDepositInfo model)
        {
            DbFactory.Default.Add(model);
            CacheManager.ClearCategoryCashDeposit();
        }
        public void DeleteCategoryCashDeposits(long categoryId)
        {
            DbFactory.Default.Del<CategoryCashDepositInfo>(item => item.CategoryId == categoryId);
            CacheManager.ClearCategoryCashDeposit();
        }

        public void UpdateNeedPayCashDeposit(long categoryId, decimal cashDeposit)
        {
            DbFactory.Default.Set<CategoryCashDepositInfo>().Set(n => n.NeedPayCashDeposit, cashDeposit).Where(p => p.CategoryId == categoryId).Succeed();
            CacheManager.ClearCategoryCashDeposit();
        }
        public void OpenNoReasonReturn(long categoryId)
        {
            DbFactory.Default.Set<CategoryCashDepositInfo>().Set(n => n.EnableNoReasonReturn, true).Where(p => p.CategoryId == categoryId).Succeed();
            CacheManager.ClearCategoryCashDeposit();
        }
        public void CloseNoReasonReturn(long categoryId)
        {
            DbFactory.Default.Set<CategoryCashDepositInfo>().Set(n => n.EnableNoReasonReturn, false).Where(p => p.CategoryId == categoryId).Succeed();
            CacheManager.ClearCategoryCashDeposit();
        }

        public ProductEnsure GetProductEnsure(long productId)
        {
            ProductEnsure ensure = new ProductEnsure()
            {
                IsCustomerSecurity = false,
                IsSevenDayNoReasonReturn = false,
                IsTimelyShip = false
            };
            var productService = ServiceProvider.Instance<ProductService>.Create;
            var shopService = ServiceProvider.Instance<ShopService>.Create;
            var shopCategoryService = ServiceProvider.Instance<ShopCategoryService>.Create;
            var categoryService = ServiceProvider.Instance<CategoryService>.Create;


            var product = productService.GetProduct(productId);
            if (product == null || product.Id <= 0)
                return ensure;

            var shop = shopService.GetShop(product.ShopId);
            if (shop == null || shop.Id <= 0)
                return ensure;

            //var cashDeposit = Context.CashDepositInfo.Where(item => item.ShopId == shop.Id).FirstOrDefault();
            var cashDeposit = DbFactory.Default.Get<CashDepositInfo>().Where(item => item.ShopId == shop.Id).FirstOrDefault();
            var categories = shopCategoryService.GetBusinessCategory(shop.Id).ToList();
            var mainCategories = categories.Where(item => item.ParentCategoryId == 0).Select(item => item.Id).ToList();
            //var cateCashDeposit = Context.CategoryCashDepositInfo.FindBy(item => mainCategories.Contains(item.CategoryId)).ToList();
            var cateCashDeposit = DbFactory.Default.Get<CategoryCashDepositInfo>().Where(item => item.CategoryId.ExIn(mainCategories)).ToList();
            if (cateCashDeposit.Count == 0)
            {
                return ensure;
            }
            var needCashDeposit = cateCashDeposit.Max(item => item.NeedPayCashDeposit);

            //平台自营，商家缴纳足够保证金或者平台未取消其资质资格
            if (shop.IsSelf || (cashDeposit != null && cashDeposit.CurrentBalance >= needCashDeposit) || (cashDeposit != null && cashDeposit.CurrentBalance < needCashDeposit && cashDeposit.EnableLabels == true))
            {
                List<long> categoryIds = new List<long>();
                categoryIds.Add(product.CategoryId);
                var mainCategory = categoryService.GetTopLevelCategories(categoryIds).FirstOrDefault();
                if (mainCategory != null)
                {
                    //var categoryCashDepositInfo = Context.CategoryCashDepositInfo.Where(item => item.CategoryId == mainCategory.Id).FirstOrDefault();
                    var categoryCashDepositInfo = DbFactory.Default.Get<CategoryCashDepositInfo>().Where(item => item.CategoryId == mainCategory.Id).FirstOrDefault();
                    if (categoryCashDepositInfo != null)
                        ensure.IsSevenDayNoReasonReturn = (bool)categoryCashDepositInfo.EnableNoReasonReturn;
                }
                ensure.IsCustomerSecurity = true;
                var template = DbFactory.Default.Get<FreightTemplateInfo>(p => p.Id == product.FreightTemplateId).FirstOrDefault();
                //设置了运费模板
                if (template != null)
                {
                    if (!string.IsNullOrEmpty(template.SendTime))
                        ensure.IsTimelyShip = true;
                }

            }
            return ensure;
        }

        


        #endregion
    }
}
