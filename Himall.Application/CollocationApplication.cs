using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Himall.Core;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.CommonModel;
using Himall.Entities;
using Himall.DTO;
using Himall.DTO.CacheData;

namespace Himall.Application
{
    public class CollocationApplication : BaseApplicaion<CollocationService>
    {

        /// <summary>
        /// 根据组合商品获取组合SKU信息
        /// </summary>
        /// <param name="colloPid"></param>
        /// <param name="skuid"></param>
        /// <returns></returns>
        public static CollocationSkuInfo GetColloSku(long colloPid, string skuid)
        {
            return Service.GetColloSku(colloPid, skuid);
        }

        //获取一个商品的组合购SKU信息
        public static List<CollocationSkuInfo> GetProductColloSKU(long productid, long colloPid)
        {
            return Service.GetProductColloSKU(productid, colloPid);
        }
        public static string GetChineseNumber(int number)
        {
            string numberStr = NumberToChinese(number);
            string firstNumber = string.Empty;
            string lastNumber = string.Empty;
            string str = number.ToString();
            firstNumber = str.Substring(0, 1);
            lastNumber = str.Substring(str.Length - 1);
            if (str.Length > 1 && lastNumber == "0")
            {
                numberStr = numberStr.Substring(0, numberStr.Length - 1);
            }
            if (str.Length == 2 && firstNumber == "1")
            {
                numberStr = numberStr.Substring(1);
            }

            return numberStr;
        }
        /// <summary>
        /// 数字转中文
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string NumberToChinese(int number)
        {
            string res = string.Empty;
            string str = number.ToString();
            string schar = str.Substring(0, 1);
            switch (schar)
            {
                case "1":
                    res = "一";
                    break;
                case "2":
                    res = "二";
                    break;
                case "3":
                    res = "三";
                    break;
                case "4":
                    res = "四";
                    break;
                case "5":
                    res = "五";
                    break;
                case "6":
                    res = "六";
                    break;
                case "7":
                    res = "七";
                    break;
                case "8":
                    res = "八";
                    break;
                case "9":
                    res = "九";
                    break;
                default:
                    res = "零";
                    break;
            }
            if (str.Length > 1)
            {
                switch (str.Length)
                {
                    case 2:
                    case 6:
                        res += "十";
                        break;
                    case 3:
                    case 7:
                        res += "百";
                        break;
                    case 4:
                        res += "千";
                        break;
                    case 5:
                        res += "万";
                        break;
                    default:
                        res += "";
                        break;
                }
                res += NumberToChinese(int.Parse(str.Substring(1, str.Length - 1)));
            }
            return res;
        }

        public static List<CollocationPoruductInfo> GetCollocationListByProductId(long productId)
        {
            return Service.GetCollocationListByProductId(productId);
        }

        /// <summary>
        /// 获取当前商品可参与的组合购
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public static int GetCollocationCount(long product)
        {
            var colls = Service.GetAvailableCollocationByProduct(product);
            var collProducts = Service.GetProducts(colls.Select(p => p.Id).ToList());
            var products = ProductManagerApplication.GetOnSaleProducts(collProducts.Select(p => p.ProductId).ToList());
            int result = 0;
            foreach (var coll in colls)
            {
                var cProduct = collProducts.Where(p => p.ColloId == coll.Id);
                var mainProduct = cProduct.FirstOrDefault(p => p.IsMain);
                if (products.Any(p => p.Id == mainProduct.ProductId))
                    result++;
            }
            return result;
        }

        public static List<CollocationData> GetAvailable(long product)
        {
            return Service.GetGoing().Where(p => p.Products.Any(i => i.ProductId == product)).ToList();
        }




        public static List<ProductCollocationModel> GetDisplayCollocation(long productId)
        {
            var result = new List<ProductCollocationModel>();

            var collocations = GetAvailable(productId);
            if (collocations.Count == 0) return result;

            var pro_list = collocations.SelectMany(p => p.Products.Select(i => i.ProductId)).Distinct().ToList();
            var allProducts = ProductManagerApplication.GetProductData(pro_list);
            var stocks = GetService<StockService>().GetStock(pro_list);

            var index = 0;
            foreach (var collocation in collocations)
            {
                index++;
                var item = new ProductCollocationModel();
                item.Id = collocation.Id;
                item.Name = "组合购" + GetChineseNumber(index);
                item.ProductId = collocation.ProductId;
                item.ShopId = collocation.ShopId;
                item.Title = collocation.Title;
                var productIds = collocation.Products.Select(p => p.ProductId).Distinct();
                item.Products = new List<CollocationProducts>();

                foreach (long pid in productIds)
                {
                    stocks.TryGetValue(pid, out var stock);
                    var pros = collocation.Products.Where(p => p.ProductId == pid).ToList();
                    var proInfo = pros.FirstOrDefault();
                    var product = allProducts.FirstOrDefault(p => p.Id == pid);
                    CollocationProducts pitem = new CollocationProducts();
                    pitem.DisplaySequence = proInfo.DisplaySequence;
                    pitem.IsMain = proInfo.IsMain;
                    pitem.Stock = stock;
                    pitem.ColloPid = proInfo.ColloProId;
                    pitem.MaxCollPrice = pros.Max(x => x.Price);
                    pitem.MaxSalePrice = pros.Max(x => x.SkuPirce);
                    pitem.MinCollPrice = pros.Min(x => x.Price);
                    pitem.MinSalePrice = pros.Min(x => x.SkuPirce);
                    pitem.ProductName = product.ProductName;
                    pitem.ProductId = proInfo.ProductId;
                    pitem.Image = Core.HimallIO.GetImagePath(product.RelativePath);
                    pitem.ImagePath = product.ImagePathUrl;
                    pitem.UpdateTime = product.UpdateTime;
                    item.Products.Add(pitem);
                }
                item.Cheap = item.Products.Sum(a => a.MaxSalePrice) - item.Products.Sum(a => a.MinCollPrice);
                result.Add(item);
                //item.Products = collocation.Products.GroupBy(p => p.ProductId).SelectMany(cp =>
                //   {
                //       var pro = cp.FirstOrDefault();
                //       var product = allProducts.FirstOrDefault(p => p.Id == cp.Key);
                //       stocks.TryGetValue(product.Id, out var stock);
                //       return new CollocationProducts()
                //       {
                //           DisplaySequence = pro.DisplaySequence,
                //           IsMain = cp.FirstOrDefault().IsMain,
                //           Stock = stock,
                //           ColloPid = pro.ColloProId,
                //           MaxCollPrice = cp.Max(x => x.Price),
                //           MaxSalePrice = cp.Max(x => x.SkuPirce),
                //           MinCollPrice = cp.Min(x => x.Price),
                //           MinSalePrice = cp.Min(x => x.SkuPirce),
                //           ProductName = product.ProductName,
                //           ProductId = cp.FirstOrDefault().ProductId,
                //           Image = Core.HimallIO.GetImagePath(product.RelativePath),
                //           ImagePath = product.ImagePathUrl,
                //           UpdateTime = product.UpdateTime
                //       };
                //       item.Cheap = item.Products.Sum(a => a.MaxSalePrice) - item.Products.Sum(a => a.MinCollPrice);
                //       result.Add(item);
                //   }

            }
            return result;
        }
    }
}
