using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.Market;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Himall.Application
{
    /// <summary>
    /// 营销活动
    /// </summary>
    public class MarketingApplication : BaseApplicaion
    {
        private static LimitTimeBuyService LimitTimeBuyService => ObjectContainer.Current.Resolve<LimitTimeBuyService>();
        private static CollocationService CollocationService => ObjectContainer.Current.Resolve<CollocationService>();
        private static FullDiscountService FullDiscountService => ObjectContainer.Current.Resolve<FullDiscountService>();
        private static CouponService CouponService => ObjectContainer.Current.Resolve<CouponService>();
        private static FightGroupService FightGroupService => ObjectContainer.Current.Resolve<FightGroupService>();
        private static ShopService ShopService => ObjectContainer.Current.Resolve<ShopService>();
        private static BonusService bonusService => ObjectContainer.Current.Resolve<BonusService>();

        /// <summary>
        /// 是否存在活动
        /// </summary>
        private static bool ContainsMarket(MarketingType types, MarketingType type) => (types & type) > 0;
        private static List<MarketingOrderItem> MutualExclusion(MarketingSubOrder order, MarketingType marketingType) =>
             MutualExclusion(order.Items, marketingType);
        private static List<MarketingOrderItem> MutualExclusion(List<MarketingOrderItem> items, MarketingType marketingType)
        {
            switch (marketingType)
            {
                case MarketingType.FullDiscount:
                case MarketingType.MemberDiscount://折扣价格
                    return items.Where(p => !ContainsMarket(p.MarketingTypes, MarketingType.FlashSale | MarketingType.Groupon | MarketingType.Collocation)).ToList();
                case MarketingType.Coupon:
                case MarketingType.Bonus:
                case MarketingType.PlatformCoupon:
                    return items.Where(p => !ContainsMarket(p.MarketingTypes, MarketingType.Groupon)).ToList();
                default:
                    return items;
            }
        }


        private static MarketingOrder Convert(OrderPreproResult source)
        {
            var order = new MarketingOrder
            {
                MemberId = source.MemberId,
                SubOrders = source.SubOrders.Select(shop => new MarketingSubOrder
                {
                    ShopId = shop.ShopId,
                    Marketings = new List<MarketingItem>(),
                    Items = shop.Items.Select(item => new MarketingOrderItem
                    {
                        ShopId = item.ShopId,
                        ProductId = item.ProductId,
                        SkuId = item.SkuId,
                        Price = item.Price,
                        Quantity = item.Quantity,
                        DiscountDetails = new List<MarketingOrderItemDiscount>()
                    }).ToList()
                }).ToList()
            };
            return order;
        }
        private static MarketingOrder Convert(OrderCreating order)
        {
            var marketing = new MarketingOrder
            {
                MemberId = order.Member.Id,
                SubOrders = order.SubOrders.Select(subOrder => new MarketingSubOrder
                {
                    ShopId = subOrder.ShopId,
                    Marketings = new List<MarketingItem>(),
                    Items = subOrder.Items.Select(item => new MarketingOrderItem
                    {
                        ShopId = subOrder.ShopId,
                        ProductId = item.ProductId,
                        SkuId = item.SkuId,
                        Price = item.SalePrice,
                        Quantity = item.Quantity,
                        DiscountDetails = new List<MarketingOrderItemDiscount>()
                    }).ToList()
                }).ToList()
            };
            HttpContext.Current.Items["OrderCreatingMarketing"] = marketing;
            return marketing;
        }

        private static void Fill(OrderPreproResult result, MarketingOrder marketingOrder, List<GeneralRecord> records)
        {
            foreach (var shopOrder in result.SubOrders)
            {
                var marketingSubOrder = marketingOrder.SubOrders.FirstOrDefault(p => p.ShopId == shopOrder.ShopId);

                //免邮
                shopOrder.FreeFreight = marketingSubOrder.FreeShipping;
                shopOrder.FreeFreightAmount = marketingSubOrder.FreeFreight;
                //满减
                if (marketingSubOrder.MarketingTypes.HasFlag(MarketingType.FullDiscount))
                    shopOrder.FullDiscount = marketingSubOrder.Marketings.Where(p => p.MarketingType == MarketingType.FullDiscount).Sum(p => p.Amount);

                foreach (var item in shopOrder.Items)
                {
                    var marketingItem = marketingSubOrder.Items.FirstOrDefault(p => p.SkuId == item.SkuId);
                    item.Price = marketingItem.Price;
                    item.Amount = marketingItem.Amount;
                }
                //商家券
                shopOrder.Records = records.Where(p => p.ShopId == shopOrder.ShopId).ToList();
            }
            //平台券
            result.Records = records.Where(p => p.ShopId == 0).ToList();
        }
        private static void Fill(OrderCreating order, MarketingOrder orderMarketing, MarketingCommand command)
        {
            foreach (var subOrder in order.SubOrders)
            {
                var subOrderMarketing = orderMarketing.SubOrders.FirstOrDefault(p => p.ShopId == subOrder.ShopId);
                subOrder.FreeFreight = subOrderMarketing.FreeShipping;

                foreach (var item in subOrder.Items)
                {
                    var marketingItem = subOrderMarketing.Items.FirstOrDefault(p => p.SkuId == item.SkuId);
                    if (marketingItem.MarketingTypes.HasFlag(MarketingType.Coupon))
                        item.CouponDiscount += marketingItem.DiscountDetails.FirstOrDefault(p => p.MarketingType == MarketingType.Coupon).Amount;
                    if (marketingItem.MarketingTypes.HasFlag(MarketingType.Bonus))
                        item.CouponDiscount += marketingItem.DiscountDetails.FirstOrDefault(p => p.MarketingType == MarketingType.Bonus).Amount;
                    if (marketingItem.MarketingTypes.HasFlag(MarketingType.PlatformCoupon))
                        item.PlatformDiscount = marketingItem.DiscountDetails.FirstOrDefault(p => p.MarketingType == MarketingType.PlatformCoupon).Amount;
                    if (marketingItem.MarketingTypes.HasFlag(MarketingType.FullDiscount))
                        item.FullDiscount = marketingItem.DiscountDetails.FirstOrDefault(p => p.MarketingType == MarketingType.FullDiscount).Amount;
                    if (marketingItem.MarketingTypes.HasFlag(MarketingType.FlashSale))
                        item.FlashSaleId = command.FlashSaleId;

                    item.SalePrice = marketingItem.Price;
                    item.Discount = marketingItem.Discount;
                    item.Amount = item.SalePrice * item.Quantity - item.Discount;
                }

                if (subOrderMarketing.MarketingTypes.HasFlag(MarketingType.PlatformCoupon))
                {//平台券
                    var item = subOrderMarketing.Marketings.FirstOrDefault(p => p.MarketingType == MarketingType.PlatformCoupon);
                    subOrder.PlatformCouponId = item.MarketId;
                }

                if (subOrderMarketing.MarketingTypes.HasFlag(MarketingType.Coupon))
                {//优惠券
                    var item = subOrderMarketing.Marketings.FirstOrDefault(p => p.MarketingType == MarketingType.Coupon);
                    subOrder.CouponId = item.MarketId;
                }
                if (subOrderMarketing.MarketingTypes.HasFlag(MarketingType.Bonus))
                {//优惠券
                    var item = subOrderMarketing.Marketings.FirstOrDefault(p => p.MarketingType == MarketingType.Bonus);
                    subOrder.BonusId = item.MarketId;
                }

                if (subOrderMarketing.MarketingTypes.HasFlag(MarketingType.Collocation))
                    subOrder.OrderType = Entities.OrderInfo.OrderTypes.Collocation;
                else if (subOrderMarketing.MarketingTypes.HasFlag(MarketingType.Groupon))
                    subOrder.OrderType = Entities.OrderInfo.OrderTypes.FightGroup;
                else if (subOrderMarketing.MarketingTypes.HasFlag(MarketingType.FlashSale))
                    subOrder.OrderType = Entities.OrderInfo.OrderTypes.LimitBuy;
            }
        }

        public static void ExecPrepro(OrderPreproResult prepro, MarketingCommand command)
        {
            var order = Convert(prepro);

            var recordSource = new List<GeneralRecord>();
            var recordResults = new List<GeneralRecord>();
            //可用优惠卷
            recordSource.AddRange(CouponService.GetMemberRecord(prepro.MemberId));
            //可用代金红包
            recordSource.AddRange(bonusService.GetMemberBonus(prepro.MemberId));
            foreach (var subOrder in order.SubOrders)
            {
                //限时购
                if (command.FlashSaleId > 0)
                    ExecPreproByFlashSale(subOrder, command.FlashSaleId);

                //拼团
                if (command.GrouponId > 0)
                    ExecPreproByGroupon(subOrder, command.GrouponId, command.GrouponGroupId);

                //组合购
                if (command.CollocationId > 0)
                    ExecPreproByCollocation(subOrder, command.CollocationId);

                //会员价(仅自营店)
                if (subOrder.ShopId == 1)
                    ExecMemberDiscount(subOrder, order.MemberId);

                //满额减
                ExecByFullDiscount(subOrder);

                //可用优惠卷与代金红包
                var recordResult = ExecPreproByGeneralRecord(subOrder.ShopId, subOrder.Items, recordSource.Where(p => p.ShopId == subOrder.ShopId).ToList());

                //最优or选中处理
                var choice = command.Choices.FirstOrDefault(p => p.ShopId == subOrder.ShopId);
                ExecPreproOptimalRecord(recordResult, choice);
                recordResults.AddRange(recordResult);

                //满额免运费
                var shop = ShopService.GetShopData(subOrder.ShopId);

                if (shop.FreeFreight > 0 && subOrder.Amount >= shop.FreeFreight)
                {
                    subOrder.FreeShipping = true;
                    subOrder.FreeFreight = shop.FreeFreight;
                }
            }

            //平台券
            var items = order.SubOrders.SelectMany(p => p.Items).ToList();
            var platformResult = ExecPreproByGeneralRecord(0, items, recordSource.Where(p => p.RecordType == GeneralRecordType.Platform).ToList());
            //最优处理
            var platformChoice = command.Choices.FirstOrDefault(p => p.ShopId == 0);
            ExecPreproOptimalRecord(platformResult, platformChoice);
            recordResults.AddRange(platformResult);

            //填充结果
            Fill(prepro, order, recordResults);
        }

        /// <summary>
        /// 预处理 通用券
        /// </summary>
        private static List<GeneralRecord> ExecPreproByGeneralRecord(long shopId, List<MarketingOrderItem> orderItems, List<GeneralRecord> records)
        {
            var result = new List<GeneralRecord>();
            if (records.Count == 0) return result;//无可用优惠卷
            orderItems = MutualExclusion(orderItems, MarketingType.Coupon);
            if (orderItems.Count == 0) return result;//无可用项目

            var coupons = CouponService.GetAvailable(shopId);
            foreach (var record in records)
            {
                var items = orderItems;
                if (record.RecordType == GeneralRecordType.Coupon && record.IsLimit)
                {//部分可用
                    var coupon = coupons.FirstOrDefault(p => p.Id == record.MarketId);
                    if (coupon == null) continue;//无效优惠券
                    items = orderItems.Where(p => coupon.Products.Contains(p.ProductId)).ToList();
                }
                else if (record.RecordType == GeneralRecordType.Platform && record.IsLimit)
                {
                    var coupon = coupons.FirstOrDefault(p => p.Id == record.MarketId);
                    if (coupons == null) continue;//无效优惠券
                    items = orderItems.Where(p => coupon.Shops.Contains(p.ShopId)).ToList();
                }

                if (items.Count == 0)
                    continue; //无可用商品

                var amount = items.Sum(p => p.Amount);

                if (record.RecordType == GeneralRecordType.Platform)
                {//平台券门槛 为商品原价
                    if (record.OrderAmount > items.Sum(p => p.Price * p.Quantity))
                        continue;
                }
                else if (record.OrderAmount > amount)
                    continue;//不满足使用门槛

                record.Items = items;
                record.ActualAmount = Math.Min(record.Amount, amount);

                result.Add(record);
            }
            return result;
        }

        /// <summary>
        /// 预处理 最优通用券
        /// </summary>
        private static void ExecPreproOptimalRecord(List<GeneralRecord> records, GeneralRecordChoice choice)
        {
            //默认最优
            var select = records.OrderByDescending(p => p.ActualAmount).FirstOrDefault();
            var iscancel = false;
            if (choice != null)
            {
                select = records.FirstOrDefault(p => p.Id == choice.RecordId && p.RecordType == choice.RecordType);
                iscancel = choice.RecordId == 0;
            }
            if (select != null && select.ActualAmount > 0 && !iscancel)
            {
                select.Selected = true;//标记选中
                Divide(select.Items, select.ActualAmount, MarketingType.Coupon);//分摊优惠
            }
        }

        /// <summary>
        /// 创建订单 使用优惠券
        /// </summary>
        private static void ExecCreatingByGenralRecord(long shopId, List<MarketingOrderItem> items, GeneralRecord record)
        {
            if (record.IsLimit)
            {//限制范围
                if (record.RecordType == GeneralRecordType.Coupon)
                {
                    var coupon = CouponService.GetAvailable(shopId).FirstOrDefault(p => p.Id == record.MarketId);
                    if (coupon == null)
                        throw new HimallException("指定的商家优惠券不存在");
                    items = items.Where(p => coupon.Products.Contains(p.ProductId)).ToList();
                }
                else if (record.RecordType == GeneralRecordType.Platform)
                {
                    var coupon = CouponService.GetAvailable(shopId).FirstOrDefault(p => p.Id == record.MarketId);
                    if (coupon == null)
                        throw new HimallException("指定的平台优惠券不存在");
                    items = items.Where(p => coupon.Shops.Contains(p.ShopId)).ToList();
                }
            }
            if (items.Count == 0)
                throw new HimallException("指定的优惠券无商品可用");
            var amount = items.Sum(p => p.Amount);
            if (record.RecordType == GeneralRecordType.Platform)
            {
                if (items.Sum(p => p.Price * p.Quantity) < record.OrderAmount)
                    throw new HimallException("指定的优惠券未满足使用门槛");
            }
            else if (amount < record.OrderAmount)
                throw new HimallException("指定的优惠券未满足使用门槛");

            record.ActualAmount = Math.Min(amount, record.Amount);
            var marketingType = GetMarketing(record.RecordType);
            Divide(items, record.ActualAmount, marketingType);//分摊优惠

        }

        private static MarketingType GetMarketing(GeneralRecordType type)
        {
            switch (type)
            {
                case GeneralRecordType.Platform:
                    return MarketingType.PlatformCoupon;
                case GeneralRecordType.Coupon:
                    return MarketingType.Coupon;
                case GeneralRecordType.Bouns:
                    return MarketingType.Bonus;
                default:
                    throw new Exception();
            }
        }


        /// <summary>
        /// 订单创建
        /// </summary>
        public static void ExecOrderCreating(OrderCreating order, MarketingCommand command)
        {
            var orderMarketing = Convert(order);
            var choiceCoupons = command.Choices.Where(p => p.RecordType == GeneralRecordType.Coupon || p.RecordType == GeneralRecordType.Platform).Select(p => p.RecordId).ToList();
            var recordData = CouponService.GetMemberRecord(order.Member.Id, choiceCoupons);
            var choiceBouns = command.Choices.Where(p => p.RecordType == GeneralRecordType.Bouns).Select(p => p.RecordId).ToList();
            var bounsData = bonusService.GetMemberBonus(order.Member.Id, choiceBouns);
            foreach (var subOrder in orderMarketing.SubOrders)
            {
                //限时购
                if (command.FlashSaleId > 0)
                    ExecCreatingByFlashSale(subOrder, command.FlashSaleId, order.Member.Id);
                //拼团
                if (command.GrouponId > 0)
                    ExecCreatingByGroupon(subOrder, order.Member.Id, command.GrouponId, command.GrouponGroupId);

                //组合购
                if (command.CollocationId > 0)
                    ExecCreatingByCollocation(subOrder, command.CollocationId);

                //会员折扣
                if (subOrder.ShopId == 1)
                    ExecMemberDiscount(subOrder, order.Member.Id);

                //满额减
                ExecByFullDiscount(subOrder);

                //选择使用优惠券
                var choice = command.Choices.FirstOrDefault(p => p.ShopId == subOrder.ShopId);
                if (choice != null)
                {
                    if (choice.RecordType == GeneralRecordType.Coupon)
                    { //使用商家优惠卷
                        var record = recordData.FirstOrDefault(p => p.Id == choice.RecordId);
                        if (record == null)
                            throw new HimallException("指定的商家优惠券不存在");
                        ExecCreatingByGenralRecord(subOrder.ShopId, subOrder.Items, record);
                        subOrder.MarketingTypes |= MarketingType.Coupon;
                        subOrder.Marketings.Add(new MarketingItem
                        {
                            Amount = record.ActualAmount,
                            MarketId = record.MarketId,
                            Title = record.Title,
                            MarketingType = MarketingType.Coupon
                        });
                    }
                    else if (choice.RecordType == GeneralRecordType.Bouns)
                    { //使用代金红包
                        var bouns = bounsData.FirstOrDefault(p => p.Id == choice.RecordId);
                        if (bouns == null)
                            throw new HimallException("指定的代金红包不存在");
                        ExecCreatingByGenralRecord(subOrder.ShopId, subOrder.Items, bouns);
                        subOrder.MarketingTypes |= MarketingType.Bonus;
                        subOrder.Marketings.Add(new MarketingItem
                        {
                            Amount = bouns.ActualAmount,
                            MarketId = bouns.MarketId,
                            Title = bouns.Title,
                            MarketingType = MarketingType.Bonus
                        });
                    }
                }
                //满额免运费
                var shop = ShopService.GetShopData(subOrder.ShopId);
                if (shop.FreeFreight > 0 && subOrder.Amount >= shop.FreeFreight)
                    subOrder.FreeShipping = true;
            }

            //选择使用平台优惠卷
            var platformChoice = command.Choices.FirstOrDefault(p => p.ShopId == 0);
            if (platformChoice != null)
            {
                var record = recordData.FirstOrDefault(p => p.Id == platformChoice.RecordId);
                if (record == null)
                    throw new HimallException("指定的平台优惠券不存在");
                var orderItems = orderMarketing.SubOrders.SelectMany(p => p.Items).ToList();
                ExecCreatingByGenralRecord(0, orderItems, record);
                foreach (var subOrder in orderMarketing.SubOrders)
                {
                    var amount = subOrder.Items.Select(p => p.DiscountDetails.FirstOrDefault(i => i.MarketingType == MarketingType.PlatformCoupon)).Sum(p => p?.Amount ?? 0);
                    subOrder.MarketingTypes |= MarketingType.PlatformCoupon;
                    subOrder.Marketings.Add(new MarketingItem
                    {
                        Title = record.Title,
                        MarketId = record.MarketId,
                        MarketingType = MarketingType.PlatformCoupon,
                        Amount = amount,
                    });
                }
            }

            Fill(order, orderMarketing, command);
        }

        /// <summary>
        /// 订单创建失败
        /// </summary>
        internal static void ExecOrderFail(OrderCreating order, MarketingCommand command)
        {
            var marketing = HttpContext.Current.Items["OrderCreatingMarketing"] as MarketingOrder;
            foreach (var subOrder in marketing.SubOrders)
            {
                if ((subOrder.MarketingTypes & MarketingType.FlashSale) > 0)
                {
                    var item = subOrder.Items.FirstOrDefault(p => (p.MarketingTypes & MarketingType.FlashSale) > 0);
                    if (item != null)//还原活动库存
                        LimitTimeBuyService.Restore(command.FlashSaleId, item.SkuId, item.Quantity);
                }
            }
        }

        #region FlashSale
        private static void ExecPreproByFlashSale(MarketingSubOrder subOrder, long flashSaleId)
        {
            if (flashSaleId == 0) return;//未选中限时购活动
            var flash = LimitTimeBuyService.GetGoing(flashSaleId);
            if (flash == null) return;//选中活动不存在

            var item = subOrder.Items.FirstOrDefault(p => p.ProductId == flash.ProductId);
            var sku = flash.Items.FirstOrDefault(p => p.SkuId == item.SkuId);

            subOrder.MarketingTypes |= MarketingType.FlashSale;
            item.MarketingTypes |= MarketingType.FlashSale;
            item.Price = sku.Price;
        }

        private static void ExecCreatingByFlashSale(MarketingSubOrder subOrder, long flashSaleId, long memberId)
        {
            if (flashSaleId == 0) return;//未选中限时购活动
            var flash = LimitTimeBuyService.GetGoing(flashSaleId);
            if (flash == null)
                throw new HimallException("指定的限时购活动不存在");

            var item = subOrder.Items.FirstOrDefault(p => p.ProductId == flash.ProductId);
            var sku = flash.Items.FirstOrDefault(p => p.SkuId == item.SkuId);

            if (flash.LimitCountOfThePeople > 0)
            {
                if (item.Quantity > flash.LimitCountOfThePeople)
                    throw new HimallException("超过限时购限购数量");
                var buyCount = LimitTimeBuyService.GetBuyCount(flashSaleId, memberId, item.ProductId);
                if (item.Quantity + buyCount > flash.LimitCountOfThePeople)
                    throw new HimallException("超过限时购限购数量");
            }

            if (!LimitTimeBuyService.Decrease(flashSaleId, item.SkuId, item.Quantity))
                throw new HimallException("限时购活动库存不足");

            subOrder.MarketingTypes |= MarketingType.FlashSale;
            item.MarketingTypes |= MarketingType.FlashSale;
            item.Price = sku.Price;
        }

        #endregion FlashSale

        #region Groupon
        private static void ExecPreproByGroupon(MarketingSubOrder subOrder, long grouponId, long groupId)
        {
            if (grouponId == 0) return;//未选中活动
            var groupon = FightGroupService.GetGoing(grouponId);
            if (groupon == null) return;//选中活动不存在

            var item = subOrder.Items.FirstOrDefault(p => p.ProductId == groupon.ProductId);
            var sku = groupon.Items.FirstOrDefault(p => p.SkuId == item.SkuId);
            subOrder.MarketingTypes |= MarketingType.Groupon;
            item.MarketingTypes |= MarketingType.Groupon;
            item.Price = sku.ActivePrice;
        }

        private static void ExecCreatingByGroupon(MarketingSubOrder subOrder, long memberId, long grouponId, long groupId)
        {
            if (grouponId == 0) return;//未选中活动
            var groupon = FightGroupService.GetGoing(grouponId);
            if (groupon == null)
                throw new HimallException("选中的拼团活动不存在");

            if (groupId > 0)
            { //参团
                var group = FightGroupService.GetGroup(groupId);
                if (group.GroupStatus != FightGroupBuildStatus.Ongoing || group.TimeOut < DateTime.Now)
                    throw new HimallException("参与的团无效");
                if (group.Items.Any(p => p.OrderUserId == memberId))
                    throw new HimallException("请勿重复参团");
            }
            else
            { //开团
                if (groupon.StartTime > DateTime.Now)
                    throw new HimallException("拼团活动尚未开始");
                if (groupon.EndTime < DateTime.Now)
                    throw new HimallException("拼团活动已结束");
                if (FightGroupService.ExistGrouping(grouponId, memberId))
                    throw new HimallException("已经存在拼团中的团");
            }
            var item = subOrder.Items.FirstOrDefault();
            var groupItem = groupon.Items.FirstOrDefault(p => p.SkuId == item.SkuId);

            // 限购检查
            if (groupon.LimitQuantity > 0)
            {
                if (item.Quantity > groupon.LimitQuantity)
                    throw new HimallException("超过拼团最大限购数");
                var buyCount = FightGroupService.GetBuyCount(grouponId, memberId);
                if (item.Quantity + buyCount > groupon.LimitQuantity)
                    throw new HimallException("超过拼团最大限购数");
            }

            subOrder.MarketingTypes |= MarketingType.Groupon;
            item.MarketingTypes |= MarketingType.Groupon;
            item.Price = groupItem.ActivePrice;
        }
        #endregion Groupon

        #region Collocation
        private static void ExecPreproByCollocation(MarketingSubOrder subOrder, long collocationId)
        {
            if (collocationId == 0) return; //未选中组合购活动
            var collocation = CollocationService.GetGoing(collocationId);
            if (collocation == null) return;//未找到进行中活动忽略

            subOrder.MarketingTypes |= MarketingType.Collocation;
            foreach (var item in subOrder.Items)
            {
                var skuItem = collocation.Products.FirstOrDefault(p => p.SkuId == item.SkuId);
                if (skuItem != null)
                {
                    item.MarketingTypes |= MarketingType.Collocation;
                    item.Price = skuItem.Price;
                }
            }
        }

        private static void ExecCreatingByCollocation(MarketingSubOrder subOrder, long collocationId)
        {
            if (collocationId == 0) return; //未选中组合购活动
            var collocation = CollocationService.GetGoing(collocationId);
            if (collocation == null)
                throw new HimallException("指定组合购活动不存在");

            var mainItem = subOrder.Items.FirstOrDefault(p => p.ProductId == collocation.ProductId);
            if (mainItem == null)
                throw new HimallException("未选购买组合购主商品");

            subOrder.MarketingTypes |= MarketingType.Collocation;
            foreach (var item in subOrder.Items)
            {
                var skuItem = collocation.Products.FirstOrDefault(p => p.SkuId == item.SkuId);
                if (skuItem != null)
                {
                    item.MarketingTypes |= MarketingType.Collocation;
                    item.Price = skuItem.Price;
                }
            }
        }
        #endregion

        #region FullDiscount
        private static void ExecByFullDiscount(MarketingSubOrder subOrder)
        {
            //满额减
            var allItems = MutualExclusion(subOrder, MarketingType.FullDiscount);
            if (allItems.Count == 0) return;//无可用商品

            var discounts = FullDiscountService.GetGoingByShop(subOrder.ShopId).ToList();
            foreach (var discount in discounts)
            {
                var items = allItems;
                if (!discount.IsAllProduct)
                    items = allItems.Where(p => discount.Products.Contains(p.ProductId)).ToList();
                if (items.Count == 0)
                    continue;//无可用商品
                var amount = items.Sum(p => p.Amount);
                var rule = discount.Rules.LastOrDefault(p => p.Quota <= amount);
                if (rule == null) continue;//未满足满减条件

                //分摊优惠
                Divide(items, rule.Discount, MarketingType.FullDiscount);
                subOrder.MarketingTypes |= MarketingType.FullDiscount;
                subOrder.Marketings.Add(new MarketingItem
                {
                    Amount = rule.Discount,
                    MarketId = discount.Id,
                    MarketingType = MarketingType.FullDiscount
                });
                items.ForEach(item => item.MarketingTypes |= MarketingType.FullDiscount);

            }
        }

        #endregion FullDiscount

        private static void ExecMemberDiscount(MarketingSubOrder subOrder, long memberId)
        {
            var discount = MemberApplication.GetMemberDiscount(memberId);
            if (discount == 1) return;
            var items = MutualExclusion(subOrder, MarketingType.MemberDiscount);
            foreach (var item in items)
            {
                item.Price = FormatMonty(item.Price * discount);
                item.MarketingTypes |= MarketingType.MemberDiscount;
            }
        }
        /// <summary>
        /// 分摊计算
        /// </summary>
        private static void Divide(List<MarketingOrderItem> items, decimal discount, MarketingType marketingType)
        {
            var total = items.Sum(p => p.Amount);
            var sum = 0M;
            var price = 0M;
            var index = 1;
            //从价格低->高进行比例分摊
            foreach (var item in items.OrderBy(p => p.Amount))
            {
                if (index == items.Count)
                    price = discount - sum; //余下的优惠
                else
                    price = FormatMonty(discount * item.Amount / total);
                sum += price - DiscountItem(item, price, marketingType);
                index++;
            }
        }

        private static decimal DiscountItem(MarketingOrderItem item, decimal discount, MarketingType marketingType)
        {
            var minPrice = 0M; //单个商品优惠极限价格
            var threshold = item.Price * item.Quantity - minPrice; //优惠极限阀值
            var overflow = 0M; //溢出值
            var itemDiscount = item.Discount;
            if (threshold < itemDiscount + discount)
            {
                overflow = itemDiscount + discount - threshold; //优惠溢出
                discount -= overflow;
            }
            item.Discount += discount;
            item.MarketingTypes |= marketingType;
            item.DiscountDetails.Add(new MarketingOrderItemDiscount
            {
                MarketingType = marketingType,
                Amount = discount
            });
            return overflow;
        }

        private static decimal FormatMonty(decimal money) =>
             Math.Floor(money * 100) / 100M;




    }
}