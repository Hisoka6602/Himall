using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.Product;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.SmallProgAPI.Model;
using Himall.SmallProgAPI.Model.ParamsModel;
using Himall.Web.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Http.Results;
using static Himall.Entities.CustomerServiceInfo;
using System.Web.Http;
using System.Diagnostics;
using System.Web;
using Himall.DTO.CacheData;

namespace Himall.SmallProgAPI
{
    public class ProductController : SmallProgAPIController
    {
        protected const string DISTRIBUTION_SPREAD_ID_PARAMETER_NAME = "SpreadId";
        /// <summary>
        /// 搜索商品
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetProducts(
             string keyword = "", /* 搜索关键字 */
            long cid = 0,  /* 分类ID */
            long bid = 0, /* 品牌ID */
            string openId = "",
            //string a_id = "",  /* 属性ID, 表现形式：attrId_attrValueId */
            string sortBy = "", /* 排序项（1：默认，2：销量，3：价格，4：评论数，5：上架时间） */
            string sortOrder = "", /* 排序方式（1：升序，2：降序） */
            int pageIndex = 1, /*页码*/
            int pageSize = 10,/*每页显示数据量*/
            long vshopId = 0,
            long sid = 0,/*商家ID*/
            long shopBranchId = 0/*门店*/
            )
        {
            #region 初始化查询Model
            SearchProductQuery model = new SearchProductQuery();
            model.VShopId = vshopId;
            model.ShopId = sid;
            model.BrandId = bid;
            if (vshopId == 0 && cid != 0)
            {
                var catelist = ServiceProvider.Instance<CategoryService>.Create.GetCategories();
                var cate = catelist.FirstOrDefault(r => r.Id == cid);
                if (cate != null)
                {
                    if (cate.Depth == 1)
                        model.FirstCateId = cid;
                    else if (cate.Depth == 2)
                        model.SecondCateId = cid;
                    else if (cate.Depth == 3)
                        model.ThirdCateId = cid;
                }
            }
            else if (vshopId != 0 && cid != 0)
            {
                VShopInfo vShopInfo = VshopApplication.GetVShop(vshopId);
                if (vShopInfo != null)
                {
                    model.ShopId = vShopInfo.ShopId;
                }
                model.ShopCategoryId = cid;
            }

            model.Keyword = keyword;
            if (sortBy == "SalePrice")
            {
                model.OrderKey = 3;//默认
            }
            else if (sortBy == "SaleCounts")
            {
                model.OrderKey = 2;
            }
            else if (sortBy == "VistiCounts")
            {
                model.OrderKey = 4;
            }
            else
            {
                model.OrderKey = 1;
            }

            if (sortOrder == "desc")
            {
                model.OrderType = false;
            }
            else
            {
                model.OrderType = true;
            }
            model.FilterNoStockProduct = true;

            model.PageNumber = pageIndex;
            model.PageSize = pageSize;
            model.ShopBranchId = shopBranchId;
            #endregion
            SearchProductResult result = ServiceProvider.Instance<SearchProductService>.Create.SearchProduct(model);
            int total = result.Total;
            //当查询的结果少于一页时用like进行补偿（与PC端同步）
            if (result.Total < pageSize)
            {
                model.IsLikeSearch = true;
                SearchProductResult result2 = ServiceProvider.Instance<SearchProductService>.Create.SearchProduct(model);
                var idList1 = result.Data.Select(a => a.ProductId).ToList();
                var nresult = result2.Data.Where(a => !idList1.Contains(a.ProductId)).ToList();

                if (nresult.Count > 0)
                {
                    result.Total += nresult.Count;
                    result.Data.AddRange(nresult);
                }
                //补充数据后，重新排序
                Func<IEnumerable<ProductView>, IOrderedEnumerable<ProductView>> orderby = null;
                Func<IEnumerable<ProductView>, IOrderedEnumerable<ProductView>> orderByDesc = null;
                switch (model.OrderKey)
                {
                    case 2:
                        orderby = e => e.OrderBy(p => p.SaleCount);
                        orderByDesc = e => e.OrderByDescending(p => p.SaleCount);
                        break;
                    case 3:
                        orderby = e => e.OrderBy(p => p.SalePrice);
                        orderByDesc = e => e.OrderByDescending(p => p.SalePrice);
                        break;
                    case 4:

                        orderby = e => e.OrderBy(p => p.Comments);
                        orderByDesc = e => e.OrderByDescending(p => p.Comments);
                        break;
                    default:

                        //按最新的排序规则作为默认排序【序号越大，在前台展示的商品越靠前，序号一致时，优先销量排前，销量一致时，优先上架时间排前】
                        orderByDesc = e => e.OrderByDescending(p => p.DisplaySequence).ThenByDescending(p => p.SaleCount).ThenByDescending(p => p.ProductId);
                        break;
                }
                if (model.OrderKey > 1)
                {
                    if (model.OrderType)
                    {
                        result.Data = orderby(result.Data).ToList();
                    }
                    else
                    {
                        result.Data = orderByDesc(result.Data).ToList();
                    }
                }
                else
                {
                    result.Data = orderByDesc(result.Data).ToList();
                }
            }

            total = result.Total;



            //补商品状态
            foreach (var item in result.Data)
            {
                var ser = ServiceProvider.Instance<ProductService>.Create;
                var _pro = ser.GetProduct(item.ProductId);
                var _skus = ser.GetSKUs(item.ProductId);
                if (_pro.SaleStatus == Entities.ProductInfo.ProductSaleStatus.OnSale && _pro.AuditStatus == Entities.ProductInfo.ProductAuditStatus.Audited)
                {
                    item.ShowStatus = 0;
                    if (_skus.Sum(d => d.Stock) < 1)
                    {
                        item.ShowStatus = 2;
                    }
                }
                else
                {
                    if (_pro.SaleStatus != Entities.ProductInfo.ProductSaleStatus.OnSale || _pro.AuditStatus == Entities.ProductInfo.ProductAuditStatus.InfractionSaleOff)
                    {
                        item.ShowStatus = 3;
                    }
                    else
                    {
                        item.ShowStatus = 1;
                    }
                }
            }
            #region 价格更新
            //会员折扣
            decimal discount = 1M;
            long SelfShopId = 0;
            long currentUserId = 0;
            if (CurrentUser != null)
            {
                discount = CurrentUser.MemberDiscount;
                var shopInfo = ShopApplication.GetSelfShop();
                SelfShopId = shopInfo.Id;
                currentUserId = CurrentUser.Id;
            }
            //填充商品和购物车数据
            var ids = result.Data.Select(d => d.ProductId).ToArray();
            var liveproducts = LiveApplication.GetLiveingProductByIds(result.Data.Select(d => d.ProductId));//获取正在直播的商品
            List<Product> products = ProductManagerApplication.GetProductsByIds(ids);
            List<SKU> skus = ProductManagerApplication.GetSKUByProducts(ids);
            List<ShoppingCartItem> cartitems = CartApplication.GetCartQuantityByIds(currentUserId, ids);
            List<dynamic> productList = new List<dynamic>();
            var productSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1;
            foreach (var item in result.Data)
            {
                bool islive = false;
                if (liveproducts.Where(p => p.ProductId == item.ProductId).FirstOrDefault() != null)
                {
                    islive = true;
                }
                Product proInfo = products.Where(d => d.Id == item.ProductId).FirstOrDefault();
                if (proInfo == null)
                {
                    continue;
                }
                SKU skuInfo = skus.Where(d => d.ProductId == item.ProductId).FirstOrDefault();
                bool hasSku = proInfo.HasSKU;
                decimal marketPrice = Core.Helper.TypeHelper.ObjectToDecimal(proInfo.MarketPrice);
                string skuId = skuInfo.Id;
                int quantity = 0;
                quantity = cartitems.Where(d => d.ProductId == item.ProductId).Sum(d => d.Quantity);
                item.ImagePath = ProductManagerApplication.GetImagePath(item.ImagePath, ImageSize.Size_350, (proInfo == null ? DateTime.MinValue : proInfo.UpdateTime), 1, true);

                var pro = new
                {
                    IsLive = islive,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Pic = item.ImagePath,
                    MarketPrice = marketPrice.ToString("0.##"),//市场价
                    SalePrice = item.SalePrice.ToString("0.##"),//当前价
                    SaleCounts = item.SaleCount + Himall.Core.Helper.TypeHelper.ObjectToInt(item.VirtualSaleCounts),
                    ProductSaleCountOnOff = productSaleCountOnOff,
                    CartQuantity = quantity,// item.cartquantity,
                    HasSKU = hasSku,//是否有规格
                    Stock = skus.Where(d => d.ProductId == item.ProductId).Sum(p => p.Stock),
                    SkuId = skuId,
                    ShowStatus = item.ShowStatus,
                    ActiveId = item.ActivityId,//活动Id
                    ActiveType = item.ActiveType,//活动类型（1代表限购，2代表团购，3代表商品预售，4代表限购预售，5代表团购预售）
                    IsVirtual = item.ProductType == 1
                };
                productList.Add(pro);
            }
            #endregion
            var json = JsonResult<dynamic>(data: productList);
            return json;
        }


        /// <summary>
        /// 门店搜索商品
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="shopBranchId"></param>
        /// <param name="keyWords"></param>
        /// <param name="productId"></param>
        /// <param name="shopCategoryId"></param>
        /// <param name="categoryId"></param>
        /// <param name="type"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>


        [HttpPost]
        public JsonResult<Result<dynamic>> ShopProductList(ShopBranchProductQuery query)
        {
            CheckOpenStore();
            if (query.ShopId <= 0)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "商家ID不允许为空");
            }
            if (query.ShopBranchId <= 0)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "门店编号不允许为空");
            }
            query.OrderKey = 5;
            var _iBranchCartService = ObjectContainer.Current.Resolve<BranchCartService>();
            var _LimitTimeBuyService = ObjectContainer.Current.Resolve<LimitTimeBuyService>();

            var dtNow = DateTime.Now;
            query.SaleStartDate = DateTime.Parse(dtNow.ToString("yyyy-MM-01 00:00:00"));//当月的
            query.SaleEndDate = dtNow;
            query.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
            var pageModel = ShopBranchApplication.GetShopBranchProductsMonth(query, (DateTime)query.SaleStartDate, dtNow);
            Himall.Entities.ShoppingCartInfo cartInfo = new Himall.Entities.ShoppingCartInfo();
            if (CurrentUser != null)
            {
                cartInfo = new BranchCartHelper().GetCart(CurrentUser.Id, query.ShopBranchId.Value);//获取购物车数据
            }
            #region 置顶商品
            if (query.RproductId.HasValue && query.RproductId.Value > 0 && query.PageNo == 1)
            {
                query.ProductId = query.RproductId.Value;
                query.RproductId = null;
                query.ShopCategoryId = null;
                query.CategoryId = null;
                query.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
                var topModel = ShopBranchApplication.GetShopBranchProductsMonth(query, (DateTime)query.SaleStartDate, dtNow);
                if (topModel.Models.Count() > 0)
                {
                    pageModel.Models.Insert(0, topModel.Models.FirstOrDefault());
                }
            }
            #endregion

            //获取门店活动
            var shopBranchs = ShopBranchApplication.GetShopBranchById(query.ShopBranchId.Value);

            if (pageModel.Models != null && pageModel.Models.Count > 0)
            {
                #region 处理商品 官方自营店会员折扣价。
                if (CurrentUser != null)
                {
                    var shopInfo = ShopApplication.GetShop(query.ShopId.Value);
                    if (shopInfo != null && shopInfo.IsSelf)//当前商家是否是官方自营店
                    {
                        decimal discount = 1M;
                        discount = CurrentUser.MemberDiscount;
                        foreach (var item in pageModel.Models)
                        {
                            item.MinSalePrice = Math.Round(item.MinSalePrice * discount, 2);
                        }
                    }
                }
                foreach (var item in pageModel.Models)
                {
                    item.Quantity = cartInfo != null ? cartInfo.Items.Where(d => d.ProductId == item.Id && d.ShopBranchId == query.ShopBranchId.Value).Sum(d => d.Quantity) : 0;
                }
                #endregion
            }

            var product = pageModel.Models.ToList().Select(item =>
            {
                var comcount = CommentApplication.GetProductCommentStatistic(productId: item.Id, shopBranchId: query.ShopBranchId.Value);
                var sbskus = ShopBranchApplication.GetSkusByProductId(query.ShopBranchId.Value, item.Id);
                return new
                {
                    Id = item.Id,
                    ProductName = item.ProductName,
                    CategoryName = ShopCategoryApplication.GetCategoryByProductId(item.Id).Name,
                    MeasureUnit = item.MeasureUnit,
                    MinSalePrice = item.MinSalePrice.ToString("f2"),
                    SaleCounts = item.ShopBranchSaleCounts,//销量统计没有考虑订单支付完成。
                    isSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1,
                    MarketPrice = item.MarketPrice,
                    HasSku = item.HasSKU,
                    Quantity = item.Quantity,
                    IsTop = item.Id == (query.RproductId.HasValue ? query.RproductId.Value : 0),
                    DefaultImage = HimallIO.GetRomoteProductSizeImage(item.RelativePath, 1, (int)ImageSize.Size_350).Replace("http://", "https://"),
                    HighCommentCount = comcount.HighComment,
                    Stock = sbskus.Sum(d => d.Stock),
                    IsVirtual = item.ProductType == 1
                };
            }).OrderByDescending(d => d.IsTop).ToList();


            return JsonResult<dynamic>(new
            {
                Products = product,
                Total = pageModel.Total
            });
        }
        //public JsonResult<Result<dynamic>> GetCombinationSku(string openId, long productId, long attributeId, long valueId, long combinationId)
        //{

        //    StringBuilder strData = new StringBuilder();
        //    DataTable dt = CollocationApplication.get
        //    List<CombinationSKUInfo> datalist = new List<CombinationSKUInfo>();
        //    if (dt != null && dt.Rows.Count > 0)
        //    {
        //        for (int i = 0; i < dt.Rows.Count; i++)
        //        {
        //            CombinationSKUInfo tempInfo = new CombinationSKUInfo()
        //            {
        //                SkuId = dt.Rows[i]["SkuId"].ToNullString(),
        //                AttributeId = dt.Rows[i]["AttributeId"].ToInt(),
        //                ValueId = dt.Rows[i]["ValueId"].ToInt(),
        //                CombinationPrice = dt.Rows[i]["CombinationPrice"].ToDecimal().ToString("f2").ToDecimal(),
        //                SalePrice = dt.Rows[i]["SalePrice"].ToDecimal(),
        //                Stock = dt.Rows[i]["Stock"].ToInt(),
        //            };
        //            datalist.Add(tempInfo);
        //        }
        //    }

        //}
        /// <summary>
        /// 获取组合购详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetCollocationProductDetail(string openId, long collId, long shopBranchId = 0)
        {
            //CheckUserLogin();
            if (shopBranchId > 0)
            {
                CheckOpenStore();
            }
            var collocationService = ObjectContainer.Current.Resolve<CollocationService>();
            //productService.GetSKUs()

            CollocationInfo info = collocationService.GetCollocation(collId);
            if (info == null)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "活动不存在或者已结束");
            }

            if (DateTime.Now.Date > info.EndTime || DateTime.Now.Date < info.StartTime)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "活动未开始或者已结束");
            }
            List<long> collIds = new List<long>();
            collIds.Add(collId);
            List<CollocationPoruductInfo> collProducts = collocationService.GetProducts(collIds);
            List<long> allProductIds = collProducts.Select(cp => cp.Id).ToList();
            ProductInfo mainProductInfo = null;
            List<ProductInfo> products = new List<ProductInfo>();
            List<CollocationSkuInfo> collocationSkus = new List<CollocationSkuInfo>();
            collocationSkus = collocationService.GetSKUs(allProductIds);
            if (collProducts != null && collProducts.Count > 1)
            {
                long mainProductId = collProducts.Where(p => p.IsMain == true).Select(p => p.ProductId).ToList()[0];
                mainProductInfo = ProductManagerApplication.GetProduct(mainProductId);
                if (mainProductInfo == null)
                {
                    throw new HimallApiException(ApiErrorCode.Parameter_Error, "活动未开始或者已结束");
                }
                allProductIds = collProducts.Where(p => p.IsMain != true).Select(p => p.ProductId).ToList();
                //List<SKU> allSkus = ProductManagerApplication.GetSKUByProducts(allProductIds);)
                products = ProductManagerApplication.GetProductByIds(allProductIds);
                mainProductInfo.ImagePathUrl = ProductManagerApplication.GetImagePath(mainProductInfo.ImagePath, ImageSize.Size_350, (mainProductInfo == null ? DateTime.MinValue : mainProductInfo.UpdateTime), 1, true);
            }
            else
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "活动未开始或者已结束");
            }




            foreach (ProductInfo product in products)
            {
                product.ImagePathUrl = ProductManagerApplication.GetImagePath(product.ImagePath, ImageSize.Size_350, (product == null ? DateTime.MinValue : product.UpdateTime), 1, true);
            }
            List<object> SKUs = new List<object>();
            return JsonResult<dynamic>(new
            {
                baseInfo = info,
                MainProductInfo = new
                {
                    ColloPid = collProducts.Where(c => c.ProductId == mainProductInfo.Id).FirstOrDefault().Id,
                    ProductId = mainProductInfo.Id,
                    mainProductInfo.Address,
                    mainProductInfo.BottomId,
                    mainProductInfo.BrandId,
                    mainProductInfo.BrandName,
                    mainProductInfo.CategoryId,
                    mainProductInfo.CategoryNames,
                    mainProductInfo.CategoryPath,
                    mainProductInfo.ColorAlias,
                    mainProductInfo.ConcernedCount,
                    mainProductInfo.DisplaySequence,
                    mainProductInfo.ProductName,
                    mainProductInfo.ProductCode,
                    mainProductInfo.ProductType,
                    mainProductInfo.ShortDescription,
                    mainProductInfo.ShopName,
                    mainProductInfo.ShopBranchSaleCounts,
                    mainProductInfo.ShopDisplaySequence,
                    mainProductInfo.Quantity,
                    mainProductInfo.RelativePath,
                    mainProductInfo.SupportRefundType,
                    mainProductInfo.StartDate,
                    mainProductInfo.ShowProductState,
                    mainProductInfo.SizeAlias,
                    mainProductInfo.UseNotice,
                    mainProductInfo.ValidityType,
                    mainProductInfo.VersionAlias,
                    VideoPath = GetVideoPath(mainProductInfo.VideoPath, mainProductInfo.UpdateTime),
                    mainProductInfo.VirtualProductItemInfo,
                    mainProductInfo.VirtualSaleCounts,
                    mainProductInfo.VistiCounts,
                    mainProductInfo.Volume,
                    mainProductInfo.Weight,
                    MinSalePrice = collocationSkus.Where(s => s.ProductId == mainProductInfo.Id).Min(s => s.Price),
                    MaxSalePrice = collocationSkus.Where(s => s.ProductId == mainProductInfo.Id).Max(s => s.Price),
                    MinOldSalePrice = collocationSkus.Where(s => s.ProductId == mainProductInfo.Id).Min(s => s.SkuPirce),
                    MaxOldSalePrice = collocationSkus.Where(s => s.ProductId == mainProductInfo.Id).Max(s => s.SkuPirce),
                    ImagePath = mainProductInfo.ImagePathUrl,

                    mainProductInfo.MaxBuyCount,
                    SkuItemList = GetProductSkuItems(mainProductInfo, collocationSkus, shopBranchId, out SKUs),
                    SKUs = SKUs
                },
                CombinationProducts = products.Select(p => new
                {
                    ColloPid = collProducts.Where(c => c.ProductId == p.Id).FirstOrDefault().Id,
                    ProductId = p.Id,
                    p.Address,

                    p.BottomId,
                    p.BrandId,
                    p.BrandName,
                    p.CategoryId,
                    p.CategoryNames,
                    p.CategoryPath,
                    p.ColorAlias,
                    p.ConcernedCount,
                    p.DisplaySequence,
                    p.ProductName,
                    p.ProductCode,
                    p.ProductType,
                    p.ShortDescription,
                    p.ShopName,
                    p.ShopBranchSaleCounts,
                    p.ShopDisplaySequence,
                    p.Quantity,
                    p.RelativePath,
                    p.SupportRefundType,
                    p.StartDate,
                    p.ShowProductState,
                    p.SizeAlias,
                    p.UseNotice,
                    p.ValidityType,
                    p.VersionAlias,
                    VideoPath = GetVideoPath(p.VideoPath, p.UpdateTime),
                    ImagePath = p.ImagePathUrl,
                    p.VirtualProductItemInfo,
                    p.VirtualSaleCounts,
                    p.VistiCounts,
                    p.Volume,
                    p.Weight,
                    MinSalePrice = collocationSkus.Where(s => s.ProductId == p.Id).Min(s => s.Price),
                    MaxSalePrice = collocationSkus.Where(s => s.ProductId == p.Id).Max(s => s.Price),
                    MinOldSalePrice = collocationSkus.Where(s => s.ProductId == p.Id).Min(s => s.SkuPirce),
                    MaxOldSalePrice = collocationSkus.Where(s => s.ProductId == p.Id).Max(s => s.SkuPirce),
                    SkuItemList = GetProductSkuItems(p, collocationSkus, shopBranchId, out SKUs),
                    p.MaxBuyCount,
                    SKUs = SKUs
                }),
            });
        }

        /// <summary>
        /// 获取商品规格信息
        /// </summary>
        /// <param name="productInfo"></param>
        /// <param name="shopBranchId"></param>
        /// <param name="branchskuList"></param>
        /// <returns></returns>
        private List<object> GetProductSkuItems(ProductInfo productInfo, List<CollocationSkuInfo> collocationSkus, long shopBranchId, out List<object> SKUs)
        {
            SKUs = new List<object>();
            List<ShopBranchSkuInfo> branchskuList = null;
            if (shopBranchId > 0)
            {
                var shopBranch = ShopBranchApplication.GetShopBranchById(shopBranchId);
                if (shopBranch == null)
                {
                    throw new HimallApiException(ApiErrorCode.Parameter_Error, "shopBranchId");
                }
                if (shopBranch.Status == ShopBranchStatus.Freeze)
                {
                    throw new HimallApiException(ApiErrorCode.Parameter_Error, "门店已冻结");
                }
                if (!ShopBranchApplication.CheckProductIsExist(shopBranchId, productInfo.Id))
                {
                    throw new HimallApiException(ApiErrorCode.Parameter_Error, "该商品已被删除或者转移");
                }

                var comment = CommentApplication.GetProductCommentStatistic(productId: productInfo.Id,
                        shopBranchId: shopBranchId);

                branchskuList = ShopBranchApplication.GetSkus(shopBranch.ShopId, shopBranch.Id, null);
                if (branchskuList == null || branchskuList.Count <= 0)
                {
                    throw new Himall.Core.HimallException("门店商品不存在");
                }
            }
            List<object> SkuItemList = new List<object>();

            var skus = ProductManagerApplication.GetSKUs(productInfo.Id);
            Entities.TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetType(productInfo.TypeId);
            string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
            string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
            string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
            if (skus.Count() > 0)
            {

                #region 颜色
                long colorId = 0, sizeId = 0, versionId = 0;
                List<ProductSKUItem> colorAttributeValue = new List<ProductSKUItem>();
                List<string> listcolor = new List<string>();
                foreach (var sku in skus)
                {

                    var specs = sku.Id.Split('_');
                    if (shopBranchId > 0 && !branchskuList.Any(x => x.SkuId == sku.Id))
                    {
                        continue;
                    }
                    if (specs.Count() > 0 && !string.IsNullOrEmpty(sku.Color))
                    {
                        if (long.TryParse(specs[1], out colorId)) { }//相同颜色规格累加对应值
                        if (colorId != 0)
                        {
                            if (!listcolor.Contains(sku.Color))
                            {
                                var c = skus.Where(s => s.Color.Equals(sku.Color)).Sum(s => s.Stock);
                                var colorvalue = new ProductSKUItem
                                {
                                    ValueId = colorId,
                                    UseAttributeImage = false,
                                    Value = sku.Color,
                                    ImageUrl = Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listcolor.Add(sku.Color);
                                colorAttributeValue.Add(colorvalue);
                            }
                        }
                    }
                }
                var color = new
                {
                    AttributeName = !string.IsNullOrWhiteSpace(productInfo.ColorAlias) ? productInfo.ColorAlias : colorAlias,
                    AttributeId = productInfo.TypeId,
                    AttributeValue = colorAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 0,
                };
                if (colorId > 0)
                {
                    SkuItemList.Add(color);
                }
                #endregion

                #region 容量
                List<ProductSKUItem> sizeAttributeValue = new List<ProductSKUItem>();
                List<string> listsize = new List<string>();
                foreach (var sku in skus.OrderBy(a => a.Size))
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 1)
                    {
                        if (long.TryParse(specs[2], out sizeId)) { }
                        if (sizeId != 0)
                        {
                            if (!listsize.Contains(sku.Size))
                            {
                                var ss = skus.Where(s => s.Size.Equals(sku.Size)).Sum(s1 => s1.Stock);
                                var sizeValue = new ProductSKUItem
                                {
                                    ValueId = sizeId,
                                    UseAttributeImage = false,
                                    Value = sku.Size,
                                    ImageUrl = ""// Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listsize.Add(sku.Size);
                                sizeAttributeValue.Add(sizeValue);
                            }
                        }
                    }
                }
                var size = new
                {
                    AttributeName = !string.IsNullOrWhiteSpace(productInfo.SizeAlias) ? productInfo.SizeAlias : sizeAlias,
                    AttributeId = productInfo.TypeId,
                    AttributeValue = sizeAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 1,
                };
                if (sizeId > 0)
                {
                    SkuItemList.Add(size);
                }
                #endregion

                #region 规格
                List<ProductSKUItem> versionAttributeValue = new List<ProductSKUItem>();
                List<string> listversion = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 2)
                    {
                        if (long.TryParse(specs[3], out versionId)) { }
                        if (versionId != 0)
                        {
                            if (!listversion.Contains(sku.Version))
                            {
                                var v = skus.Where(s => s.Version.Equals(sku.Version));
                                var versionValue = new ProductSKUItem
                                {
                                    ValueId = versionId,
                                    UseAttributeImage = false,
                                    Value = sku.Version,
                                    ImageUrl = ""// Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listversion.Add(sku.Version);
                                versionAttributeValue.Add(versionValue);
                            }
                        }
                    }
                }
                var version = new
                {
                    AttributeName = !string.IsNullOrWhiteSpace(productInfo.VersionAlias) ? productInfo.VersionAlias : versionAlias,
                    AttributeId = productInfo.TypeId,
                    AttributeValue = versionAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 2,
                };
                if (versionId > 0)
                {
                    SkuItemList.Add(version);
                }
                #endregion

            }
            #region Sku值
            foreach (var sku in skus)
            {
                decimal salePrice = 0;
                if (collocationSkus != null)
                {
                    CollocationSkuInfo collSkuInfo = collocationSkus.Where(s => s.SkuID == sku.Id).FirstOrDefault();
                    if (collSkuInfo != null)
                    {
                        salePrice = collSkuInfo.Price;
                    }
                }
                ShopBranchSkuInfo shopSku = null;
                if (shopBranchId > 0)
                {
                    shopSku = branchskuList.Where(x => x.SkuId == sku.Id).FirstOrDefault();
                    if (shopSku != null) sku.Stock = shopSku.Stock;
                }
                var prosku = new
                {
                    SkuItems = "",
                    MemberPrices = "",
                    SkuId = sku.Id,
                    ProductId = productInfo.Id,
                    SKU = sku.Sku,
                    Weight = 0,
                    Stock = shopSku != null ? shopSku.Stock : sku.Stock,
                    WarningStock = sku.SafeStock,
                    CostPrice = sku.CostPrice,
                    SalePrice = salePrice > 0 ? salePrice : sku.SalePrice,
                    OldSalePrice = sku.SalePrice,
                    StoreStock = 0,
                    StoreSalePrice = 0,
                    ImageUrl = "",
                    ThumbnailUrl40 = "",
                    ThumbnailUrl410 = "",
                    MaxStock = 0,
                    FreezeStock = 0,
                };
                SKUs.Add(prosku);
            }
            #endregion
            return SkuItemList;
        }
        /// <summary>
        /// 获取商品详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetProductDetail(long productId, long shopBranchId = 0)
        {
            if (shopBranchId > 0)
                CheckOpenStore();

            ProductDetailModelForMobie model = new ProductDetailModelForMobie()
            {
                Product = new ProductInfoModel(),
                Shop = new ShopInfoModel(),
                Color = new CollectionSKU(),
                Size = new CollectionSKU(),
                Version = new CollectionSKU()
            };

            var product = ProductManagerApplication.GetProductData(productId);
            if (product == null || product.IsDeleted)
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "该商品已被删除或者转移");
            var stocks = ProductManagerApplication.GetStocks(productId);
            List<ShopBranchSkuInfo> branchskuList = null;
            if (shopBranchId > 0)
            {
                var shopBranch = ShopBranchApplication.GetShopBranchById(shopBranchId);
                if (shopBranch == null)
                {
                    throw new HimallApiException(ApiErrorCode.Parameter_Error, "shopBranchId");
                }
                if (shopBranch.Status == ShopBranchStatus.Freeze)
                {
                    throw new HimallApiException(ApiErrorCode.Parameter_Error, "门店已冻结");
                }
                if (!ShopBranchApplication.CheckProductIsExist(shopBranchId, productId))
                {
                    throw new HimallApiException(ApiErrorCode.Parameter_Error, "该商品已被删除或者转移");
                }

                var comment = CommentApplication.GetProductCommentStatistic(productId: productId,
                        shopBranchId: shopBranchId);

                branchskuList = ShopBranchApplication.GetSkus(shopBranch.ShopId, shopBranch.Id, null);
                if (branchskuList == null || branchskuList.Count <= 0)
                {
                    throw new Himall.Core.HimallException("门店商品不存在");
                }
            }

            //提供服务（消费者保障、七天无理由、及时发货）;
            model.CashDepositsServer = ProductManagerApplication.GetProductEnsure(product.Id);

            if (product.ProductType == 0)
            {
                var template = FreightTemplateApplication.GetFreightTemplate(product.FreightTemplateId);
                if (template != null)
                {
                    model.FreightTemplate = template;
                    var fullName = RegionApplication.GetFullName(template.SourceAddress);
                    if (fullName != null)
                    {
                        var ass = fullName.Split(' ');
                        if (ass.Length >= 2)
                            model.ProductAddress = ass[0] + " " + ass[1];
                        else
                            model.ProductAddress = ass[0];
                    }
                }
            }
            var commentSummary = CommentApplication.GetSummary(productId, shopBranchId);

            #region 店铺
            var shop = ShopApplication.GetShop(product.ShopId);

            var vshopinfo = ServiceProvider.Instance<VShopService>.Create.GetVshopDataByShopId(shop.Id);
            if (vshopinfo != null)
            {
                model.VShopLog = vshopinfo.WXLogo;
                model.Shop.VShopId = vshopinfo.Id;
            }
            else
            {
                model.Shop.VShopId = -1;
                model.VShopLog = string.Empty;
            }

            var mark = ShopApplication.GetMarks(shop.Id);

            model.Shop.PackMark = mark.PackMark;
            model.Shop.ServiceMark = mark.ServiceMark;
            model.Shop.ComprehensiveMark = mark.ComprehensiveMark;

            model.Shop.Name = shop.ShopName;
            model.Shop.ProductMark = commentSummary.Average;
            model.Shop.Id = product.ShopId;
            model.Shop.FreeFreight = shop.FreeFreight;
            model.Shop.ProductNum = ServiceProvider.Instance<ProductService>.Create.GetOnSaleCountData(product.ShopId);

            var statistic = ShopApplication.GetStatisticOrderComment(product.ShopId);
            //宝贝与描述
            model.Shop.ProductAndDescription = statistic.ProductAndDescription;
            model.Shop.SellerServiceAttitude = statistic.SellerServiceAttitude;
            model.Shop.SellerDeliverySpeed = statistic.SellerDeliverySpeed;
            #endregion

            #region 商品SKU



            List<object> SkuItemList = new List<object>();
            List<object> Skus = new List<object>();


            long[] Ids = { productId };
            long userId = 0;
            decimal discount = 1M;
            if (CurrentUser != null)
            {
                userId = CurrentUser.Id;
                if (shop.IsSelf)
                {
                    discount = CurrentUser.MemberDiscount;
                }
            }

            var cartcount = 0;
            ShoppingCartInfo cart = null;
            List<ShoppingCartItem> cartItems = null;
            if (CurrentUser != null)
            {
                cart = CartApplication.GetShopBranchCart(CurrentUser.Id, shopBranchId);
                cartItems = cart.Items.Where(d => d.ProductId == productId).ToList();
                if (branchskuList != null)
                {
                    foreach (var cartitem in cartItems)
                    {
                        var branchskuInfo = branchskuList.FirstOrDefault(x => x.SkuId == cartitem.SkuId);
                        if (branchskuInfo.Status == ShopBranchSkuStatus.Normal && branchskuInfo.Stock >= cartitem.Quantity)
                        {
                            cartcount += cartitem.Quantity;
                        }
                    }
                }
            }

            var skus = product.Skus.Map<List<SKU>>();
            if (skus.Count() > 0)
            {
                #region 颜色
                long colorId = 0, sizeId = 0, versionId = 0;
                List<ProductSKUItem> colorAttributeValue = new List<ProductSKUItem>();
                List<string> listcolor = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (shopBranchId > 0 && !branchskuList.Any(x => x.SkuId == sku.Id))
                    {
                        continue;
                    }
                    if (specs.Count() > 0 && !string.IsNullOrEmpty(sku.Color))
                    {
                        if (long.TryParse(specs[1], out colorId)) { }//相同颜色规格累加对应值
                        if (colorId != 0)
                        {
                            if (!listcolor.Contains(sku.Color))
                            {
                                var c = skus.Where(s => s.Color.Equals(sku.Color)).Sum(s => s.Stock);
                                var colorvalue = new ProductSKUItem
                                {
                                    ValueId = colorId,
                                    UseAttributeImage = false,
                                    Value = sku.Color,
                                    ImageUrl = Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listcolor.Add(sku.Color);
                                colorAttributeValue.Add(colorvalue);
                            }
                        }
                    }
                }
                var color = new
                {
                    AttributeName = product.ColorAlias,
                    AttributeId = product.TypeId,
                    AttributeValue = colorAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 0,
                };
                if (colorId > 0)
                {
                    SkuItemList.Add(color);
                }
                #endregion

                #region 容量
                List<ProductSKUItem> sizeAttributeValue = new List<ProductSKUItem>();
                List<string> listsize = new List<string>();
                foreach (var sku in skus.OrderBy(a => a.Size))
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 1)
                    {
                        if (long.TryParse(specs[2], out sizeId)) { }
                        if (sizeId != 0)
                        {
                            if (!listsize.Contains(sku.Size))
                            {
                                var ss = skus.Where(s => s.Size.Equals(sku.Size)).Sum(s1 => s1.Stock);
                                var sizeValue = new ProductSKUItem
                                {
                                    ValueId = sizeId,
                                    UseAttributeImage = false,
                                    Value = sku.Size,
                                    ImageUrl = ""
                                };
                                listsize.Add(sku.Size);
                                sizeAttributeValue.Add(sizeValue);
                            }
                        }
                    }
                }
                var size = new
                {
                    AttributeName = product.SizeAlias,
                    AttributeId = product.TypeId,
                    AttributeValue = sizeAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 1,
                };
                if (sizeId > 0)
                {
                    SkuItemList.Add(size);
                }
                #endregion

                #region 规格
                List<ProductSKUItem> versionAttributeValue = new List<ProductSKUItem>();
                List<string> listversion = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 2)
                    {
                        if (long.TryParse(specs[3], out versionId)) { }
                        if (versionId != 0)
                        {
                            if (!listversion.Contains(sku.Version))
                            {
                                var v = skus.Where(s => s.Version.Equals(sku.Version));
                                var versionValue = new ProductSKUItem
                                {
                                    ValueId = versionId,
                                    UseAttributeImage = false,
                                    Value = sku.Version,
                                    ImageUrl = ""
                                };
                                listversion.Add(sku.Version);
                                versionAttributeValue.Add(versionValue);
                            }
                        }
                    }
                }
                var version = new
                {
                    AttributeName = product.VersionAlias,
                    AttributeId = product.TypeId,
                    AttributeValue = versionAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 2,
                };

                if (versionId > 0)
                {
                    SkuItemList.Add(version);
                }
                #endregion

                #region Sku值
                foreach (var sku in skus)
                {
                    int quantity = cartItems != null ? cartItems.Where(d => d.SkuId == sku.Id).Sum(d => d.Quantity) : 0;//购物车购买数量
                    ShopBranchSkuInfo shopSku = null;
                    if (shopBranchId > 0 && branchskuList != null)
                    {
                        shopSku = branchskuList.Where(x => x.SkuId == sku.Id).FirstOrDefault();
                        if (shopSku != null) sku.Stock = shopSku.Stock;
                    }
                    var prosku = new
                    {
                        SkuItems = "",
                        MemberPrices = "",
                        SkuId = sku.Id,
                        ProductId = product.Id,
                        SKU = sku.Sku,
                        Weight = 0,
                        Stock = shopSku != null ? shopSku.Stock : (stocks.ContainsKey(sku.Id) ? stocks[sku.Id] : 0),
                        WarningStock = sku.SafeStock,
                        CostPrice = sku.CostPrice,
                        SalePrice = shop.IsSelf ? decimal.Parse((sku.SalePrice * discount).ToString("F2")) : sku.SalePrice,
                        StoreStock = 0,
                        StoreSalePrice = 0,
                        OldSalePrice = 0,
                        ImageUrl = "",
                        ThumbnailUrl40 = "",
                        ThumbnailUrl410 = "",
                        MaxStock = 0,
                        FreezeStock = 0,
                        Quantity = quantity
                    };
                    Skus.Add(prosku);
                }
                #endregion
            }
            #endregion

            #region 商品

            bool isFavorite = false;
            bool isFavoriteShop = false;
            if (CurrentUser != null)
            {
                isFavorite = FavoriteApplication.HasFavoriteProduct(product.Id, CurrentUser.Id);
                isFavoriteShop = FavoriteApplication.HasFavoriteShop(product.ShopId, CurrentUser.Id);
            }

            decimal maxprice = shop.IsSelf ? skus.Max(d => d.SalePrice) * discount : skus.Max(d => d.SalePrice);//最高SKU价格
            decimal minprice = shop.IsSelf ? skus.Min(d => d.SalePrice) * discount : skus.Min(d => d.SalePrice);//最低价

            var productImage = new List<string>();
            for (int i = 1; i < 6; i++)
            {
                if (Core.HimallIO.ExistFile(product.RelativePath + string.Format("/{0}.png", i)))
                {
                    var path = Core.HimallIO.GetRomoteProductSizeImage(product.RelativePath, i, (int)Himall.CommonModel.ImageSize.Size_350);
                    productImage.Add(path);
                }
            }
            long activeId = 0;
            int activetype = 0;

            var isValidLimitBuy = false;
            var limitBuy = LimitTimeApplication.GetAvailableByProduct(product.Id);

            if (limitBuy != null && shopBranchId <= 0)
            {
                var limitSku = ServiceProvider.Instance<LimitTimeBuyService>.Create.Get(limitBuy.Id);
                var limitSkuItem = limitSku.Details.OrderBy(d => d.Price).FirstOrDefault();
                if (limitSkuItem != null)
                    product.MinSalePrice = limitSkuItem.Price;
                if (limitBuy.BeginDate <= DateTime.Now)
                {
                    maxprice = limitBuy.Items.Max(p => p.Price);
                    minprice = limitBuy.Items.Min(p => p.Price);
                    activeId = limitBuy.Id;
                    activetype = 1;
                    isValidLimitBuy = true;
                }
                else
                {
                    var config = ServiceProvider.Instance<LimitTimeBuyService>.Create.GetConfig();
                    var surplus = limitBuy.BeginDate - DateTime.Now;  //开始时间还剩多久
                    if (surplus.TotalHours >= config.Preheat && !config.IsNormalPurchase)  //预热大于开始
                    {
                        activeId = limitBuy.Id;
                        activetype = 1;
                        isValidLimitBuy = true;
                    }
                }
            }

            model.Product = new ProductInfoModel()
            {
                ProductId = product.Id,
                CommentCount = commentSummary.Total,
                Consultations = ConsultationApplication.GetConsultationCount(productId),
                ImagePath = productImage,
                IsFavorite = isFavorite,
                MarketPrice = product.MarketPrice,
                NicePercent = model.Shop.ProductMark == 0 || commentSummary.Positive == 0 ? 100 : (commentSummary.Positive / commentSummary.Total * 100),
                ProductName = product.ProductName,
                ProductSaleStatus = product.SaleStatus,
                AuditStatus = product.AuditStatus,
                ShortDescription = product.ShortDescription,
                ProductDescription = ProductManagerApplication.GetDescriptionContent(product.Id),
                IsOnLimitBuy = limitBuy != null,
                IsOpenLadder = product.IsOpenLadder,
            };

            var ladderPrices = new List<ProductLadderPrice>();
            if (product.IsOpenLadder)
            {
                ladderPrices = product.LadderPrice.Select(item => new ProductLadderPrice
                {
                    MinBath = item.MinBath,
                    MaxBath = item.MaxBath,
                    Price = Convert.ToDecimal((item.Price * discount).ToString("f2"))
                }).ToList();
                var minLadder = ladderPrices.OrderBy(l => l.MinBath).FirstOrDefault();
                model.Product.MinMath = minLadder.MinBath;
                if (minLadder != null)
                    minprice = minLadder.Price;
            }
            #endregion

            //统计商品浏览量、店铺浏览人数
            StatisticApplication.StatisticVisitCount(product.Id, product.ShopId);
            //图片集合
            List<object> ProductImgs = new List<object>();
            for (int i = 1; i < 6; i++)
            {
                if (i == 1 || Himall.Core.HimallIO.ExistFile(product.ImagePath + string.Format("/{0}.png", i)))
                {
                    ProductImgs.Add(Core.HimallIO.GetRomoteProductSizeImage(product.ImagePath, i, (int)ImageSize.Size_500).Replace("http://", "https://"));
                }
            }
            //优惠劵
            var coupons = GetShopCouponList(shop.Id, productId);

            dynamic Promotes = new System.Dynamic.ExpandoObject();
            Promotes.freeFreight = shop.FreeFreight;
            #region  代金红包

            var bonus = ShopBonusApplication.GetAvailableBonusByShop(shop.Id);
            int BonusCount = 0;
            decimal BonusGrantPrice = 0;
            decimal BonusRandomAmountStart = 0;
            decimal BonusRandomAmountEnd = 0;

            if (bonus != null)
            {
                BonusCount = bonus.Count;
                BonusGrantPrice = bonus.GrantPrice;
                BonusRandomAmountStart = bonus.RandomAmountStart;
                BonusRandomAmountEnd = bonus.RandomAmountEnd;
            }
            #endregion
            Promotes.FullDiscount = FullDiscountApplication.GetGoingByProduct(product.Id, shop.Id);

            bool hasFightGroup = false;
            decimal fightFroupPrice = 0;
            var fightGroup = FightGroupApplication.GetAvailableByProduct(product.Id);
            if (fightGroup != null)
            {
                hasFightGroup = true;
                activeId = fightGroup.Id;
                fightFroupPrice = fightGroup.Items.Min(p => p.ActivePrice);
            }

            VirtualProductModel virtualPInfo = null;
            List<VirtualProductItemModel> virtualProductItemModels = null;
            if (product.ProductType == 1)
            {
                var virtualData = product.VirtualData;
                virtualPInfo = new Model.VirtualProductModel()
                {
                    EndDate = virtualData.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    StartDate = virtualData.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    SupportRefundType = virtualData.SupportRefundType,
                    EffectiveType = virtualData.EffectiveType,
                    Hour = virtualData.Hour,
                    UseNotice = virtualData.UseNotice,
                    ValidityType = virtualData.ValidityType ? 1 : 0,
                    IsOverdue = virtualData.ValidityType && DateTime.Now > virtualData.EndDate.Value
                };

                virtualProductItemModels = virtualData.Items.Select(item => new VirtualProductItemModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Required = item.Required,
                    Type = item.Type,
                }).ToList();
            }

            var isSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1; //是否显示销量
            long saleCounts = 0;
            if (shopBranchId > 0)
            {
                var dtNow = DateTime.Now;
                saleCounts = OrderApplication.GetSaleCount(dtNow.AddDays(-30).Date, dtNow, shopBranchId: shopBranchId, productId: product.Id);
                saleCounts = saleCounts + Core.Helper.TypeHelper.ObjectToInt(product.VirtualSaleCounts);
            }
            else
            {
                saleCounts = isSaleCountOnOff ? ProductWebApplication.GetProductSaleCounts(productId) : 0;
                saleCounts = saleCounts + Himall.Core.Helper.TypeHelper.ObjectToInt(product.VirtualSaleCounts);
            }
            var collocationsCount = CollocationApplication.GetAvailable(productId).Count;
            long roomId = LiveApplication.IsLiveProduct(product.Id);
            var result = new
            {
                IsLive = roomId > 0 ? true : false,
                roomId = roomId,
                IsFavorite = isFavorite,
                IsFavoriteShop = isFavoriteShop,
                ProductId = product.Id,
                ProductName = product.ProductName,
                ShortDescription = product.ShortDescription,
                MetaDescription = model.Product.ProductDescription.Replace("\"/Storage/Shop", "\"" + Core.HimallIO.GetImagePath("/Storage/Shop")),//替换链接  /Storage/Shop
                MarketPrice = product.MarketPrice.ToString("0.##"),//市场价
                IsfreeShipping = "False",//是否免费送货
                MaxSalePrice = maxprice.ToString("0.##"),
                MinSalePrice = minprice.ToString("0.##"),//限时抢购或商城价格
                ThumbnailUrl60 = ProductManagerApplication.GetImagePath(product.ImagePath, ImageSize.Size_500, product.UpdateTime, 1, true),
                ProductImgs = ProductImgs,
                ReviewCount = commentSummary.Total,
                Stock = shopBranchId <= 0 ? stocks.Sum(p => p.Value) : branchskuList.Where(s => s.ProductId == productId).Sum(p => p.Stock),
                SkuItemList = SkuItemList,
                Skus = Skus,
                Coupons = coupons,//优惠劵
                Promotes = Promotes,//活动
                IsUnSale = product.SaleStatus == Entities.ProductInfo.ProductSaleStatus.InStock ? true : false,
                ProductSaleStatus = product.SaleStatus.GetHashCode(),
                AuditStatus = product.AuditStatus.GetHashCode(),
                ShowStatus = ProductManagerApplication.GetProductShowStatus(product),
                ActiveId = activeId,
                ActiveType = activetype,
                IsOpenLadder = product.IsOpenLadder,//是否开启阶梯价
                LadderPrices = ladderPrices,//阶梯价
                MinBath = model.Product.MinMath,//最小批量
                Shop = model.Shop,
                VShopLog = Himall.Core.HimallIO.GetRomoteImagePath(model.VShopLog).Replace("http://", "https://"),
                MeasureUnit = string.IsNullOrEmpty(product.MeasureUnit) ? "" : product.MeasureUnit, //单位
                MaxBuyCount = product.MaxBuyCount,//限购数
                IsOnLimitBuy = isValidLimitBuy,
                IsSaleCountOnOff = isSaleCountOnOff,//是否显示销量
                ShowSaleCounts = saleCounts,
                Freight = product.ProductType == 1 ? "0" : FreightTemplateApplication.GetFreightStr(product.Id, model.FreightTemplate, userId, product.ProductType),
                SendTime = (model.FreightTemplate != null && !string.IsNullOrEmpty(model.FreightTemplate.SendTime) ? (model.FreightTemplate.SendTime + "h内发货") : ""), //运费模板发货时间
                hasFightGroup = hasFightGroup,
                fightFroupPrice = fightFroupPrice,
                ProductType = product.ProductType,
                VirtualProductInfo = virtualPInfo,
                VirtualProductItemModels = virtualProductItemModels,
                BonusCount = BonusCount,
                BonusGrantPrice = BonusGrantPrice,
                BonusRandomAmountStart = BonusRandomAmountStart,
                BonusRandomAmountEnd = BonusRandomAmountEnd,
                CartCount = cartcount,
                VideoPath = GetVideoPath(product.VideoPath, product.UpdateTime),
                CollocationsCount = collocationsCount
            };

            return JsonResult<dynamic>(result);
        }

        private string GetVideoPath(string videoPath, DateTime updatetime)
        {
            return ProductManagerApplication.GetVideoPath(videoPath, updatetime, true);
        }

        public JsonResult<Result<dynamic>> GetCollocations(long productId, long shopBranchId = 0)
        {
            var result = CollocationApplication.GetDisplayCollocation(productId);
            result.ForEach(collocation =>
            {
                collocation.Products.ForEach(item =>
                {
                    item.Image = Core.HimallIO.GetRomoteProductSizeImage(item.Image, 1, (int)ImageSize.Size_220).Replace("http://", "https://");
                });
            });
            return JsonResult<dynamic>(new { ProductCollocations = result });
        }
        /// <summary>
        /// 判断商品是否参加限时购
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="ProductID"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetIsOnLimitBuy(string openId, long ProductID)
        {
            var iLimitService = ObjectContainer.Current.Resolve<LimitTimeBuyService>();
            var flashSaleModel = iLimitService.GetFlaseSaleByProductId(ProductID);
            var flashSaleConfig = iLimitService.GetConfig();

            if (flashSaleModel != null)
            {
                if (DateTime.Parse(flashSaleModel.BeginDate) > DateTime.Now)
                {
                    TimeSpan flashSaleTime = DateTime.Parse(flashSaleModel.BeginDate) - DateTime.Now;  //开始时间还剩多久
                    TimeSpan preheatTime = new TimeSpan(flashSaleConfig.Preheat, 0, 0);  //预热时间是多久
                    if (preheatTime >= flashSaleTime)  //预热大于开始
                    {

                        if (!flashSaleConfig.IsNormalPurchase)
                            return JsonResult<dynamic>(data: new { Id = flashSaleModel.Id });
                    }
                }
                else
                {
                    return JsonResult<dynamic>(data: new { Id = flashSaleModel.Id });
                }
            }
            return Json(ErrorResult<dynamic>(msg: "可以正常购买"));

        }
        /// <summary>
        /// 获取商品的规格信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetProductSkus(long productId, string openId = "")
        {

            var product = ServiceProvider.Instance<ProductService>.Create.GetProduct(productId);
            var limitBuy = ServiceProvider.Instance<LimitTimeBuyService>.Create.GetLimitTimeMarketItemByProductId(productId);
            var shopInfo = ShopApplication.GetShop(product.ShopId);

            decimal discount = 1M;
            if (CurrentUser != null && shopInfo.IsSelf)
            {
                discount = CurrentUser.MemberDiscount;
            }
            Himall.Entities.ShoppingCartInfo cartInfo = CurrentUser == null ? new ShoppingCartInfo() : CartApplication.GetCart(CurrentUser.Id);

            var skuArray = new List<ProductSKUModel>();
            object defaultsku = new object();
            int activetype = 0;

            decimal SalePrice = 0;
            var skus = ProductManagerApplication.GetSKUs(productId);
            foreach (var sku in skus.Where(s => s.Stock > 0))
            {
                var price = SalePrice = sku.SalePrice;
                if (product.IsOpenLadder)
                {
                    var ladderPrices = ProductManagerApplication.GetLadderPriceByProductIds(product.Id);
                    var ladder = ladderPrices.OrderBy(l => l.MinBath).FirstOrDefault();
                    if (ladder != null)
                    {
                        price = SalePrice = ladder.Price;
                    }

                }
                SalePrice = shopInfo.IsSelf ? SalePrice * discount : SalePrice;
                price = shopInfo.IsSelf ? price * discount : price;
                ProductSKUModel skuMode = new ProductSKUModel
                {
                    Price = price,
                    SkuId = sku.Id,
                    Stock = sku.Stock
                };
                if (limitBuy != null)
                {
                    activetype = 1;
                    var limitSku = ServiceProvider.Instance<LimitTimeBuyService>.Create.Get(limitBuy.Id);
                    var limitSkuItem = limitSku.Details.Where(r => r.SkuId.Equals(sku.Id)).FirstOrDefault();
                    if (limitSkuItem != null)
                        skuMode.Price = limitSkuItem.Price;
                }
                skuArray.Add(skuMode);
            }

            Entities.TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetType(product.TypeId);
            string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
            string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
            string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;

            List<object> SkuItemList = new List<object>();
            List<object> Skus = new List<object>();
            //var skus = ProductManagerApplication.GetSKUs(product.Id);
            if (skus.Count > 0)
            {
                #region 颜色
                long colorId = 0, sizeId = 0, versionId = 0;
                List<ProductSKUItem> colorAttributeValue = new List<ProductSKUItem>();
                List<string> listcolor = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 0 && !string.IsNullOrEmpty(sku.Color))
                    {
                        if (long.TryParse(specs[1], out colorId)) { }//相同颜色规格累加对应值
                        if (colorId != 0)
                        {
                            if (!listcolor.Contains(sku.Color))
                            {
                                var c = skus.Where(s => s.Color.Equals(sku.Color)).Sum(s => s.Stock);
                                var colorvalue = new ProductSKUItem
                                {
                                    ValueId = colorId,
                                    Value = sku.Color,
                                    UseAttributeImage = false,
                                    ImageUrl = HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listcolor.Add(sku.Color);
                                colorAttributeValue.Add(colorvalue);
                            }
                        }
                    }
                }
                var color = new
                {
                    AttributeId = product.TypeId,
                    AttributeName = !string.IsNullOrWhiteSpace(product.ColorAlias) ? product.ColorAlias : colorAlias,
                    AttributeValue = colorAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 0,
                };
                if (colorId > 0)
                {
                    SkuItemList.Add(color);
                }
                #endregion

                #region 容量
                List<ProductSKUItem> sizeAttributeValue = new List<ProductSKUItem>();
                List<string> listsize = new List<string>();
                foreach (var sku in skus.OrderBy(a => a.Size))
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 1)
                    {
                        if (long.TryParse(specs[2], out sizeId)) { }
                        if (sizeId != 0)
                        {
                            if (!listsize.Contains(sku.Size))
                            {
                                var ss = skus.Where(s => s.Size.Equals(sku.Size)).Sum(s1 => s1.Stock);
                                var sizeValue = new ProductSKUItem
                                {
                                    ValueId = sizeId,
                                    Value = sku.Size,
                                    UseAttributeImage = false,
                                    ImageUrl = Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listsize.Add(sku.Size);
                                sizeAttributeValue.Add(sizeValue);
                            }
                        }
                    }
                }
                var size = new
                {
                    AttributeId = product.TypeId,
                    AttributeName = !string.IsNullOrWhiteSpace(product.SizeAlias) ? product.SizeAlias : sizeAlias,
                    AttributeValue = sizeAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 1,
                };
                if (sizeId > 0)
                {
                    SkuItemList.Add(size);
                }
                #endregion

                #region 规格
                List<ProductSKUItem> versionAttributeValue = new List<ProductSKUItem>();
                List<string> listversion = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 2)
                    {
                        if (long.TryParse(specs[3], out versionId)) { }
                        if (versionId != 0)
                        {
                            if (!listversion.Contains(sku.Version))
                            {
                                var v = skus.Where(s => s.Version.Equals(sku.Version));
                                var versionValue = new ProductSKUItem
                                {
                                    ValueId = versionId,
                                    Value = sku.Version,
                                    UseAttributeImage = false,
                                    ImageUrl = Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listversion.Add(sku.Version);
                                versionAttributeValue.Add(versionValue);
                            }
                        }
                    }
                }
                var version = new
                {
                    AttributeId = product.TypeId,
                    AttributeName = !string.IsNullOrWhiteSpace(product.VersionAlias) ? product.VersionAlias : versionAlias,
                    AttributeValue = versionAttributeValue.OrderBy(p => p.ValueId),
                    AttributeIndex = 2,
                };
                if (versionId > 0)
                {
                    SkuItemList.Add(version);
                }
                #endregion

                #region Sku值
                foreach (var sku in skus)
                {
                    var sku_price = skuArray.FirstOrDefault(e => e.SkuId == sku.Id);
                    var prosku = new
                    {
                        SkuId = sku.Id,
                        SKU = sku.Sku,
                        Weight = product.Weight,
                        Stock = sku.Stock,
                        WarningStock = sku.SafeStock,
                        SalePrice = sku_price != null ? sku_price.Price.ToString("0.##") : sku.SalePrice.ToString("0.##"),
                        CartQuantity = cartInfo.Items.Where(d => d.SkuId == sku.Id).Sum(d => d.Quantity),
                        ImageUrl = Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                    };
                    Skus.Add(prosku);
                }
                defaultsku = Skus[0];
                #endregion
            }
            var json = JsonResult<dynamic>(new
            {
                ProductId = productId,
                ProductName = product.ProductName,
                product.MaxBuyCount,
                ImageUrl = ProductManagerApplication.GetImagePath(product.ImagePath, ImageSize.Size_350, product.UpdateTime, 1, true),
                Stock = skuArray.Sum(s => s.Stock),// skus.Sum(s => s.Stock),
                ActivityUrl = activetype,
                SkuItems = SkuItemList,
                Skus = Skus,
                DefaultSku = defaultsku
            });
            return json;
        }

        /// <summary>
        /// 根据商品Id获取商品规格【门店】
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetProductSkuInfo(long id, long shopBranchId)
        {
            var _ProductService = ObjectContainer.Current.Resolve<ProductService>();
            var _iBranchCartService = ObjectContainer.Current.Resolve<BranchCartService>();
            var _iTypeService = ObjectContainer.Current.Resolve<TypeService>();
            if (id <= 0)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "id");
            }
            if (shopBranchId <= 0)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "shopBranchId");
            }
            var product = _ProductService.GetProduct(id);
            var shopInfo = ShopApplication.GetShop(product.ShopId);

            decimal discount = 1M;
            if (CurrentUser != null && shopInfo.IsSelf)
            {
                discount = CurrentUser.MemberDiscount;
            }

            var skuArray = new List<ProductSKUModel>();
            object defaultsku = new object();
            Himall.Entities.ShoppingCartInfo cartInfo = null;
            long userId = 0;
            if (CurrentUser != null)
            {
                cartInfo = _iBranchCartService.GetCart(CurrentUser.Id, shopBranchId);//获取购物车数据
                userId = CurrentUser.Id;
            }

            var shopBranchInfo = ShopBranchApplication.GetShopBranchById(shopBranchId);
            var branchskuList = ShopBranchApplication.GetSkus(shopBranchInfo.ShopId, shopBranchId);
            var shopcartinfo = new BranchCartHelper().GetCart(userId, shopBranchId);
            var skus = ProductManagerApplication.GetSKUs(product.Id);
            foreach (var sku in skus)
            {
                decimal price = 1M;
                if (shopInfo.IsSelf)
                {
                    price = sku.SalePrice * discount;
                }
                else
                {
                    price = sku.SalePrice;
                }
                if (branchskuList.Count(x => x.SkuId == sku.Id && x.Stock > 0) > 0)
                {
                    var skuCartNumber = 0;
                    if (shopcartinfo != null && shopcartinfo.Items != null && shopcartinfo.Items.Count() > 0)
                    {
                        var _tmp = shopcartinfo.Items.FirstOrDefault(x => x.SkuId == sku.Id);
                        if (_tmp != null)
                        {
                            skuCartNumber = _tmp.Quantity;
                        }
                    }
                    skuArray.Add(new ProductSKUModel
                    {
                        Price = price,
                        SkuId = sku.Id,
                        Stock = branchskuList.FirstOrDefault(x => x.SkuId == sku.Id).Stock
                    });
                }
            }

            Entities.TypeInfo typeInfo = _iTypeService.GetType(product.TypeId);
            string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
            string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
            string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;

            List<object> SkuItemList = new List<object>();
            List<object> Skus = new List<object>();
            if (skus.Count > 0)
            {
                #region 颜色
                long colorId = 0, sizeId = 0, versionId = 0;
                var colorAttributeValue = new List<ProductSKUItem>();
                List<string> listcolor = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 0 && !string.IsNullOrEmpty(sku.Color))
                    {
                        if (long.TryParse(specs[1], out colorId)) { }//相同颜色规格累加对应值
                        if (colorId != 0)
                        {
                            sku.ColorId = colorId;
                            if (!listcolor.Contains(sku.Color))
                            {
                                var colorvalue = new ProductSKUItem
                                {
                                    ValueId = colorId,
                                    Value = sku.Color,
                                    UseAttributeImage = true,
                                    ImageUrl = HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listcolor.Add(sku.Color);
                                colorAttributeValue.Add(colorvalue);
                            }
                        }
                    }
                }
                var color = new
                {
                    AttributeId = product.TypeId,
                    AttributeName = !string.IsNullOrWhiteSpace(product.ColorAlias) ? product.ColorAlias : colorAlias,//如果商品有自定义规格则用
                    AttributeValue = colorAttributeValue.OrderBy(p => p.ValueId).ToList(),
                    AttributeIndex = 0,
                };
                if (colorId > 0)
                {
                    SkuItemList.Add(color);
                }
                #endregion

                #region 容量
                var sizeAttributeValue = new List<ProductSKUItem>();
                List<string> listsize = new List<string>();
                foreach (var sku in skus.OrderBy(a => a.Size))
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 1)
                    {
                        if (long.TryParse(specs[2], out sizeId)) { }
                        if (sizeId != 0)
                        {
                            sku.SizeId = sizeId;
                            if (!listsize.Contains(sku.Size))
                            {
                                var sizeValue = new ProductSKUItem
                                {
                                    ValueId = sizeId,
                                    Value = sku.Size,
                                    UseAttributeImage = false,
                                    ImageUrl = HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listsize.Add(sku.Size);
                                sizeAttributeValue.Add(sizeValue);
                            }
                        }
                    }
                }
                var size = new
                {
                    AttributeId = product.TypeId,
                    AttributeName = !string.IsNullOrWhiteSpace(product.SizeAlias) ? product.SizeAlias : sizeAlias,
                    AttributeValue = sizeAttributeValue.OrderBy(p => p.ValueId).ToList(),
                    AttributeIndex = 1,
                };
                if (sizeId > 0)
                {
                    SkuItemList.Add(size);
                }
                #endregion

                #region 规格
                var versionAttributeValue = new List<ProductSKUItem>();
                List<string> listversion = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 2)
                    {
                        if (long.TryParse(specs[3], out versionId)) { }
                        if (versionId != 0)
                        {
                            sku.VersionId = versionId;
                            if (!listversion.Contains(sku.Version))
                            {
                                var versionValue = new ProductSKUItem
                                {
                                    ValueId = versionId,
                                    Value = sku.Version,
                                    UseAttributeImage = false,
                                    ImageUrl = Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listversion.Add(sku.Version);
                                versionAttributeValue.Add(versionValue);
                            }
                        }
                    }
                }
                var version = new
                {
                    AttributeId = product.TypeId,
                    AttributeName = !string.IsNullOrWhiteSpace(product.VersionAlias) ? product.VersionAlias : versionAlias,
                    AttributeValue = versionAttributeValue.OrderBy(p => p.ValueId).ToList(),
                    AttributeIndex = 2,
                };
                if (versionId > 0)
                {
                    SkuItemList.Add(version);
                }
                #endregion

                #region Sku值
                foreach (var sku in skus.OrderBy(p => p.ColorId).OrderBy(p => p.SizeId).OrderBy(p => p.VersionId))
                {
                    var prosku = new
                    {
                        SkuId = sku.Id,
                        SKU = sku.Sku,
                        Weight = product.Weight,
                        Stock = branchskuList.FirstOrDefault(x => x.SkuId == sku.Id).Stock,
                        WarningStock = sku.SafeStock,
                        SalePrice = shopInfo.IsSelf ? (sku.SalePrice * discount).ToString("0.##") : sku.SalePrice.ToString("0.##"),
                        CartQuantity = cartInfo != null ? cartInfo.Items.Where(d => d.SkuId == sku.Id && d.ShopBranchId == shopBranchId).Sum(d => d.Quantity) : 0,
                        ImageUrl = Core.HimallIO.GetRomoteProductSizeImage(sku.ShowPic, 1, (int)ImageSize.Size_350)
                    };
                    Skus.Add(prosku);
                }
                defaultsku = Skus[0];
                #endregion
            }
            return JsonResult<dynamic>(new
            {
                ProductId = id,
                ProductName = product.ProductName,
                ImageUrl = ProductManagerApplication.GetImagePath(product.ImagePath, ImageSize.Size_350, product.UpdateTime, 1, true),
                Stock = skuArray.Sum(s => s.Stock),// skus.Sum(s => s.Stock),
                                                   //ActivityUrl = activetype,
                SkuItems = SkuItemList,
                Skus = Skus,
                DefaultSku = defaultsku
            });
        }

        /// <summary>
        /// 获取门店商品规格【门店】
        /// </summary>
        /// <param name="pId"></param>
        /// <param name="bid"></param>
        /// <returns></returns>
        public JsonResult<Result<List<ProductSKUModel>>> GetSKUInfo(long pId, long bid)
        {
            var _ProductService = ObjectContainer.Current.Resolve<ProductService>();
            var _iShopBranchService = ObjectContainer.Current.Resolve<ShopBranchService>();
            var _iBranchCartService = ObjectContainer.Current.Resolve<BranchCartService>();
            var product = _ProductService.GetProduct(pId);
            var shopBranchInfo = _iShopBranchService.GetShopBranchById(bid);
            var branchskuList = ShopBranchApplication.GetSkus(shopBranchInfo.ShopId, bid);

            Himall.Entities.ShoppingCartInfo memberCartInfo = null;
            decimal discount = 1M;
            if (CurrentUser != null)
            {
                //如果已登陆取购物车数据
                memberCartInfo = _iBranchCartService.GetCart(CurrentUser.Id, bid);
                discount = CurrentUser.MemberDiscount;
            }
            var shopInfo = ShopApplication.GetShop(product.ShopId);

            var skuArray = new List<ProductSKUModel>();
            var skus = ProductManagerApplication.GetSKUs(product.Id);
            foreach (var sku in skus.Where(s => s.Stock > 0))
            {
                decimal price = 1M;
                if (shopInfo.IsSelf)
                {
                    price = sku.SalePrice * discount;
                }
                else
                {
                    price = sku.SalePrice;
                }
                if (branchskuList.Count(x => x.SkuId == sku.Id && x.Stock > 0) > 0)
                {
                    skuArray.Add(new ProductSKUModel
                    {
                        Price = price,
                        SkuId = sku.Id,
                        Stock = branchskuList.FirstOrDefault(x => x.SkuId == sku.Id).Stock,
                        cartCount = (memberCartInfo == null || memberCartInfo.Items.Count() == 0) ? 0 : memberCartInfo.Items.FirstOrDefault(x => x.SkuId == sku.Id) == null ? 0 : memberCartInfo.Items.FirstOrDefault(x => x.SkuId == sku.Id).Quantity
                    });
                }
            }
            return JsonResult(skuArray);
        }

        //public JsonResult<Result<dynamic>> GetSellOut(long pid)
        //{
        //    var skus = ProductManagerApplication.GetSKUs(pid);
        //    long stock = skus.Sum(a => a.Stock);
        //    return JsonResult<dynamic>(new { issellout = stock });
        //}
        /// <summary>
        /// 商品评价数接口
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetStatisticsReview(long productId, long shopBranchId = 0)
        {
            var product = ServiceProvider.Instance<ProductService>.Create.GetProduct(productId);
            var statistic = CommentApplication.GetProductCommentStatistic(productId, shopBranchId: shopBranchId);
            var json = JsonResult<dynamic>(new
            {
                productName = product.ProductName,
                reviewNum = statistic.AllComment,
                reviewNum1 = statistic.HighComment,
                reviewNum2 = statistic.MediumComment,
                reviewNum3 = statistic.LowComment,
                reviewNumImg = statistic.HasImageComment,
                appendReviewNum = statistic.AppendComment
            });
            return json;
        }
        /// <summary>
        /// 商品评价列表
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetLoadReview(long productId, int pageIndex, int pageSize, int type, long shopBranchId = 0)
        {
            var query = new ProductCommentQuery
            {
                ProductId = productId,
                PageNo = pageIndex,
                PageSize = pageSize,
            };
            switch (type)
            {
                case 1: query.CommentType = ProductCommentMarkType.High; break;
                case 2: query.CommentType = ProductCommentMarkType.Medium; break;
                case 3: query.CommentType = ProductCommentMarkType.Low; break;
                case 4: query.CommentType = ProductCommentMarkType.HasImage; break;
                case 5: query.CommentType = ProductCommentMarkType.Append; break;
            }
            if (shopBranchId > 0)
                query.ShopBranchId = shopBranchId;

            var product = ProductManagerApplication.GetProduct(productId);
            var result = CommentApplication.GetCommentList(query);

            var json = JsonResult<dynamic>(new
            {
                totalCount = result.Total,
                Data = result.Models.Select(d => new
                {
                    UserName = d.UserName,
                    Picture = d.Picture,
                    ProductName = product != null ? product.ProductName : string.Empty,
                    SKUContent = d.Sku,
                    ReviewText = d.ReviewContent,
                    Score = d.ReviewMark,
                    ImageUrl1 = (d.Images != null && d.Images.Count > 0 && !string.IsNullOrEmpty(d.Images[0].CommentImage)) ? HimallIO.GetRomoteImagePath(MoveImages(d.Images[0].CommentImage)) : string.Empty,
                    ImageUrl2 = (d.Images != null && d.Images.Count > 1 && !string.IsNullOrEmpty(d.Images[1].CommentImage)) ? HimallIO.GetRomoteImagePath(MoveImages(d.Images[1].CommentImage)) : string.Empty,
                    ImageUrl3 = (d.Images != null && d.Images.Count > 2 && !string.IsNullOrEmpty(d.Images[2].CommentImage)) ? HimallIO.GetRomoteImagePath(MoveImages(d.Images[2].CommentImage)) : string.Empty,
                    ImageUrl4 = (d.Images != null && d.Images.Count > 3 && !string.IsNullOrEmpty(d.Images[3].CommentImage)) ? HimallIO.GetRomoteImagePath(MoveImages(d.Images[3].CommentImage)) : string.Empty,
                    ImageUrl5 = (d.Images != null && d.Images.Count > 4 && !string.IsNullOrEmpty(d.Images[4].CommentImage)) ? HimallIO.GetRomoteImagePath(MoveImages(d.Images[4].CommentImage)) : string.Empty,
                    AppendImages = d.AppendImages == null ? null : d.AppendImages.Select(a => new { CommentImage = Core.HimallIO.GetRomoteImagePath(MoveImages(a.CommentImage)) }),
                    ReplyText = d.ReplyContent,
                    ReviewDate = d.ReviewDate,
                    ReplyDate = d.ReplyDate,
                    AppendContent = d.AppendContent,
                    AppendDate = d.AppendDate,
                    ReplyAppendContent = d.ReplyAppendContent,
                    ReplyAppendDate = d.ReplyAppendDate
                })
            });

            return json;
        }
        /// <summary>
        /// 添加商品评论（评价送积分）
        /// </summary>
        public JsonResult<Result<string>> GetAddProductReview(string openId, string DataJson)
        {
            CheckUserLogin();
            if (!string.IsNullOrEmpty(DataJson))
            {
                bool result = false;
                List<AddOrderCommentModel> orderComment = DataJson.FromJSON<List<AddOrderCommentModel>>();
                if (orderComment != null)
                {
                    List<ProductComment> list = new List<ProductComment>();
                    string orderIds = "";
                    foreach (var item in orderComment)
                    {
                        AddOrderCommentModel ordercom = new AddOrderCommentModel();
                        ordercom.ReviewDate = DateTime.Now;
                        ordercom.UserId = CurrentUser.Id;
                        ordercom.UserName = CurrentUser.UserName;
                        ordercom.UserEmail = CurrentUser.Email;
                        ordercom.OrderId = item.OrderId;
                        if (!orderIds.Contains(item.OrderId))
                        {
                            AddOrderComment(ordercom, orderComment.Where(a => a.OrderId == item.OrderId).Count());//添加订单评价（订单评价只一次）
                            orderIds += item.OrderId + ",";
                        }

                        var model = new ProductComment();

                        var OrderInfo = ObjectContainer.Current.Resolve<OrderService>().GetOrderItemsByOrderId(long.Parse(item.OrderId)).Where(d => d.ProductId == item.ProductId).FirstOrDefault();
                        if (OrderInfo != null)
                        {
                            model.ReviewDate = DateTime.Now;
                            model.ReviewContent = item.ReviewText;
                            model.UserId = CurrentUser.Id;
                            model.UserName = CurrentUser.UserName;
                            model.Email = CurrentUser.Email;
                            model.SubOrderId = OrderInfo.Id;//订单明细Id
                            model.ReviewMark = item.Score;
                            model.ProductId = item.ProductId;
                            model.Images = new List<ProductCommentImage>();
                            foreach (var img in item.ImageUrl1.Split(','))
                            {
                                var p = new ProductCommentImage();

                                p.CommentType = 0;//0代表默认的表示评论的图片
                                p.CommentImage = Core.HimallIO.GetImagePath(img);
                                if (!string.IsNullOrEmpty(p.CommentImage))
                                {
                                    model.Images.Add(p);
                                }
                            }
                            list.Add(model);
                        }
                        result = true;
                    }
                    CommentApplication.Add(list);
                }
                if (result)
                {
                    return Json(SuccessResult("评价成功", "评价成功"));
                }
                else
                {
                    return Json(ErrorResult("评价失败", "评价失败"));
                }
            }
            return Json(ApiResult<string>(true));
        }

        /// <summary>
        /// 增加订单评论
        /// </summary>
        /// <param name="comment"></param>
        void AddOrderComment(AddOrderCommentModel comment, int productNum)
        {
            TradeCommentApplication.Add(new OrderComment()
            {
                OrderId = long.Parse(comment.OrderId),
                DeliveryMark = 5,//物流评价
                ServiceMark = 5,//服务评价
                PackMark = 5,//包装评价
                UserId = comment.UserId,
                CommentDate = comment.ReviewDate,
                UserName = comment.UserName
            }, productNum);
        }

        /// <summary>
        /// 获取商品批发价
        /// </summary>
        /// <param name="pid">商品ID</param>
        /// <param name="buyNum">数量</param>
        /// <returns></returns>
        public JsonResult<Result<string>> GetChangeNum(long pid, int buyNum)
        {
            var _price = 0m;
            var product = ProductManagerApplication.GetProduct(pid);
            if (product.IsOpenLadder)
            {
                _price = ProductManagerApplication.GetProductLadderPrice(pid, buyNum);
                var shop = ShopApplication.GetShop(product.ShopId);
                var discount = 1m;
                if (CurrentUser != null && shop.IsSelf)
                    discount = CurrentUser.MemberDiscount;

                if (shop.IsSelf)
                    _price = _price * discount;
            }

            return JsonResult(_price.ToString("F2"));
        }

        internal void LogProduct(long pid)
        {
            if (CurrentUser != null)
            {
                BrowseHistrory.AddBrowsingProduct(pid, CurrentUser.Id);
            }
            else
            {
                BrowseHistrory.AddBrowsingProduct(pid);
            }
        }


        /// <summary>
        /// 获取店铺优惠券列表（包含平台券）
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        private dynamic GetShopCouponList(long shopId, long productId)
        {
            //门店券
            var coupons = CouponApplication.GetAvailable(shopId).Where(p => p.UseArea == 0 || p.Products.Contains(productId)).ToList();
            //平台券
            var platform = CouponApplication.GetAvailable(0).Where(p => p.UseArea == 0 || p.Shops.Contains(shopId)).ToList();
            coupons.AddRange(platform);
            //移动端显示
            return coupons.Where(p => p.ShowWap).Select(a => new
            {
                CouponId = a.Id,
                CouponName = a.CouponName,
                Price = a.Price,
                SendCount = a.Num,
                UserLimitCount = a.PerMax,
                OrderUseLimit = a.OrderAmount,
                StartTime = a.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ClosingTime = a.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                CanUseProducts = "",
                ObtainWay = a.ReceiveType,
                NeedPoint = a.NeedIntegral,
                UseWithGroup = false,
                UseWithPanicBuying = false,
                UseWithFireGroup = false,
                LimitText = a.CouponName,
                CanUseProduct = a.Products.Count > 0 ? "部分商品可用" : "全店通用",
                StartTimeText = a.StartTime.ToString("yyyy.MM.dd"),
                ClosingTimeText = a.EndTime.ToString("yyyy.MM.dd"),
                EndTime = a.EndTime,
                Remark = a.Remark,
                UseArea = a.UseArea,
                ShopId = a.ShopId
            });
        }

        private string MoveImages(string image)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                return "";
            }
            var oldname = Path.GetFileName(image);
            string ImageDir = string.Empty;

            //转移图片
            string relativeDir = "/Storage/Plat/Comment/";
            string fileName = oldname;
            if (image.Replace("\\", "/").Contains("/temp/"))//只有在临时目录中的图片才需要复制
            {
                var de = image.Substring(image.LastIndexOf("/temp/"));
                Core.HimallIO.CopyFile(de, relativeDir + fileName, true);
                return relativeDir + fileName;
            }  //目标地址
            else if (image.Contains("/Storage"))
            {
                return image.Substring(image.LastIndexOf("/Storage"));
            }
            return image;
        }



        /// <summary>
        /// 获取追加评论
        /// </summary>
        /// <param name="orderid"></param>
        /// <returns></returns>
        //public JsonResult<Result<dynamic>> GetAppendComment(long orderId)
        //{
        //    CheckUserLogin();
        //    var model = CommentApplication.GetProductEvaluationByOrderIdNew(orderId, CurrentUser.Id);

        //    if (model.Count() > 0 && model.FirstOrDefault().AppendTime.HasValue)
        //        return Json(ErrorResult<dynamic>("追加评论时，获取数据异常", new int[0]));
        //    else
        //    {
        //        var listResult = model.Select(item => new
        //        {
        //            Id = item.Id,
        //            CommentId = item.CommentId,
        //            ProductId = item.ProductId,
        //            ProductName = item.ProductName,
        //            //ThumbnailsUrl = item.ThumbnailsUrl,
        //            ThumbnailsUrl = Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_220), //商城App追加评论时获取商品图片
        //            BuyTime = item.BuyTime,
        //            EvaluationStatus = item.EvaluationStatus,
        //            EvaluationContent = item.EvaluationContent,
        //            AppendContent = item.AppendContent,
        //            AppendTime = item.AppendTime,
        //            EvaluationTime = item.EvaluationTime,
        //            ReplyTime = item.ReplyTime,
        //            ReplyContent = item.ReplyContent,
        //            ReplyAppendTime = item.ReplyAppendTime,
        //            ReplyAppendContent = item.ReplyAppendContent,
        //            EvaluationRank = item.EvaluationRank,
        //            OrderId = item.OrderId,
        //            CommentImages = item.CommentImages.Select(r => new
        //            {
        //                CommentImage = r.CommentImage,
        //                CommentId = r.CommentId,
        //                CommentType = r.CommentType
        //            }).ToList(),
        //            Color = item.Color,
        //            Size = item.Size,
        //            Version = item.Version
        //        }).ToList();
        //        return JsonResult<dynamic>(listResult);
        //    }
        //}
        /// <summary>
        /// 追加评价
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        //public JsonResult<Result<int>> PostAppendComment(CommentAppendCommentModel value)
        //{
        //    CheckUserLogin();
        //    string productCommentsJSON = value.productCommentsJSON;
        //    //var commentService = ServiceProvider.Instance<CommentService>.Create;
        //    var productComments = JsonConvert.DeserializeObject<List<AppendCommentModel>>(productCommentsJSON);

        //    foreach (var m in productComments)
        //    {
        //        m.UserId = CurrentUser.Id;
        //    }
        //    CommentApplication.Append(productComments);
        //    return JsonResult<int>();
        //}


        #region 分销 
        /// <summary>
        /// 获取商品的佣金信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetDistributionInfo(long id)
        {
            dynamic model = new System.Dynamic.ExpandoObject();
            //ProductGetDistributionInfoModel model = new ProductGetDistributionInfoModel { };
            var user = CurrentUser;
            //model.ShareUrl = Request.RequestUri.Scheme + "://" + Request.RequestUri.Authority + "/m-wap/product/Detail/" + id.ToString();
            if (user != null && user.Id > 0 && SiteSettingApplication.SiteSettings.DistributionIsEnable)
            {
                if (SiteSettingApplication.SiteSettings.DistributionIsProductShowTips)
                {
                    model.IsShowBrokerage = true;
                }
                var prom = DistributionApplication.GetDistributor(user.Id);
                if (prom != null && prom.DistributionStatus == (int)DistributorStatus.Audited)
                {
                    model.IsShowBrokerage = true;
                    //model.ShareUrl += "?" + DISTRIBUTION_SPREAD_ID_PARAMETER_NAME + "=" + user.Id.ToString();
                }
            }

            var probroker = DistributionApplication.GetProduct(id);
            if (probroker != null && probroker.ProductStatus == DistributionProductStatus.Normal)
            {
                model.Brokerage = probroker.MaxBrokerage;
                model.SaleCount = probroker.SaleCount;
            }
            else
            {
                model.IsShowBrokerage = false;
            }

            //model.WeiXinShareArgs = Application.WXApiApplication.GetWeiXinShareArgs(Request.RequestUri.AbsoluteUri);
            return JsonResult<dynamic>(model);
        }
        #endregion

        /// <summary>
        /// 获取该商品所在商家下距离用户最近的门店
        /// </summary>
        /// <param name="shopId">商家ID</param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetStroreInfo(long shopId, long productId, string fromLatLng = "")
        {
            if (shopId <= 0) return Json(ErrorResult<dynamic>("请传入合法商家ID"));
            if (!(fromLatLng.Split(',').Length == 2)) return Json(ErrorResult<dynamic>("您当前定位信息异常"));

            var query = new ShopBranchQuery()
            {
                ShopId = shopId,
                FromLatLng = fromLatLng,
                Status = CommonModel.ShopBranchStatus.Normal,
                ShopBranchProductStatus = ShopBranchSkuStatus.Normal,
                ProductIds = new long[] { productId }
            };
            //商家下门店总数
            var shopbranchs = ShopBranchApplication.GetShopBranchsAll(query).Models.Where(p => (p.Latitude > 0 && p.Longitude > 0)).ToList();
            int total = shopbranchs.Count;
            //商家下有该产品的且距离用户最近的门店
            var shopBranch = shopbranchs.Where(p => (p.Latitude > 0 && p.Longitude > 0)).OrderBy(p => p.Distance).FirstOrDefault();
            return JsonResult<dynamic>(new
            {
                StoreInfo = shopBranch,
                total = total
            });
        }


        /// <summary>
        /// 新增或取消商品收藏
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public object PostAddFavoriteProduct(ProductAddFavoriteProductModel value)
        {
            CheckUserLogin();
            long productId = value.productId;
            int status = 0;
            var productService = ServiceProvider.Instance<ProductService>.Create;
            bool isFavorite = productService.IsFavorite(productId, CurrentUser.Id);
            if (isFavorite)
            {
                productService.DeleteFavorite(productId, CurrentUser.Id);
                return SuccessResult("取消成功");
            }
            else
            {
                productService.AddFavorite(productId, CurrentUser.Id, out status);
                return SuccessResult("收藏成功");
            }
        }

        public Result<dynamic> GetVideoNumberUrl(long productId, long distributroId = 0)
        {
            var productArticleShare = ProductManagerApplication.MultipleImgTextNews(productId, distributroId);
            return SuccessResult<dynamic>(productArticleShare);
        }
    }
}
