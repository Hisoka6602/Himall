using System;
using System.Collections.Generic;
using Himall.Core;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.CommonModel;
using Himall.DTO;
using System.Linq;

namespace Himall.Application
{
    public class CashDepositsApplication:BaseApplicaion
    {
        private static CashDepositsService _iCashDepositsService = ObjectContainer.Current.Resolve<CashDepositsService>();

        /// <summary>
        /// 获取保证金列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<CashDeposit> GetCashDeposits(CashDepositQuery query)
        {
            var data = _iCashDepositsService.GetCashDeposits(query);
            var shops = GetService<ShopService>().GetShops(data.Models.Select(p => p.ShopId).ToList());
            var result = data.Models.Select(item =>
            {
                var needPay = _iCashDepositsService.GetNeedPayCashDepositByShopId(item.ShopId);
                var shop = shops.FirstOrDefault(p => p.Id == item.ShopId);
                return new CashDeposit
                {
                    Id = item.Id,
                    ShopName = shop.ShopName,
                    Type = needPay > 0 ? "欠费" : "正常",
                    TotalBalance = item.TotalBalance,
                    CurrentBalance = item.CurrentBalance,
                    Date = item.Date,
                    NeedPay = needPay,
                    EnableLabels = item.EnableLabels,
                };
            }).ToList();

            return new QueryPageModel<CashDeposit>
            {
                Models = result,
                Total = data.Total
            };
        }

        /// <summary>
        /// 新增类目保证金
        /// </summary>
        /// <param name="model"></param>
        public static void AddCategoryCashDeposits(CategoryCashDepositInfo model)
        {
            _iCashDepositsService.AddCategoryCashDeposits(model);
        }

        /// <summary>
        /// 根据一级分类删除类目保证金
        /// </summary>
        /// <param name="categoryId"></param>
        public static void DeleteCategoryCashDeposits(long categoryId)
        {
            _iCashDepositsService.DeleteCategoryCashDeposits(categoryId);
        }
       
        /// <summary>
        /// 获取店铺应缴保证金
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static decimal GetNeedPayCashDepositByShopId(long shopId)
        {
            return _iCashDepositsService.GetNeedPayCashDepositByShopId(shopId);
        }

        /// <summary>
        /// 获取提供特殊服务实体
        /// </summary>
        /// <param name="productId"></param>
        public static ProductEnsure GetCashDepositsObligation(long productId)
        {
            return _iCashDepositsService.GetProductEnsure(productId);
        }
    }
}
