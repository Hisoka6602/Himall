using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Extends;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.Live;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class TemplateVisualizationAjaxController : BaseAdminController
    {
        private BonusService _BonusService;
        private TopicService _iTopicService;
        private CouponService _CouponService;
        private LimitTimeBuyService _LimitTimeBuyService;
        private FightGroupService _FightGroupService;
        private ProductService _ProductService;
        private PhotoSpaceService _iPhotoSpaceService = null;
        private GiftService _iGiftService;
        private ShopService _ShopService;
        private VShopService _VShopService;
        private CategoryService _iCategoryService;
        public TemplateVisualizationAjaxController(
           BonusService BonusService,
            TopicService TopicService,
            CouponService CouponService,
            LimitTimeBuyService LimitTimeBuyService,
             ProductService ProductService,
            PhotoSpaceService PhotoSpaceService,
            GiftService GiftService,
            ShopService ShopService,
            VShopService VShopService,
            CategoryService CategoryService)
        {
            _BonusService = BonusService;
            _iTopicService = TopicService;
            _CouponService = CouponService;
            _LimitTimeBuyService = LimitTimeBuyService;
            _ProductService = ProductService;
            _iPhotoSpaceService = PhotoSpaceService;
            _iGiftService = GiftService;
            _ShopService = ShopService;
            _VShopService = VShopService;
            _iCategoryService = CategoryService;
        }


        #region Hi_Ajax_GetAppHomeGiftList

        public ActionResult Hi_Ajax_GetAppHomeGiftList(int status = 2, string title = "", int p = 1)
        {
            int pageNo = p;
            GiftAjaxModel model = new GiftAjaxModel() { list = new List<GiftContent>() };
            InitialAppHomeGiftModel(model, status, title, pageNo);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialAppHomeGiftModel(GiftAjaxModel model, int status, string name, int pageNo)
        {
            var giftList = _iGiftService.GetGifts(new GiftQuery
            {
                isShowAll = false,
                status = GiftInfo.GiftSalesStatus.Normal,
                skey = name,
                PageNo = pageNo,
                PageSize = 10,
                IsAsc = true
            });
            int pageCount = TemplatePageHelper.GetPageCount(giftList.Total, 10);

            if (giftList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialAppHomeGiftContentModel(giftList.Models, model);
            }
        }

        private void InitialAppHomeGiftContentModel(IEnumerable<GiftModel> giftList, GiftAjaxModel model)
        {
            foreach (var gift in giftList)
            {
                model.list.Add(new GiftContent
                {
                    item_id = gift.Id,
                    link = "/Gift/Detail/" + gift.Id,
                    pic = HimallIO.GetRomoteImagePath(gift.GetImage(ImageSize.Size_350)),
                    title = gift.GiftName,
                    adddate = gift.AddDate.ToString("yyyy-MM-dd"),
                    enddate = gift.EndDate.ToString("yyyy-MM-dd"),
                    limtquantity = gift.ShowLimtQuantity,
                    needintegral = gift.NeedIntegral.ToString(),
                    realsales = gift.RealSales.ToString(),
                    stockquantity = gift.StockQuantity.ToString()
                });
            }
        }

        #endregion

        #region Hi_Ajax_GetAppHomeGoodsList
        public ActionResult Hi_Ajax_GetAppHomeGoodsList(int status = 2, string title = "", int p = 1, long categoryId = 0, string shopName = "", int size = 10, string sort = "Id", bool isasc = false)
        {
            int pageNo = p;
            ProductAjaxModel model = new ProductAjaxModel() { list = new List<ProductContent>() };
            InitialAppHomeProductModel(model, status, title, pageNo, categoryId, shopName, size, sort, isasc);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialAppHomeProductModel(ProductAjaxModel model, int status, string name, int pageNo, long categoryId = 0, string shopName = "", int size = 10, string sort = "Id", bool isasc = false)
        {
            var query = new ProductQuery
            {
                AuditStatus = new ProductInfo.ProductAuditStatus[] { (ProductInfo.ProductAuditStatus)status },
                KeyWords = name,
                PageNo = pageNo,
                PageSize = size,
                Sort = sort,
                IsAsc = isasc,
                ShopName = shopName
            };
            if (categoryId > 0)
            {
                query.CategoryId = categoryId;
            };
            var products = ProductManagerApplication.GetProducts(query);
            int pageCount = TemplatePageHelper.GetPageCount(products.Total, 10);
            List<Entities.SKUInfo> skuItems = new List<Entities.SKUInfo>();
            if (products != null && products.Models.Count > 0)
            {
                skuItems = ProductManagerApplication.GetSKUsByProduct(products.Models.Select(p => p.Id));
            }
            model.status = 1;
            model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
            foreach (var product in products.Models)
            {
                var proSkuItems = skuItems.Where(p => p.ProductId == product.Id);
                model.list.Add(new ProductContent
                {
                    create_time = "",
                    item_id = product.Id,
                    link = "/m-wap/product/detail/" + product.Id,
                    pic = HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_350),
                    title = product.ProductName,
                    price = product.MinSalePrice.ToString("F2"),
                    original_price = product.MarketPrice.ToString("F2"),
                    is_compress = "0",
                    desc = product.ShortDescription,
                    Stock = proSkuItems == null ? 0 : proSkuItems.Sum(s => s.Stock)
                });
            }
        }
        #endregion

        #region Hi_Ajax_Bonus
        public ActionResult Hi_Ajax_Bonus(string title = "", int p = 1, PlatformType platform = PlatformType.Mobile)
        {
            int pageNo = p;
            BonusAjaxModel model = new BonusAjaxModel() { list = new List<BonusContent>() };
            InitialBonusModel(model, title, pageNo, platform);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialBonusModel(BonusAjaxModel model, string name = "", int pageNo = 1, PlatformType platform = PlatformType.Mobile)
        {
            var query = new BonusQuery
            {
                Type = BonusInfo.BonusType.Activity,
                State = 1,
                Name = name,
                PageSize = 10,
                PageNo = pageNo
            };
            var brandList = _BonusService.Get(query);
            int pageCount = TemplatePageHelper.GetPageCount(brandList.Total, 10);
            if (brandList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialBonusContentModel(brandList.Models, model, platform);
            }
        }

        private bool IsWXSamllProgram(PlatformType platform)
        {
            if (platform == PlatformType.WeiXinSmallProg)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void InitialBonusContentModel(IEnumerable<Himall.Entities.BonusInfo> bonusList, BonusAjaxModel model, PlatformType platform = PlatformType.Mobile)
        {

            foreach (var bouns in bonusList)
            {
                model.list.Add(new BonusContent
                {
                    create_time = "",
                    item_id = bouns.Id,
                    link = IsWXSamllProgram(platform) ? "/pages/userasset/userasset?id=" + bouns.Id.ToString() : "/m-weixin/bonus/index/" + bouns.Id.ToString(),
                    pic = Core.HimallIO.GetRomoteImagePath(bouns.ImagePath),

                    title = bouns.Name,
                    endTime = bouns.EndTime.ToShortDateString(),
                    startTime = bouns.StartTime.ToShortDateString(),
                    price = bouns.TotalPrice.ToString()
                });
            }
        }

        #endregion

        #region Hi_Ajax_Topic
        public ActionResult Hi_Ajax_Topic(string title = "", int p = 1, PlatformType platform = PlatformType.Mobile)
        {
            int pageNo = p;
            TopicAjaxModel model = new TopicAjaxModel() { list = new List<TopicContent>() };
            InitialCateModel(model, title, pageNo, platform);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialCateModel(TopicAjaxModel model, string name, int pageNo, PlatformType platform)
        {
            var topicList = _iTopicService.GetTopics(new TopicQuery
            {
                PageNo = pageNo,
                PageSize = 10,
                PlatformType = platform,
                Name = name
            });
            int pageCount = TemplatePageHelper.GetPageCount(topicList.Total, 10);

            if (topicList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialCateContentModel(topicList.Models, model, platform);
            }
        }
        private void InitialCateContentModel(IEnumerable<Entities.TopicInfo> topicList, TopicAjaxModel model, PlatformType platform)
        {

            foreach (var topic in topicList)
            {
                model.list.Add(new TopicContent
                {
                    create_time = "",
                    item_id = topic.Id,
                    link = IsWXSamllProgram(platform) ? "/pages/topic/topic?id=" + topic.Id : "/m-wap/topic/detail/" + topic.Id,
                    pic = "",
                    title = topic.Name,
                    tag = topic.Tags
                });
            }
        }

        #endregion

        #region Hi_Ajax_Coupons
        public ActionResult Hi_Ajax_Coupons(int p = 1, long shopId = -1, string title = "", int size = 0, PlatformType platform = PlatformType.Mobile)
        {
            int pageNo = p;
            CouponsAjaxModel model = new CouponsAjaxModel() { list = new List<CouponsContent>() };
            InitialCouponsModel(model, shopId, size, title, pageNo, platform);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialCouponsModel(CouponsAjaxModel model, long shopId, int size, string name = "", int pageNo = 1, PlatformType platform = PlatformType.Mobile)
        {
            var couponsList = _CouponService.GetCouponList(new
            Himall.DTO.QueryModel.CouponQuery
            {
                CouponName = name,
                IsOnlyShowNormal = true,
                IsShowAll = false,
                ShowPlatform = Himall.Core.PlatformType.Wap,
                ShopId = shopId,
                PageNo = pageNo,
                PageSize = size > 0 ? size : 10,
                ReceiveType = CouponInfo.CouponReceiveType.ShopIndex
            });

            int pageCount = TemplatePageHelper.GetPageCount(couponsList.Total, 10);

            if (couponsList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialCouponsContentModel(couponsList.Models, model, platform);
            }
        }
        private void InitialCouponsContentModel(List<Entities.CouponInfo> couponsList, CouponsAjaxModel model, PlatformType platform = PlatformType.Mobile)
        {

            foreach (var coupon in couponsList)
            {
                model.list.Add(new CouponsContent
                {
                    create_time = coupon.CreateTime.ToString(),
                    game_id = coupon.Id,
                    link = IsWXSamllProgram(platform) ? "/pages/coupondetail/coupondetail?id=" + coupon.Id : "/m-wap/vshop/CouponInfo/" + coupon.Id,
                    pc_link = "/m-wap/vshop/CouponInfo/" + coupon.Id,
                    type = 1,
                    title = coupon.CouponName,
                    condition = coupon.OrderAmount.ToString(),
                    endTime = coupon.EndTime.ToShortDateString(),
                    price = coupon.Price.ToString(),
                    shopName = coupon.ShopName
                });
            }
        }

        #endregion

        #region 小程序 优惠劵
        public ActionResult Hi_Ajax_SmallProgCoupons(int p = 1, int size = 10, long shopId = 1, string title = "")
        {
            int pageNo = p;
            CouponsAjaxModel model = new CouponsAjaxModel() { list = new List<CouponsContent>() };
            SmallProInitialCouponsModel(model, size, shopId, title, pageNo);
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private void SmallProInitialCouponsModel(CouponsAjaxModel model, int size, long shopId, string name = "", int pageNo = 1)
        {
            var couponsList = _CouponService.GetCouponList(new
            Himall.DTO.QueryModel.CouponQuery
            {
                CouponName = name,
                IsOnlyShowNormal = true,
                IsShowAll = false,
                ShowPlatform = Himall.Core.PlatformType.Wap,
                PageNo = pageNo,
                PageSize = size
            });
            int pageCount = TemplatePageHelper.GetPageCount(couponsList.Total, size);

            if (couponsList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                SmallProInitialCouponsContentModel(couponsList.Models, model);
            }
        }
        private void SmallProInitialCouponsContentModel(List<Entities.CouponInfo> couponsList, CouponsAjaxModel model)
        {

            foreach (var coupon in couponsList)
            {
                model.list.Add(new CouponsContent
                {
                    create_time = coupon.CreateTime.ToString(),
                    game_id = coupon.Id,
                    link = "/pages/coupondetail/coupondetail?id=" + coupon.Id,
                    pc_link = "/m-wap/vshop/CouponInfo/" + coupon.Id,
                    type = 1,
                    title = coupon.CouponName,
                    condition = coupon.OrderAmount.ToString(),
                    endTime = coupon.EndTime.ToShortDateString(),
                    price = coupon.Price.ToString(),
                    shopName = coupon.ShopName
                });
            }
        }
        #endregion

        #region Hi_Ajax_GetSmallProgGoodsList
        public ActionResult Hi_Ajax_GetSmallProgGoodsList(string sort = "Id", bool isasc = true, int size = 10, int status = 2, string title = "", int p = 1, long categoryId = 0, string shopName = "")
        {
            int pageNo = p;
            ProductAjaxModel model = new ProductAjaxModel() { list = new List<ProductContent>() };
            InitialSmallProgProductModel(model, status, title, pageNo, sort, isasc, size, categoryId, shopName);
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        //TODO:FG 多个Initial高度相似
        private void InitialSmallProgProductModel(ProductAjaxModel model, int status, string name, int pageNo, string sort, bool isasc, int size, long categoryId = 0, string shopName = "")
        {
            var query = new ProductQuery
            {
                AuditStatus = new ProductInfo.ProductAuditStatus[] { (ProductInfo.ProductAuditStatus)status },
                KeyWords = name,
                PageNo = pageNo,
                PageSize = size,
                Sort = sort,
                IsAsc = isasc,
                ShopName = shopName
            };
            if (categoryId > 0)
            {
                query.CategoryId = categoryId;
            };
            var productList = ProductManagerApplication.GetProducts(query);
            int pageCount = TemplatePageHelper.GetPageCount(productList.Total, size);

            List<Entities.SKUInfo> skuItems = new List<Entities.SKUInfo>();
            if (productList != null && productList.Models.Count > 0)
            {
                skuItems = ProductManagerApplication.GetSKUsByProduct(productList.Models.Select(p => p.Id));
            }
            model.status = 1;
            model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
            foreach (var product in productList.Models)
            {
                var proSkuItems = skuItems.Where(p => p.ProductId == product.Id);
                model.list.Add(new ProductContent
                {
                    create_time = "",
                    item_id = product.Id,
                    link = "../productdetail/productdetail?id=" + product.Id.ToString(),
                    pc_link = "/Product/Detail/" + product.Id.ToString(),
                    pic = Himall.Core.HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_350),
                    title = product.ProductName,
                    price = product.MinSalePrice.ToString("F2"),
                    original_price = product.MarketPrice.ToString("F2"),
                    is_compress = "0",
                    desc = product.ShortDescription.ToNullString(),
                    product_id = product.Id,
                    Stock = proSkuItems == null ? 0 : proSkuItems.Sum(s => s.Stock)
                });
            }
        }

        #endregion

        #region Hi_Ajax_GetGoodsList
        public ActionResult Hi_Ajax_GetGoodsList(int status = 2, string title = "", int p = 1, long categoryId = 0, string shopName = "", PlatformType platform = PlatformType.Mobile, string sort = "Id", string isasc = "true", int size = 10)
        {
            int pageNo = p;
            ProductAjaxModel model = new ProductAjaxModel() { list = new List<ProductContent>() };
            InitialProductModel(model, status, title, pageNo, sort, bool.Parse(isasc), size, categoryId, shopName, platform);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialProductModel(ProductAjaxModel model, int status, string name, int pageNo, string sort, bool isasc, int size, long categoryId = 0, string shopName = "", PlatformType platform = PlatformType.Mobile)
        {
            var query = new ProductQuery
            {
                AuditStatus = new ProductInfo.ProductAuditStatus[] { (Entities.ProductInfo.ProductAuditStatus)status },
                SaleStatus = ProductInfo.ProductSaleStatus.OnSale,
                KeyWords = name,
                PageNo = pageNo,
                PageSize = size,
                Sort = sort,
                IsAsc = isasc,
                ShopName = shopName
            };
            if (categoryId > 0)
            {
                query.CategoryId = categoryId;
            }
            var productList = ProductManagerApplication.GetProducts(query);
            int pageCount = TemplatePageHelper.GetPageCount(productList.Total, 10);
            var productSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1;
            List<Entities.SKUInfo> skuItems = new List<Entities.SKUInfo>();
            if (productList != null && productList.Models.Count > 0)
            {
                skuItems = ProductManagerApplication.GetSKUsByProduct(productList.Models.Select(p => p.Id));
            }
            if (productList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                foreach (var product in productList.Models)
                {
                    var proSkuItems = skuItems.Where(p => p.ProductId == product.Id);
                    var newpic = product.GetImage(ImageSize.Size_350);
                    if (IsWXSamllProgram(platform))
                    {
                        newpic = Core.HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)Himall.CommonModel.ImageSize.Size_350);
                    }
                    model.list.Add(new ProductContent
                    {

                        create_time = "",
                        item_id = product.Id,
                        link = IsWXSamllProgram(platform) ? "/pages/productdetail/productdetail?id=" + product.Id.ToString() : "/m-wap/Product/Detail/" + product.Id.ToString(),
                        pic = newpic,
                        title = product.ProductName,
                        desc = product.ShortDescription.ToNullString(),
                        price = product.MinSalePrice.ToString("F2"),
                        original_price = product.MarketPrice.ToString("F2"),
                        is_compress = "0",
                        SaleCounts = product.SaleCounts,
                        ProductSaleCountOnOff = productSaleCountOnOff,
                        productType = product.ProductType,
                        Stock = proSkuItems == null ? 0 : proSkuItems.Sum(s => s.Stock)
                    });
                }
            }
        }

        #endregion

        #region Hi_Ajax_LimitBuy
        public ActionResult Hi_Ajax_LimitBuy(int p = 1, long shopId = 1, string title = "", PlatformType platform = PlatformType.Mobile)
        {
            int pageNo = p;
            LimitBuyAjaxModel model = new LimitBuyAjaxModel() { list = new List<LimitBuyContent>() };
            InitialLimitBuyModel(model, shopId, title, pageNo, platform);
            return Json(model, JsonRequestBehavior.AllowGet);
        }



        private void InitialLimitBuyModel(LimitBuyAjaxModel model, long shopId, string name = "", int pageNo = 1, PlatformType platform = PlatformType.Mobile)
        {
            var limitBuyList = _LimitTimeBuyService.GetAll(
                new FlashSaleQuery
                {
                    ItemName = name,
                    ShopId = null,    //取所有
                    PageNo = pageNo,
                    PageSize = 10,
                    AuditStatus = FlashSaleInfo.FlashSaleStatus.Ongoing,
                    CheckProductStatus = true
                });
            int pageCount = TemplatePageHelper.GetPageCount(limitBuyList.Total, 10);

            if (limitBuyList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialLimitBuyContentModel(limitBuyList.Models, model, platform);
            }
        }
        private void InitialLimitBuyContentModel(IEnumerable<FlashSaleInfo> limitBuyList, LimitBuyAjaxModel model, PlatformType platform = PlatformType.Mobile)
        {
            var datalist = limitBuyList.ToList();
            var products = ProductManagerApplication.GetProducts(datalist.Select(p => p.ProductId));
            var flashskus = LimitTimeApplication.GetFlashSaleDetailByFlashSaleIds(limitBuyList.Select(f => f.Id));//获取限时购规格
            var shops = ShopApplication.GetShops(datalist.Select(p => p.ShopId));
            foreach (var limitBuy in limitBuyList)
            {
                long stime = DateTimeHelper.ToSeconds(limitBuy.BeginDate);
                long etime = DateTimeHelper.ToSeconds(limitBuy.EndDate);
                var product = products.FirstOrDefault(p => p.Id == limitBuy.ProductId);
                var sku = flashskus.Where(s => s.ProductId == limitBuy.ProductId);
                var shop = shops.FirstOrDefault(p => p.Id == limitBuy.ShopId);
                int count = 0;

                foreach (var item in sku)
                {
                    count += item.TotalCount;
                }
                var newpic = product.GetImage(ImageSize.Size_350);
                if (IsWXSamllProgram(platform))
                {
                    newpic = Core.HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)Himall.CommonModel.ImageSize.Size_350);
                }
                model.list.Add(new LimitBuyContent
                {
                    create_time = "",
                    item_id = limitBuy.Id,
                    pid = limitBuy.ProductId,
                    link = IsWXSamllProgram(platform) ? "/pages/countdowndetail/countdowndetail?id=" + limitBuy.Id + "?productId=" + limitBuy.ProductId : "/m-wap/limittimebuy/detail/" + limitBuy.Id + "?productId=" + limitBuy.ProductId,
                    title = product.ProductName,
                    endTime = limitBuy.EndDate.ToShortDateString(),
                    startTime = limitBuy.BeginDate.ToShortDateString(),
                    price = limitBuy.MinPrice.ToString(),
                    saleprice = product.MarketPrice.ToString("F2"),
                    shopName = shop.ShopName,
                    beginSec = stime,
                    endSec = etime,
                    sellingPoint = product.ShortDescription,
                    pic = newpic,
                    number = limitBuy.SaleCount,//已售出数量
                    stock = count,//剩余库存
                });

            }
        }

        #endregion

        #region  Hi_Ajax_FightGroup

        public ActionResult Hi_Ajax_FightGroup(int p = 1, long shopId = 0, string title = "", PlatformType platform = PlatformType.Mobile, string shopname = "", long categoryId = 0)
        {
            FightGroupAjaxModel model = new FightGroupAjaxModel() { list = new List<FightGroupContent>() };

            List<FightGroupActiveStatus> fightstatus = new List<FightGroupActiveStatus>();
            fightstatus.Add(FightGroupActiveStatus.Ongoing);
            fightstatus.Add(FightGroupActiveStatus.WillStart);
            FightGroupActiveQuery query = new FightGroupActiveQuery()
            {
                PageNo = p < 0 ? 1 : p,
                PageSize = 10,
                ActiveStatusList = fightstatus,
                ShopId = shopId,
                ShopName = shopname,
                CategoryId = categoryId,
                ProductName = title,
            };

            InitialFightGroupModel(model, query, platform);

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialFightGroupModel(FightGroupAjaxModel model, FightGroupActiveQuery query, PlatformType platform)
        {
            var fightgrouplist = FightGroupApplication.GetActives(query);

            int pageCount = TemplatePageHelper.GetPageCount(fightgrouplist.Total, query.PageSize);

            if (fightgrouplist != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, query.PageNo);
                InitialFightGroupContentModel(fightgrouplist.Models, model, platform);
            }
        }

        private void InitialFightGroupContentModel(IEnumerable<FightGroupActiveListModel> fightgroupList, FightGroupAjaxModel model, PlatformType platform)
        {
            var datalist = fightgroupList.ToList();
            var products = ProductManagerApplication.GetProducts(datalist.Select(p => p.ProductId));
            var shops = ShopApplication.GetShops(datalist.Select(p => p.ShopId));
            foreach (var fightGroup in fightgroupList)
            {
                var product = products.FirstOrDefault(p => p.Id == fightGroup.ProductId);
                var shop = shops.FirstOrDefault(p => p.Id == fightGroup.ShopId);
                long stime = DateTimeHelper.ToSeconds(fightGroup.StartTime);
                long etime = DateTimeHelper.ToSeconds(fightGroup.EndTime);
                var newpic = product.GetImage(ImageSize.Size_350);
                if (IsWXSamllProgram(platform))
                {
                    newpic = Core.HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)Himall.CommonModel.ImageSize.Size_350);
                }
                model.list.Add(new FightGroupContent
                {
                    create_time = "",
                    item_id = fightGroup.Id,
                    pid = fightGroup.ProductId,
                    link = IsWXSamllProgram(platform) ? "/pages/grouporderdetail/grouporderdetail?id=" + fightGroup.Id + "?productId=" + fightGroup.ProductId : "/m-wap/fightgroup/detail/" + fightGroup.Id,
                    title = product.ProductName,
                    beginSec = stime,
                    endSec = etime,
                    endTime = fightGroup.EndTime.ToShortDateString(),
                    startTime = fightGroup.StartTime.ToShortDateString(),
                    price = fightGroup.MiniGroupPrice.ToString(),
                    shopName = shop.ShopName,
                    number = fightGroup.LimitedNumber,
                    saleprice = fightGroup.MiniSalePrice.ToString("F2"),
                    pic = newpic
                });
            }
        }
        #endregion

        #region Hi_Ajax_SmallProgLimitBuy
        public ActionResult Hi_Ajax_SmallProgLimitBuy(int p = 1, long shopId = 1, string title = "")
        {
            int pageNo = p;
            LimitBuyAjaxModel model = new LimitBuyAjaxModel() { list = new List<LimitBuyContent>() };
            InitialSmallProgLimitBuyModel(model, shopId, title, pageNo);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialSmallProgLimitBuyModel(LimitBuyAjaxModel model, long shopId, string name = "", int pageNo = 1)
        {
            var limitBuyList = _LimitTimeBuyService.GetAll(
                new FlashSaleQuery
                {
                    ItemName = name,
                    ShopId = null,    //取所有
                    PageNo = pageNo,
                    PageSize = 10,
                    AuditStatus = FlashSaleInfo.FlashSaleStatus.Ongoing,
                    CheckProductStatus = true
                });
            int pageCount = TemplatePageHelper.GetPageCount(limitBuyList.Total, 10);

            if (limitBuyList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialSmallProgLimitBuyContentModel(limitBuyList.Models, model);
            }
        }
        private void InitialSmallProgLimitBuyContentModel(IEnumerable<FlashSaleInfo> limitBuyList, LimitBuyAjaxModel model)
        {
            var datalist = limitBuyList.ToList();
            var products = ProductManagerApplication.GetProducts(datalist.Select(p => p.ProductId));
            var shops = ShopApplication.GetShops(datalist.Select(p => p.ShopId));
            var flashskus = LimitTimeApplication.GetFlashSaleDetailByFlashSaleIds(limitBuyList.Select(f => f.Id));//获取限时购规格
            foreach (var limitBuy in limitBuyList)
            {

                long stime = DateTimeHelper.ToSeconds(limitBuy.BeginDate);
                long etime = DateTimeHelper.ToSeconds(limitBuy.EndDate);

                var sku = flashskus.Where(s => s.ProductId == limitBuy.ProductId);
                int count = 0;
                foreach (var item in sku)
                {
                    count += item.TotalCount;
                }
                var product = products.FirstOrDefault(p => p.Id == limitBuy.ProductId);
                var shop = shops.FirstOrDefault(p => p.Id == limitBuy.ShopId);
                model.list.Add(new LimitBuyContent
                {
                    create_time = "",
                    item_id = limitBuy.Id,
                    link = "../countdowndetail/countdowndetail?id=" + limitBuy.Id,
                    pc_link = "/LimitTimeBuy/Detail/" + limitBuy.Id,
                    title = product.ProductName,
                    endTime = limitBuy.EndDate.ToShortDateString(),
                    startTime = limitBuy.BeginDate.ToShortDateString(),
                    price = limitBuy.MinPrice.ToString(),
                    shopName = shop.ShopName,
                    beginSec = stime,
                    endSec = etime,
                    sellingPoint = product.ShortDescription,
                    pic = HimallIO.GetRomoteImagePath(limitBuy.ImagePath).Replace("http://", "https://"),
                    number = limitBuy.SaleCount,//已售出数量
                    stock = count - limitBuy.SaleCount,//剩余库存
                });
            }
        }

        #endregion

        #region Hi_Ajax_SaveTemplate
        [ValidateInput(false)]
        public ActionResult Hi_Ajax_SaveTemplate(int is_preview, string client, string content = "", int type = 1, long shopId = 0)
        {
            string dataName = client;
            string json = content;
            VTemplateClientTypes clientType = (VTemplateClientTypes)type;
            if (clientType == VTemplateClientTypes.WXSmallProgram || clientType == VTemplateClientTypes.WXSmallProgramSpecial)
            {
                json = json.Replace("http://", "https://");//小程序里路径全用https路径访问

            }

            JObject jo = (JObject)JsonConvert.DeserializeObject(json);
            var jpage = jo["page"];
            string title = TryGetJsonString(jpage, "title");
            string describe = TryGetJsonString(jpage, "describe");
            string tags = TryGetJsonString(jpage, "tags");
            string icon = TryGetJsonString(jpage, "icon");

            string pagetitle = SiteSettings.SiteName + "首页";
            if (clientType == VTemplateClientTypes.WapSpecial || clientType == VTemplateClientTypes.WXSmallProgramSpecial || clientType == VTemplateClientTypes.AppSpecial)
            {
                int topicid = 0;
                if (!int.TryParse(client, out topicid))
                {
                    topicid = 0;
                }
                icon = SaveIcon(icon, clientType);
                if (!string.IsNullOrWhiteSpace(icon))
                {
                    //回写
                    jo["page"]["icon"] = Himall.Core.HimallIO.GetImagePath(icon);
                    json = JsonConvert.SerializeObject(jo);
                }
                PlatformType platform = PlatformType.Mobile;
                if (clientType == VTemplateClientTypes.WXSmallProgramSpecial)
                { platform = PlatformType.WeiXinSmallProg; }
                else if (clientType == VTemplateClientTypes.AppSpecial)
                {
                    platform = PlatformType.IOS;
                }
                client = AddOrUpdateTopic(topicid, title, tags, icon, platform).ToString();
                if (clientType != VTemplateClientTypes.WXSmallProgramSpecial)
                {
                    string basetemp = Server.MapPath(VTemplateHelper.GetTemplatePath("0", clientType));
                    string _curtemp = Server.MapPath(VTemplateHelper.GetTemplatePath(client, clientType));
                    if (!System.IO.Directory.Exists(_curtemp))
                    {
                        Core.HimallIO.CopyFolder(basetemp, _curtemp, true);
                    }
                }
                pagetitle = "专题-" + title;

                #region //操作日志
                string strdesc = string.Empty;
                if (clientType == VTemplateClientTypes.WapSpecial)
                    strdesc += "移动端";
                else if (clientType == VTemplateClientTypes.WXSmallProgramSpecial)
                    strdesc += "小程序";
                else if (clientType == VTemplateClientTypes.AppSpecial)
                    strdesc += "App";
                strdesc = (topicid <= 0 ? "增加" : "修改") + strdesc + "【" + pagetitle + "】tName=" + client;

                //操作日志
                OperationLogApplication.AddPlatformOperationLog(new Entities.LogInfo
                {
                    Date = DateTime.Now,
                    Description = strdesc,
                    IPAddress = Request.UserHostAddress,
                    PageUrl = "/Admin/TemplateVisualizationAjax/Hi_Ajax_SaveTemplate",
                    UserName = CurrentManager.UserName,
                    ShopId = 0

                });
                #endregion
            }

            string templatePath = VTemplateHelper.GetTemplatePath(client, clientType, shopId);
            string datapath = templatePath + "data/default.json";
            string cshtmlpath = templatePath + "Skin-HomePage.cshtml";

            string msg = "保存成功";
            string status = "1";
            try
            {
                Core.HimallIO.CreateFile(datapath, json, FileCreateType.Create);
                //小程序专题只需要保存json文件
                //if (clientType != VTemplateClientTypes.WXSmallProgramSpecial)
                //{
                //    StringBuilder htmlsb = new StringBuilder();
                //    if (clientType == VTemplateClientTypes.WapSpecial)
                //    {
                //        htmlsb.Append("@model Himall.Entities.TopicInfo \n");
                //    }
                //    htmlsb.Append("@{\n");
                //    htmlsb.Append("ViewBag.ShowAside = 2;//显示返回顶部按钮 \n");
                //    htmlsb.Append("}\n");
                //    htmlsb.Append("@{Layout = \"/Areas/Mobile/Templates/Default/Views/Shared/_Base.cshtml\";}\n");
                //    htmlsb.Append("<div class=\"container\">\n");
                //    htmlsb.Append("<script src=\"https://res.wx.qq.com/open/js/jweixin-1.3.2.js\"></script>\n");
                //    if (clientType != VTemplateClientTypes.WapSpecial)
                //    {
                //        htmlsb.Append("@{Html.RenderPartial(\"~/Areas/Mobile/Templates/Default/Views/Shared/_SearchBox.cshtml\");}\n");
                //    }
                //    if (clientType == VTemplateClientTypes.WXSmallProgramSpecial)
                //    {
                //        htmlsb.Append("@{Html.RenderPartial(\"~/Areas/Mobile/Templates/Default/Views/Topic/AppletTopicExtend.cshtml\");}\n");
                //        htmlsb.Append("<style>#footerbt {display: none;}</style>");
                //    }
                //    htmlsb.Append("<script src=\"~/Areas/Mobile/Templates/Default/Scripts/WeiXinShare.js\"></script>\n");
                //    htmlsb.Append("<link rel=\"stylesheet\" href=\"/Content/PublicMob/css/style.css\" />\n");
                //    htmlsb.Append("<link rel=\"stylesheet\" href=\"/Areas/Admin/templates/common/style/mycss.css\" rev=\"stylesheet\" type=\"text/css\">\n");
                //    htmlsb.Append("<link rel=\"stylesheet\" href=\"/Areas/Admin/templates/common/style/head.css\">\n");

                //    htmlsb.Append(GetPModulesHtml(jo));
                //    string lModuleHtml = GetLModulesHtml(jo, dataName);
                //    htmlsb.Append(lModuleHtml);
                //    htmlsb.Append("@{Html.RenderPartial(\"~/Areas/Mobile/Templates/Default/Views/Shared/_4ButtonsFoot.cshtml\");}\n");
                //    htmlsb.Append("</div>\n");
                //    htmlsb.Append("<script src=\"~/Areas/Mobile/Templates/Default/Scripts/mui.min.js\"></script>\n");
                //    htmlsb.Append("<script src=\"~/Areas/Mobile/Templates/Default/Scripts/AppAuto.js\"></script>\n");
                //    htmlsb.Append("<script src=\"~/Areas/Mobile/Templates/Default/Scripts/home.js\"></script>\n");
                //    htmlsb.Append(" <script src=\"~/Scripts/swipe-template.js\"></script>\n");
                //    if (clientType == VTemplateClientTypes.WapSpecial)
                //    {//专题页面分享设置
                //        htmlsb.Append("@{Html.RenderPartial(\"~/Areas/Mobile/Templates/Default/Views/Topic/TopicDetailShare.cshtml\");}\n");
                //    }
                //    else
                //    {//首页分享设置
                //        htmlsb.Append("<script src=\"~/Areas/Mobile/Templates/Default/Scripts/IndexShare.js\"></script>\n");
                //    }
                //    string html = htmlsb.ToString();

                //    Core.HimallIO.CreateFile(cshtmlpath, html, FileCreateType.Create);
                //    VTemplateHelper.ClearCache(client, VTemplateClientTypes.WapIndex);
                //}
                MvcApplication.WapIndexCache = DateTime.Now.ToFileTime().ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex.StackTrace + "    " + ex.Message);
                msg = ex.Message;
                status = "0";
            }
            if (is_preview == 1)
                return Json(new { status = status, msg = msg, link = "/m-wap/?ispv=1&tn=" + dataName, tname = dataName }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { status = status, msg = "", link = "/admin/VTemplate/EditTemplate?client=" + type.ToString() + "&tName=" + client, tname = client }, JsonRequestBehavior.AllowGet);


        }

        public string GetPModulesHtml(JObject jo)
        {

            string templateHtml = "";
            foreach (var module in jo["PModules"])
            {
                templateHtml += Base64Decode(module["dom_conitem"].ToString());
            }
            return templateHtml;

        }

        public string GetLModulesHtml(JObject jo, string template)
        {
            string templateHtml = "";
            StringBuilder tmpsb = new StringBuilder();
            int lRecords = 1;
            string content = string.Empty;
            foreach (var module in jo["LModules"])
            {
                string mtype = module.TryGetJsonString("type");
                switch (mtype)
                {
                    case "3"://富文本
                        content = Base64Decode(module["dom_conitem"].ToString());
                        content = content.Replace("@", "&#64;");
                        tmpsb.Append(content);
                        break;
                    default:
                        //if (lRecords > firstPageNum)
                        //{
                        //    tmpsb.Append("<div class=\"scrollLoading\" data-url=\"/m-wap/Home/GetTemplateItem/" + module.TryGetJsonString("id") + "\"><div class=\"htmlloading\"></div></div>");
                        //}
                        //else
                        //{
                        content = Base64Decode(module["dom_conitem"].ToString());
                        tmpsb.Append(content);
                        //}
                        break;
                }
                lRecords++;
            }
            templateHtml = tmpsb.ToString();
            return templateHtml;
        }
        /// <summary>
        /// 商品模块
        /// </summary>
        /// <param name="context"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetGoodTag(JToken data)
        {
            try
            {
                string ids = "";
                List<long> arr_id = new List<long>();
                foreach (var item in data["content"]["goodslist"])
                {
                    long _tmp;
                    if (long.TryParse(item["item_id"].ToString(), out _tmp))
                    {
                        arr_id.Add(_tmp);
                    }
                }

                bool showIco = bool.Parse(data["content"]["showIco"].ToString());
                bool showPrice = bool.Parse(data["content"]["showPrice"].ToString());
                string showName = data["content"]["showName"].ToString();

                int idlen = arr_id.Count();
                StringBuilder html = new StringBuilder(200);
                if (arr_id != null & idlen > 0)
                {
                    string gdboxid = "goods_" + Guid.NewGuid().ToString("N");
                    //首节数量
                    string layout = data["content"]["layout"].ToString();
                    ids = string.Join(",", arr_id);
                    html.Append("@{Html.RenderAction(\"GoodsListAction\", \"TemplateVisualizationProcess\",new { Layout=\""
                        + layout + "\", ShowName=\"" + showName +
                        "\", IDs=\"" + ids +
                        "\", ShowIco=\"" + showIco +
                        "\", ShowPrice=\"" + showPrice +
                        "\", DataUrl=\"" + Request.Form["getGoodUrl"] +
                        "\", ID=\"" + gdboxid + "\"});}");
                }
                else
                {
                    html.Append(Base64Decode(data["dom_conitem"].ToString()));
                }
                return html.ToString();
            }
            catch
            {
                return "";
            }
        }

        private string CreateProductHtml(JToken data, List<long> idArray)
        {
            string layout = data["content"]["layout"].ToString();//1:小图，2：大图，3：一大两小，4：列表，5：小图有标题
            var name = "~/Views/Shared/GoodGroup" + layout + ".cshtml";
            ProductAjaxModel model = new ProductAjaxModel() { list = new List<ProductContent>() };
            model.showIco = bool.Parse(data["content"]["showIco"].ToString());
            model.showPrice = bool.Parse(data["content"]["showPrice"].ToString());
            model.showName = data["content"]["showName"].ToString() == "1";
            var prod = ProductManagerApplication.GetProductByIds(idArray);
            foreach (var id in idArray)
            {
                var pro = prod.FirstOrDefault(d => d.Id == id);
                if (pro != null)
                {
                    model.list.Add(
                   new ProductContent
                   {
                       product_id = pro.Id,
                       link = "/m-wap/Product/Detail/" + pro.Id.ToString(),
                       price = pro.MinSalePrice.ToString("f2"),
                       original_price = pro.MarketPrice.ToString("f2"),
                       pic = pro.ImagePath + "/1_350.png" + "?r=" + pro.UpdateTime.ToString("yyyyMMddHHmmss"),
                       title = pro.ProductName,
                       is_limitbuy = _ProductService.IsLimitBuy(pro.Id),
                       SaleCounts = pro.SaleCounts + (int)pro.VirtualSaleCounts
                   });
                }
            }
            return this.ControllerContext.RenderViewToString(name, model);
        }
        /// <summary>
        /// 商品分组模块
        /// </summary>
        /// <param name="context"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetGoodGroupTag(string path, JToken data)
        {
            try
            {
                string html = "@{Html.RenderAction(\"GoodsListAction\", \"TemplateVisualizationProcess\",new { Layout=\""
                    + data["content"]["layout"] +
                    "\", ShowName=\"" + data["content"]["showName"] +
                    "\", ShowIco=\"" + data["content"]["showIco"] +
                    "\", ShowPrice=\"" + data["content"]["showPrice"] +
                    "\", DataUrl=\"" + Request.Form["getGoodGroupUrl"] +
                    "\", TemplateFile=\"" + path +
                    "\", GoodListSize=\"" + data["content"]["goodsize"] +
                    "\", FirstPriority=\"" + data["content"]["firstPriority"] +
                    "\", SecondPriority=\"" + data["content"]["secondPriority"] +
                    "\", ID=\"goods_" + Guid.NewGuid().ToString("N") + "\"});}";
                return html;
            }
            catch
            {
                return "";
            }
        }
        /// <summary>  
        /// Base64加密  
        /// </summary>  
        /// <param name="message"></param>  
        /// <returns></returns>  
        public string Base64Code(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            return Convert.ToBase64String(bytes);
        }
        /// <summary>  
        /// Base64解密  
        /// </summary>  
        /// <param name="message"></param>  
        /// <returns></returns>  
        public string Base64Decode(string message)
        {
            byte[] bytes = Convert.FromBase64String(message);
            return Encoding.UTF8.GetString(bytes);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Hi_Ajax_GetTemplateByID
        public ActionResult Hi_Ajax_GetTemplateByID(string client, int type = 1)
        {
            string templatePath = VTemplateHelper.GetTemplatePath(client, (VTemplateClientTypes)type);
            var datapath = templatePath + "data/default.json";
            var fileName = Server.MapPath(datapath);
            if (!System.IO.File.Exists(fileName))
            {
                return Content(string.Empty);
            }
            StreamReader sr = new StreamReader(Server.MapPath(datapath),
                System.Text.Encoding.UTF8);
            try
            {
                string input = sr.ReadToEnd();
                sr.Close();
                input = input.Replace("\r\n", "").Replace("\n", "");
                return Content(input);
            }
            catch
            {
                return Content("");
            }
        }
        #endregion

        #region Hi_Ajax_GetSmallProgVshopTemplateByID
        public ActionResult Hi_Ajax_GetSmallProgVshopTemplateByID(string client, int type = 1, int shopId = 0)
        {
            string templatePath = VTemplateHelper.GetTemplatePath(client, (VTemplateClientTypes)type, shopId);
            var datapath = templatePath + "data/default.json";
            var fileName = Server.MapPath(datapath);
            if (!System.IO.File.Exists(fileName))
            {
                return Content(string.Empty);
            }
            StreamReader sr = new StreamReader(Server.MapPath(datapath),
                System.Text.Encoding.UTF8);
            try
            {
                string input = sr.ReadToEnd();
                sr.Close();
                input = input.Replace("\r\n", "").Replace("\n", "");
                return Content(input);
            }
            catch
            {
                return Content("");
            }
        }
        #endregion

        #region Hi_Ajax_GetFolderTree
        public ActionResult Hi_Ajax_GetFolderTree(string areaName = "")
        {
            long shopId = 0;
            CouponsAjaxModel model = new CouponsAjaxModel() { list = new List<CouponsContent>() };
            return Json(GetTreeListJson(shopId), JsonRequestBehavior.AllowGet);
        }

        private PhotoCategoryAjaxModel GetTreeListJson(long shopId = 0)
        {
            PhotoCategoryAjaxModel model = new PhotoCategoryAjaxModel() { status = "1", msg = "", data = new photoCateNumber() };
            model.data.total = _iPhotoSpaceService.GetPhotoList("", 1, 10, 1, 0, shopId).Total.ToString();
            GetImgTypeJson(model, shopId);
            return model;
        }
        public void GetImgTypeJson(PhotoCategoryAjaxModel model, long shopId = 0)
        {
            //string json = "{\"name\":\"所有图片\",\"subFolder\":[],\"id\":0,\"picNum\":" + GalleryHelper.GetPhotoList("", 0, 10, PhotoListOrder.UploadTimeDesc).TotalRecords + "},";
            var list = new List<photoCateContent>();
            var cate = _iPhotoSpaceService.GetPhotoCategories().ToList();
            for (int i = 0; i < cate.Count(); i++)
            {
                list.Add(new photoCateContent
                {
                    id = cate[i].Id.ToString(),
                    name = cate[i].PhotoSpaceCatrgoryName,
                    parent_id = 0,
                    picNum = _iPhotoSpaceService.GetPhotoList("", 1, 10, 1, cate[i].Id, shopId).Total.ToString()
                });
            }
            model.data.tree = list;
        }

        #endregion

        #region Hi_Ajax_AddFolder

        public ActionResult Hi_Ajax_AddFolder(string areaName = "")
        {
            try
            {

                var photoSpaceInfo = _iPhotoSpaceService.AddPhotoCategory("新建文件夹");
                return Json(new { status = "1", data = photoSpaceInfo.Id, msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "1", data = 0, msg = "" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Hi_Ajax_GetImgList

        public ActionResult Hi_Ajax_GetImgList(int id = 0, string areaName = "", int p = 0, string file_Name = "")
        {
            var photo = _iPhotoSpaceService.GetPhotoList(file_Name, p, 24, 1, id);
            PhotoSpaceAjaxModel model = new PhotoSpaceAjaxModel() { status = "1", msg = "" };
            InitialPhotoSpaceModel(model, photo, p);
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private void InitialPhotoSpaceModel(PhotoSpaceAjaxModel model, QueryPageModel<Entities.PhotoSpaceInfo> photo, int pageNo = 1)
        {
            var list = new List<photoContent>();
            int pageCount = TemplatePageHelper.GetPageCount(photo.Total, 24);
            model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
            foreach (var item in photo.Models)
            {
                list.Add(new photoContent
                {
                    file = Core.HimallIO.GetRomoteImagePath(item.PhotoPath),
                    id = item.Id.ToString(),
                    name = item.PhotoName
                });
            }
            model.data = list;
        }


        #endregion

        #region Hi_Ajax_RenameFolder

        public ActionResult Hi_Ajax_RenameFolder(long category_img_id, string name)
        {
            try
            {
                Dictionary<long, string> photoCategorys = new Dictionary<long, string>();
                photoCategorys.Add(category_img_id, name);
                _iPhotoSpaceService.UpdatePhotoCategories(photoCategorys);
                return Json(new { status = "1", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "0", msg = "" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Hi_Ajax_RenameImg

        public ActionResult Hi_Ajax_RenameImg(long file_id, string file_name)
        {
            try
            {

                _iPhotoSpaceService.RenamePhoto(file_id, file_name);
                return Json(new { status = "1", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "0", msg = "" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Hi_Ajax_RemoveImgByFolder

        public ActionResult Hi_Ajax_RemoveImgByFolder(long cid, int cate_id)
        {
            try
            {
                var mamagerRecordset = _iPhotoSpaceService.GetPhotoList("", 1, 100000000, 1, cid);
                List<long> list = new List<long>();
                foreach (var item in mamagerRecordset.Models)
                {
                    list.Add(item.Id);
                }
                _iPhotoSpaceService.MovePhotoType(list, cate_id);
                return Json(new { status = "1", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "0", msg = "请选择一个分类" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Hi_Ajax_MoveImg

        public ActionResult Hi_Ajax_MoveImg(int cate_id, string file_id)
        {
            try
            {
                List<long> ids = file_id.Split(',').ToList<string>().Select(x => long.Parse(x)).ToList();
                _iPhotoSpaceService.MovePhotoType(ids, cate_id);
                return Json(new { status = "1", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "0", msg = "请选择一个分类" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Hi_Ajax_DelImg

        public ActionResult Hi_Ajax_DelImg(string file_id)
        {
            try
            {
                string[] ids = file_id.Split(',');
                foreach (string id in ids)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    _iPhotoSpaceService.DeletePhoto(Convert.ToInt64(id), 0);
                }
                return Json(new { status = "1", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "0", msg = "请勾选图片" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Hi_Ajax_DelFolder

        public ActionResult Hi_Ajax_DelFolder(string id, string type)
        {
            try
            {
                PhotoSpaceApplication.DeletePhotoCategory(Convert.ToInt64(id), 0, type);
                return Json(new { status = "1", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { status = "0", msg = "请选择一个分类" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion


        #region Hi_Ajax_Shops
        public ActionResult Hi_Ajax_Shops(int p = 1, string shopName = "", int type = 1)
        {
            int pageNo = p;
            ShopsAjaxModel model = new ShopsAjaxModel() { list = new List<ShopsContent>() };
            InitialShopsModel(model, type, shopName, pageNo);
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private void InitialShopsModel(ShopsAjaxModel model, int type, string shopName = "", int pageNo = 1)
        {
            var shopsList = _ShopService.GetShopList(new
            Himall.DTO.QueryModel.ShopQuery
            {
                ShopName = shopName,
                PageNo = pageNo,
                PageSize = 10
            });
            int pageCount = TemplatePageHelper.GetPageCount(shopsList.Total, 10);

            if (shopsList != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialShopsContentModel(shopsList.Models, model, type);
            }
        }
        private void InitialShopsContentModel(List<Entities.ShopInfo> shopsList, ShopsAjaxModel model, int type)
        {
            VTemplateClientTypes clientType = (VTemplateClientTypes)type;

            string linkstr = "/m-wap/vshop/detail/{0}";
            string pc_linkstr = "/m-wap/vshop/detail/{0}";
            if (clientType == VTemplateClientTypes.WXSmallProgram)
            {
                linkstr = "/pages/vShopHome/vShopHome?id={0}";
                pc_linkstr = "/vShopHome/vShopHome?id={0}";
            }
            else if (clientType == VTemplateClientTypes.WapIndex)
            {
                linkstr = "/m-wap/vshop/detail/{0}";
                pc_linkstr = "/m-wap/vshop/detail/{0}";
            }
            foreach (var shop in shopsList)
            {
                var grade = _ShopService.GetShopGrade(shop.GradeId);
                var vshop = _VShopService.GetVShopByShopId(shop.Id);
                if (vshop != null)
                {
                    var info = new ShopsContent
                    {
                        shopId = shop.Id,
                        shopName = shop.ShopName,
                        shopGrade = grade != null ? grade.Name : "",
                        //pc_link = "/vShopHome/vShopHome?id=" + vshop.Id,
                        //link = "/m-wap/vshop/detail/" + vshop.Id,
                        title = shop.ShopName,
                    };
                    info.link = string.Format(linkstr, vshop.Id);
                    info.pc_link = string.Format(pc_linkstr, vshop.Id);
                    model.list.Add(info);
                }
            }
        }
        #endregion

        #region Hi_Ajax_Category
        public ActionResult Hi_Ajax_Categorys(int type, PlatformType platform = PlatformType.Mobile)
        {
            CategoryAjaxModel model = new CategoryAjaxModel() { list = new List<CategorysContent>() };
            InitialCategorysModel(model, type, platform);
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private void InitialCategorysModel(CategoryAjaxModel model, int type, PlatformType platform = PlatformType.Mobile)
        {
            var list = _iCategoryService.GetCategories();
            VTemplateClientTypes clientType = (VTemplateClientTypes)type;

            string linkstr = "/m-Wap/search?keywords=&cid={0}";
            string pc_linkstr = "/searchresult/searchresult?keyword=&cid={0}";
            if (clientType == VTemplateClientTypes.WXSmallProgram || platform == PlatformType.WeiXinSmallProg)
            {
                linkstr = "../searchresult/searchresult?cid={0}";
                pc_linkstr = "/searchresult/searchresult?keyword=&cid={0}";
            }
            else if (clientType == VTemplateClientTypes.WXSmallProgramSpecial)
            {
                linkstr = "../searchresult/searchresult?keyword=&cid={0}";
                pc_linkstr = "/searchresult/searchresult?keyword=&cid={0}";
            }
            else if (clientType == VTemplateClientTypes.WapIndex)
            {
                linkstr = "/m-Wap/search?keywords=&cid={0}";
                pc_linkstr = "/m-Wap/search?keywords=&cid={0}";
            }

            if (list != null && list.Count > 0)
            {
                model.status = 1;

                foreach (var info in list.Where(t => t.Depth == 1))
                {
                    GetCategorys(model.list, info, linkstr, pc_linkstr);

                    foreach (var second in list.Where(t => t.ParentCategoryId == info.Id))
                    {
                        GetCategorys(model.list, second, linkstr, pc_linkstr);

                        foreach (var three in list.Where(t => t.ParentCategoryId == second.Id))
                        {
                            GetCategorys(model.list, three, linkstr, pc_linkstr);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 分类级别整理
        /// </summary>
        private void GetCategorys(List<CategorysContent> categorysContents, CategoryInfo cate, string linkstr, string pc_linkstr)
        {
            var str = string.Empty;
            if (cate.Depth == 1) { str = string.Format("{0}", ""); }
            else if (cate.Depth == 2) { str = string.Format("{0}", "&nbsp;&nbsp;&nbsp;"); }
            else if (cate.Depth == 3) { str = string.Format("{0}", "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"); }
            var cName = string.Format("{0}{1}", str, cate.Name);

            var cinfo = new CategorysContent
            {
                Id = cate.Id,
                Name = string.Format("{0}{1}", "", cName),
                link = string.Format(linkstr, cate.Id),
                pc_link = string.Format(pc_linkstr, cate.Id),
                title = cate.Name,
            };
            categorysContents.Add(cinfo);
        }
        #endregion

        #region Hi_Ajax_ShopBranchTag(门店标品)
        public ActionResult Hi_Ajax_GetShopBranchTags(int p = 0, int pageNo = 0, string title = "", PlatformType platform = PlatformType.Mobile)
        {
            CommonAjaxModel model = new CommonAjaxModel() { list = new List<CommonContent>() };
            InitialShopBranchTagsModel(model, p, pageNo, title, platform);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialShopBranchTagsModel(CommonAjaxModel model, int p, int pageNo, string title, PlatformType platform = PlatformType.Mobile)
        {
            pageNo = pageNo > 0 ? pageNo : 7;
            var list = ShopBranchApplication.GetShopBranchTags(p, pageNo, title);
            if (list != null && list.Total > 0)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(list.Total, pageNo);
                foreach (var info in list.Models)
                {
                    var cinfo = new CommonContent
                    {
                        Id = info.Id,
                        Name = info.Title,
                        link = IsWXSamllProgram(platform) ? "../tag/tag?tagid=" + info.Id : "../tag/tag?tagid=" + info.Id,
                        wap_link = "/m-Wap/ShopBranch/Tags/" + info.Id,
                        title = info.Title,
                    };
                    model.list.Add(cinfo);
                }
            }
        }
        #endregion

        #region Hi_Ajax_ShopBranchList(门店列表)
        public ActionResult Hi_Ajax_GetShopBranchList(bool? isRecommend, int p = 0, int rows = 0, string titleKeyword = "", string tagsId = "", string addressId = "", PlatformType platform = PlatformType.Mobile)
        {
            CommonAjaxModel model = new CommonAjaxModel() { list = new List<CommonContent>() };
            InitialShopBranchListModel(isRecommend, model, p, rows, titleKeyword, tagsId, addressId, platform);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private void InitialShopBranchListModel(bool? isRecommend, CommonAjaxModel model, int page = 0, int rows = 0, string titleKeyword = "", string tagsId = "", string addressId = "", PlatformType platform = PlatformType.Mobile)
        {
            ShopBranchQuery query = new ShopBranchQuery();
            query.PageNo = page > 0 ? page : 1;
            query.PageSize = rows > 0 ? rows : 7;
            if (!string.IsNullOrEmpty(titleKeyword))
                query.ShopBranchName = titleKeyword;
            if (!string.IsNullOrEmpty(addressId))
            {
                var regionid = Convert.ToInt32(addressId);
                var region = RegionApplication.GetRegion(regionid);
                switch (region.Level)
                {
                    case Region.RegionLevel.Province: query.ProvinceId = regionid; break;
                    case Region.RegionLevel.City: query.CityId = regionid; break;
                }

            }
            if (!string.IsNullOrEmpty(tagsId))
                query.ShopBranchTagId = Convert.ToInt64(tagsId);
            if (isRecommend.HasValue) query.IsRecommend = isRecommend;

            var shopBranchs = ShopBranchApplication.GetShopBranchs(query);
            if (shopBranchs.Total <= 0)
            {
                model.status = 1;//没数据 状态需正常
                return;
            }

            if (shopBranchs != null && shopBranchs.Total > 0)
            {
                model.status = 1;
                int count = (shopBranchs.Total % query.PageSize) > 0 ? (int)Math.Floor(Convert.ToDecimal(shopBranchs.Total / query.PageSize)) + 1 : (int)Math.Floor(Convert.ToDecimal(shopBranchs.Total / query.PageSize));

                model.page = TemplatePageHelper.GetPageHtml(count, query.PageNo);
                foreach (var info in shopBranchs.Models)
                {
                    var cinfo = new CommonContent
                    {
                        Id = info.Id,
                        Name = info.ShopBranchName,
                        link = "../shophome/shophome?id=" + info.Id,
                        wap_link = "/m-Wap/ShopBranch/Index/" + info.Id,
                        title = info.ShopBranchName,
                    };
                    model.list.Add(cinfo);
                }
            }
        }

        /// <summary>
        /// 获取所有门店标签
        /// </summary>
        /// <returns></returns>
        public ActionResult Hi_Ajax_GetAllShopBranchTags()
        {
            var shopBranchTagInfos = ShopBranchApplication.GetAllShopBranchTagInfos();

            var models = shopBranchTagInfos.Select(p => new
            {
                p.Id,
                p.Title
            }).ToList();

            return Json(models, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Hi_Ajax_SmallProgBrand(品牌列表)
        public ActionResult Hi_Ajax_SmallProgBrand(int p = 1, string title = "", PlatformType platform = PlatformType.Mobile)
        {
            int pageNo = p;
            BrandAjaxModel model = new BrandAjaxModel() { list = new List<BrandsContent>() };
            var brandlist = BrandApplication.GetBrands(title, pageNo, 10);

            int pageCount = TemplatePageHelper.GetPageCount(brandlist.Total, 10);

            if (brandlist != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialSmallProgBrandContentModel(brandlist.Models, model, platform);
            }
            return Json(model, JsonRequestBehavior.AllowGet);
        }
        private void InitialSmallProgBrandContentModel(List<Brand> datalist, BrandAjaxModel model, PlatformType platform)
        {
            foreach (var brand in datalist)
            {
                model.list.Add(new BrandsContent
                {
                    Id = brand.Id,
                    pic = Core.HimallIO.GetRomoteImagePath(brand.Logo),
                    title = brand.Name,
                    link = "/search/searchad?b_id=" + brand.Id.ToString()
                });
            }
        }
        #endregion

        #region Hi_Ajax_GetLivesList(直播间列表)
        public ActionResult Hi_Ajax_GetLivesList(string title, int pageNo = 10, int p = 1)
        {
            LiveAjaxModel model = new LiveAjaxModel() { list = new List<LivesContent>() };

            List<int> roomstatue = new List<int>();
            roomstatue.Add((int)LiveRoomStatus.Living);
            roomstatue.Add((int)LiveRoomStatus.NotStart);

            LiveQuery query = new LiveQuery()
            {
                PageSize = 10,
                PageNo = p,
                HasPage = true,
                Name = title,
                StatusList = roomstatue
            };
            var liveroomlist = LiveApplication.GetLiveList(query);//获取直播列表
            if (liveroomlist.Total > 0)
            {
                int pageCount = TemplatePageHelper.GetPageCount(liveroomlist.Total, pageNo);
                model.list = FillLiveRoom(liveroomlist.Models);
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, p);
            }

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public List<LivesContent> FillLiveRoom(List<LiveViewModel> roomlist)
        {
            List<LivesContent> livecontentlist = new List<LivesContent>();
            var roomIds = roomlist.Select(r => r.RoomId).ToList<long>();
            var productmode = LiveApplication.GetLiveProducts(roomIds);//获取直播间的商品
            foreach (var room in roomlist)
            {
                List<LiveProductContent> productlist = new List<LiveProductContent>();
                productlist = FillLiveProduct(productmode, room.RoomId);
                LivesContent livemodel = new LivesContent()
                {
                    AnchorImg = room.AnchorImg,
                    AnchorName = room.AnchorName,
                    CoverImg = room.CoverImg,
                    Link = "/pages/livedetail/livedetail?roomid=" + room.RoomId,
                    Name = room.Name,
                    ProductCount = productlist.Count(),
                    ProductList = productlist,
                    RoomId = room.RoomId,
                    StartTime = room.StartTime,
                    StartTimeDesc = room.StartTimeDesc,
                    Status = room.Status,
                    StatusDesc = room.StatusDesc
                };
                livecontentlist.Add(livemodel);

            }
            return livecontentlist;

        }

        /// <summary>
        /// 填充直播间商品值
        /// </summary>
        /// <param name="productlist"></param>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public List<LiveProductContent> FillLiveProduct(List<LiveProductInfo> productlist, long roomId)
        {
            List<LiveProductContent> prolist = new List<LiveProductContent>();
            var roomProdcutlist = productlist.Where(p => p.RoomId == roomId);
            foreach (var pro in roomProdcutlist)
            {
                LiveProductContent currentlivepro = new LiveProductContent()
                {
                    ProductId = pro.ProductId,
                    Image = pro.Image,
                    Name = pro.Name,
                    Price = pro.Price,
                    RoomId = roomId,
                    SaleAmount = pro.SaleAmount,
                    SaleCount = pro.SaleCount,
                    Url = pro.Url
                };
                prolist.Add(currentlivepro);
            }
            return prolist;
        }

        #endregion


        #region Hi_Ajax_ArticleList文章列表

        public ActionResult Hi_Ajax_ArticleList(int p = 1, string title = "", PlatformType platform = PlatformType.Mobile)
        {
            int pageNo = p;
            ArticleAjaxModel model = new ArticleAjaxModel() { list = new List<ArticleContent>() };

            var articlelist = ArticleApplication.GetArticleList(10, pageNo, 0);

            int pageCount = TemplatePageHelper.GetPageCount(articlelist.Total, 10);

            if (articlelist != null)
            {
                model.status = 1;
                model.page = TemplatePageHelper.GetPageHtml(pageCount, pageNo);
                InitialArticleContentModel(articlelist.Models, model, platform);
            }
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public void InitialArticleContentModel(List<ArticleInfo> datalist, ArticleAjaxModel model, PlatformType platform)
        {

            foreach (var article in datalist)
            {
                var url = "/m-wap/Article/Detail/" + article.Id.ToString();
                if (platform == PlatformType.WeiXinSmallProg)
                {
                    url = "/pages/articledetail/articledetail?id=" + article.Id.ToString();
                }
                else if (platform == PlatformType.Android)
                {
                    url = "articledetail/articledetail?id=" + article.Id.ToString();
                }
                model.list.Add(new ArticleContent
                {
                    Id = article.Id,
                    title = article.Title,
                    link = url
                });
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 添加或修改专题
        /// </summary>
        /// <param name="topicId"></param>
        /// <param name="title"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        private long AddOrUpdateTopic(long topicId, string title, string tags, string icon, PlatformType platform)
        {
            long result = 0;
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(tags))
            {
                throw new HimallException("请填写专题的标题与标签");
            }
            if (string.IsNullOrWhiteSpace(icon))
            {
                throw new HimallException("请上传专题的图标");
            }

            if (topicId > 0)
            {
                var topic = TopicApplication.GetTopic(topicId);
                if (topic == null)
                {
                    throw new HimallException("错误的专题编号");
                }
                topic.Name = title;
                topic.Tags = tags;
                topic.PlatForm = platform;
                topic.TopImage = icon;
                _iTopicService.UpdateTopicInfo(topic);
            }
            else
            {
                var topic = new TopicInfo
                {
                    Name = title,
                    Tags = tags,
                    TopImage = icon,
                    PlatForm = platform
                };
                _iTopicService.AddTopicInfo(topic);
                topicId = topic.Id;
            }
            result = topicId;
            if (result <= 0)
            {
                throw new HimallException("数据添加异常");
            }
            return result;
        }
        /// <summary>
        /// 保存图标
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private string SaveIcon(string filepath, VTemplateClientTypes clientTypes)
        {
            string result = filepath;
            if (!string.IsNullOrWhiteSpace(filepath))
            {
                string dest = @"/Storage/Special/Icon/";

                if (result.Contains("/temp/"))
                {
                    var d = result.Substring(result.LastIndexOf("/temp/"));

                    var destimg = Path.Combine(dest, Path.GetFileName(result));
                    Core.HimallIO.CopyFile(d, destimg, true);
                    result = destimg;
                }
                else if (result.Contains("/Storage/"))
                {
                    result = result.Substring(result.LastIndexOf("/Storage/"));
                }
                else
                {
                    result = "";
                }
            }
            if (clientTypes == VTemplateClientTypes.WXSmallProgram || clientTypes == VTemplateClientTypes.WXSmallProgramSpecial)
            {

                return result == "" ? "" : Himall.Core.HimallIO.GetRomoteImagePath(result);
            }
            else
            {
                return result;
            }
        }
        /// <summary>
        /// 获取json对应值
        /// </summary>
        /// <param name="jt"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string TryGetJsonString(JToken jt, string name)
        {
            string result = "";
            var _tmp = jt[name];
            if (_tmp != null)
            {
                result = _tmp.ToString();
            }
            return result;
        }
        /// <summary>
        /// 获取json对应值
        /// </summary>
        /// <param name="jt"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string TryGetJsonString(JObject jt, string name)
        {
            string result = "";
            var _tmp = jt[name];
            if (_tmp != null)
            {
                result = _tmp.ToString();
            }
            return result;
        }
        #endregion
    }

    public static class TemplatePageHelper
    {
        public static int GetPageCount(int totalRecords, int pageSize)
        {
            int pageCount = 1;
            if (totalRecords % pageSize != 0)
                pageCount = (totalRecords / pageSize) + 1;
            else
                pageCount = totalRecords / pageSize;
            return pageCount;
        }
        /// <summary>
        /// 获取分页
        /// </summary>
        /// <param name="pageCount">总页数</param>
        /// <param name="pageIndex">当前页</param>
        /// <returns>返回分页HTML</returns>
        public static string GetPageHtml(int pageCount, int pageIndex)
        {
            if (pageIndex < 1)
                pageIndex = 1;


            string prevPageHtml = "<a href='javascript:;' class='prev' href='javascript:void(0);' page='" + (pageIndex - 1) + "'></a>";
            if (pageIndex == 1)
                prevPageHtml = "<a href='javascript:;' class='prev disabled' ></a>";
            string pageNumHtml = "";
            if (pageCount > 9)
                prevPageHtml += PageNumHtmlMoreThanTen(pageCount, pageIndex);
            else
                prevPageHtml += PageNumHtmlLessThanTen(pageCount, pageIndex);
            string nextPageHtml = "<a href='javascript:;' class='next' href='javascript:void(0);' page='" + (pageIndex + 1) + "'></a>";
            if (pageIndex == pageCount)
                nextPageHtml = "<a href='javascript:;' class='next disabled' ></a>";
            return prevPageHtml + pageNumHtml + nextPageHtml;
        }

        public static string PageNumHtmlMoreThanTen(int pageCount, int pageIndex)
        {
            string pageNumHtml = "";
            bool showHidePage = true;
            if (pageIndex < 0)
                pageIndex = 1;


            int firstPage = 1;
            if (pageIndex > 9)
                firstPage = pageIndex - 5;

            int hidePage = firstPage + 9 + 1;

            if (hidePage >= pageCount)
            {
                hidePage = pageCount;
                showHidePage = false;
            }

            int lastPage = pageCount;
            if ((hidePage + 2) > pageCount)
                lastPage = 0;

            for (int i = firstPage; i < hidePage; i++)
            {
                if (i == pageIndex)
                    pageNumHtml += "<a class='cur' >" + i + "</a>";
                else
                    pageNumHtml += "<a href='javascript:void(0);' page='" + i + "' >" + i + "</a>";
            }
            if (showHidePage)
                pageNumHtml += "<a href='javascript:void(0);' page='" + hidePage + "' >.....</a>";

            if (lastPage != 0)
            {
                pageNumHtml += "<a href='javascript:void(0);' page='" + (lastPage - 2) + "' >" + (lastPage - 2) + "</a>";
                pageNumHtml += "<a href='javascript:void(0);' page='" + (lastPage - 1) + "' >" + (lastPage - 1) + "</a>";
            }
            return pageNumHtml;
        }

        public static string PageNumHtmlLessThanTen(int pageCount, int pageIndex)
        {
            string pageNumHtml = "";
            for (int i = 1; i <= pageCount; i++)
            {
                if (i == pageIndex)
                    pageNumHtml += "<a class='cur' >" + i + "</a>";
                else
                    pageNumHtml += "<a href='javascript:void(0);' page='" + i + "' >" + i + "</a>";
            }
            return pageNumHtml;
        }

        public static string RenderViewToString(this ControllerContext context, string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = context.RouteData.GetRequiredString("action");

            context.Controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(context, viewName);
                var viewContext = new ViewContext(context,
                                  viewResult.View,
                                  context.Controller.ViewData,
                                  context.Controller.TempData,
                                  sw);

                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }
    }

}