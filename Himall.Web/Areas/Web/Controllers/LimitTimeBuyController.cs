﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.SellerAdmin.Models;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class LimitTimeBuyController : BaseWebController
    {
        private LimitTimeBuyService _LimitTimeBuyService;
        private SlideAdsService _iSlideAdsService;
        private ShopService _ShopService;
        private ProductService _ProductService;
        private ProductDescriptionTemplateService _iProductDescriptionTemplateService;
        private ShopCategoryService _iShopCategoryService;
        private CommentService _CommentService;
        private ConsultationService _iConsultationService;
        private CouponService _CouponService;
        private CashDepositsService _iCashDepositsService;
        private TypeService _iTypeService;
        public LimitTimeBuyController(
            LimitTimeBuyService LimitTimeBuyService,
            SlideAdsService SlideAdsService,
            ShopService ShopService,
            ProductService ProductService,
            ProductDescriptionTemplateService ProductDescriptionTemplateService,
            ShopCategoryService ShopCategoryService,
            CommentService CommentService,
            ConsultationService ConsultationService,
            CouponService CouponService,
            CashDepositsService CashDepositsService, TypeService TypeService
            )
        {
            _LimitTimeBuyService = LimitTimeBuyService;
            _iSlideAdsService = SlideAdsService;
            _ShopService = ShopService;
            _ProductService = ProductService;
            _iProductDescriptionTemplateService = ProductDescriptionTemplateService;
            _iShopCategoryService = ShopCategoryService;
            _CommentService = CommentService;
            _iConsultationService = ConsultationService;
            _CouponService = CouponService;
            _iCashDepositsService = CashDepositsService;
            _iTypeService = TypeService;

        }
        // GET: Web/LimitTimeBuy
        public ActionResult Home(
            string keywords = "", /* 搜索关键字 */
            string catename = "",/* 分类名*/
            int orderKey = 5, /* 排序项（1：默认，2：销量，3：价格，4 : 结束时间,5:状态） */
            int orderType = 1, /* 排序方式（1：升序，2：降序） */
            int isStart = 0,     /*是否开始( 1 : 开始 , 2 : 未开始 )*/
            int pageNo = 1, /*页码*/
            int pageSize = 60 /*每页显示数据量*/
            )
        {
            #region 初始化查询Model
            FlashSaleQuery model = new FlashSaleQuery()
            {
                ItemName = keywords,
                OrderKey = orderKey,
                OrderType = orderType,
                CategoryName = catename,
                IsStart = isStart,
                IsPreheat = true,
                PageNo = pageNo,
                PageSize = pageSize,
                AuditStatus = FlashSaleInfo.FlashSaleStatus.Ongoing,
                CheckProductStatus = true
            };

            #endregion

            #region ViewBag

            List<SelectListItem> CateSelItem = new List<SelectListItem>();
            var cateArray = _LimitTimeBuyService.GetServiceCategories();
            foreach (var cate in cateArray)
            {
                CateSelItem.Add(new SelectListItem { Selected = false, Text = cate, Value = cate });
            }
            if (!string.IsNullOrWhiteSpace(catename))
            {
                var _tmp = CateSelItem.FirstOrDefault(c => c.Text.Equals(catename));
                if (_tmp != null)
                {
                    _tmp.Selected = true;
                }
            }

            ViewBag.Cate = CateSelItem;
            ViewBag.keywords = keywords;
            ViewBag.orderKey = orderKey;
            ViewBag.orderType = orderType;
            ViewBag.catename = catename;
            ViewBag.Logined = (null != CurrentUser) ? 1 : 0;
            ViewBag.isStart = isStart;
            ViewBag.Slide = _iSlideAdsService.GetSlidAds(0, Entities.SlideAdInfo.SlideAdType.PlatformLimitTime);

            #endregion


            #region 查询商品

            //var itemsModel = _LimitTimeBuyService.GetItemList(model);
            var itemsModel = _LimitTimeBuyService.GetAll(model);
            int total = itemsModel.Total;
            var items = itemsModel.Models.ToArray();


            if (itemsModel.Total == 0)
            {
                ViewBag.keywords = keywords;
                return View();
            }

            ViewBag.Products = ProductManagerApplication.GetProducts(items.Select(p => p.ProductId));
            #endregion


            #region 分页控制
            PagingInfo info = new PagingInfo
            {
                CurrentPage = model.PageNo,
                ItemsPerPage = pageSize,
                TotalItems = total
            };
            ViewBag.pageInfo = info;
            #endregion

            return View(items ?? new FlashSaleInfo[] { });
        }

        private bool WXIsConfig(string appId, string appSecret)
        {
            return (!string.IsNullOrWhiteSpace(appId)) && (!string.IsNullOrWhiteSpace(appSecret));
        }

        public ActionResult Detail(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Error404", "Error", new { area = "Web" });
            string price = "";

            #region 定义Model和变量

            LimitTimeProductDetailModel model = new LimitTimeProductDetailModel
            {
                MainId = long.Parse(id),
                HotAttentionProducts = new List<HotProductInfo>(),
                HotSaleProducts = new List<HotProductInfo>(),
                Product = new Entities.ProductInfo(),
                Shop = new ShopInfoModel(),
                ShopCategory = new List<CategoryJsonModel>(),
                Color = new CollectionSKU(),
                Size = new CollectionSKU(),
                Version = new CollectionSKU()
            };

            FlashSaleModel market = null;
            Entities.ShopInfo shop = null;

            long gid = 0, mid = 0;

            #endregion


            #region 商品Id不合法
            if (long.TryParse(id, out mid)) { }
            if (mid == 0)
            {
                //跳转到出错页面
                return RedirectToAction("Error404", "Error", new { area = "Web" });
            }
            #endregion


            #region 初始化商品和店铺

            market = _LimitTimeBuyService.Get(mid);

            switch (market.Status)
            {
                case Entities.FlashSaleInfo.FlashSaleStatus.Ended:
                    return RedirectToAction("Detail", "Product", new { id = market.ProductId });
                case Entities.FlashSaleInfo.FlashSaleStatus.Cancelled:
                    return RedirectToAction("Detail", "Product", new { id = market.ProductId });
            }
            if (market.Status != Entities.FlashSaleInfo.FlashSaleStatus.Ongoing)
            {
                return RedirectToAction("Home");
            }
            model.FlashSale = market;
            if (market == null || market.Id == 0 || market.Status != FlashSaleInfo.FlashSaleStatus.Ongoing)
            {
                //可能参数是商品ID
                market = market == null ? _LimitTimeBuyService.GetFlaseSaleByProductId(mid) : market;
                if (market == null)
                {
                    //跳转到404页面
                    return RedirectToAction("Error404", "Error", new { area = "Mobile" });
                }
                if (market.Status != FlashSaleInfo.FlashSaleStatus.Ongoing)
                {
                    return RedirectToAction("Detail", "Product", new { id = market.ProductId });
                }
            }

            model.MaxSaleCount = market.LimitCountOfThePeople;
            model.Title = market.Title;

            shop = _ShopService.GetShop(market.ShopId);
            model.Shop.Name = shop.ShopName;
            #endregion

            #region 商品描述
            var product = _ProductService.GetProduct(market.ProductId);
            gid = market.ProductId;


            var brandModel = ObjectContainer.Current.Resolve<BrandService>().GetBrand(product.BrandId);
            product.BrandName = brandModel == null ? "" : brandModel.Name;
            if (CurrentUser != null && CurrentUser.Id > 0)
                product.IsFavorite = ProductManagerApplication.IsFavorite(product.Id, CurrentUser.Id);
            model.Product = product;
            model.Skus = ProductManagerApplication.GetSKUsByProduct(new List<long> { product.Id });
            var description = ProductManagerApplication.GetProductDescription(product.Id);
            model.ProductDescription = description.Description;
            if (description.DescriptionPrefixId != 0)
            {
                var desc = _iProductDescriptionTemplateService
                    .GetTemplate(description.DescriptionPrefixId, product.ShopId);
                model.DescriptionPrefix = desc == null ? "" : desc.Content;
            }

            if (description.DescriptiondSuffixId != 0)
            {
                var desc = _iProductDescriptionTemplateService
                    .GetTemplate(description.DescriptiondSuffixId, product.ShopId);
                model.DescriptiondSuffix = desc == null ? "" : desc.Content;
            }

            #endregion

            #region 店铺

            var categories = _iShopCategoryService.GetShopCategory(product.ShopId);
            List<Entities.ShopCategoryInfo> allcate = categories.ToList();
            foreach (var main in allcate.Where(s => s.ParentCategoryId == 0))
            {
                var topC = new CategoryJsonModel()
                {
                    Name = main.Name,
                    Id = main.Id.ToString(),
                    SubCategory = new List<SecondLevelCategory>()
                };
                foreach (var secondItem in allcate.Where(s => s.ParentCategoryId == main.Id))
                {
                    var secondC = new SecondLevelCategory()
                    {
                        Name = secondItem.Name,
                        Id = secondItem.Id.ToString(),
                    };

                    topC.SubCategory.Add(secondC);
                }
                model.ShopCategory.Add(topC);
            }
            model.CashDeposits = _iCashDepositsService.GetProductEnsure(product.Id);

            #endregion

            #region 热门销售

            //会员折扣
            decimal discount = 1M;
            long SelfShopId = 0;
            if (CurrentUser != null)
            {
                discount = CurrentUser.MemberDiscount;
                var shopInfo = ShopApplication.GetSelfShop();
                SelfShopId = shopInfo.Id;
            }
            var sale = _ProductService.GetHotSaleProduct(shop.Id, 5);
            if (sale != null)
            {
                foreach (var item in sale.ToArray())
                {
                    var salePrice = item.MinSalePrice;
                    if (item.ShopId == SelfShopId)
                        salePrice = item.MinSalePrice * discount;
                    var limitBuy = LimitTimeApplication.GetLimitTimeMarketItemByProductId(item.Id);
                    if (limitBuy != null)
                    {
                        salePrice = limitBuy.MinPrice;
                    }
                    model.HotSaleProducts.Add(new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = Math.Round(salePrice, 2),
                        Id = item.Id,
                        SaleCount = (int)item.SaleCounts + Himall.Core.Helper.TypeHelper.ObjectToInt(item.VirtualSaleCounts)
                    });
                }
            }

            #endregion

            #region 热门关注

            var hot = _ProductService.GetHotConcernedProduct(shop.Id, 5);
            if (hot != null)
            {
                foreach (var item in hot.ToArray())
                {
                    model.HotAttentionProducts.Add(new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = item.MinSalePrice,
                        Id = item.Id,
                        SaleCount = (int)item.ConcernedCount
                    });
                }
            }
            #endregion

            #region 商品规格

            Entities.TypeInfo typeInfo = _iTypeService.GetType(product.TypeId);
            string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
            string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
            string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
            if (product != null)
            {
                colorAlias = !string.IsNullOrWhiteSpace(product.ColorAlias) ? product.ColorAlias : colorAlias;
                sizeAlias = !string.IsNullOrWhiteSpace(product.SizeAlias) ? product.SizeAlias : sizeAlias;
                versionAlias = !string.IsNullOrWhiteSpace(product.VersionAlias) ? product.VersionAlias : versionAlias;
            }
            model.ColorAlias = colorAlias;
            model.SizeAlias = sizeAlias;
            model.VersionAlias = versionAlias;
            var skus = _ProductService.GetSKUs(product.Id);
            if (skus.Count > 0)
            {
                long colorId = 0, sizeId = 0, versionId = 0;
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 0 && !string.IsNullOrEmpty(sku.Color))
                    {
                        if (long.TryParse(specs[1], out colorId)) { }
                        if (colorId != 0)
                        {
                            if (!model.Color.Any(v => v.Value.Equals(sku.Color)))
                            {
                                var c = skus.Where(s => s.Color.Equals(sku.Color)).Sum(s => s.Stock);
                                model.Color.Add(new ProductSKU
                                {
                                    //Name = "选择颜色",
                                    Name = "选择" + colorAlias,
                                    EnabledClass = c != 0 ? "enabled" : "disabled",
                                    //SelectedClass = !model.Color.Any(c1 => c1.SelectedClass.Equals("selected")) && c != 0 ? "selected" : "",
                                    SelectedClass = "",
                                    SkuId = colorId,
                                    Value = sku.Color,
                                    Img = Himall.Core.HimallIO.GetImagePath(sku.ShowPic)
                                });
                            }
                        }
                    }
                    if (specs.Count() > 1 && !string.IsNullOrEmpty(sku.Size))
                    {
                        if (long.TryParse(specs[2], out sizeId)) { }
                        if (sizeId != 0)
                        {
                            if (!model.Size.Any(v => v.Value.Equals(sku.Size)))
                            {
                                var ss = skus.Where(s => s.Size.Equals(sku.Size)).Sum(s1 => s1.Stock);
                                model.Size.Add(new ProductSKU
                                {
                                    //Name = "选择尺码",
                                    Name = "选择" + sizeAlias,
                                    EnabledClass = ss != 0 ? "enabled" : "disabled",
                                    //SelectedClass = !model.Size.Any(s1 => s1.SelectedClass.Equals("selected")) && ss != 0 ? "selected" : "",
                                    SelectedClass = "",
                                    SkuId = sizeId,
                                    Value = sku.Size

                                });
                            }
                        }
                    }

                    if (specs.Count() > 2 && !string.IsNullOrEmpty(sku.Version))
                    {
                        if (long.TryParse(specs[3], out versionId)) { }
                        if (versionId != 0)
                        {
                            if (!model.Version.Any(v => v.Value.Equals(sku.Version)))
                            {
                                var v = skus.Where(s => s.Version.Equals(sku.Version)).Sum(s => s.Stock);
                                model.Version.Add(new ProductSKU
                                {
                                    //Name = "选择版本",
                                    Name = "选择" + versionAlias,
                                    EnabledClass = v != 0 ? "enabled" : "disabled",
                                    //SelectedClass = !model.Version.Any(v1 => v1.SelectedClass.Equals("selected")) && v != 0 ? "selected" : "",
                                    SelectedClass = "",
                                    SkuId = versionId,
                                    Value = sku.Version

                                });
                            }
                        }
                    }

                }

                price = ProductWebApplication.GetProductPriceStr(product, skus, discount);//最小价或区间价文本

            }
            model.Price = string.IsNullOrWhiteSpace(price) ? product.MinSalePrice.ToString("f2") : price;
            #endregion

            #region 商品属性
            List<TypeAttributesModel> ProductAttrs = new List<TypeAttributesModel>();
            var prodAttrs = ProductManagerApplication.GetProductAttributes(product.Id);
            foreach (var attr in prodAttrs)
            {
                if (!ProductAttrs.Any(p => p.AttrId == attr.AttributeId))
                {
                    var attribute = _iTypeService.GetAttribute(attr.AttributeId);
                    var values = _iTypeService.GetAttributeValues(attr.AttributeId);
                    var attrModel = new TypeAttributesModel()
                    {
                        AttrId = attr.AttributeId,
                        AttrValues = new List<TypeAttrValue>(),
                        Name = attribute.Name
                    };
                    foreach (var attrV in values)
                    {
                        if (prodAttrs.Any(p => p.ValueId == attrV.Id))
                        {
                            attrModel.AttrValues.Add(new TypeAttrValue
                            {
                                Id = attrV.Id.ToString(),
                                Name = attrV.Value
                            });
                        }
                    }
                    ProductAttrs.Add(attrModel);
                }
                else
                {
                    var attrTemp = ProductAttrs.FirstOrDefault(p => p.AttrId == attr.AttributeId);
                    var values = _iTypeService.GetAttributeValues(attr.AttributeId);
                    if (!attrTemp.AttrValues.Any(p => p.Id == attr.ValueId.ToString()))
                    {
                        attrTemp.AttrValues.Add(new TypeAttrValue
                        {
                            Id = attr.ValueId.ToString(),
                            Name = values.FirstOrDefault(a => a.Id == attr.ValueId).Value
                        });
                    }
                }
            }
            model.ProductAttrs = ProductAttrs;
            #endregion

            #region 获取评论、咨询数量

            model.CommentCount = CommentApplication.GetCommentCountByProduct(product.Id);

            var consultations = _iConsultationService.GetConsultations(gid);
            model.Consultations = consultations.Count();

            #endregion

            #region 累加浏览次数、 加入历史记录
            if (CurrentUser != null)
            {
                BrowseHistrory.AddBrowsingProduct(product.Id, CurrentUser.Id);
            }
            else
            {
                BrowseHistrory.AddBrowsingProduct(product.Id);
            }

            //_ProductService.LogProductVisti(gid);
            //统计商品浏览量、店铺浏览人数
            StatisticApplication.StatisticVisitCount(product.Id, product.ShopId);
            #endregion

            #region 红包
            var bonus = ObjectContainer.Current.Resolve<ShopBonusService>().GetByShopId(product.ShopId);
            if (bonus != null)
            {
                model.GrantPrice = bonus.GrantPrice;
            }
            else
            {
                model.GrantPrice = 0;
            }
            #endregion

            #region 获取店铺的评价统计

            var statistic = ShopApplication.GetStatisticOrderComment(shop.Id);
            ViewBag.ProductAndDescription = statistic.ProductAndDescription;
            ViewBag.ProductAndDescriptionPeer = statistic.ProductAndDescriptionPeer;
            ViewBag.ProductAndDescriptionMin = statistic.ProductAndDescriptionMin;
            ViewBag.ProductAndDescriptionMax = statistic.ProductAndDescriptionMax;

            ViewBag.SellerServiceAttitude = statistic.SellerServiceAttitude;
            ViewBag.SellerServiceAttitudePeer = statistic.SellerServiceAttitudePeer;
            ViewBag.SellerServiceAttitudeMax = statistic.SellerServiceAttitudeMax;
            ViewBag.SellerServiceAttitudeMin = statistic.SellerServiceAttitudeMin;

            ViewBag.SellerDeliverySpeed = statistic.SellerDeliverySpeed;
            ViewBag.SellerDeliverySpeedPeer = statistic.SellerDeliverySpeedPeer;
            ViewBag.SellerDeliverySpeedMax = statistic.SellerDeliverySpeedMax;
            ViewBag.sellerDeliverySpeedMin = statistic.SellerDeliverySpeedMin;
            #endregion

            #region 客服
            model.Service = ObjectContainer.Current.Resolve<CustomerCustomerService>().GetCustomerService(shop.Id).Where(c => c.Type == Entities.CustomerServiceInfo.ServiceType.PreSale && c.TerminalType == Entities.CustomerServiceInfo.ServiceTerminalType.PC).OrderBy(m => m.Tool);
            #endregion

            #region 开团提醒场景二维码
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (DateTime.Parse(model.FlashSale.BeginDate) > DateTime.Now && WXIsConfig(siteSetting.WeixinAppId, siteSetting.WeixinAppSecret))
            {
                try
                {
                    var token = WXApiApplication.TryGetToken(siteSetting.WeixinAppId, siteSetting.WeixinAppSecret);
                    SceneHelper helper = new SceneHelper();
                    SceneModel scene = new SceneModel(QR_SCENE_Type.FlashSaleRemind, model.FlashSale.Id);
                    int sceneId = helper.SetModel(scene);
                    var ticket = QrCodeApi.Create(token, 86400, sceneId, Senparc.Weixin.MP.QrCode_ActionName.QR_SCENE).ticket;
                    ViewBag.ticket = ticket;
                }
                catch { }
            }
            #endregion

            model.Logined = (null != CurrentUser) ? 1 : 0;
            model.EnabledBuy = product.AuditStatus == Entities.ProductInfo.ProductAuditStatus.Audited && DateTime.Parse(market.BeginDate) <= DateTime.Now && DateTime.Parse(market.EndDate) > DateTime.Now && product.SaleStatus == Entities.ProductInfo.ProductSaleStatus.OnSale;
            if (market.Status == FlashSaleInfo.FlashSaleStatus.Ongoing && DateTime.Parse(market.BeginDate) < DateTime.Now && DateTime.Parse(market.EndDate) > DateTime.Now)
            {
                TimeSpan end = new TimeSpan(DateTime.Parse(market.EndDate).Ticks);
                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                TimeSpan ts = end.Subtract(start);
                model.Second = ts.TotalSeconds < 0 ? 0 : ts.TotalSeconds;
            }
            else if (market.Status == FlashSaleInfo.FlashSaleStatus.Ongoing && DateTime.Parse(market.BeginDate) > DateTime.Now)
            {
                TimeSpan end = new TimeSpan(DateTime.Parse(market.BeginDate).Ticks);
                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                TimeSpan ts = end.Subtract(start);
                model.Second = ts.TotalSeconds < 0 ? 0 : ts.TotalSeconds;
            }

            //补充当前店铺红包功能
            ViewBag.isShopPage = true;
            ViewBag.CurShopId = product.ShopId;
            TempData["isShopPage"] = true;
            TempData["CurShopId"] = product.ShopId;

            ViewBag.Keyword = SiteSettings.Keyword;
            ViewBag.Quantity = market.Quantity;//总活动库存
            model.VirtualProductInfo = ProductManagerApplication.GetVirtualProductInfoByProductId(product.Id);
            model.FreightTemplate = FreightTemplateApplication.GetFreightTemplate(product.FreightTemplateId);
            return View(model);
        }

        [HttpPost]
        public ActionResult GetSkus(long id)
        {
            var model = _LimitTimeBuyService.Get(id);
            if (model != null)
            {
                List<FlashSaleDetailModel> details = new List<FlashSaleDetailModel>();
                foreach (var sku in model.Details.Where(a => a.TotalCount > 0))
                {
                    details.Add(sku);
                }

                model.Details = details;
                return Json(model);
            }

            return Json(null);
        }

        [HttpPost]
        public JsonResult CheckLimitTimeBuy(string skuIds, string counts)
        {
            var skuIdsArr = skuIds.Split(',');
            var pCountsArr = counts.TrimEnd(',').Split(',').Select(t => int.Parse(t));
            var productService = _ProductService;
            int index = 0;
            var products = skuIdsArr.Select(item =>
            {
                var sku = productService.GetSku(item);
                var count = pCountsArr.ElementAt(index++);
                return new CartItemModel()
                {
                    id = sku.ProductId,
                    count = count
                };
            }).ToList().FirstOrDefault();

            int exist = _LimitTimeBuyService.GetBuyCount(products.id, CurrentUser.Id);
            int MaxSaleCount = 0;
            var model = _LimitTimeBuyService.GetLimitTimeMarketItemByProductId(products.id);
            if (model != null)
            {
                MaxSaleCount = model.LimitCountOfThePeople;
            }
            return Json(new { success = MaxSaleCount >= exist + products.count, maxSaleCount = MaxSaleCount, remain = MaxSaleCount - exist });
        }


        public ActionResult CustmerServices(long shopId,long productId=0)
        {
            var product = productId > 0 ? ProductManagerApplication.GetProduct(productId) : null;
            //排除掉移动端的客服
            var model = CustomerServiceApplication.GetPreSaleByShopId(shopId, true, CurrentUser).Where(c => c.TerminalType == Entities.CustomerServiceInfo.ServiceTerminalType.PC || c.Tool == CustomerServiceInfo.ServiceTool.HiChat).ToList();
            ViewBag.Keyword = SiteSettings.Keyword;
            return View(model);
        }

        public JsonResult GetShopInfo(long sid, long productId = 0)
        {
            string cacheKey = CacheKeyCollection.CACHE_SHOPINFO(sid, productId);
            if (Cache.Exists(cacheKey))
                return Cache.Get<JsonResult>(cacheKey);

            string brandLogo = string.Empty;
            long brandId = 0L;
            decimal cashDeposits = 0M;
            if (productId != 0)
            {
                var product = ObjectContainer.Current.Resolve<ProductService>().GetProduct(productId);
                if (product != null)
                {
                    var brand = ObjectContainer.Current.Resolve<BrandService>().GetBrand(product.BrandId);
                    brandLogo = brand == null ? string.Empty : brand.Logo;
                    brandId = brand == null ? brandId : brand.Id;
                }
            }
            var shop = ObjectContainer.Current.Resolve<ShopService>().GetShop(sid);
            var mark = Framework.ShopServiceMark.GetShopComprehensiveMark(sid);
            var cashDepositsInfo = ObjectContainer.Current.Resolve<CashDepositsService>().GetCashDepositByShopId(sid);
            if (cashDepositsInfo != null)
                cashDeposits = cashDepositsInfo.CurrentBalance;

            var cashDepositModel = ObjectContainer.Current.Resolve<CashDepositsService>().GetProductEnsure(productId);
            var model = new
            {
                CompanyName = shop.CompanyName,
                Id = sid,
                PackMark = mark.PackMark,
                ServiceMark = mark.ServiceMark,
                ComprehensiveMark = mark.ComprehensiveMark,
                Phone = shop.CompanyPhone,
                Name = shop.ShopName,
                Address = ObjectContainer.Current.Resolve<RegionService>().GetFullName(shop.CompanyRegionId),
                ProductMark = 3,
                IsSelf = shop.IsSelf,
                BrandLogo = Core.HimallIO.GetImagePath(brandLogo),
                BrandId = brandId,
                CashDeposits = cashDeposits,
                IsSevenDayNoReasonReturn = cashDepositModel.IsSevenDayNoReasonReturn,
                IsCustomerSecurity = cashDepositModel.IsCustomerSecurity,
                TimelyDelivery = cashDepositModel.IsTimelyShip
            };
            JsonResult result = Json(model, true);
            Cache.Insert(cacheKey, result, 600);
            return result;
        }
    }
}