﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Mobile.Models;
using Himall.Web.Areas.SellerAdmin.Models;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    //ToDO...VIEWbag太多
    public class LimitTimeBuyController : BaseMobileTemplatesController
    {
        private ShopCategoryService _iShopCategoryService;
        private LimitTimeBuyService _LimitTimeBuyService;
        private ShopService _ShopService;
        private ConsultationService _iConsultationService;
        private ProductService _ProductService;
        private VShopService _VShopService;
        private ProductDescriptionTemplateService _iProductDescriptionTemplateService;
        private CommentService _CommentService;
        private CustomerCustomerService _CustomerCustomerService;
        private TypeService _iTypeService;
        /// <summary>
        /// 限时购销售计数器
        /// </summary>
        private const string LIMIT_COUNT_TAG = "limitcount:";
        /// <summary>
        /// 限时购总库存
        /// </summary>
        private const string LIMIT_TOTAL_TAG = "limittotal:";

        public LimitTimeBuyController(ShopCategoryService ShopCategoryService,
        LimitTimeBuyService LimitTimeBuyService,
        ShopService ShopService,
        ProductService ProductService,
        CommentService CommentService,
        VShopService VShopService,
            ConsultationService ConsultationService,
        ProductDescriptionTemplateService ProductDescriptionTemplateService,
        CustomerCustomerService CustomerCustomerService, TypeService TypeService)
        {
            _iShopCategoryService = ShopCategoryService;
            _LimitTimeBuyService = LimitTimeBuyService;
            _ShopService = ShopService;
            _ProductService = ProductService;
            _CommentService = CommentService;
            _VShopService = VShopService;
            _iProductDescriptionTemplateService = ProductDescriptionTemplateService;
            _iConsultationService = ConsultationService;
            _CustomerCustomerService = CustomerCustomerService;
            _iTypeService = TypeService;
        }

        public ActionResult Home(long? ShopId, string catename = "")
        {
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
            var flashSaleConfig = _LimitTimeBuyService.GetConfig();
            ViewBag.Preheat = flashSaleConfig.Preheat;
            ViewBag.Cate = CateSelItem;
            if (ShopId.HasValue)
            {
                ViewBag.ShopId = ShopId.Value;
            }
            #region 初始化查询Model
            FlashSaleQuery query = new FlashSaleQuery()
            {
                CategoryName = catename,
                OrderKey = 5, /* 排序项（1：默认，2：销量，3：价格，4 : 结束时间,5:状态 开始排前面） */
                IsPreheat = true,
                PageNo = 1,
                PageSize = 14,
                AuditStatus = FlashSaleInfo.FlashSaleStatus.Ongoing,
                CheckProductStatus = true,
                ShopId = ShopId
            };

            #endregion
            var model = _LimitTimeBuyService.GetAll(query);

            ViewBag.Products = ProductManagerApplication.GetProducts(model.Models.Select(p => p.ProductId));
            return View(model);
        }

        [HttpPost]
        public ActionResult GetData(int index, int size, string cname)
        {
            #region 初始化查询Model
            FlashSaleQuery query = new FlashSaleQuery()
            {
                ItemName = cname,
                OrderKey = 5, /* 排序项（1：默认，2：销量，3：价格，4 : 结束时间,5:状态 开始排前面） */
                IsPreheat = true,
                PageNo = index,
                PageSize = size,
                AuditStatus = FlashSaleInfo.FlashSaleStatus.Ongoing,
                CheckProductStatus = true
            };

            #endregion
            var obj = _LimitTimeBuyService.GetAll(query);
            var products = ProductManagerApplication.GetProducts(obj.Models.Select(p => p.ProductId));
            List<FlashSaleModel> list = new List<FlashSaleModel>();
            foreach (var model in obj.Models)
            {
                var product = products.FirstOrDefault(p => p.Id == model.ProductId);
                FlashSaleModel result = new FlashSaleModel();
                result.Id = model.Id;
                result.Title = model.Title;
                result.ShopId = model.ShopId;
                result.ProductId = model.ProductId;
                result.Status = model.Status;
                result.ProductName = product.ProductName;
                result.ProductImg = Himall.Core.HimallIO.GetProductSizeImage(product.RelativePath, 1);
                result.MarketPrice = product.MarketPrice;
                result.BeginDate = model.BeginDate.ToString("yyyy-MM-dd HH:mm");
                result.EndDate = model.EndDate.ToString("yyyy-MM-dd HH:mm");
                result.LimitCountOfThePeople = model.LimitCountOfThePeople;
                result.SaleCount = model.SaleCount;
                result.CategoryName = model.CategoryName;
                result.MinPrice = model.MinPrice;
                list.Add(result);
            }
            return Json(new { success = true, data = list, total = obj.Total });
        }

        public ActionResult Detail(string id)
        {
            LimitTimeBuyDetailModel detailModel = new LimitTimeBuyDetailModel();
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
                return RedirectToAction("Error404", "Error", new { area = "Mobile" });
            }
            #endregion

            #region 初始化商品和店铺
            //参数是限时购活动ID
            try
            {
                market = _LimitTimeBuyService.Get(mid);
            }
            catch
            {
                market = null;
            }
            if (market != null)
            {
                switch (market.Status)
                {
                    case FlashSaleInfo.FlashSaleStatus.Ended:
                        return RedirectToAction("Detail", "Product", new { id = market.ProductId });
                    case FlashSaleInfo.FlashSaleStatus.Cancelled:
                        return RedirectToAction("Detail", "Product", new { id = market.ProductId });
                }

                model.FlashSale = market;
            }
            if (market == null || market.Status != FlashSaleInfo.FlashSaleStatus.Ongoing)
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
                market = _LimitTimeBuyService.Get(market.Id);
            }
            model.FlashSale = market;

            if (market != null && (market.Status != FlashSaleInfo.FlashSaleStatus.Ongoing || DateTime.Parse(market.EndDate) < DateTime.Now))
            {
                return RedirectToAction("Detail", "Product", new { id = market.ProductId });
            }

            model.MaxSaleCount = market.LimitCountOfThePeople;
            model.Title = market.Title;

            shop = _ShopService.GetShop(market.ShopId);

            #endregion

            #region 不存在的商品
            if (null == market || market.Id == 0)
            {
                //跳转到出错页面
                return RedirectToAction("Error404", "Error", new { area = "Web" });
            }
            #endregion

            #region 商品描述
            var product = _ProductService.GetProduct(market.ProductId);
            gid = market.ProductId;

            model.Product = product;
            var description = ProductManagerApplication.GetProductDescription(product.Id);
            model.ProductDescription = description.ShowMobileDescription;
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

            var mark = ShopServiceMark.GetShopComprehensiveMark(shop.Id);
            model.Shop.PackMark = mark.PackMark;
            model.Shop.ServiceMark = mark.ServiceMark;
            model.Shop.ComprehensiveMark = mark.ComprehensiveMark;
            model.Shop.Name = shop.ShopName;
            model.Shop.ProductMark = CommentApplication.GetProductAverageMark(gid);
            model.Shop.Id = product.ShopId;
            model.Shop.FreeFreight = shop.FreeFreight;
            detailModel.ProductNum = _ProductService.GetShopOnsaleProducts(product.ShopId);
            detailModel.FavoriteShopCount = _ShopService.GetShopFavoritesCount(product.ShopId);
            if (CurrentUser == null)
            {
                detailModel.IsFavorite = false;
                detailModel.IsFavoriteShop = false;
            }
            else
            {
                detailModel.IsFavorite = _ProductService.IsFavorite(product.Id, CurrentUser.Id);
                var favoriteShopIds = _ShopService.GetFavoriteShopInfos(CurrentUser.Id).Select(item => item.ShopId).ToArray();//获取已关注店铺
                detailModel.IsFavoriteShop = favoriteShopIds.Contains(product.ShopId);
            }
            #endregion

            #region 店铺分类

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

            #endregion

            #region 热门销售

            var sale = _ProductService.GetHotSaleProduct(shop.Id, 5);
            if (sale != null)
            {
                foreach (var item in sale.ToArray())
                {
                    model.HotSaleProducts.Add(new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = item.MinSalePrice,
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
            var skus = ProductManagerApplication.GetSKUs(product.Id);
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
                                    Img = Core.HimallIO.GetImagePath(sku.ShowPic)
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
                //var min = skus.Where(s => s.Stock >= 0).Min(s => s.SalePrice);
                //var max = skus.Where(s => s.Stock >= 0).Max(s => s.SalePrice);
                //if (min == 0 && max == 0)
                //{
                //    price = product.MinSalePrice.ToString("f2");
                //}
                //else if (max > min)
                //{
                //    price = string.Format("{0}-{1}", min.ToString("f2"), max.ToString("f2"));
                //}
                //else
                //{
                //    price = string.Format("{0}", min.ToString("f2"));
                //}
                price = ProductWebApplication.GetProductPriceStr2(product, skus);//最小价或区间价文本
            }
            detailModel.Price = string.IsNullOrWhiteSpace(price) ? product.MinSalePrice.ToString("f2") : price;
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
                    TypeAttributesModel attrModel = new TypeAttributesModel()
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
            detailModel.ProductAttrs = ProductAttrs;
            #endregion

            #region 获取评论、咨询数量

            var comments = CommentApplication.GetCommentsByProduct(product.Id);
            detailModel.CommentCount = comments.Count;

            var consultations = ObjectContainer.Current.Resolve<ConsultationService>().GetConsultations(gid);

            var total = comments.Count;
            var niceTotal = comments.Count(item => item.ReviewMark >= 4);
            detailModel.NicePercent = (int)((niceTotal / (double)total) * 100);
            detailModel.Consultations = consultations.Count();

            if (_VShopService.GetVShopByShopId(shop.Id) == null)
                detailModel.VShopId = -1;
            else
                detailModel.VShopId = _VShopService.GetVShopByShopId(shop.Id).Id;
            #endregion

            #region 累加浏览次数、 加入历史记录
            //if (CurrentUser != null)
            //{
            //    BrowseHistrory.AddBrowsingProduct(product.Id, CurrentUser.Id);
            //}
            //else
            //{
            //    BrowseHistrory.AddBrowsingProduct(product.Id);
            //}
            //_ProductService.LogProductVisti(gid);
            #endregion

            #region 获取店铺的评价统计
            var statistic = ShopApplication.GetStatisticOrderComment(product.ShopId);
            detailModel.ProductAndDescription = statistic.ProductAndDescription;
            detailModel.ProductAndDescriptionPeer = statistic.ProductAndDescriptionPeer;
            detailModel.ProductAndDescriptionMin = statistic.ProductAndDescriptionMin;
            detailModel.ProductAndDescriptionMax = statistic.ProductAndDescriptionMax;

            detailModel.SellerServiceAttitude = statistic.SellerServiceAttitude;
            detailModel.SellerServiceAttitudePeer = statistic.SellerServiceAttitudePeer;
            detailModel.SellerServiceAttitudeMax = statistic.SellerServiceAttitudeMax;
            detailModel.SellerServiceAttitudeMin = statistic.SellerServiceAttitudeMin;

            detailModel.SellerDeliverySpeed = statistic.SellerDeliverySpeed;
            detailModel.SellerDeliverySpeedPeer = statistic.SellerDeliverySpeedPeer;
            detailModel.SellerDeliverySpeedMax = statistic.SellerDeliverySpeedMax;
            detailModel.sellerDeliverySpeedMin = statistic.SellerDeliverySpeedMin;

            #endregion

            #region 是否收藏此商品
            if (CurrentUser != null && CurrentUser.Id > 0)
            {
                model.IsFavorite = _ProductService.IsFavorite(product.Id, CurrentUser.Id);
            }
            else
            {
                model.IsFavorite = false;
            }
            #endregion

            long vShopId;
            var vshopinfo = _VShopService.GetVShopByShopId(shop.Id);
            if (vshopinfo == null)
                vShopId = -1;
            else
                vShopId = vshopinfo.Id;
            detailModel.VShopId = vShopId;
            model.Shop.VShopId = vShopId;

            model.VShopLog = _VShopService.GetVShopLog(model.Shop.VShopId);
            if (string.IsNullOrWhiteSpace(model.VShopLog))
            {
                //throw new Himall.Core.HimallException("店铺未开通微店功能");
                model.VShopLog = SiteSettings.WXLogo;
            }
            detailModel.Logined = (null != CurrentUser) ? 1 : 0;
            model.EnabledBuy = product.AuditStatus == Entities.ProductInfo.ProductAuditStatus.Audited && DateTime.Parse(market.BeginDate) <= DateTime.Now && DateTime.Parse(market.EndDate) > DateTime.Now && product.SaleStatus == Entities.ProductInfo.ProductSaleStatus.OnSale;
            int saleCounts = 0;
            saleCounts = market.SaleCount;
            if (market.Status == FlashSaleInfo.FlashSaleStatus.Ongoing && DateTime.Parse(market.BeginDate) < DateTime.Now && DateTime.Parse(market.EndDate) > DateTime.Now)
            {
                TimeSpan end = new TimeSpan(DateTime.Parse(market.EndDate).Ticks);
                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                TimeSpan ts = end.Subtract(start);
                detailModel.Second = ts.TotalSeconds < 0 ? 0 : ts.TotalSeconds;
            }
            else if (market.Status == FlashSaleInfo.FlashSaleStatus.Ongoing && DateTime.Parse(market.BeginDate) > DateTime.Now)
            {
                TimeSpan end = new TimeSpan(DateTime.Parse(market.BeginDate).Ticks);
                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                TimeSpan ts = end.Subtract(start);
                detailModel.Second = ts.TotalSeconds < 0 ? 0 : ts.TotalSeconds;
                saleCounts = Himall.Core.Helper.TypeHelper.ObjectToInt(product.SaleCounts) + Himall.Core.Helper.TypeHelper.ObjectToInt(product.VirtualSaleCounts);
            }
            ViewBag.DetailModel = detailModel;

            var customerServices = CustomerServiceApplication.GetMobileCustomerServiceAndMQ(market.ShopId, true, CurrentUser, product);
            ViewBag.CustomerServices = customerServices;

            //统计商品浏览量、店铺浏览人数
            StatisticApplication.StatisticVisitCount(product.Id, product.ShopId);

            model.IsSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1;//是否显示销量
            model.SaleCount = saleCounts;//销量
            model.FreightTemplate = FreightTemplateApplication.GetFreightTemplate(product.FreightTemplateId);
            model.Freight = FreightTemplateApplication.GetFreightStr(market.ProductId, model.FreightTemplate, CurrentUser?.Id ?? 0, product.ProductType);//运费或免运费
            model.StockAll = market.Quantity;
            ViewBag.ProName = SubstrProcductname(product.ProductName);
            return View(model);
        }

        private string SubstrProcductname(string productname)
        {
            string result = productname;
            int length = 0;
            int index = 0;
            for (var i = 0; i < productname.Length; i++)
            {
                if (length > 20)
                {
                    break;
                }
                ASCIIEncoding asciiEncoding = new ASCIIEncoding();
                int intAsciiCode = (int)asciiEncoding.GetBytes(productname)[i];
                if (intAsciiCode >= 0 && intAsciiCode <= 128)
                {
                    length += 1;
                }
                else
                {
                    length += 2;
                }
                index = i;
            }
            if (index > 0 && length > 20)
            {
                result = productname.Substring(0, length);
            }
            return result;
        }

        [HttpPost]
        public ActionResult AddFavorite(long pid)
        {
            int state = 0;
            _ProductService.AddFavorite(pid, CurrentUser.Id, out state);
            if (state == 0)
            {
                return SuccessResult("成功关注");
            }
            return ErrorResult("关注失败");
        }

        [HttpPost]
        public ActionResult GetSkus(long id)
        {
            var model = _LimitTimeBuyService.Get(id);
            if (model != null)
            {
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
            var limitbuy = _LimitTimeBuyService.GetLimitTimeMarketItemByProductId(products.id);
            var maxSaleCount = 0;
            if (limitbuy != null)
            {
                maxSaleCount = limitbuy.LimitCountOfThePeople;
            }

            return Json(new { success = maxSaleCount >= exist + products.count, data = new { maxSaleCount = maxSaleCount, remain = maxSaleCount - exist, FlashSaleId = limitbuy.Id, ProductId = limitbuy.ProductId } });
        }
    }
}