using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.Product;
using Himall.Entities;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Service
{
    /// <summary>
    /// 库存管理服务
    /// </summary>
    public class StockService : ServiceBase
    {
        public Dictionary<string, int> GetStock(long product) =>
           DbFactory.Default.Get<SKUInfo>(p => p.ProductId == product)
               .Select(p => new
               {
                   Item1 = p.Id,
                   Item2 = p.Stock
               }).ToList<SimpItem<string, int>>()
               .ToDictionary(p => p.Item1, p => p.Item2);

        public Dictionary<long, int> GetStock(List<long> products) =>
            DbFactory.Default.Get<SKUInfo>(p => p.ProductId.ExIn(products))
            .GroupBy(p => p.ProductId)
            .Select(p => new
            {
                Item1 = p.ProductId,
                Item2 = p.Stock.ExSum()
            }).ToList<SimpItem<long, int>>()
            .ToDictionary(p => p.Item1, p => p.Item2);


        public void SetSkuStock(string skuId, long stock, StockOptionType option)
        {
            var sku = DbFactory.Default.Set<SKUInfo>().Where(p => p.Id == skuId);
            switch (option)
            {
                case StockOptionType.Add: sku.Set(p => p.Stock, n => n.Stock + stock); break;
                case StockOptionType.Reduce: sku.Set(p => p.Stock, n => n.Stock - stock); break;
                case StockOptionType.Normal: sku.Set(p => p.Stock, stock); break;
            }
            sku.Succeed();
        }
        /// <summary>
        /// 设置商品
        /// </summary>
        public void SetSkuStock(StockOptionType option, Dictionary<string, long> changes)
        {
            foreach (var item in changes)
                SetSkuStock(item.Key, item.Value, option);
        }
        /// <summary>
        /// 设置商品库存
        /// </summary>
        public void SetProductStock(List<long> products, long stock, StockOptionType option)
        {
            var sku = DbFactory.Default.Set<SKUInfo>().Where(p => p.ProductId.ExIn(products));
            switch (option)
            {
                case StockOptionType.Add: sku.Set(p => p.Stock, n => n.Stock + stock); break;
                case StockOptionType.Reduce: sku.Set(p => p.Stock, n => n.Stock - stock); break;
                case StockOptionType.Normal: sku.Set(p => p.Stock, stock); break;
            }
            sku.Succeed();
        }

        public void Decrease(List<StockChange> changes, bool force = false)
        {
            DbFactory.Default.InTransaction(() =>
             {
                 foreach (var item in changes)
                 {
                     if (item.ShopBranchId > 0)
                     {
                         var db = DbFactory.Default.Set<ShopBranchSkuInfo>()
                              .Where(p => p.ShopBranchId == item.ShopBranchId && p.SkuId == item.SkuId)
                              .Set(p => p.Stock, p => p.Stock - item.Number);

                         if (!force)
                             db.Where(p => p.Stock >= item.Number);

                         if (db.Execute() == 0)
                             throw new HimallException("库存不足");
                     }
                     else
                     {
                         var db = DbFactory.Default.Set<SKUInfo>()
                             .Where(p => p.Id == item.SkuId)
                             .Set(p => p.Stock, p => p.Stock - item.Number);

                         if (!force)
                             db.Where(p => p.Stock >= item.Number);

                         if (db.Execute() == 0)
                             throw new HimallException("库存不足");
                     }
                 }
             });
        }

        public void Increase(List<StockChange> changes)
        {
            DbFactory.Default.InTransaction(() =>
            {
                foreach (var item in changes)
                {
                    if (item.ShopBranchId > 0)
                    {
                        DbFactory.Default.Set<ShopBranchSkuInfo>()
                             .Where(p => p.ShopBranchId == item.ShopBranchId && p.SkuId == item.SkuId)
                             .Set(p => p.Stock, p => p.Stock + item.Number)
                             .Execute();
                    }
                    else
                    {
                        DbFactory.Default.Set<SKUInfo>()
                            .Where(p => p.Id == item.SkuId)
                            .Set(p => p.Stock, p => p.Stock + item.Number)
                            .Execute();
                    }
                }
            });
        }

        /// <summary>
        /// 设置库存
        /// </summary>
        public void SetStock(List<long> products, Dictionary<long, long> stocks)
        {
            DbFactory.Default.InTransaction(() =>
            {
                foreach (var item in stocks)
                {
                    DbFactory.Default.Set<SKUInfo>()
                        .Where(e => e.AutoId == item.Key)
                        .Set(n => n.Stock, item.Value)
                        .Execute();
                }
            });
        }
        /// <summary>
        /// 根据规格ID获取更新后库存的规格信息，如果规格ID已经不存在则不更新
        /// </summary>
        /// <param name="skuIds"></param>
        /// <param name="stocks"></param>
        /// <returns></returns>
        public List<SKUInfo> GetSKUInfos(List<SKUInfo> wdtSKUInfos)
        {
            List<SKUInfo> sKUInfos = DbFactory.Default.Get<SKUInfo>().Where(s => s.Id.ExIn(wdtSKUInfos.Select(w => w.Id).ToList())).ToList();
            List<SKUInfo> stockSKUInfo = new List<SKUInfo>();
            if (sKUInfos != null)
            {
                var index = 0;
                foreach (SKUInfo sku in wdtSKUInfos)
                {
                    SKUInfo temp = sKUInfos.Where(s => s.Id == sku.Id).FirstOrDefault();
                    if (temp != null)
                    {
                        temp.Stock = sku.Stock;
                        stockSKUInfo.Add(temp);
                    }
                    index += 1;
                }
            }
            return stockSKUInfo;
        }

        /// <summary>
        /// 设置库存
        /// </summary>
        public void SetWDTSkuStock(List<SKUInfo> skus)
        {
            DbFactory.Default.InTransaction(() =>
            {
                foreach (var item in skus)
                {
                    DbFactory.Default.Set<SKUInfo>()
                        .Where(e => e.Id == item.Id)
                        .Set(n => n.Stock, item.Stock)
                        .Execute();
                }
            });
        }
    }
}
