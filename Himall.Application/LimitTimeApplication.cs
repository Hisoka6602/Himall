using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Application
{
    /// <summary>
    /// 限时购
    /// </summary>
    public class LimitTimeApplication : BaseApplicaion<LimitTimeBuyService>
    {
        /// <summary>
        /// 是否正在限时购
        /// </summary>
        /// <param name="pid">商品ID</param>
        /// <returns></returns>
        public static bool IsLimitTimeMarketItem(long pid)
        {
            return Service.IsLimitTimeMarketItem(pid);
        }

        public static void UpdateProductMinPrice()
        {
            var models = Service.GetLastModify();
            foreach (var item in models)
                ProductManagerApplication.SaveCaculateMinPrice(item.ProductId, item.ShopId);
        }

        /// <summary>
        /// 取限时购价格
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<FlashSalePrice> GetPriceByProducrIds(List<long> ids)
        {
            return Service.GetPriceByProducrIds(ids);
        }

        public static List<FlashSalePrice> GetLimitProducts(List<long> ids = null)
        {
            if (Cache.Exists(CacheKeyCollection.CACHE_LIMITPRODUCTS))
                return Cache.Get<List<FlashSalePrice>>(CacheKeyCollection.CACHE_LIMITPRODUCTS);
            if (ids == null)
            {
                ids = new List<long>();
            }
            var result = Service.GetPriceByProducrIds(ids);
            Cache.Insert(CacheKeyCollection.CACHE_LIMITPRODUCTS, result, 120);
            return result;
        }

        public static void AddFlashSaleDetails(List<FlashSaleDetailInfo> details)
        {
            Service.AddFlashSaleDetails(details);
        }
        
        /// <summary>
        ///  根据商品Id获取一个限时购的详细信息
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static FlashSaleInfo GetLimitTimeMarketItemByProductId(long pid)
        {
            return Service.GetLimitTimeMarketItemByProductId(pid);
        }

        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="skuid"></param>
        /// <returns></returns>
        public static FlashSaleDetailInfo GetDetail(string skuid)
        {
            return Service.GetDetail(skuid);
        }


        public static FlashSaleModel GetDetail(long Id)
        {
            return Service.Get(Id);
        }
        public static FlashSaleModel IsFlashSaleDoesNotStarted(long productid)
        {
            return Service.IsFlashSaleDoesNotStarted(productid);
        }
        public static FlashSaleConfigModel GetConfig()
        {
            return Service.GetConfig();
        }

        

        public static bool IsAdd(long productid)
        {
            return Service.IsAdd(productid);
        }

        public static int GetFlashSaleCount(LimitTimeQuery query)
        {
            return Service.GetFlashSaleCount(query);
        }

        /// <summary>
        /// 根据限时购id集合获取限时购详细列表
        /// </summary>
        /// <param name="flashSaleIds">限时购ids</param>
        /// <returns></returns>
        public static List<FlashSaleDetailInfo> GetFlashSaleDetailByFlashSaleIds(IEnumerable<long> flashSaleIds)
        {
            return Service.GetFlashSaleDetailByFlashSaleIds(flashSaleIds);
        }

        public static List<FlashSaleInfo> GetFlashSaleInfos(IEnumerable<long> flashId)
        {
            return Service.GetFlashSaleInfos(flashId);
        }

        public static long UpdateFlashSaleStock(string skuId, long stock)
        {
            return Service.UpdateStockBySkuId(skuId, stock);
        }

        public static FlashSaleData GetAvailableByProduct(long productId)
        {
            var list = Service.GetNotEnd();
            return list.FirstOrDefault(p => p.ProductId == productId);
        }
        public static List<FlashSaleData> GetAvailable(List<long> list) =>
             Service.GetNotEnd().Where(p => list.Contains(p.Id)).ToList();
        public static List< FlashSaleData> GetAvailableByProduct(List<long> products) {
            var list = Service.GetNotEnd();
            return list.Where(p => products.Contains(p.ProductId)).ToList();
        }

        public static List<FlashSaleItemSimp> GetLimitItems(long id) =>
            Service.GetLimitItems(id);

        public static FlashSaleData GetFlashSaleData(long id) =>
            Service.GetFlashSaleData(id);

        /// <summary>
        /// 获取限购购活动销量
        /// </summary>
        /// <param name="id">限购购活动id</param>
        /// <returns></returns>
        public static long GetFlashSaleSaleCount(long id) =>
            Service.GetFlashSaleSaleCount(id);
    }
}
