using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.Live;
using Himall.DTO.Product;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static Himall.Entities.LiveProductLibraryInfo;
using static Himall.Entities.ProductInfo;

namespace Himall.Application
{
    public class ProductManagerApplication : BaseApplicaion<ProductService>
    {
        private static ProductDescriptionTemplateService _productDescriptionTemplateService = ObjectContainer.Current.Resolve<ProductDescriptionTemplateService>();
        private static SearchProductService _searchProductService = ObjectContainer.Current.Resolve<SearchProductService>();
        private static ProductLadderPriceService _productLadderPriceService = ObjectContainer.Current.Resolve<ProductLadderPriceService>();
        private static FightGroupService _FightGroupService = ObjectContainer.Current.Resolve<FightGroupService>();
        private static LimitTimeBuyService _LimitTimeBuyService = ObjectContainer.Current.Resolve<LimitTimeBuyService>();
        private static CollocationService _CollocationService = ObjectContainer.Current.Resolve<CollocationService>();
        private static ProductService _ProductService = ObjectContainer.Current.Resolve<ProductService>();
        private static WXApiService _iWXApiService = ObjectContainer.Current.Resolve<WXApiService>();
        private static LiveService _LiveService = ObjectContainer.Current.Resolve<LiveService>();
        private static WXMsgTemplateService _WXMsgTemplateService = ObjectContainer.Current.Resolve<WXMsgTemplateService>();
        private static WXSmallProgramService _WXSmallProgramService = ObjectContainer.Current.Resolve<WXSmallProgramService>();

        /// <summary>
        /// 添加商品
        /// </summary>
        /// <param name="shopId">店铺id</param>
        /// <param name="product">商品信息</param>
        /// <param name="pics">需要转移的商品图片地址</param>
        /// <param name="skus">skus，至少要有一项</param>
        /// <param name="description">描述</param>
        /// <param name="attributes">商品属性</param>
        /// <param name="goodsCategory">商家分类</param>
        /// <param name="sellerSpecifications">商家自定义规格</param>
        public static Product AddProduct(long shopId, Product product, string[] pics, SKU[] skus, ProductDescription description, ProductAttribute[] attributes, long[] goodsCategory, SellerSpecificationValue[] sellerSpecifications, ProductLadderPrice[] prices)
        {
            var productInfo = product.Map<Entities.ProductInfo>();
            var skuInofs = skus.Map<Entities.SKUInfo[]>();
            var descriptionInfo = description.Map<Entities.ProductDescriptionInfo>();
            var attributeInfos = attributes.Map<Entities.ProductAttributeInfo[]>();
            var sellerSpecificationInfos = sellerSpecifications.Map<Entities.SellerSpecificationValueInfo[]>();
            var ladderpricesInfos = prices.Select(p =>
            {
                var ladder = new Entities.ProductLadderPriceInfo();
                ladder.Id = p.Id;
                ladder.MinBath = p.MinBath;
                ladder.MaxBath = p.MaxBath;
                ladder.ProductId = p.ProductId;
                ladder.Price = p.Price;
                return ladder;
            }).ToArray();
            Service.AddProduct(shopId, productInfo, pics, skuInofs, descriptionInfo, attributeInfos,
                goodsCategory, sellerSpecificationInfos, ladderpricesInfos);
            if ((!productInfo.IsPushGoods || !productInfo.IsPushArchivesGoods) &&
                productInfo.AuditStatus == ProductInfo.ProductAuditStatus.Audited)
            {
                long selfShopId = ShopApplication.GetSelfShop().Id;
                //只推送自营店的
                if (shopId == selfShopId)
                {
                    WDTProductApplication.PushArchivesById(productInfo.Id);
                    WDTProductApplication.PushById(productInfo.Id);
                }
            }
            CreateHtml(productInfo.Id);
            return AutoMapper.Mapper.Map<Product>(productInfo);
        }

        /// <summary>
        /// 更新商品
        /// </summary>
        /// <param name="product">修改后的商品</param>
        /// <param name="pics">需要转移的商品图片地址</param>
        /// <param name="skus">skus，至少要有一项</param>
        /// <param name="description">描述</param>
        /// <param name="attributes">商品属性</param>
        /// <param name="goodsCategory">商家分类</param>
        /// <param name="sellerSpecifications">商家自定义规格</param>
        public static void UpdateProduct(Product product, string[] pics, SKU[] skus, ProductDescription description, ProductAttribute[] attributes, long[] goodsCategory, SellerSpecificationValue[] sellerSpecifications, ProductLadderPrice[] prices)
        {
            var productInfo = Service.GetProduct(product.Id);
            if (productInfo == null)
                throw new HimallException("指定id对应的数据不存在");

            var editStatus = productInfo.EditStatus;

            if (product.ProductName != productInfo.ProductName)
                editStatus = GetEditStatus(editStatus);
            if (product.ShortDescription != productInfo.ShortDescription)
                editStatus = GetEditStatus(editStatus);

            product.AddedDate = productInfo.AddedDate;
            product.CheckTime = productInfo.CheckTime;
            if (productInfo.SaleStatus != Entities.ProductInfo.ProductSaleStatus.InDraft)
            {
                product.SaleStatus = productInfo.SaleStatus;
            }
            product.AuditStatus = productInfo.AuditStatus;
            product.DisplaySequence = productInfo.DisplaySequence;
            product.ShopId = productInfo.ShopId;
            product.HasSKU = productInfo.HasSKU;
            product.ImagePath = productInfo.ImagePath;
            product.SaleCounts = productInfo.SaleCounts;
            product.VirtualSaleCounts = productInfo.VirtualSaleCounts;

            if (product.IsOpenLadder)
            {
                editStatus = GetEditStatus(editStatus);
            }

            productInfo.ColorAlias = product.ColorAlias;
            productInfo.SizeAlias = product.SizeAlias;
            productInfo.VersionAlias = product.VersionAlias;
            productInfo.VideoPath = product.VideoPath;
            product.DynamicMap(productInfo);

            productInfo.EditStatus = editStatus;
            productInfo.UpdateTime = DateTime.Now;

            var skuInofs = skus.Map<Entities.SKUInfo[]>();
            var descriptionInfo = description.Map<Entities.ProductDescriptionInfo>();
            var attributeInfos = attributes.Map<Entities.ProductAttributeInfo[]>();
            var sellerSpecificationInfos = sellerSpecifications.Map<Entities.SellerSpecificationValueInfo[]>();
            var ladderpricesInfos = prices.Select(p =>
            {
                var ladder = new Entities.ProductLadderPriceInfo();
                ladder.Id = p.Id;
                ladder.MinBath = p.MinBath;
                ladder.MaxBath = p.MaxBath;
                ladder.ProductId = p.ProductId;
                ladder.Price = p.Price;
                return ladder;
            }).ToArray();
            Service.UpdateProduct(productInfo, pics, skuInofs, descriptionInfo, attributeInfos, goodsCategory,
                sellerSpecificationInfos, ladderpricesInfos);
            if (productInfo.IsOpenLadder)
            {
                //处理门店
                ShopBranchApplication.UnSaleProduct(productInfo.Id);
            }
            if (product.EditStatus == ProductEditStatus.Edited || product.EditStatus == ProductEditStatus.Normal)
            {
                //创建一个新线程去执行，不影响审核操作的时间
                Task.Factory.StartNew(() =>
                {
                    LiveProductLibaryQuery query = new LiveProductLibaryQuery();
                    query.ProductIds = product.Id.ToString();
                    QueryPageModel<LiveProductLibaryModel> liveProducts = LiveApplication.GetLiveProductLibrarys(query);
                    if (liveProducts.Total > 0)
                    {

                        foreach (LiveProductLibaryModel liveProductInfo in liveProducts.Models)
                        {
                            if (liveProductInfo != null && liveProductInfo.LiveAuditStatus != LiveProductAuditStatus.NoSubmit)
                            {
                                //未审核状态，允许更新所有字段,已审核状态可以更新价格
                                if (liveProductInfo.LiveAuditStatus == LiveProductAuditStatus.Audited || liveProductInfo.LiveAuditStatus == LiveProductAuditStatus.NoAudit)
                                {
                                    string msg = "";
                                    LiveApplication.UpdateAppletLiveProduct(liveProductInfo, out msg);
                                }
                            }
                        }
                    }
                });
            }
            CreateHtml(product.Id);
        }


        public static bool HasSKU(long product)
        {
            return Service.HasSKU(product);
        }
        /// <summary>
        /// 生成指定商品详情html
        /// </summary>
        public static void CreateHtml(long productId)
        {
            WebClient wc = new WebClient();
            var preUrl = SiteSettingApplication.GetCurDomainUrl();
            string url = preUrl + "/Product/Details/" + productId;
            string wapurl = preUrl + "/m-wap/Product/Details/" + productId + "?nojumpfg=1";
            string urlHtml = "/Storage/Products/Statics/" + productId + ".html";
            string wapHtml = "/Storage/Products/Statics/" + productId + "-wap.html";
            var data = wc.DownloadData(url);
            var wapdata = wc.DownloadData(wapurl);
            MemoryStream memoryStream = new MemoryStream(data);
            MemoryStream wapMemoryStream = new MemoryStream(wapdata);
            HimallIO.CreateFile(urlHtml, memoryStream, FileCreateType.Create);
            HimallIO.CreateFile(wapHtml, wapMemoryStream, FileCreateType.Create);
        }

        static void CreatPCHtml(long productId)
        {
            WebClient wc = new WebClient();
            var preUrl = SiteSettingApplication.GetCurDomainUrl();
            string url = preUrl + "/Product/Details/" + productId;
            string urlHtml = "/Storage/Products/Statics/" + productId + ".html";
            var data = wc.DownloadData(url);
            MemoryStream memoryStream = new MemoryStream(data);
            HimallIO.CreateFile(urlHtml, memoryStream, FileCreateType.Create);
        }

        static void CreatWAPHtml(long productId)
        {
            WebClient wc = new WebClient();
            var preUrl = SiteSettingApplication.GetCurDomainUrl();
            string wapurl = preUrl + "/m-wap/Product/Details/" + productId + "?nojumpfg=1";
            string wapHtml = "/Storage/Products/Statics/" + productId + "-wap.html";
            var wapdata = wc.DownloadData(wapurl);
            MemoryStream wapMemoryStream = new MemoryStream(wapdata);
            HimallIO.CreateFile(wapHtml, wapMemoryStream, FileCreateType.Create);
        }

        static void CreatBrandchWAPHtml(long productId, long branchId)
        {
            WebClient wc = new WebClient();
            var preUrl = SiteSettingApplication.GetCurDomainUrl();
            string wapBranchurl = preUrl + "/m-wap/BranchProduct/Details/" + productId + "?nojumpfg=1&shopBranchId=" + branchId;
            string wapBranchHtml = "/Storage/Products/Statics/" + productId + "-" + branchId + "-wap-branch.html";
            var wapbranchdata = wc.DownloadData(wapBranchurl);
            MemoryStream wapMemoryStream = new MemoryStream(wapbranchdata);
            HimallIO.CreateFile(wapBranchHtml, wapMemoryStream, FileCreateType.Create);
        }

        /// <summary>
        /// 获取指定商品详情html
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static void GetPCHtml(long productId)
        {
            string pcUrlHtml = "/Storage/Products/Statics/" + productId + ".html";
            string fuleUrl = Core.Helper.IOHelper.GetMapPath(pcUrlHtml);
            RefreshLocalProductHtml(productId, pcUrlHtml, fuleUrl);
        }

        /// <summary>
        /// 获取指定商品详情html
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static void GetWAPHtml(long productId)
        {
            string wapUrlHtml = "/Storage/Products/Statics/" + productId + "-wap.html";
            string fullUrl = Core.Helper.IOHelper.GetMapPath(wapUrlHtml);
            RefreshWAPLocalProductHtml(productId, wapUrlHtml, fullUrl);
        }
        /// <summary>
        /// 获取指定门店商品详情html
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static void GetWAPBranchHtml(long productId, long branchId)
        {
            string wapUrlHtml = "/Storage/Products/Statics/" + productId + "-" + branchId + "-wap-branch.html";
            string fullUrl = Core.Helper.IOHelper.GetMapPath(wapUrlHtml);
            RefreshWAPLocalBranchProductHtml(productId, wapUrlHtml, fullUrl, branchId);
        }

        /// <summary>
        /// 本地文件是否更新
        /// </summary>
        /// <param name="targetFilename">本地文件</param>
        /// <param name="htmlUrl">html请求文件</param>
        /// <returns></returns>
        static bool IsRefreshProductHTML(string htmlUrl, string targetFilename)
        {
            if (!File.Exists(targetFilename))
                return true;//文件不存在要刷新

            var locallastTime = File.GetLastWriteTime(targetFilename);//本地文件最后修改时间
            if (CheckNeedRefreshFile(locallastTime, 20))
                return true;//本地文件超过20分钟要刷新

            if (Core.HimallIO.GetHimallIO().GetType().FullName.Equals("Himall.Strategy.OSS"))
            {
                var metaRemoteInfo = HimallIO.GetFileMetaInfo(htmlUrl);
                if (metaRemoteInfo == null || metaRemoteInfo.LastModifiedTime > locallastTime)
                    return true;//oss静态文件不存在或最新更新比本地早，本地要刷新
            }

            return false;//本地不更新
        }

        /// <summary>
        /// 刷新本地缓存商品html文件 
        /// </summary>     
        /// <param name="targetFilename">本地待生成的html文件名</param>
        static void RefreshWAPLocalProductHtml(long productId, string htmlUrl, string targetFilename)
        {
            lock (htmlUrl)
            {
                if (IsRefreshProductHTML(htmlUrl, targetFilename))
                {
                    if (!HimallIO.ExistFile(htmlUrl))
                        CreatWAPHtml(productId);
                    else
                    {
                        var metaRemoteInfo = HimallIO.GetFileMetaInfo(htmlUrl);
                        if (null == metaRemoteInfo || CheckNeedRefreshFile(metaRemoteInfo.LastModifiedTime, 20))
                        {
                            CreatWAPHtml(productId);
                        }
                    }

                    var dirFullname = Core.Helper.IOHelper.GetMapPath("/Storage/Products/Statics");
                    if (!Directory.Exists(dirFullname))
                        Directory.CreateDirectory(dirFullname);
                    byte[] test = HimallIO.GetFileContent(htmlUrl);
                    File.WriteAllBytes(targetFilename, HimallIO.GetFileContent(htmlUrl));
                }
            }
        }

        /// <summary>
        /// 检查文件信息
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="local"></param>
        /// <returns></returns>
        private static bool CheckNeedRefreshFile(DateTime modified, int minutes)
        {
            return (DateTime.Now - modified).TotalMinutes > minutes;
        }

        /// <summary>
        /// 刷新本地缓存门店商品html文件 
        /// </summary>     
        /// <param name="targetFilename">本地待生成的html文件名</param>
        static void RefreshWAPLocalBranchProductHtml(long productId, string htmlUrl, string targetFilename, long branchId)
        {
            lock (htmlUrl)
            {
                if (IsRefreshProductHTML(htmlUrl, targetFilename))
                {
                    if (!HimallIO.ExistFile(htmlUrl))
                        CreatBrandchWAPHtml(productId, branchId);
                    else
                    {
                        var metaRemoteInfo = HimallIO.GetFileMetaInfo(htmlUrl);
                        if (null == metaRemoteInfo || CheckNeedRefreshFile(metaRemoteInfo.LastModifiedTime, 20))
                        {
                            CreatBrandchWAPHtml(productId, branchId);
                        }
                    }
                    var dirFullname = Core.Helper.IOHelper.GetMapPath("/Storage/Products/Statics");
                    if (!Directory.Exists(dirFullname))
                        Directory.CreateDirectory(dirFullname);
                    byte[] test = HimallIO.GetFileContent(htmlUrl);
                    File.WriteAllBytes(targetFilename, HimallIO.GetFileContent(htmlUrl));
                }
            }
        }

        /// <summary>
        /// 刷新本地缓存商品html文件 
        /// </summary>
        /// <param name="htmlUrl">远程html文件地址</param>
        /// <param name="targetFilename">本地待生成的html文件名</param>
        static void RefreshLocalProductHtml(long productId, string htmlUrl, string targetFilename)
        {
            lock (htmlUrl)
            {
                if (IsRefreshProductHTML(htmlUrl, targetFilename))
                {
                    if (!HimallIO.ExistFile(htmlUrl))
                        CreatPCHtml(productId);
                    else
                    {
                        var metaRemoteInfo = HimallIO.GetFileMetaInfo(htmlUrl);
                        if (null == metaRemoteInfo || CheckNeedRefreshFile(metaRemoteInfo.LastModifiedTime, 20))
                        {
                            CreatPCHtml(productId);
                        }
                    }
                    var dirFullname = Core.Helper.IOHelper.GetMapPath("/Storage/Products/Statics");
                    if (!Directory.Exists(dirFullname))
                        Directory.CreateDirectory(dirFullname);
                    byte[] test = HimallIO.GetFileContent(htmlUrl);
                    File.WriteAllBytes(targetFilename, HimallIO.GetFileContent(htmlUrl));
                }
            }
        }





        /// <summary>
        /// 获取一个商品
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static ProductInfo GetProduct(long id) =>
            Service.GetProduct(id);
        /// <summary>
        /// 获取一个商品
        /// </summary>
        public static ProductData GetProductData(long id) =>
            Service.GetProductData(id);
        public static List<ProductData> GetProductData(List<long> products) =>
            Service.GetProductData(products);

        public static Dictionary<string, int> GetStocks(long product) =>
            GetService<StockService>().GetStock(product);



        public static List<ProductInfo> GetOnSaleProducts(List<long> products) =>
             Service.GetProducts(products).Where(p => p.AuditStatus == ProductInfo.ProductAuditStatus.Audited && p.SaleStatus == ProductInfo.ProductSaleStatus.OnSale).ToList();

        public static Dictionary<string, int> GetCommentsNumber(long product)
        {
            return Service.GetCommentsNumber(product);
        }
        /// <summary>
        /// 根据多个ID取多个商品信息
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<Product> GetProductsByIds(IEnumerable<long> ids)
        {
            var products = Service.GetProducts(ids.ToList());
            return products.Map<List<Product>>();
        }
        /// <summary>
        /// 获取商品
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="shopid"></param>
        /// <returns></returns>
        public static List<Product> GetProducts(List<long> ids, long shopid)
        {
            var products = GetProductsByIds(ids);
            //TODO:FG 查询方法待优化
            return products.Where(p => p.ShopId == shopid).ToList();
        }

        public static List<ProductInfo> GetProducts(IEnumerable<long> products)
        {
            return Service.GetProducts(products.ToList());
        }
        /// <summary>
        /// 根据多个ID，取商品信息（所有状态）
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<Product> GetAllStatusProductByIds(IEnumerable<long> ids)
        {
            var productsInfo = Service.GetAllStatusProductByIds(ids);
            return productsInfo.ToList().Map<List<Product>>();
        }
        public static QueryPageModel<Product> GetProducts(ProductSearch query)
        {
            var data = Service.SearchProduct(query);

            return new QueryPageModel<Product>()
            {
                Models = data.Models.ToList().Map<List<Product>>(),
                Total = data.Total
            };
        }

        public static QueryPageModel<ProductInfo> GetProducts(ProductQuery query)
        {
            if (query.CategoryId.HasValue)
            {
                var categories = GetService<CategoryService>().GetAllCategoryByParent(query.CategoryId.Value);
                query.Categories = categories.Select(p => p.Id).ToList();
                query.Categories.Add(query.CategoryId.Value);
            }
            return Service.GetProducts(query);
        }

        public static int GetProductCount(ProductQuery query)
        {
            return Service.GetProductCount(query);
        }

        /// <summary>
        /// 根据商品id获取属性
        /// </summary>
        /// <param name="id">商品id</param>
        /// <returns></returns>
        public static List<ProductAttributeInfo> GetProductAttributes(long id)
        {
            return Service.GetProductAttribute(id);
        }

        /// <summary>
        /// 根据商品id获取描述
        /// </summary>
        /// <param name="id">商品id</param>
        /// <returns></returns>
        public static ProductDescriptionInfo GetProductDescription(long id)
        {
            return Service.GetProductDescription(id);

        }

        /// <summary>
        /// 将商品关联版式组合商品描述
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static string GetDescriptionContent(long id)
        {
            var product = Service.GetProductData(id);
            var descirption = product.Description;
            if (descirption == null)
                return string.Empty;
            string content = descirption.ShowMobileDescription?.Replace("src=\"/Storage/Shop", "src=\"" + Core.HimallIO.GetRomoteImagePath("/Storage/Shop"));//商品描述

            string descriptionPrefix = string.Empty, descriptiondSuffix = string.Empty; //顶部底部版式
            if (descirption.DescriptionPrefixId != 0)
            {
                var top = GetTemplate(descirption.DescriptionPrefixId);
                descriptionPrefix = top == null ? "" : top.MobileContent?.Replace("src=\"/Storage/Shop", "src=\"" + Core.HimallIO.GetRomoteImagePath("/Storage/Shop"));
            }

            if (descirption.DescriptiondSuffixId != 0)
            {
                var botton = GetTemplate(descirption.DescriptiondSuffixId);
                descriptiondSuffix = botton == null ? "" : botton.MobileContent?.Replace("src=\"/Storage/Shop", "src=\"" + Core.HimallIO.GetRomoteImagePath("/Storage/Shop"));
            }

            return string.Format("{0}{1}{2}", descriptionPrefix, content, descriptiondSuffix);
        }

        /// <summary>
        /// 根据商品id获取描述
        /// </summary>
        /// <param name="ids">商品ids</param>
        /// <returns></returns>
        public static List<DTO.ProductDescription> GetProductDescription(long[] ids)
        {
            var description = Service.GetProductDescriptions(ids);
            return AutoMapper.Mapper.Map<List<DTO.ProductDescription>>(description);
        }

        public static List<ProductShopCategory> GetProductShopCategoriesByProductId(long productId)
        {
            return Service.GetProductShopCategories(productId).ToList().Map<List<ProductShopCategory>>();
        }



        /// <summary>
        /// 获取商品的评论数
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static int GetProductCommentCount(long productId)
        {
            return Service.GetProductCommentCount(productId);
        }
        /// <summary>
        /// 取店铺超出安全库存的商品数
        /// </summary>
        /// <param name="shopid"></param>
        /// <returns></returns>
		public static long GetOverSafeStockProducts(long shopid)
        {
            return Service.GetProductCount(new ProductQuery
            {
                ShopId = shopid,
                OverSafeStock = true,
                SaleStatus = ProductInfo.ProductSaleStatus.OnSale,
                AuditStatus = new Entities.ProductInfo.ProductAuditStatus[] { ProductInfo.ProductAuditStatus.Audited }
            });
        }

        /// <summary>
        /// 取店铺商品数量
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static long GetProductCount(long shopId)
        {
            return Service.GetProductCount(new ProductQuery
            {
                ShopId = shopId,
                SaleStatus = ProductInfo.ProductSaleStatus.OnSale,
                AuditStatus = new Entities.ProductInfo.ProductAuditStatus[] { ProductInfo.ProductAuditStatus.Audited }
            });
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pids"></param>
        /// <param name="stock"></param>
        public static void SetProductOverSafeStock(List<long> pids, long stock)
        {
            Service.SetSafeStock(pids, stock);
        }
        /// <summary>
        /// 删除门店对应的商品
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="shopId"></param>
        public static void DeleteProduct(IEnumerable<long> ids, long shopId)
        {
            Service.DeleteProduct(ids, shopId);
        }

        /// <summary>
        /// 修改推荐商品
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="relationProductIds"></param>
        public static void UpdateRelationProduct(long productId, string relationProductIds)
        {
            Service.UpdateRelationProduct(productId, relationProductIds);
        }

        /// <summary>
        /// 获取商品的推荐商品
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static DTO.ProductRelationProduct GetRelationProductByProductId(long productId)
        {
            return Service.GetRelationProductByProductId(productId).Map<DTO.ProductRelationProduct>();
        }

        /// <summary>
        /// 获取商品的推荐商品
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static List<ProductRelationProduct> GetRelationProductByProductIds(IEnumerable<long> productIds)
        {
            return Service.GetRelationProductByProductIds(productIds).Map<List<DTO.ProductRelationProduct>>();
        }

        /// <summary>
        /// 获取指定类型下面热销的前N件商品
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<Product> GetHotSaleProductByCategoryId(int categoryId, int count)
        {
            return Service.GetHotSaleProductByCategoryId(categoryId, count).Map<List<Product>>();
        }

        /// <summary>
        /// 获取商家所有商品描述模板
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static List<ProductDescriptionTemplate> GetDescriptionTemplatesByShopId(long shopId)
        {
            return _productDescriptionTemplateService.GetTemplates(shopId).ToList().Map<List<ProductDescriptionTemplate>>();
        }
        /// <summary>
        /// 批量下架商品
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="shopid"></param>
        public static void BatchSaleOff(IEnumerable<long> ids, long shopid)
        {
            Service.SaleOff(ids, shopid);
        }
        /// <summary>
        /// 批量上架商品
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="shopid"></param>
        public static void BatchOnSale(IEnumerable<long> ids, long shopid)
        {
            Service.OnSale(ids, shopid);
        }



        /// <summary>
        /// 设置商品库存
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <param name="products"></param>
        /// <param name="stock"></param>
        /// <param name="option"></param>
        public static void SetProductStock(List<long> products, int stock, StockOptionType option)
        {
            GetService<StockService>().SetProductStock(products, stock, option);
        }

        public static void SetProductStock(long product, int stock, StockOptionType option)
        {
            SetProductStock(new List<long> { product }, stock, option);
        }

        public static bool BranchCanBuy(long userId, long productId, int count, string skuId, long shopBranchId, out int reason)
        {
            var product = Service.GetProduct(productId);
            if (product.SaleStatus != Entities.ProductInfo.ProductSaleStatus.OnSale || product.AuditStatus != Entities.ProductInfo.ProductAuditStatus.Audited)
            {
                //商城商品下架，但是门店的商品状态销售中，允许用户购买。
                //商城商品下架后，销售状态-仓库中，审核状态-待审核
                if (product.SaleStatus != Entities.ProductInfo.ProductSaleStatus.InStock && product.AuditStatus != Entities.ProductInfo.ProductAuditStatus.WaitForAuditing)
                {
                    reason = 1;
                    return false;
                }
            }
            var sku = ProductManagerApplication.GetSKU(skuId);
            if (sku == null)
            {
                reason = 2;
                return false;
            }
            var shopBranch = ShopBranchApplication.GetShopBranchById(shopBranchId);
            if (shopBranch == null)
            {
                reason = 4;
                return false;
            }
            var shopBranchSkuList = ShopBranchApplication.GetSkusByIds(shopBranchId, new List<string> { skuId });
            if (shopBranchSkuList == null || shopBranchSkuList.Count == 0 || shopBranchSkuList[0].Status == ShopBranchSkuStatus.InStock)
            {
                reason = 2;
                return false;
            }
            var sbsku = shopBranchSkuList.FirstOrDefault();
            if (sbsku.Stock < count)
            {
                reason = 9;
                return false;
            }
            if (product.IsDeleted)
            {
                reason = 2;
                return false;
            }

            if (product.MaxBuyCount <= 0)
            {
                reason = 0;
                return true;
            }

            var buyedCounts = OrderApplication.GetProductBuyCount(userId, new long[] { productId });
            if (product.MaxBuyCount < count + (buyedCounts.ContainsKey(productId) ? buyedCounts[productId] : 0))
            {
                reason = 3;
                return false;
            }
            reason = 0;
            return true;
        }
        /// <summary>
        /// 普通商品是否可购买（过滤活动购买数量）
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="productId"></param>
        /// <param name="count"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static bool CanBuy(long userId, long productId, int count, out int reason)
        {
            //TODO:FG 禁止使用 数字常量直接表示意义。 返回值不推荐使用out进行方法返回
            var product = Service.GetProduct(productId);
            if (product.SaleStatus != Entities.ProductInfo.ProductSaleStatus.OnSale || product.AuditStatus != Entities.ProductInfo.ProductAuditStatus.Audited)
            {
                reason = 1;
                return false;
            }
            var skus = Service.GetSKUs(productId);
            long stock = skus.Sum(p => p.Stock);
            if (stock == 0)
            {
                reason = 9;
                return false;
            }
            if (product.IsDeleted)
            {
                reason = 2;
                return false;
            }

            if (product.MaxBuyCount <= 0)
            {
                reason = 0;
                return true;
            }

            if (product.IsOpenLadder)
            {
                reason = 0;
                return true;
            }

            var buyedCounts = OrderApplication.GetProductBuyCount(userId, new long[] { productId });
            if (product.MaxBuyCount < count + (buyedCounts.ContainsKey(productId) ? buyedCounts[productId] : 0))
            {
                reason = 3;
                return false;
            }

            reason = 0;
            return true;
        }
        public static void AddBrowsingProduct(Entities.BrowsingHistoryInfo info)
        {
            Service.AddBrowsingProduct(info);
        }
        /// <summary>
        /// 获取指定商品ID并且不存在于直播商品库的商品列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<ProductInfo> GetNotInLiveLibraryProductByIds(IEnumerable<long> ids)
        {
            return Service.GetNotInLiveLibraryProductByIds(ids);
        }
        /// <summary>
        /// 获取指定商品ID并且存在于直播商品库的商品列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<ProductInfo> GetInLiveLibraryProductByIds(IEnumerable<long> ids)
        {
            return Service.GetInLiveLibraryProductByIds(ids);
        }

        /// <summary>
		/// 批量获取商品信息
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
        public static List<ProductInfo> GetProductByIds(IEnumerable<long> ids) =>
             Service.GetProducts(ids.ToList());


        /// <summary>
        /// 批量获取商品信息
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<Entities.ProductInfo> GetAllProductByIds(IEnumerable<long> ids)
        {
            return Service.GetAllProductByIds(ids);
        }

        public static List<Entities.BrowsingHistoryInfo> GetBrowsingProducts(long userId)
        {
            return Service.GetBrowsingProducts(userId);
        }

        #region 阶梯价--张宇枫
        /// <summary>
        /// 根据商品ID获取阶梯价列表
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="isOpenLadder">实付开启了阶梯价</param>
        /// <returns></returns>
        public static List<ProductLadderPriceInfo> GetLadderPriceInfosByProductId(long productId, bool isOpenLadder = true)
        {
            if (isOpenLadder)
                return _productLadderPriceService.GetLadderPricesByProductIds(productId);
            return null;
        }

        /// <summary>
        /// 根据商品ID获取多个价格
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="IsSelf">是否官方直营店</param>
        /// <param name="discount">会员折扣(0.01-1)</param>
        /// <returns></returns>
        public static List<ProductLadderPrice> GetLadderPriceByProductIds(long productId, bool IsSelf = false, decimal discount = 1m)
        {
            var priceInfo = _productLadderPriceService.GetLadderPricesByProductIds(productId);
            if (priceInfo == null)
                priceInfo = new List<ProductLadderPriceInfo>();//特意不让为空，便于前台调用可取值

            return priceInfo.Select(p =>
            {
                var lprice = p.Price;
                if (IsSelf)
                    lprice = p.Price * discount;
                var price = new ProductLadderPrice
                {
                    Id = p.Id,
                    MinBath = p.MinBath,
                    MaxBath = p.MaxBath,
                    ProductId = p.ProductId,
                    Price = Convert.ToDecimal(lprice.ToString("F2"))
                };
                return price;
            }).ToList();
        }

        /// <summary>
        /// 获取商品销售价格
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public static decimal GetProductLadderPrice(long productId, int quantity)
        {
            var ladderPrices = _productLadderPriceService.GetLadderPricesByProductIds(productId);
            var price = 0m;
            if (ladderPrices.Count > 0)
                return GetProductLadderPrice(ladderPrices, quantity);
            return price;
        }

        public static decimal GetProductLadderPrice(List<ProductLadderPriceInfo> source, int quantity)
        {
            return source.Find(i => (quantity <= i.MinBath) || (quantity >= i.MinBath && quantity <= i.MaxBath)).Price;
        }


        public static void FillSkuAlias(SKUInfo sku, ProductInfo product, TypeInfo type)
        {
            //默认名
            sku.ColorAlias = SpecificationType.Color.ToDescription();
            sku.SizeAlias = SpecificationType.Size.ToDescription();
            sku.VersionAlias = SpecificationType.Version.ToDescription();

            //分类别名
            if (type != null)
            {
                if (!string.IsNullOrEmpty(type.ColorAlias)) sku.ColorAlias = type.ColorAlias;
                if (!string.IsNullOrEmpty(type.SizeAlias)) sku.SizeAlias = type.SizeAlias;
                if (!string.IsNullOrEmpty(type.VersionAlias)) sku.VersionAlias = type.VersionAlias;
            }

            //商品别名
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ColorAlias)) sku.ColorAlias = product.ColorAlias;
                if (!string.IsNullOrEmpty(product.SizeAlias)) sku.SizeAlias = product.SizeAlias;
                if (!string.IsNullOrEmpty(product.VersionAlias)) sku.VersionAlias = product.VersionAlias;
            }
        }
        /// <summary>
        /// 获取阶梯商品最小批量
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static int GetProductLadderMinMath(long productId)
        {
            var minMath = 0;
            var ladder = GetLadderPriceByProductIds(productId);
            if (ladder.Any())
                minMath = ladder.Min(p => p.MinBath);
            return minMath;
        }

        /// <summary>
        /// 判断购物车提交时，阶梯商品是否达最小批量
        /// </summary>
        /// <param name="cartItemIds"></param>
        /// <returns></returns>
        public static bool IsExistLadderMinMath(string cartItemIds, ref string msg)
        {
            msg = "结算的商品必须满足最小批量才能购买！";
            var result = true;
            var cartItemIdsArr = cartItemIds.Split(',').Where(d => !string.IsNullOrWhiteSpace(d)).Select(t => long.Parse(t));
            var cartItems = CartApplication.GetCartItems(cartItemIdsArr);
            if (cartItems.Any())
            {
                var groupCart = cartItems.Where(item => item.ShopBranchId == 0).ToList().Select(c =>
               {
                   var cItem = new Himall.Entities.ShoppingCartItem();
                   var skuInfo = Service.GetSku(c.SkuId);
                   if (skuInfo != null)
                       cItem = c;
                   return cItem;
               }).GroupBy(i => i.ProductId);
                foreach (var cart in cartItems.ToList())
                {
                    var product = GetProduct(cart.ProductId);
                    if (product.IsOpenLadder)
                    {
                        var quantity =
                            groupCart.Where(i => i.Key == cart.ProductId)
                                .ToList()
                                .Sum(cartitem => cartitem.Sum(i => i.Quantity));
                        var minMath = GetProductLadderMinMath(cart.ProductId);
                        if (minMath > 0 && quantity < minMath)
                            result = false;
                    }
                    else
                    {
                        var sku = Service.GetSku(cart.SkuId);
                        if (cart.Quantity > sku.Stock)
                        {
                            msg = string.Format("商品“{0}”库存不足,仅剩{1}件", CutString(product.ProductName, 10, "..."), sku.Stock);
                            return false;
                        }
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 切割字符
        /// </summary>
        /// <param name="str"></param>
        /// <param name="len"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        private static string CutString(string str, int len, string suffix)
        {
            if (!string.IsNullOrWhiteSpace(str) && str.Length > len)
            {
                str = str.Substring(0, len) + suffix;
            }
            return str;
        }
        #endregion

        /// <summary>
        /// 指定地区包邮
        /// </summary>
        /// <returns></returns>
        public static bool IsFreeRegion(long productId, decimal discount, int streetId, int count, string skuId)
        {
            return Service.IsFreeRegion(productId, discount, streetId, count, skuId);
        }

        public static bool BatchSettingFreightTemplate(IEnumerable<long> pids, long freightTemplateId)
        {
            return Service.BatchSettingFreightTemplate(pids, freightTemplateId);
        }
        public static void SetStock(List<long> products, Dictionary<long, long> stocks)
        {
            GetService<StockService>().SetStock(products, stocks);
        }
        public static bool BatchSettingPrice(Dictionary<long, decimal> productDics, Dictionary<long, string> priceDics)
        {
            return Service.BatchSettingPrice(productDics, priceDics);
        }
        public static bool UpdateShopDisplaySequence(long id, int order)
        {
            return Service.UpdateShopDisplaySequence(id, order);
        }
        public static bool UpdateDisplaySequence(long id, int order)
        {
            return Service.UpdateDisplaySequence(id, order);
        }
        /// <summary>
        /// 批量更新虚拟销量
        /// </summary>
        /// <param name="productIds"></param>
        /// <param name="virtualSaleCounts"></param>
        /// <returns></returns>
        public static bool BtachUpdateSaleCount(List<long> productIds, long virtualSaleCounts, int minSaleCount = 0, int maxSaleCount = 0)
        {
            return Service.BtachUpdateSaleCount(productIds, virtualSaleCounts, minSaleCount, maxSaleCount);
        }

        /// <summary>
        /// 当前参加的活动
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static string CurrentJoinActive(long productId)
        {
            string result = Service.CurrentJoinActive(productId);
            return result;
        }
        public static void DeleteImportProduct(List<long> productIds)
        {
            Service.DeleteImportProduct(productIds);
        }

        /// <summary>
        /// 获取商品显示状态
        /// </summary>
        /// <param name="pro"></param>
        /// <param name="sku"></param>
        /// <param name="quantity"></param>
        /// <param name="sb"></param>
        /// <param name="sbsku"></param>
        /// <returns>状态值 0:正常；1：已失效；2：库存不足；3：已下架；</returns>
        public static int GetProductShowStatus(ProductData pro, DTO.SKU sku = null, int quantity = 1, DTO.ShopBranch sb = null, ShopBranchSkuInfo sbsku = null)
        {
            int result = 0;
            //己删除商品快速处理
            if (pro.IsDeleted)
            {
                return 1;
            }
            if (sb != null && pro.IsOpenLadder)
            {
                return 1;
            }
            if (sb != null && sbsku != null)
            {
                if (sbsku.Status == ShopBranchSkuStatus.Normal)
                {
                    if (sbsku.Stock < quantity)
                    {
                        result = 2;
                    }
                }
                else
                {
                    result = 3;
                }
                return result;
            }
            if (pro.AuditStatus == ProductInfo.ProductAuditStatus.Audited && pro.SaleStatus == ProductInfo.ProductSaleStatus.OnSale)
            {
                result = 0;
                if (sku == null)
                {
                    var stocks = GetService<StockService>().GetStock(pro.Id);
                    if (stocks.Sum(d => d.Value) < quantity)
                    {
                        result = 2;
                    }
                }
                else
                {
                    if (sku.Stock < quantity)
                    {
                        result = 2;
                    }
                }
            }
            else
            {
                result = 3;
            }
            return result;
        }


        /// <summary>
        /// 获取商品显示状态
        /// </summary>
        /// <param name="pro"></param>
        /// <param name="sku"></param>
        /// <param name="quantity"></param>
        /// <param name="sb"></param>
        /// <param name="sbsku"></param>
        /// <returns>状态值 0:正常；1：已失效；2：库存不足；3：已下架；</returns>
        public static int GetProductShowStatus(ProductInfo pro, DTO.SKU sku = null, int quantity = 1, DTO.ShopBranch sb = null, ShopBranchSkuInfo sbsku = null)
        {
            int result = 0;
            //己删除商品快速处理
            if (pro.IsDeleted)
            {
                return 1;
            }
            if (sb != null && pro.IsOpenLadder)
            {
                return 1;
            }
            if (sb != null && sbsku != null)
            {
                if (sbsku.Status == ShopBranchSkuStatus.Normal)
                {
                    if (sbsku.Stock < quantity)
                    {
                        result = 2;
                    }
                }
                else
                {
                    result = 3;
                }
                return result;
            }
            if (pro.AuditStatus == ProductInfo.ProductAuditStatus.Audited && pro.SaleStatus == ProductInfo.ProductSaleStatus.OnSale)
            {
                result = 0;
                if (sku == null)
                {
                    var stocks = GetService<StockService>().GetStock(pro.Id);
                    if (stocks.Sum(d => d.Value) < quantity)
                    {
                        result = 2;
                    }
                }
                else
                {
                    if (sku.Stock < quantity)
                    {
                        result = 2;
                    }
                }
            }
            else
            {
                result = 3;
            }
            return result;
        }
        #region 私有方法
        private static Entities.ProductInfo.ProductEditStatus GetEditStatus(Entities.ProductInfo.ProductEditStatus status)
        {
            if (status > Entities.ProductInfo.ProductEditStatus.EditedAndPending)
                return Entities.ProductInfo.ProductEditStatus.CompelPendingHasEdited;
            return Entities.ProductInfo.ProductEditStatus.EditedAndPending;
        }
        #endregion



        #region SKU相关  待移入SKUApplication

        /// <summary>
        /// 根据商品id获取SKUInfo
        /// </summary>
        /// <param name="id">商品id</param>
        /// <returns></returns>
        public static List<Himall.Entities.SKUInfo> GetSKUsByProductId(long productId)
        {
            return Service.GetSKUs(productId);
        }

        /// <summary>
        /// 根据商品id获取SKU
        /// </summary>
        /// <param name="id">商品id</param>
        /// <returns></returns>
        public static List<SKU> GetSKUs(long product)
        {
            var skus = Service.GetSKUs(product);
            return AutoMapper.Mapper.Map<List<SKU>>(skus);
        }

        /// <summary>
        /// 根据商品id获取SKU
        /// </summary>
        /// <param name="productIds">商品id</param>
        /// <returns></returns>
        public static List<SKU> GetSKUByProducts(IEnumerable<long> productIds)
        {
            var skus = Service.GetSKUs(productIds);
            return AutoMapper.Mapper.Map<List<DTO.SKU>>(skus);
        }

        public static List<SKUInfo> GetSKUsByProduct(IEnumerable<long> productIds)
        {
            return Service.GetSKUs(productIds);
        }
        /// <summary>
        /// 根据sku id 获取sku信息
        /// </summary>
        /// <param name="skuIds"></param>
        /// <returns></returns>
        public static List<SKU> GetSKUs(IEnumerable<string> skuIds)
        {
            var list = Service.GetSKUs(skuIds);
            return list.Map<List<DTO.SKU>>();
        }

        public static List<SKUInfo> GetSKUInfos(IEnumerable<string> skuIds)
        {
            return Service.GetSKUs(skuIds);
        }
        /// <summary>
        /// 根据商品id获取SKU
        /// </summary>
        /// <param name="id">商品id</param>
        /// <returns></returns>
        public static SKU GetSKU(string skuId)
        {
            var sku = Service.GetSku(skuId);
            var ret = AutoMapper.Mapper.Map<DTO.SKU>(sku);
            return ret;
        }
        /// <summary>
        /// 根据商品id获取SKU
        /// </summary>
        /// <param name="skuId"></param>
        /// <returns></returns>
        public static SKUInfo GetSKUInfo(string skuId)
        {
            var sku = Service.GetSku(skuId);
            return sku;
        }


        public static void SetSkuStock(StockOptionType option, Dictionary<string, long> changes)
        {
            GetService<StockService>().SetSkuStock(option, changes);
        }
        public static void SetSkuStock(StockOptionType option, string skuId, long change)
        {
            GetService<StockService>().SetSkuStock(skuId, change, option);
        }



        /// <summary>
        /// 取超出警戒库存的商品ID
        /// </summary>
        /// <param name="pids"></param>
        /// <returns></returns>
        public static IEnumerable<long> GetOverSafeStockProductIds(IEnumerable<long> pids)
        {
            var skus = Service.GetSKUs(pids).ToList();
            var overStockPids = skus.Where(e => e.SafeStock >= e.Stock).Select(e => e.ProductId).Distinct();
            return overStockPids;
        }



        #endregion

        #region 虚拟商品
        public static VirtualProductInfo GetVirtualProductInfoByProductId(long productId)
        {
            return Service.GetVirtualProductInfoByProductId(productId);
        }

        public static List<VirtualProductItemInfo> GetVirtualProductItemInfoByProductId(long productId)
        {
            return Service.GetVirtualProductItemInfoByProductId(productId);
        }

        /// <summary>
        /// 核销码生效类型文本
        /// </summary>
        /// <param name="effectiveType">核销码生效类型</param>
        /// <param name="hour">付完款几小时后生效</param>
        /// <returns></returns>
        public static string GetSupportRefundTypeText(sbyte supportRefundType)
        {
            string strText = string.Empty;
            switch (supportRefundType)
            {
                case 1:
                    strText = "支持有效期内退款";
                    break;
                case 2:
                    strText = "支持随时退款";
                    break;
                case 3:
                    strText = "不支持退款";
                    break;
            }
            return strText;
        }

        /// <summary>
        /// 核销码生效类型文本
        /// </summary>
        /// <param name="effectiveType">核销码生效类型</param>
        /// <param name="hour">付完款几小时后生效</param>
        /// <returns></returns>
        public static string GetEffectiveTypeText(sbyte effectiveType, int hour)
        {
            string strText = string.Empty;
            switch (effectiveType)
            {
                case 1:
                    strText = "立即生效";
                    break;
                case 2:
                    strText = string.Format("付款完成{0}小时后生效", hour);
                    break;
                case 3:
                    strText = "次日生效";
                    break;
            }
            return strText;
        }
        #endregion

        /// <summary>
        /// 同步商品的查询价格
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="newPrice"></param>
        public static void UpdateSearchProductPrice(long productId, long shopId, decimal newPrice)
        {
            _searchProductService.UpdateSearchProductPrice(productId, shopId, newPrice);
        }

        public static SearchProductInfo GetSingleSearchProductInfo(long productId, long shopId)
        {
            return _searchProductService.GetSingleSearchProductInfo(productId, shopId);
        }

        /// <summary>
        /// 更新查询表的活动商品信息
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="shopId"></param>
        /// <param name="activeType"></param>
        /// <param name="activtyId"></param>
        public static void SaveProdcutActivty(long productId, long shopId, ProductInfo.ProductActiveType activeType = ProductInfo.ProductActiveType.Default, long activtyId = 0)
        {

            var product = Service.GetProduct(productId);
            if (product == null) return;
            decimal minPrice = 0;
            if (product.IsOpenLadder)
            {
                //比较阶梯价，取阶梯价最小价格，不用管梯度
                var ladderSkus = _productLadderPriceService.GetLadderPricesByProductIds(productId);
                if (ladderSkus != null && ladderSkus.Count() > 0)
                {
                    decimal minLadderPrice = ladderSkus.Min(p => p.Price);
                    minPrice = minLadderPrice;
                }
            }
            else
            {
                //先取sku最小价格
                var skus = Service.GetSKUs(productId);
                minPrice = skus.Min(p => p.SalePrice);
            }
            //比较拼团价格
            var fightGroup = _FightGroupService.GetActiveIdByProductIdAndShopId(productId, shopId);
            if (fightGroup != null && fightGroup.ActiveStatus == FightGroupActiveStatus.Ongoing)
            {
                var miniGroupPrice = fightGroup.ActiveItems.Min(p => p.ActivePrice);
                if (miniGroupPrice < minPrice)
                {
                    minPrice = miniGroupPrice;
                }
            }
            //比较限时购价格
            var ltmbuy = GetService<LimitTimeBuyService>().GetFlashSaleInfoByProductIdAndShopId(productId, shopId);
            if (ltmbuy != null)
            {
                if (ltmbuy.BeginDate <= DateTime.Now)
                {
                    if (ltmbuy.MinPrice < minPrice)
                    {
                        minPrice = ltmbuy.MinPrice;
                    }
                }
                else
                {
                    var flashSaleConfig = _LimitTimeBuyService.GetConfig();
                    TimeSpan flashSaleTime = ltmbuy.BeginDate - DateTime.Now;  //开始时间还剩多久
                    TimeSpan preheatTime = new TimeSpan(flashSaleConfig.Preheat, 0, 0);  //预热时间是多久
                    if (preheatTime >= flashSaleTime && !flashSaleConfig.IsNormalPurchase && ltmbuy.MinPrice < minPrice)  //预热大于开始并且不能购买，写入最低价格，需求386
                    {
                        minPrice = ltmbuy.MinPrice;
                    }
                }
            }
            var searchInfo = _searchProductService.GetSingleSearchProductInfo(productId, shopId);
            if (searchInfo != null && (minPrice != searchInfo.SalePrice || searchInfo.ActiveType != activeType))
            {
                _searchProductService.UpdateSearchProductActivty(productId, shopId, minPrice, activeType, activtyId);
            }

        }
        /// <summary>
        /// 审核商品
        /// </summary>
        /// <param name="productIds"></param>
        /// <param name="auditStatus"></param>
        /// <param name="message"></param>
        public static void AuditProducts(IEnumerable<long> productIds, ProductInfo.ProductAuditStatus auditStatus, string message)
        {
            _ProductService.AuditProducts(productIds, auditStatus, message);
            if (auditStatus == ProductInfo.ProductAuditStatus.Audited)
            {
                //创建一个新线程去执行，不影响审核操作的时间
                Task.Factory.StartNew(() =>
                {
                    LiveProductLibaryQuery query = new LiveProductLibaryQuery();
                    query.ProductIds = string.Join(",", productIds);
                    QueryPageModel<LiveProductLibaryModel> liveProducts = LiveApplication.GetLiveProductLibrarys(query);
                    if (liveProducts.Total > 0)
                    {

                        foreach (LiveProductLibaryModel liveProductInfo in liveProducts.Models)
                        {
                            if (liveProductInfo != null && liveProductInfo.LiveAuditStatus != LiveProductAuditStatus.NoSubmit)
                            {
                                //未审核状态，允许更新所有字段,已审核状态可以更新价格
                                if (liveProductInfo.LiveAuditStatus == LiveProductAuditStatus.Audited || liveProductInfo.LiveAuditStatus == LiveProductAuditStatus.NoAudit)
                                {
                                    string msg = "";
                                    LiveApplication.UpdateAppletLiveProduct(liveProductInfo, out msg);
                                }
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 将当前活动信息保存在商品查询表中
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="shopId"></param>
        public static void SaveCaculateMinPrice(long productId, long shopId)
        {

            var product = Service.GetProduct(productId);
            if (product == null) return;
            decimal minPrice = 0;
            if (product.IsOpenLadder)
            {
                //比较阶梯价，取阶梯价最小价格，不用管梯度
                var ladderSkus = _productLadderPriceService.GetLadderPricesByProductIds(productId);
                if (ladderSkus != null && ladderSkus.Count() > 0)
                {
                    decimal minLadderPrice = ladderSkus.Min(p => p.Price);
                    minPrice = minLadderPrice;
                }
            }
            else
            {
                //先取sku最小价格
                var skus = Service.GetSKUs(productId);
                minPrice = skus.Min(p => p.SalePrice);
            }
            long actityId = 0;
            var activeType = ProductInfo.ProductActiveType.Default;
            //比较拼团价格
            var fightGroup = _FightGroupService.GetActiveIdByProductIdAndShopId(productId, shopId);
            if (fightGroup != null && fightGroup.ActiveStatus == FightGroupActiveStatus.Ongoing)
            {
                activeType = ProductInfo.ProductActiveType.Group;
                actityId = fightGroup.Id;
                var miniGroupPrice = fightGroup.ActiveItems.Min(p => p.ActivePrice);
                if (miniGroupPrice < minPrice)
                {
                    minPrice = miniGroupPrice;
                }
            }
            //比较限时购价格
            var ltmbuy = GetService<LimitTimeBuyService>().GetFlashSaleInfoByProductIdAndShopId(productId, shopId);
            if (ltmbuy != null)
            {
                activeType = ProductActiveType.LimitTime;
                actityId = ltmbuy.Id;
                if (ltmbuy.BeginDate <= DateTime.Now)
                {
                    if (ltmbuy.MinPrice < minPrice)
                    {
                        minPrice = ltmbuy.MinPrice;
                    }
                }
                else
                {

                    var flashSaleConfig = _LimitTimeBuyService.GetConfig();
                    TimeSpan flashSaleTime = ltmbuy.BeginDate - DateTime.Now;  //开始时间还剩多久
                    TimeSpan preheatTime = new TimeSpan(flashSaleConfig.Preheat, 0, 0);  //预热时间是多久
                    if (preheatTime >= flashSaleTime && !flashSaleConfig.IsNormalPurchase && ltmbuy.MinPrice < minPrice)  //预热大于开始并且不能购买，写入最低价格，需求386
                    {
                        minPrice = ltmbuy.MinPrice;
                    }
                }
            }
            var searchInfo = _searchProductService.GetSingleSearchProductInfo(productId, shopId);
            if (searchInfo != null && minPrice != searchInfo.SalePrice)
            {
                _searchProductService.UpdateSearchProductActivty(productId, shopId, minPrice, activeType, actityId);
            }
        }

        /// <summary>
        /// 判断是否关注过此商品
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns></returns>
        public static bool IsFavorite(long productId, long userId)
        {
            return Service.IsFavorite(productId, userId);
        }

        /// <summary>
        ///用于渲染可视化时调用
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetViewProductsByIds(List<long> ids, bool isApplet = false)
        {
            var searchpro = _searchProductService.GetSalePriceByIds(ids);
            var models = Service.GetProductData(ids)
               .Select(item => new
               {
                   id = item.Id,
                   activityid = searchpro.Where(p => p.ProductId == item.Id).Select(s => s.ActivityId).FirstOrDefault(),
                   activitytype = searchpro.Where(p => p.ProductId == item.Id).Select(s => s.ActiveType).FirstOrDefault(),
                   price = searchpro.Where(p => p.ProductId == item.Id).Select(pro => pro.SalePrice).FirstOrDefault(),
                   productType = item.ProductType,
                   pname = item.ProductName,
                   uptime = item.UpdateTime,
                   pathurl = GetImagePath(item.ImagePath, ImageSize.Size_350, item.UpdateTime, 1, isApplet).Replace("http://", "https://"),
                   statue = item.ShowProductState,
                   islive = LiveApplication.IsLiveProduct(item.Id) > 0
               }).ToList();
            return models;
        }

        /// <summary>
        /// 用于渲染前台可视化的限时购列表
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetLimitBuyViewByIds(List<long> list)
        {
            var limibuys = LimitTimeApplication.GetAvailable(list);
            var summarys = ServiceProvider.Instance<LimitTimeBuyService>.Create.GetSummary(list);
            var products = GetProductData(limibuys.Select(p => p.ProductId).ToList());

            var flashlist = limibuys.Select(item =>
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                var summary = summarys.FirstOrDefault(p => p.Id == item.Id);
                return new
                {
                    item_id = item.Id,
                    pic = HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_350).Replace("http://", "https://"),
                    pid = item.ProductId,
                    title = products.Where(p => p.Id == item.ProductId).Select(p => p.ProductName).FirstOrDefault(),
                    startTime = item.BeginDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    endTime = item.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    beginSec = DateTimeHelper.ToSeconds(item.BeginDate),
                    endSec = DateTimeHelper.ToSeconds(item.EndDate),
                    statue = item.Status,
                    price = item.Items.Min(i => i.Price),
                    saleprice = products.Where(p => p.Id == item.ProductId).Select(p => p.MarketPrice).FirstOrDefault(),
                    number = summary.SaleCount,
                    stock = summary.Total,
                    ShopId = product.ShopId,
                    sellingPoint = products.Where(p => p.Id == item.ProductId).Select(p => p.ShortDescription).FirstOrDefault()
                };
            }).ToList();

            return flashlist;
        }


        public static IEnumerable<dynamic> GetFightGroupViewByIds(List<long> ids)
        {
            var data = FightGroupApplication.GetAvailable(ids);
            if (data.Count == 0)
                return new List<dynamic>();
            var items = ServiceProvider.Instance<FightGroupService>.Create.GetActiveItemsSimp(data.Select(p => p.Id).ToList());
            var salecounts = items.GroupBy(p => p.ActiveId).ToDictionary(p => p.Key, v => v.Sum(p => p.BuyCount));
            var products = GetProductData(data.Select(p => p.ProductId).ToList());
            return data
                .Select(item =>
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    salecounts.TryGetValue(item.Id, out var salecount);
                    return new
                    {
                        item_id = item.Id,
                        pid = item.ProductId,
                        pic = HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_350).Replace("http://", "https://"),
                        title = product.ProductName,
                        item.ShopId,
                        item.StartTime,
                        item.EndTime,
                        beginSec = DateTimeHelper.ToSeconds(item.StartTime),
                        endSec = DateTimeHelper.ToSeconds(item.EndTime),
                        statue = item.ActiveStatus,
                        price = item.Items.Min(i => i.ActivePrice),
                        saleprice = product.Skus.Min(p => p.SalePrice),
                        number = item.LimitedNumber,
                        salecount = salecount,
                        sellingPoint = product.ShortDescription
                    };
                }).ToList();
        }

        #region  本地数据包导入功能
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt">读取到的table</param>
        /// <param name="userId">用户Id</param>
        /// <param name="shopid">商家编号</param>
        /// <param name="paraSaleStatus">商品状态</param>
        /// <param name="filepath">文件路径</param>
        /// <param name="imgpath1">图片路径</param>
        /// <returns>要异步上传的商品图片</returns>
        public static Dictionary<long, string> ValidProduct(DataTable dt, long userId, long shopid, ProductSaleStatus status, string filepath, string imgpath1, out int errorsum)
        {
            int successcount = 0;
            int errorcount = 0;
            DataColumn datacolumn = new DataColumn("失败原因", typeof(string));
            dt.Columns.Add(datacolumn);
            var shopfregght = FreightTemplateApplication.GetShopFreightTemplate(shopid);//运费模板
            var shopcategorys = ShopCategoryApplication.GetShopCategory(shopid);//获取商家的分类
            var categorylist = CategoryApplication.GetCategories();//获取平台所有分类
            Brand currentbrand = new Brand();
            Dictionary<long, string> proimgs = new Dictionary<long, string>();//待异步处理主图的图片
            foreach (DataRow row in dt.Rows)
            {
                bool tag = true;
                string columname = "";//列名称
                foreach (DataColumn column in dt.Columns)
                {
                    columname = column.ColumnName;
                    if (columname.Contains("*"))//验证必填
                    {
                        if (row[columname] == null || string.IsNullOrEmpty(row[columname].ToString()))
                        {
                            tag = false;
                            break;
                        }
                    }

                }
                if (!tag)
                { //有存在为空的值
                    row["失败原因"] = columname.Replace("*", "") + "必填";
                    errorcount++;//记录错误商品数
                    continue;
                }


                string cnameone = row["*平台一级分类"].ToString();
                var categoryone = categorylist.Where(c => c.Name == cnameone).FirstOrDefault();
                if (categoryone == null || categoryone.Id <= 0)
                {
                    row["失败原因"] = "平台一级分类不存在";
                    errorcount++;//记录错误商品数
                    continue;
                }

                string cnametwo = row["*平台二级分类"].ToString();
                var categorytwo = categorylist.Where(c => c.Name == cnametwo && c.ParentCategoryId == categoryone.Id).FirstOrDefault();//验证二级分类
                if (categorytwo == null || categorytwo.Id <= 0)
                {
                    row["失败原因"] = "平台二级分类不存在";
                    errorcount++;//记录错误商品数
                    continue;
                }

                string cnamethree = row["*平台三级分类"].ToString();
                var categorythree = categorylist.Where(c => c.Name == cnamethree && c.ParentCategoryId == categorytwo.Id).FirstOrDefault();//验证二级分类
                if (categorythree == null || categorythree.Id <= 0)
                {
                    row["失败原因"] = "平台三级分类不存在";
                    errorcount++;//记录错误商品数
                    continue;
                }

                string shopcategoryname = row["*商家分类"].ToString();

                var currentcategory = shopcategorys.Where(s => s.Name == shopcategoryname).FirstOrDefault();
                if (currentcategory == null || currentcategory.Id <= 0)
                {
                    row["失败原因"] = "商品分类不存在";
                    errorcount++;//记录错误商品数
                    continue;
                }
                string proname = row["*商品名称"].ToString();//商品名称
                string procode = row["*商品货号"].ToString();//商品编码
                ProductQuery productQuery = new ProductQuery();
                productQuery.CategoryId = categorythree.Id;
                productQuery.ShopId = shopid;
                productQuery.KeyWords = proname;

                var proCount = GetProductCount(productQuery);
                if (proCount > 0)
                {//当前店铺、分类已经存在相同编码的商品
                    row["失败原因"] = "改商品不能重复导入，当前店铺分类已存在相同编码的商品";
                    errorcount++;//记录错误商品数
                    continue;
                }

                long lngStock = 0;//库存
                decimal saleprice = 0;//市场价
                if (!decimal.TryParse(row["*商城价"].ToString(), out decimal price))
                {
                    row["失败原因"] = "商城价格格式不正确";
                    errorcount++;//记录错误商品数
                    continue;
                }


                if (!long.TryParse(row["*限购数"].ToString(), out long limitbuy) || limitbuy < 0)
                {
                    row["失败原因"] = "限购数量格式不正确";
                    errorcount++;//记录错误商品数
                    continue;
                }

                string brandnme = row["品牌"].ToString();
                if (brandnme != "")
                {
                    currentbrand = BrandApplication.GetShopBrands(shopid).Where(b => b.Name.ToLower() == brandnme.ToLower()).FirstOrDefault();
                    if (currentbrand == null)
                    {
                        row["失败原因"] = "品牌不存在";
                        errorcount++;//记录错误商品数
                        continue;
                    }
                }

                string proimg = row["商品主图"].ToString();
                if (proimg != "")
                {
                    if (!IsExitImg(filepath + "\\products\\" + proimg))
                    {
                        row["失败原因"] = "未上传主图";
                        errorcount++;//记录错误商品数
                        continue;
                    }
                }

                string freightname = row["*运费模板"].ToString();

                var currentfreight = shopfregght.Where(f => f.Name == freightname).FirstOrDefault();
                if (currentfreight == null || currentfreight.Id <= 0)
                {
                    row["失败原因"] = "运费模板不存在";
                    errorcount++;//记录错误商品数
                    continue;
                }

                if (row["市场价"].ToString() != "")
                {
                    if (!decimal.TryParse(row["市场价"].ToString(), out saleprice))
                    {
                        row["失败原因"] = "市场价格式不正确";
                        errorcount++;//记录错误商品数
                        continue;
                    }
                }

                if (!long.TryParse(row["*库存"].ToString(), out lngStock) || lngStock < 0)
                {
                    row["失败原因"] = "库存格式不正确";
                    errorcount++;//记录错误商品数
                    continue;
                }



                long pid = Service.GetNextProductId();//获取商品编号


                var product = new ProductInfo()
                {
                    Id = pid,
                    TypeId = currentcategory.Id,
                    AddedDate = DateTime.Now,
                    BrandId = currentbrand.Id,
                    CategoryId = categorythree.Id,
                    CategoryPath = categorythree.Path,
                    MarketPrice = saleprice > 0 ? saleprice : price,
                    ShortDescription = row["广告词"].ToString(),
                    ProductCode = procode,
                    ImagePath = "",
                    DisplaySequence = 0,//默认的序号都为0
                    ProductName = proname,
                    MinSalePrice = price,
                    ShopId = shopid,
                    HasSKU = false,//判断是否有多规格才能赋值
                    SaleStatus = status,
                    AuditStatus = ProductAuditStatus.WaitForAuditing,
                    FreightTemplateId = currentfreight.Id,
                    MeasureUnit = row["*计量单位"].ToString()
                };
                var skus = new List<SKUInfo>() { new SKUInfo()
                                        { Id=string.Format("{0}_{1}_{2}_{3}" , pid , "0" , "0" , "0"),
                                          Stock=lngStock,
                                          SalePrice=price,
                                          CostPrice=price
                                        }};
                var description = new ProductDescriptionInfo
                {
                    AuditReason = "",
                    Description = "宝贝描述",//不能纯去除
                    DescriptiondSuffixId = 0,
                    DescriptionPrefixId = 0,
                    Meta_Description = string.Empty,
                    Meta_Keywords = string.Empty,
                    Meta_Title = string.Empty,
                    ProductId = pid
                };
                //图片处理
                product.ImagePath = imgpath1 + "/" + product.Id.ToString();
                Service.AddProduct(shopid, product, null, skus.ToArray(), description, null, new long[] { currentcategory.Id }, null, null);
                if (!string.IsNullOrEmpty(proimg) && !proimgs.Keys.Contains(product.Id))
                {
                    proimgs.Add(product.Id, proimg);//保存图片路径，让下面异步处理
                }
                successcount++;
                Core.Cache.Insert<int>(CacheKeyCollection.UserImportProductCount(userId), successcount);

            }
            errorsum = errorcount;
            return proimgs;
        }





        /// <summary>
        /// 查找是否存在指定名称的图片
        /// </summary>
        /// <param name="proimg"></param>
        /// <returns></returns>
        private static bool IsExitImg(string proimg)
        {
            return File.Exists(proimg);
        }

        /// <summary>
        /// 获取指定路径下的所有图片文件
        /// </summary>
        /// <param name="filepath"></param>


        #endregion


        #region 视频号功能

        /// <summary>
        /// 生成商品图文素材并群发
        /// </summary>
        public static ProductArticleShare MultipleImgTextNews(long productId, long shopBranchId)
        {
            if (Cache.Exists(productId + "|" + shopBranchId))
                throw new HimallException("正在生成中...");
            // 获得分享商品
            var product = GetProduct(productId);
            // 存在文章链接不生成直接返回（链接中是不会更新主图和价格的）
            var articleUrl = _WXMsgTemplateService.ExistWeiXinArticleUrl(productId, shopBranchId);
            if (!string.IsNullOrWhiteSpace(articleUrl))
            {
                return new ProductArticleShare()
                {
                    ArticleUrl = articleUrl,
                    ImageUrl = HimallIO.GetRomoteImagePath(HimallIO.GetProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_500)),
                    VideoUrl = string.IsNullOrWhiteSpace(product.VideoPath) ? "" : HimallIO.GetFilePath(product.VideoPath)
                };
            }
            var siteSettings = SiteSettingApplication.SiteSettings;
            // 通过接口获取微信openid
            var openIds = _WXMsgTemplateService.GetWeiXinOpenId(siteSettings.WeixinAppId, siteSettings.WeixinAppSecret, out string msg);
            if (openIds == null || openIds.Count < 2)
            {
                if (openIds != null)
                    msg = "微信公众号关注用户少于2个，无法生成链接";
                return new ProductArticleShare()
                {
                    SendSuccess = false,
                    ErrorMsg = msg,
                    ImageUrl = HimallIO.GetRomoteImagePath(HimallIO.GetProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_500)),
                    VideoUrl = string.IsNullOrWhiteSpace(product.VideoPath) ? "" : HimallIO.GetFilePath(product.VideoPath)
                };
            }
            var imgUrl = HimallIO.GetProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_500);
            if (!imgUrl.StartsWith("http://") && !imgUrl.StartsWith("https://"))
            {
                imgUrl = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + imgUrl.Replace("/", "\\");
            }
            else
            {
                imgUrl = HttpContext.Current.Request.MapPath(DownloadImage(imgUrl));
            }
            // 上传图片获取media_id
            var thumb_media_id = _WXMsgTemplateService.AddImage(imgUrl, siteSettings.WeixinAppId, siteSettings.WeixinAppSecret);
            // 添加图文消息
            WXMaterialInfo info = new WXMaterialInfo()
            {
                title = "🛒点击购买视频同款商品",
                author = "好物推荐👇",
                thumb_media_id = thumb_media_id,
                content = GetWXMaterialContent(product, siteSettings.WeixinAppletId, shopBranchId)
            };
            List<WXMaterialInfo> list = new List<WXMaterialInfo>() { info };
            var newsResult = _WXMsgTemplateService.Add(list, siteSettings.WeixinAppId, siteSettings.WeixinAppSecret);
            if (string.IsNullOrEmpty(newsResult.media_id))
                throw new HimallException(newsResult.errmsg);
            // 群发文章
            var takeOpenId = openIds.Take(2).ToArray();
            WXSendMessage message = new WXSendMessage()
            {
                Mpnews = new WXSendMessageMpnews() { Media_id = newsResult.media_id },
                Touser = takeOpenId
            };
            var errorMsg = "";
            var sendSuccess = true;
            try
            {
                var msg_id = _WXMsgTemplateService.SendMessage(siteSettings.WeixinAppId, siteSettings.WeixinAppSecret, message);
                // 根据群发ID保存商品ID用于回调修改
                Cache.Insert(msg_id, product.Id + "|" + shopBranchId);
                // 用来判断请求（前端会刷新接口）
                Cache.Insert(product.Id + "|" + shopBranchId, "load");
            }
            catch (Exception ex)
            {
                // 群发每天100次，超过之后会生成失败
                errorMsg = "生成失败请重试，错误：" + ex.Message;
                sendSuccess = false;
            }
            return new ProductArticleShare()
            {
                SendSuccess = sendSuccess,
                ErrorMsg = errorMsg,
                ImageUrl = HimallIO.GetRomoteImagePath(HimallIO.GetProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_500)),
                VideoUrl = string.IsNullOrWhiteSpace(product.VideoPath) ? "" : HimallIO.GetFilePath(product.VideoPath)
            };
        }
        /// <summary>
        /// 从网络下载图片
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string DownloadImage(string image)
        {
            var webClient = new WebClient();
            string shortName = image.Substring(image.LastIndexOf('/'));
            string ext = string.Empty;//获取文件扩展名
            if (shortName.LastIndexOf('.') <= 0)//如果扩展名不包含 '.'，那么说明该文件名不包含扩展名，因此扩展名赋空
                ext = ".jpg";
            else
                ext = shortName.Substring(shortName.LastIndexOf('.'));//否则取扩展名

            string localpath = "/temp/" + DateTime.Now.ToString("yyMMddHHmmssff") + ext;
            try
            {
                webClient.DownloadFile(image, HttpContext.Current.Request.MapPath(localpath));
               
            }
            catch (Exception ex)
            {
                Core.Log.Error(ex.Message);
            }
            return localpath;
        }
        /// <summary>
        /// 拼接文章Content
        /// </summary>
        private static string GetWXMaterialContent(ProductInfo product, string appletAppId, long superId)
        {
            var path = "pages/productdetail/productdetail?id=" + product.Id + "&distributorId=" + superId;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<div style='margin: 0 auto;background-color: #ffffff;padding: 12px;'><div style='display: flex;justify-content: space-between;'>");
            builder.AppendLine($"<div style='display: flex;'><div style='color: #eb4f50;font-size: 20px;'><em style='font-style: normal;font-size: 16px;'>￥</em>{product.MinSalePrice}</div>");
            builder.AppendLine($"<div style='border-radius: 4px;padding: 4px 6px;color: #eb4f50;background-color: #c5bab9;font-size: 12px;transform: scale(0.75);'>火爆抢购中</div></div>");
            builder.AppendLine($"</div>");
            builder.AppendLine($"<p style='margin: 12px 0;color: #a6a6a6;font-size: 16px;'>{product.ProductName}</p>");
            builder.AppendLine($"<p><a style='text-align: center;line-height: 44px;height: 44px;color: #fff;background-color: #eb4f50;display: block;border-radius: 5px;' data-miniprogram-appid=\"{appletAppId}\" data-miniprogram-path=\"{path}\">立即抢购</a></p>");
            return builder.ToString();
        }

        /// <summary>
        /// 获取视频号分享二维码
        /// </summary>
        public static string GetVideoNumberCode(long productId)
        {
            var sitesetting = SiteSettingApplication.SiteSettings;
            var data = "{\"path\":\"/subPages/videoNumber/videoNumber?productId=" + productId + "\",\"width\":600}";
            var fileName = string.Format(@"/Storage/VideoNumber/VideoNumber-Pro{0}-Shop.png", productId);
            _WXSmallProgramService.CreateAppletCode(fileName, data, sitesetting.WeixinAppletId, sitesetting.WeixinAppletSecret);
            return fileName;
        }

        /// <summary>
        /// 增加视频号URL
        /// </summary>
        public static void AddWeiXinArticleUrl(string values, string articleUrl)
        {
            List<long> list = values.Split('|').Select(p => Convert.ToInt64(p)).ToList();
            _WXMsgTemplateService.AddWeiXinArticleUrl(list[0], list[1], articleUrl);
        }
        #endregion

        public static ProductEnsure GetProductEnsure(long productId) =>
            CacheManager.GetProductEnsure(productId, () => BuilderProductEnsure(productId));

        private static ProductEnsure BuilderProductEnsure(long productId)
        {
            ProductEnsure ensure = new ProductEnsure()
            {
                IsCustomerSecurity = false,
                IsSevenDayNoReasonReturn = false,
                IsTimelyShip = false
            };
            var shopService = ServiceProvider.Instance<ShopService>.Create;
            var shopCategoryService = ServiceProvider.Instance<ShopCategoryService>.Create;
            var categoryService = ServiceProvider.Instance<CategoryService>.Create;
            var depositService = ServiceProvider.Instance<CashDepositsService>.Create;


            var product = Service.GetProductData(productId);
            if (product == null)
                return ensure;

            var shop = shopService.GetShop(product.ShopId);
            if (shop == null)
                return ensure;

            var cashDeposit = depositService.GetCashDeposit(shop.Id);
            var categories = shopCategoryService.GetBusinessCategory(shop.Id);
            var mainCategories = categories.Where(item => item.ParentCategoryId == 0).Select(item => item.Id).ToList();
            var cateCashDeposit = depositService.GetCategoryCashDeposits().Where(item => mainCategories.Contains(item.CategoryId)).ToList();
            if (cateCashDeposit.Count == 0)
                return ensure;

            var needCashDeposit = cateCashDeposit.Max(item => item.NeedPayCashDeposit);

            //平台自营，商家缴纳足够保证金或者平台未取消其资质资格
            if (shop.IsSelf || (cashDeposit != null && cashDeposit.CurrentBalance >= needCashDeposit) || (cashDeposit != null && cashDeposit.CurrentBalance < needCashDeposit && cashDeposit.EnableLabels == true))
            {
                List<long> categoryIds = new List<long>();
                categoryIds.Add(product.CategoryId);
                var mainCategory = categoryService.GetTopLevelCategories(categoryIds).FirstOrDefault();
                if (mainCategory != null)
                {
                    var categoryCashDepositInfo = depositService.GetCategoryCashDeposits().FirstOrDefault(item => item.CategoryId == mainCategory.Id);
                    if (categoryCashDepositInfo != null)
                        ensure.IsSevenDayNoReasonReturn = categoryCashDepositInfo.EnableNoReasonReturn;
                }
                ensure.IsCustomerSecurity = true;
                var template = FreightTemplateApplication.GetFreightTemplate(product.FreightTemplateId);
                //设置了运费模板
                if (template != null)
                {
                    if (!string.IsNullOrEmpty(template.SendTime))
                        ensure.IsTimelyShip = true;
                }
            }
            return ensure;
        }

        public static ProductTemplateData GetTemplate(long id)
        {
            if (id <= 0) return null;
            return _productDescriptionTemplateService.GetTemplateData(id);
        }

        /// <summary>
        /// 获取图片路径
        /// </summary>
        /// <param name="imagepath"></param>
        /// <param name="size"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public static string GetImagePath(string imagepath, ImageSize size, DateTime? updateTime, int index = 1, bool isApplet = false)
        {
            var image = Core.HimallIO.GetRomoteProductSizeImage(Core.HimallIO.GetImagePath(imagepath), index, (int)size);
            if (isApplet)
            {
                image = image.Replace("http://", "https://");
            }
            if (image.IndexOf("?") > -1)
            {
                return image + "&r=" + (updateTime.HasValue ? updateTime.Value.ToString("yyyyMMddHHmmss") : "");
            }
            else
            {
                return image + "?r=" + (updateTime.HasValue ? updateTime.Value.ToString("yyyyMMddHHmmss") : "");
            }

        }

        /// <summary>
        /// 获取图片路径
        /// </summary>
        /// <param name="imagepath"></param>
        /// <param name="size"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public static string GetVideoPath(string videoPath, DateTime? updateTime, bool isApplet = false)
        {
            var video = string.IsNullOrWhiteSpace(videoPath) ? string.Empty : (Himall.Core.HimallIO.GetRomoteImagePath(videoPath));
            if (string.IsNullOrEmpty(video))
            {
                return video;
            }
            if (isApplet)
            {
                video = video.Replace("http://", "https://");
            }
            if (video.IndexOf("?") > -1)
            {
                return video + "&r=" + (updateTime.HasValue ? updateTime.Value.ToString("yyyyMMddHHmmss") : "");
            }
            else
            {
                return video + "?r=" + (updateTime.HasValue ? updateTime.Value.ToString("yyyyMMddHHmmss") : ""); ;
            }

        }
    }
}
