using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Service
{
    public class CacheManager
    {
        public static T GetOrCreate<T>(string key, Func<T> builder, int second = 3600)
        {
            var model = Cache.Get<CacheDataItem<T>>(key);
            if (model == null)
            {
                model = new CacheDataItem<T>
                {
                    Time = DateTime.Now,
                    Data = builder()
                };
                Cache.Insert(key, model, second);
            }
            return model.Data;
        }
        internal static FreightTemplateData GetFreightTemplate(long templateId, Func<FreightTemplateData> builder) =>
                GetOrCreate($"freightTempalte:{templateId}", builder);
        internal static FreightTemplateInfo GetFreightTemplateInfo(long templateId, Func<FreightTemplateInfo> builder) =>
            GetOrCreate(CacheKeyCollection.CACHE_FREIGHTTEMPLATE(templateId), builder);

        internal static void ClearFreightTemplate(long templateId)
        {
            Cache.Remove($"freightTempalte:{templateId}");
            Cache.Remove(CacheKeyCollection.CACHE_FREIGHTTEMPLATE(templateId));
            Cache.Remove(CacheKeyCollection.CACHE_FREIGHTAREADETAIL(templateId));
        }

        /// <summary>
        /// 商品
        /// </summary>
        internal static ProductData GetProduct(long productId, Func<ProductData> builder) =>
            GetOrCreate($"product:{productId}", builder);
        internal static void ClearProduct(long product) =>
           Cache.Remove($"product:{product}");
        internal static ProductTemplateData GetProductTemplate(long id, Func<ProductTemplateData> builder) =>
            GetOrCreate($"product:template:{id}", builder);

        internal static void ClearProductTemplate(long id) =>
            Cache.Remove($"product:template:{id}");

        internal static void ClearProduct(List<long> products) =>
            products.ForEach(product => ClearProduct(product));

        public static ProductEnsure GetProductEnsure(long productId, Func<ProductEnsure> builder) =>
            GetOrCreate($"productEnsure:{productId}", builder);

        #region 客服CustomerService
        /// <summary>
        /// 客服
        /// </summary>
        internal static List<CustomerServiceInfo> GetCustomerService(long shopId, Func<List<CustomerServiceInfo>> builder) =>
            GetOrCreate($"CustomerService:{shopId}", builder);

        public static List<ShippingAddressData> GetShippingAddress(long memberId, Func<List<ShippingAddressData>> builder) =>
            GetOrCreate($"shippingaddress:{memberId}", builder, 300);

        public static void ClearShippingAddress(long memberId) =>
            Clear($"shippingaddress:{memberId}");
        /// <summary>
        /// 客服
        /// </summary>
        internal static void ClearCustomerService(long shopId) =>
            Cache.Remove($"CustomerService:{shopId}");
        #endregion

        /// <summary>
        /// 商家保证金
        /// </summary>
        public static CashDepositInfo GetShopCashDeposit(long shopId, Func<CashDepositInfo> builder) =>
            GetOrCreate($"shop-cash-deposit:{shopId}", builder);

        /// <summary>
        /// 商家保证金
        /// </summary>
        public static void ClearShopCashDeposit(long shopId) =>
            Cache.Remove($"shop-cash-deposit:{shopId}");


        public static List<CategoryCashDepositInfo> GetCategoryCashDeposit(Func<List<CategoryCashDepositInfo>> builder) =>
            GetOrCreate($"category-cash-deposit", builder);
        public static void ClearCategoryCashDeposit() =>
            Cache.Remove("category-cash-deposit");
        internal static List<CategoryInfo> GetCategories(Func<List<CategoryInfo>> builder) =>
            GetOrCreate(CacheKeyCollection.Category, builder);

        public static void ClearCategories() =>
            Cache.Remove(CacheKeyCollection.Category);

        internal static BusinessCategories GetCategories(long shopId, Func<BusinessCategories> builder) =>
            GetOrCreate($"categories:{shopId}", builder);

        public static void ClearCategories(long shopId) =>
            Cache.Remove($"categories:{shopId}");

        internal static int GetConsultationCount(long product, Func<int> builder) =>
            GetOrCreate($"consultationCount:{product}", builder, 600);
        public static StatisticOrderComment GetStatisticOrderComment(long shop, Func<StatisticOrderComment> builder) =>
            GetOrCreate($"statistic-order-comment:{shop}", builder);

        internal static List<long> FavoriteProduct(long userId, Func<List<long>> builder) =>
            GetOrCreate($"favorite:product:{userId}", builder);

        internal static List<FlashSaleData> GetAvailableFlashSale(Func<List<FlashSaleData>> builder) =>
            GetOrCreate($"available:flashSale", builder);
        internal static FlashSaleData GetFlashSale(long id, Func<FlashSaleData> builder) =>
            GetOrCreate($"flashSale:{id}", builder);
        internal static List<FullDiscountData> GetAvailableFullDiscount(Func<List<FullDiscountData>> builder) =>
            GetOrCreate($"available:fullDiscount", builder);

        internal static void ClearAvailableFullDiscount() =>
            Cache.Remove($"available:fullDiscount");

        /// <summary>
        /// 优惠券
        /// </summary>
        /// <param name="shopId">店铺id</param>
        internal static List<CouponData> GetAvailableCoupon(long shopId, Func<List<CouponData>> builder) =>
            GetOrCreate($"available:coupon:{shopId}", builder);

        /// <summary>
        /// 优惠券清缓存
        /// </summary>
        /// <param name="shopId">店铺id</param>
        internal static void ClearAvailableCoupon(long shopId) =>
            Cache.Remove($"available:coupon:{shopId}");

        /// <summary>
        /// 拼团
        /// </summary>
        /// <param name="id">活动id</param>
        internal static FightGroupData GetFightGroup(long id, Func<FightGroupData> builder) =>
            GetOrCreate($"fightGroup:{id}", builder);

        /// <summary>
        /// 拼团列表
        /// </summary>
        internal static List<FightGroupData> GetAvailableFightGroup(Func<List<FightGroupData>> builder) =>
            GetOrCreate($"available:fightGroup", builder);

        /// <summary>
        /// 拼团清缓存
        /// </summary>
        /// <param name="id">活动id</param>
        internal static void ClearAvailableFightGroup(long id)
        {
            Cache.Remove($"fightGroup:{id}");
            Cache.Remove($"available:fightGroup");
        }

        internal static void ClearAvailableFlashSale(long id)
        {
            Cache.Remove($"flashSale:{id}");
            Cache.Remove($"available:flashSale");
        }


        internal static List<ShopBonusData> GetAvailableBonus(Func<List<ShopBonusData>> builder) =>
            GetOrCreate($"available:bonus", builder);

        /// <summary>
        /// 组合购缓存
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        internal static List<CollocationData> GetAvailableCollocation(Func<List<CollocationData>> builder) =>
            GetOrCreate($"available:collocation", builder);

        /// <summary>
        /// 清组合购缓存
        /// </summary>
        internal static void ClearAvailableCollocation() =>
          Cache.Remove("available:collocation");
        internal static void ClearAvailableBonus() =>
            Cache.Remove("available:bonus");
        internal static void ClearFavoriteProduct(long userId) =>
            Cache.Remove($"favorite:product:{userId}");

        internal static List<long> GetFavoriteShop(long userId, Func<List<long>> builder) =>
            GetOrCreate($"favorite:shop:{userId}", builder);

        internal static void ClearFavoriteShop(long userId) =>
            Cache.Remove($"favorite:shop:{userId}");

        /// <summary>
        /// 获取商品评论概览
        /// </summary>
        internal static CommentSummaryData GetProductCommentSummary(long product, long shopBranchId, Func<CommentSummaryData> builder) =>
            GetOrCreate($"comment:summary:{product}_{shopBranchId}", builder, 300);

        /// <summary>
        /// 商家评分
        /// </summary>
        internal static ShopMarksData GetShopMarks(long id, Func<ShopMarksData> builder) =>
            GetOrCreate($"shopMarks:{id}", builder, 300);



        internal static VShopInfo GetVShop(long shopId, Func<VShopInfo> builder) =>
            GetOrCreate($"vShop:{shopId}", builder);

        internal static int GetOnSaleCount(long shopId, Func<int> builder) =>
            GetOrCreate($"shopOnSaleCount:{shopId}", builder);

        /// <summary>
        /// 会员根据provider类型openid缓存
        /// </summary>
        /// <param name="provider">类型</param>
        /// <param name="openId">openId</param>
        internal static MemberData GetMemberData(string provider, string openId, Func<MemberData> builder)
        {
            string key = $"member:{provider}:{openId}";
            var memberId = Cache.Get<long>(key);
            if (memberId > 0)
            {
                var memberKey = $"member:{memberId}";
                var memberData = Cache.Get<CacheDataItem<MemberData>>(memberKey);
                if (memberData != null)
                    return memberData.Data;
            }

            var member = builder();
            if (member != null)
            {
                Cache.Insert(key, member.Id);
                Cache.Insert($"member-openid:{member.Id}", key); //缓存键再缓存键值
                Cache.Insert($"member:{member.Id}", new CacheDataItem<MemberData>
                {
                    Data = member,
                    Time = DateTime.Now
                });
            }
            return member;
        }
        internal static MemberData GetMemberData(long id, Func<MemberData> builder) =>
            GetOrCreate($"member:{id}", builder);

        /// <summary>
        /// 清除会员缓存
        /// </summary>
        /// <param name="memberId">会员Id</param>
        internal static void ClearMemberData(long memberId)
        {
            Cache.Remove(CacheKeyCollection.Member(memberId));//移除用户缓存
            var openKey = Cache.Get<string>($"member-openid:{memberId}");
            if (string.IsNullOrEmpty(openKey))
                Cache.Remove(openKey);
        }

        internal static List<LiveRoomData> GetRoomLiveing(Func<List<LiveRoomData>> builder) =>
            GetOrCreate($"room:liveing", builder);

        internal static void ClearRoomLiveing() =>
            Cache.Remove("room:liveing");

        internal static ShopData GetShop(long id, Func<ShopData> builder) =>
            GetOrCreate($"shop:{id}.data", builder);
        internal static void ClearShop(long id) =>
            Cache.Remove($"shop:{id}.data");

        internal static void Clear(string key) =>
            Cache.Remove(key);

        internal static ShopInvoiceConfigInfo GetInvoiceConfig(long shopId, Action p)
        {
            throw new NotImplementedException();
        }
    }
}
