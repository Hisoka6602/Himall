using Himall.Core;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Application
{
    public class ProductWebApplication 
    {
        private static ProductService Service = ObjectContainer.Current.Resolve<ProductService>();
        
        /// <summary>
        /// 获取一个最小价格或区间价格文本
        /// </summary>
        /// <param name="productInfo">商品实体</param>
        /// <param name="skus">商品规格集合</param>
        /// <param name="discount">会员几折</param>
        public static string GetProductPriceStr(ProductInfo productInfo,List<Himall.Entities.SKUInfo> skus,decimal discount=1M)
        {
            if (productInfo == null || skus == null)
                return string.Empty;
            var shopInfo = ShopApplication.GetShop(productInfo.ShopId);
            if (!shopInfo.IsSelf)
            {
                discount = 1M;
            }

            decimal min = productInfo.MinSalePrice, max = 0;
            var strPrice = string.Empty;
            var skusql = skus.Where(s => s.Stock >= 0);
            if (skusql.Count() > 0)
            {
                min = skusql.Min(s => s.SalePrice);
                max = skusql.Max(s => s.SalePrice);
            }

            if (max > min)
            {
                strPrice = string.Format("{0}-{1}", (min * discount).ToString("f2"), (max * discount).ToString("f2"));
            }
            else if (min == 0 && max == 0)
            {
                strPrice = (productInfo.MinSalePrice * discount).ToString("f2");
            }
            else
            {
                strPrice = string.Format("{0}", (min * discount).ToString("f2"));
            }

            return strPrice;
        }
        
        /// <summary>
        /// 获取一个最小价格或区间价格文本
        /// </summary>
        /// <param name="productInfo">商品实体</param>
        /// <param name="skus">商品规格集合</param>
        /// <param name="discount">会员几折</param>
        public static string GetProductPriceStr2(ProductInfo productInfo, List<Himall.DTO.SKU> skus, decimal discount = 1M)
        {
            if (productInfo == null || skus == null)
                return string.Empty;
            var shopInfo = ShopApplication.GetShop(productInfo.ShopId);
            if (!shopInfo.IsSelf)
            {
                discount = 1M;
            }

            decimal min = productInfo.MinSalePrice, max = 0;
            var strPrice = string.Empty;
            var skusql = skus.Where(s => s.Stock >= 0);
            if (skusql.Count() > 0)
            {
                min = skusql.Min(s => s.SalePrice);
                max = skusql.Max(s => s.SalePrice);
            }

            if (max > min)
            {
                strPrice = string.Format("{0}-{1}", (min * discount).ToString("f2"), (max * discount).ToString("f2"));
            }
            else if (min == 0 && max == 0)
            {
                strPrice = (productInfo.MinSalePrice * discount).ToString("f2");
            }
            else
            {
                strPrice = string.Format("{0}", (min * discount).ToString("f2"));
            }

            return strPrice;
        }

        /// <summary>
        /// 获取商品销量
        /// </summary>
        /// <param name="productId">商品id</param>
        /// <returns></returns>
        public static long GetProductSaleCounts(long productId)
        {
            return Service.GetProductSaleCounts(productId);
        }
    }
}
