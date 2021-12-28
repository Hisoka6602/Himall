using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Extends;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.Market;
using Himall.DTO.Product;
using Himall.Entities;
using Himall.Service;
using Himall.ServiceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Himall.DTO.OrderCreateCommand;

namespace Himall.Application
{
    /// <summary>
    /// 订单流程处理
    /// </summary>
    public class OrderProcessApplication : BaseApplicaion
    {
        private static OrderService orderService = ObjectContainer.Current.Resolve<OrderService>();
        private static ShopService shopService = ObjectContainer.Current.Resolve<ShopService>();

        /// <summary>
        /// 预订单
        /// </summary>
        public static OrderPreproResult Prepro(OrderPreproCommand command)
        {
            var setting = SiteSettingApplication.SiteSettings;
            var result = new OrderPreproResult
            {
                MemberId = command.MemberId,
                SubOrders = new List<OrderPreproResult.SubOrder> { }
            };

            //会员资产
            var assets = GetService<MemberService>().GetAssets(command.MemberId);
            result.IsPassword = !string.IsNullOrEmpty(assets.PayPassword);
            result.Integral = assets.Integral;
            result.CapitalAmount = assets.Balance;

            //购物车还原商品项目
            if (command.Items == null || command.Items.Count == 0)
            {
                var items = CartApplication.GetCartItems(command.CartItems);
                command.Items = items.Select(item => new OrderPreproCommand.ProductItem
                {
                    ProductId = item.ProductId,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    RoomId = item.RoomId,
                }).ToList();
            }

            //商品信息构建
            var products = GetService<ProductService>().GetProductData(command.Items.Select(p => p.ProductId).Distinct().ToList());
            command.IsVirtual = products.Exists(p => p.ProductType == 1);
            //收货地址
            if (!command.IsVirtual) //虚拟订单忽略收货地址
            {
                var addresses = ShippingAddressApplication.GetAddress(command.MemberId);
                if (command.AddressId > 0)
                    result.Address = addresses.FirstOrDefault(p => p.Id == command.AddressId);
                else
                {
                    result.Address = addresses.FirstOrDefault(p => p.IsDefault);
                    if (result.Address == null)
                        result.Address = addresses.FirstOrDefault();
                }
            }

            var productItems = new List<OrderPreproResult.ProductItem>();
            foreach (var item in command.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                var sku = product.Skus.FirstOrDefault(p => p.Id == item.SkuId);
                var productItem = new OrderPreproResult.ProductItem
                {
                    ProductId = product.Id,
                    SkuId = sku.Id,
                    ShopId = product.ShopId,
                    FreightTemplateId = product.FreightTemplateId,
                    Color = sku.Color,
                    ColorAlias = sku.ColorAlias,
                    Size = sku.Size,
                    SizeAlias = sku.SizeAlias,
                    Version = sku.Version,
                    VersionAlias = sku.VersionAlias,
                    Price = sku.SalePrice,
                    Name = product.ProductName,
                    Thumbnail = !string.IsNullOrEmpty(sku.ShowPic) ? HimallIO.GetRomoteImagePath(sku.ShowPic) : HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_350),
                    Quantity = item.Quantity,
                    Volume = product.Volume,
                    Weight = product.Weight,
                    ProductCode = product.ProductCode,
                    RoomId = item.RoomId,
                };

                if (product.IsOpenLadder)
                {//阶梯价商品
                    var productQuantity = command.Items.Where(p => p.ProductId == item.ProductId).Sum(p => p.Quantity);
                    var ladder = product.LadderPrice.LastOrDefault(p => p.MinBath <= productQuantity);
                    if (ladder == null)
                        productItem.Exception = "未满足最低起购量";
                    else
                        productItem.Price = ladder.Price;
                }

                if (product.ProductType == 1)
                { //虚拟商品
                    productItem.VirtualItems = product.VirtualData.Items.Select(p => new OrderPreproResult.VirtualItem
                    {
                        Name = p.Name,
                        Type = p.Type,
                        Required = p.Required,
                        Id = p.Id
                    }).ToList();
                }
                productItem.Amount = productItem.Price * productItem.Quantity;
                productItems.Add(productItem);
            }

            //补充门店相关信息
            foreach (var shopProducts in productItems.GroupBy(p => p.ShopId))
            {
                var shop = GetService<ShopService>().GetShopData(shopProducts.Key);
                var subOrder = new OrderPreproResult.SubOrder
                {
                    ShopId = shop.Id,
                    ShopName = shop.ShopName,
                    Items = shopProducts.ToList(),

                };
                //发票相关
                var invoiceSetting = shopService.GetInvoiceConfig(shop.Id);
                if (invoiceSetting != null)
                {
                    var min = SiteSettingApplication.SiteSettings.SalesReturnTimeout;
                    var max = min + invoiceSetting.VatInvoiceDay;
                    var day = min == max ? $"{min}" : $"{min}-{max}";
                    subOrder.IsInvoice = invoiceSetting.IsInvoice;
                    subOrder.Invoice = new OrderPreproResult.InvoiceConfig
                    {
                        IsInvoice = invoiceSetting.IsInvoice,
                        IsPlainInvoice = invoiceSetting.IsPlainInvoice,
                        IsElectronicInvoice = invoiceSetting.IsElectronicInvoice,
                        PlainInvoiceRate = invoiceSetting.PlainInvoiceRate,
                        IsVatInvoice = invoiceSetting.IsVatInvoice,
                        VatInvoiceRate = invoiceSetting.VatInvoiceRate,
                        VatInvoiceDay = day,
                    };
                }
                //发货方式
                if (setting.IsOpenStore && result.Address != null)
                { //开启门店
                    var address = result.Address;
                    var city = GetService<RegionService>().GetRegion(address.RegionId, Region.RegionLevel.City);
                    var items = subOrder.Items.ToDictionary(p => p.SkuId, v => v.Quantity);
                    var stores = GetService<ShopBranchService>().GetStores(subOrder.ShopId, city.Id, address.Longitude, address.Latitude, items);
                    if (stores.Count > 0)
                    {
                        var data = GetService<ShopBranchService>().GetShopBranchData(stores);
                        subOrder.IsPickup = data.Any(p => p.IsAboveSelf);
                        subOrder.IsStoreDelive = data.Any(p => p.IsStoreDelive);
                    }
                }

                if (command.DeliverTypes != null)
                {
                    var deliveryType = command.DeliverTypes.FirstOrDefault(p => p.ShopId == shop.Id);
                    subOrder.DeliveryType = deliveryType?.DeliveryType ?? DeliveryType.Express;
                }

                if (subOrder.ShopId == 1 && command.PlatformType == PlatformType.WeiXin)
                    subOrder.IsCashOnDelivery = GetService<PaymentConfigService>().IsEnable();

                result.SubOrders.Add(subOrder);
            }
            //允许开票 
            if (result.SubOrders.Any(p => p.IsInvoice))
                result.InvoiceContext = orderService.GetInvoiceContexts().Select(p => p.Name).ToList();

            //虚拟商品 需要核销地址
            if (command.IsVirtual)
            {
                if (command.ShopBranchId > 0)
                { //门店获取门店地址
                    var branch = GetService<ShopBranchService>().GetShopBranchData(command.ShopBranchId);
                    result.PickupAddress = new OrderPreproResult.PickupAddressData
                    {
                        Address = RegionApplication.GetFullName(branch.AddressId) + branch.AddressDetail,
                        Contact = branch.ContactPhone
                    };
                }
                else
                { //非门店获取商家默认地址
                    var subOrder = result.SubOrders.FirstOrDefault();
                    var shipper = GetService<ShopShippersService>().GetDefaultVerificationShipper(subOrder.ShopId);
                    if (shipper != null)
                    {
                        result.PickupAddress = new OrderPreproResult.PickupAddressData
                        {
                            Address = RegionApplication.GetFullName(shipper.RegionId) + shipper.Address,
                            Contact = shipper.TelPhone
                        };
                    }
                    else
                    {
                        result.PickupAddress = new OrderPreproResult.PickupAddressData
                        {
                            Address = "",
                            Contact = ""
                        };
                    }
                }
            }


            //门店提交订单
            if (command.ShopBranchId > 0)
            {
                var branch = ShopBranchApplication.GetShopBranchById(command.ShopBranchId);
                var subOrder = result.SubOrders.FirstOrDefault(p => p.ShopId == branch.ShopId);
                subOrder.ShopBranchId = branch.Id;
                subOrder.ShopBranchName = branch.ShopBranchName;
                subOrder.IsPickup = branch.IsAboveSelf;//允许自提
                subOrder.IsStoreDelive = branch.IsStoreDelive;//允许门店配送
                subOrder.ShopBranchAttach = new OrderPreproResult.ShopBranchAttach
                {
                    Address = branch.AddressDetail,
                    OpenTime = branch.StoreOpenStartTime.ToString(@"hh\:mm"),
                    CloseTime = branch.StoreOpenEndTime.ToString(@"hh\:mm"),
                    Contact = branch.ContactPhone
                };
                if (subOrder.DeliveryType == DeliveryType.Express)
                    subOrder.DeliveryType = DeliveryType.ShopStore;//默认门店配送
                if (result.Address != null)
                {
                    var distance = Instance<ShopBranchService>.Create.GetLatLngDistancesFromAPI($"{branch.Latitude},{branch.Longitude}", $"{result.Address.Latitude},{result.Address.Longitude}");
                    if (distance > branch.ServeRadius)
                        result.Address = null;
                }
            }

            //营销活动
            MarketingApplication.ExecPrepro(result, new MarketingCommand
            {
                CollocationId = command.CollocationId,
                FlashSaleId = command.FlashSaleId,
                GrouponGroupId = command.GroupId,
                GrouponId = command.GrouponId,
                Choices = command.Records ?? new List<GeneralRecordChoice>()
            });

            //积分抵扣相关
            if (result.Integral > 0)
            {
                //抵扣规则
                var rule = GetService<MemberIntegralService>().GetIntegralChangeRule();
                result.IntegralPerMoney = rule.IntegralPerMoney;
                //最大抵扣金额
                result.IntegralMaxMoney = FormatMonty(result.Amount * setting.IntegralDeductibleRate / 100);
            }
            //虚拟订单不计算运费
            if (!command.IsVirtual)
            {
                //快递运费模板计算
                foreach (var shop in result.SubOrders)
                {
                    if (shop.DeliveryType == DeliveryType.Express && !shop.FreeFreight) //快递配送
                        shop.Freight = GetFreight(result.Address, shop.Items.Select(item => new FreightItem
                        {
                            FreightTemplateId = item.FreightTemplateId,
                            Volume = item.Volume,
                            Weight = item.Weight,
                            Quantity = item.Quantity,
                            Amount = item.Amount,
                            ProductId = item.ProductId
                        }).ToList());
                    else if (shop.DeliveryType == DeliveryType.ShopStore && shop.ShopBranchId > 0) //门店配送 
                    {
                        var store = ShopBranchApplication.GetStores(command.ShopBranchId);
                        if (shop.Amount < store.DeliveTotalFee)
                            shop.Exception = "未满足订单起送费用";
                        if (store.IsFreeMail && shop.Amount >= store.FreeMailFee)
                            continue;// 包邮
                        if (shop.FreeFreight)
                            continue;// 包邮
                        shop.Freight = store.DeliveFee;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 获取运费
        /// </summary>
        /// <param name="address">地址信息</param>
        /// <param name="products">商品信息</param>
        /// <returns></returns>
        private static Decimal GetFreight(ShippingAddressData address, List<FreightItem> products)
        {
            int addressId = address == null ? 0 : address.RegionId;
            List<long> excludeIds = new List<long>();//排除掉包邮的商品
            var productIds = products.Select(p => p.ProductId).ToList();
            var productInfos = ProductManagerApplication.GetProductsByIds(productIds);//商家下所有的商品集合
            if (productInfos != null && productInfos.Count > 0)
            {
                var templateIds = productInfos.Select(a => a.FreightTemplateId).ToList();
                if (templateIds != null && templateIds.Count > 0)
                {
                    templateIds.ForEach(tid =>
                    {
                        var ids = productInfos.Where(a => a.FreightTemplateId == tid).Select(b => b.Id).ToList();//属于当前模板的商品ID集合
                        bool isFree = false;
                        var freeRegions = ServiceProvider.Instance<FreightTemplateService>.Create.GetShippingFreeRegions(tid);
                        freeRegions.ForEach(c =>
                        {
                            c.RegionSubList = ServiceProvider.Instance<RegionService>.Create.GetSubsNew(c.RegionId, true).Select(a => a.Id).ToList();
                        });
                        var regions = freeRegions.Where(d => d.RegionSubList.Contains(addressId)).ToList();//根据模板设置的包邮地区过滤出当前配送地址所在地址
                        if (regions != null && regions.Count > 0)
                        {
                            var groupIds = regions.Select(e => e.GroupId).ToList();
                            var freeGroups = ServiceProvider.Instance<FreightTemplateService>.Create.GetShippingFreeGroupInfos(tid, groupIds);

                            //只要有一个符合包邮条件，则退出
                            long count = products.Where(p => ids.Contains(p.ProductId)).Sum(p => p.Quantity);
                            decimal amount = products.Where(p => ids.Contains(p.ProductId)).Sum(p => p.Amount);
                            freeGroups.ForEach(f =>
                            {
                                if (f.ConditionType == 1)//购买件数
                                {
                                    if (count >= int.Parse(f.ConditionNumber))
                                    {
                                        isFree = true;
                                        return;
                                    }
                                }
                                else if (f.ConditionType == 2)//金额
                                {
                                    if (amount >= decimal.Parse(f.ConditionNumber))
                                    {
                                        isFree = true;
                                        return;
                                    }
                                }
                                else if (f.ConditionType == 3)//件数+金额
                                {
                                    var condition1 = int.Parse(f.ConditionNumber.Split('$')[0]);
                                    var condition2 = decimal.Parse(f.ConditionNumber.Split('$')[1]);
                                    if (count >= condition1 && amount >= condition2)
                                    {
                                        isFree = true;
                                        return;
                                    }
                                }
                            });
                        }
                        if (isFree)
                        {
                            excludeIds.AddRange(ids);
                        }

                    });
                }

                //要排除掉指定地区包邮的商品ID
                IEnumerable<long> pIds = productIds.Where(p => !excludeIds.Contains(p)).ToList();
                IEnumerable<int> pCounts = products.Where(p => !excludeIds.Contains(p.ProductId)).Select(p => p.Quantity).ToList();
                decimal freight = 0;
                if (pIds != null && pIds.Count() > 0 && pCounts != null && pCounts.Count() > 0)
                {
                    ProductService _ProductService = ObjectContainer.Current.Resolve<ProductService>();
                    freight = _ProductService.GetFreight(pIds, pCounts, addressId);
                }
                return freight;
            }

            return 0;
        }


        /// <summary>
        /// 计算邮费
        /// </summary>
        //private static decimal GetFreight(ShippingAddressData address, List<FreightItem> items)
        //{
        //    if (address == null)
        //        return 0;//无收货地址

        //    var freght = 0M;
        //    foreach (var templateGroup in items.GroupBy(p => p.FreightTemplateId))
        //    {
        //        if (templateGroup.Key == 0)
        //            continue;//未设置运费模板
        //        var template = GetService<FreightTemplateService>().GetTempalteData(templateGroup.Key);
        //        if (template.IsFree)
        //            continue;//包邮
        //        if (template.FreeRulesMap.TryGetValue(address.RegionId, out var serial))
        //        {
        //            var freeRule = template.FreeRules.FirstOrDefault(p => p.Serial == serial);
        //            if (freeRule.FreeType == FreightTempateFreeType.Piece && templateGroup.Sum(p => p.Quantity) >= freeRule.Piece)
        //                continue;
        //            else if (freeRule.FreeType == FreightTempateFreeType.Amount && templateGroup.Sum(p => p.Amount) >= freeRule.Amount)
        //                continue;
        //            else if (freeRule.FreeType == FreightTempateFreeType.PieceAndAmount && templateGroup.Sum(p => p.Quantity) >= freeRule.Piece && templateGroup.Sum(p => p.Amount) >= freeRule.Amount)
        //                continue;
        //        }
        //        template.RulesMap.TryGetValue(address.RegionId, out var ruleId);
        //        var rule = template.Rules.FirstOrDefault(p => p.Id == ruleId);
        //        if (rule == null)
        //            rule = template.Rules.FirstOrDefault(p => p.IsDefault);
        //        var totalValue = 0M;
        //        if (template.ValuationMethod == ValuationMethodType.Piece)//按件数
        //            totalValue = templateGroup.Sum(p => p.Quantity);
        //        else if (template.ValuationMethod == ValuationMethodType.Weight)//按重量
        //            totalValue = templateGroup.Sum(p => p.Weight * p.Quantity);
        //        else if (template.ValuationMethod == ValuationMethodType.Bulk)
        //            totalValue = templateGroup.Sum(p => p.Volume * p.Quantity); //按体积
        //        freght += GetFreght(totalValue, rule);
        //    }
        //    return freght;
        //}
        /// <summary>
        /// <summary>
        /// 按计算规则计算费用
        /// </summary>
        private static decimal GetFreght(decimal total, FreightTemplateRuleData rule)
        {
            total -= rule.FirstUnit;
            if (total <= 0)
                return rule.FirstUnitMonry;
            var number = Math.Ceiling(total / rule.AccumulationUnit);
            return rule.FirstUnitMonry + rule.AccumulationUnitMoney * number;
        }

        /// <summary>
        /// 提交订单
        /// </summary>
        public static OrderSubmitResult Submit(OrderCreateCommand command)
        {
            var source = Verify(command);
            var order = BuildOrder(command, source);
            var marketingCommand = new MarketingCommand
            {
                CollocationId = command.CollocationId,
                FlashSaleId = command.FlashSaleId,
                GrouponGroupId = command.GrouponGroupId,
                GrouponId = command.GrouponId,
                Choices = command.Choices,
            };
            try
            {
                MarketingApplication.ExecOrderCreating(order, marketingCommand);

                //计算运费
                OrderCreatingByFreight(order, source);

                //处理积分
                if (command.UseIntegral > 0)
                    IntegralDivide(order, command.UseIntegral);

                //计算发票相关税费
                OrderCreateingByInvoice(order, command);

                //处理预存款
                if (command.UseCapital > 0)
                    OrderCreatingByCapital(order, command);

                if (command.Amount != order.SubOrders.Sum(p => p.OrderAmount - p.Capital))
                    throw new HimallException("订单金额发生变化，请重新提交订单");

                //扣减库存
                DecreaseStock(order);

                orderService.CreateOrder(order);
                //分销
                if (SiteSettingApplication.SiteSettings.DistributionIsEnable)
                {
                    try
                    {
                        Instance<DistributionService>.Create.TreatedOrderDistribution(order.Member.Id, order.SubOrders.Select(p => p.OrderId).ToList());
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"订单Created:分销异常:{ex.Message},{string.Join(",", order.SubOrders.Select(p => p.OrderId))}", ex);
                    }
                }
                else
                {
                    Log.Info("分销日志：站点没有开启分销功能");
                }
                Task.Factory.StartNew(() =>
                {
                    OrderCreated(order, command);
                });
            }
            catch (Exception ex)
            {
                //订单创建
                RestoreStock(order);
                //营销失败还原
                MarketingApplication.ExecOrderFail(order, marketingCommand);
                throw ex;
            }
            var paidOrders = order.SubOrders.Where(p => (p.OrderAmount - p.Capital) <= 0).ToList();
            if (paidOrders.Count > 0)
            {
                var list = paidOrders.Select(p => p.OrderId).ToList();
                WDTConfigModel wDTConfigModel = WDTOrderApplication.GetConfigModel();
                orderService.PaySucceed(list, "预存款支付", DateTime.Now, wDTConfigModel);
            }


            return new OrderSubmitResult
            {
                Orders = order.SubOrders.Select(p => p.OrderId).ToList(),
                Amount = order.SubOrders.Sum(p => p.OrderAmount - p.Capital)
            };
        }
        /// <summary>
        /// 分摊积分优惠
        /// </summary>
        private static void IntegralDivide(OrderCreating order, int integral)
        {
            var rule = GetService<MemberIntegralService>().GetIntegralChangeRule();
            var discount = FormatMonty(integral / (decimal)rule.IntegralPerMoney);
            var total = order.SubOrders.Sum(p => p.ProductAmount);
            var index = 1;
            var amount = 0M;
            foreach (var item in order.SubOrders.OrderBy(p => p.ProductAmount))
            {
                var itemAmount = 0M;
                if (index == order.SubOrders.Count)
                    itemAmount = discount - amount;
                else
                    itemAmount = FormatMonty(discount * item.ProductAmount / total);
                item.IntegralDiscount = itemAmount;
                amount += itemAmount;
                index++;
            }
        }

        /// <summary>
        /// 订单创建运费计算
        /// </summary>
        private static void OrderCreatingByFreight(OrderCreating order, OrderCreateingSource source)
        {
            foreach (var subOrder in order.SubOrders)
            {
                if (subOrder.DeliveryType == DeliveryType.Express && !subOrder.FreeFreight) //快递配送
                    subOrder.Freight = GetFreight(source.Address, subOrder.Items.Select(item =>
                     {
                         var product = source.Products.FirstOrDefault(p => p.Id == item.ProductId);
                         return new FreightItem
                         {
                             FreightTemplateId = product.FreightTemplateId,
                             Volume = product.Volume,
                             Weight = product.Weight,
                             Quantity = item.Quantity,
                             Amount = item.Amount,
                             ProductId = item.ProductId,
                         };
                     }).ToList());

                else if (subOrder.DeliveryType == DeliveryType.ShopStore) //门店配送 
                {
                    var branch = ShopBranchApplication.GetShopBranchById(subOrder.ShopBranchId);
                    if (branch == null)
                        throw new HimallException("请选择门店");
                    var amount = subOrder.Items.Sum(p => p.SalePrice * p.Quantity);
                    if (amount < branch.DeliveTotalFee)
                        throw new HimallException("未满足订单起送费用");
                    if (subOrder.FreeFreight)
                        continue;// 包邮
                    if (branch.IsFreeMail && subOrder.ProductAmount >= branch.FreeMailFee)
                        continue;// 包邮
                    subOrder.Freight = branch.DeliveFee;
                }
            }
        }
        /// <summary>
        /// 计算税费
        /// </summary>
        private static void OrderCreateingByInvoice(OrderCreating order, OrderCreateCommand source)
        {
            foreach (var subOrder in order.SubOrders)
            {
                if (subOrder.Invoice == null || subOrder.Invoice.InvoiceType == InvoiceType.None)
                    continue;//无需开票

                var config = GetService<ShopService>().GetInvoiceConfig(subOrder.ShopId);
                if (!config.IsInvoice)
                    throw new HimallException("商家未开启开票");

                if (subOrder.Invoice.InvoiceType == InvoiceType.VATInvoice)
                {//增值税发票
                    if (!config.IsVatInvoice)
                        throw new HimallException("商家未开启增值税发票");
                    subOrder.Tax = FormatMonty((subOrder.ProductAmount - subOrder.IntegralDiscount) * config.VatInvoiceRate / 100);
                    subOrder.Invoice.VatInvoiceDay = config.VatInvoiceDay;
                }
                else if (subOrder.Invoice.InvoiceType == InvoiceType.ElectronicInvoice)
                {//电子发票
                    if (!config.IsElectronicInvoice)
                        throw new HimallException("商家未开启电子普通发票");
                    subOrder.Tax = FormatMonty((subOrder.ProductAmount - subOrder.IntegralDiscount) * config.PlainInvoiceRate / 100);
                }
                else if (subOrder.Invoice.InvoiceType == InvoiceType.OrdinaryInvoices)
                { //普通发票
                    if (!config.IsElectronicInvoice)
                        throw new HimallException("商家未开启普通发票");
                    subOrder.Tax = FormatMonty((subOrder.ProductAmount - subOrder.IntegralDiscount) * config.PlainInvoiceRate / 100);
                }
            }
        }

        /// <summary>
        /// 计算预存款分摊
        /// </summary>
        public static void OrderCreatingByCapital(OrderCreating order, OrderCreateCommand command)
        {
            if (command.UseCapital == 0) return;//未使用预存款
            var capital = command.UseCapital;
            foreach (var subOrder in order.SubOrders.OrderBy(p => p.OrderAmount))
            {
                subOrder.Capital = Math.Min(subOrder.OrderAmount, capital);
                capital -= subOrder.Capital;
                if (capital <= 0)
                    break;
            }
        }


        /// <summary>
        /// 订单创建之后
        /// </summary>
        /// <param name="order"></param>
        private static void OrderCreated(OrderCreating order, OrderCreateCommand command)
        {

            if (command.GrouponId > 0)
            {//拼团
                try
                {
                    var subOrder = order.SubOrders.FirstOrDefault();
                    var item = subOrder.Items.FirstOrDefault();
                    Instance<FightGroupService>.Create.ExecCreated(command.GrouponId, command.GrouponGroupId, subOrder.OrderId, order.Member.Id, item);
                }
                catch (Exception ex)
                {
                    Log.Error($"订单Created:拼团异常:{ex.Message},{string.Join(",", order.SubOrders.Select(p => p.OrderId))}", ex);
                }
            }

            var setting = SiteSettingApplication.SiteSettings;
            if (command.CartItems != null && command.CartItems.Count > 0)
            {
                try
                {
                    Instance<CartService>.Create.Remove(command.MemberId, command.CartItems);
                }
                catch (Exception ex)
                {
                    Log.Error($"订单Created:购物车清理异常:{ex.Message},{string.Join(",", command.CartItems)}", ex);
                }
            }
            if (command.UseIntegral > 0)
            {//消费积分
                try
                {
                    var orders = order.SubOrders.Where(p => p.IntegralDiscount > 0).Select(p => p.OrderId).ToList();
                    var record = new MemberIntegralRecordInfo
                    {
                        UserName = order.Member.CellPhone ?? string.Empty,
                        MemberId = order.Member.Id,
                        RecordDate = DateTime.Now,
                        TypeId = MemberIntegralInfo.IntegralType.Exchange,
                        ReMark = string.Join(",", order.SubOrders.Select(p => p.OrderId)),
                        MemberIntegralRecordActionInfo = orders.Select(item => new MemberIntegralRecordActionInfo
                        {
                            VirtualItemTypeId = MemberIntegralInfo.VirtualItemType.Exchange,
                            VirtualItemId = item,
                        }).ToList()
                    };
                    var memberIntegral = Instance<MemberIntegralConversionFactoryService>.Create.Create(MemberIntegralInfo.IntegralType.Exchange, command.UseIntegral);
                    Instance<MemberIntegralService>.Create.AddMemberIntegral(record, memberIntegral);
                }
                catch (Exception ex)
                {
                    Log.Error($"订单Created:积分费异常:{ex.Message},{string.Join(",", order.SubOrders.Select(p => p.OrderId))}", ex);
                }
            }

            if (command.UseCapital > 0)
            {//消耗预存款
                try
                {
                    var subOrders = order.SubOrders.Where(p => p.Capital > 0).ToList();
                    foreach (var subOrder in subOrders)
                        Instance<MemberCapitalService>.Create.ConsumeCapital(command.MemberId, subOrder.OrderId, subOrder.Capital);
                }
                catch (Exception ex)
                {
                    Log.Error($"订单Created:预存款消费异常:{ex.Message},{string.Join(",", order.SubOrders.Select(p => p.OrderId))}", ex);
                }
            }

            var bonusService = Instance<ShopBonusService>.Create;
            if (command.Choices?.Count > 0)
            { //消费优惠券
                try
                {
                    foreach (var record in command.Choices)
                    {
                        if (record.RecordType == GeneralRecordType.Coupon)
                        {
                            var orderId = order.SubOrders.FirstOrDefault(p => p.ShopId == record.ShopId).OrderId;
                            Instance<CouponService>.Create.UseRecord(record.RecordId, new List<long> { orderId });
                        }
                        else if (record.RecordType == GeneralRecordType.Platform)
                        {
                            var subOrders = order.SubOrders.Where(p => p.PlatformCouponId > 0).Select(p => p.OrderId).ToList();
                            Instance<CouponService>.Create.UseRecord(record.RecordId, subOrders);
                        }
                        else if (record.RecordType == GeneralRecordType.Bouns)
                        {
                            var orderId = order.SubOrders.FirstOrDefault(p => p.ShopId == record.ShopId).OrderId;
                            bonusService.UesRecord(record.RecordId, orderId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"订单Created:优惠卷消费异常:{ex.Message},{string.Join(",", order.SubOrders.Select(p => p.OrderId))}", ex);
                }
            }

            try
            {
                foreach (var subOrder in order.SubOrders)
                {
                    //生成红包
                    var bouns = bonusService.GetByShopId(subOrder.ShopId);
                    if (bouns != null)
                    {
                        var domain = SiteSettingApplication.GetCurDomainUrl();
                        var url = $"{domain}/m-weixin/shopbonus/index/";
                        bonusService.GenerateBonusDetail(bouns, subOrder.OrderId, url, subOrder.OrderAmount);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"订单Created:红包生成异常:{ex.Message},{string.Join(",", order.SubOrders.Select(p => p.OrderId))}", ex);
            }





        }

        /// <summary>
        /// 自动分配门店
        /// </summary>
        public static void AllotOrder(ShippingAddressData address, OrderCreating.SubOrder order, ShopData shop)
        {
            if (address == null) return;//无收货地址
            var setting = SiteSettingApplication.SiteSettings;
            if (!setting.IsOpenStore) return;//未授权门店模块

            var city = RegionApplication.GetRegion(address.RegionId, Region.RegionLevel.City);
            if (city == null) return;//无有效城市信息

            order.ShopBranchId = GetService<ShopBranchService>().AllotStore(shop.Id, city.Id, address.Longitude, address.Latitude, order.Items.ToDictionary(k => k.SkuId, v => v.Quantity));
        }

        /// <summary>
        /// 扣减库存
        /// </summary>
        private static void DecreaseStock(OrderCreating order)
        {
            //库存变更项目
            var changes = order.SubOrders.SelectMany(subOrder => subOrder.Items.Select(item => new StockChange
            {
                ShopBranchId = subOrder.ShopBranchId,
                ProductId = item.ProductId,
                SkuId = item.SkuId,
                Number = item.Quantity,
            })).ToList();
            //扣减库存
            GetService<StockService>().Decrease(changes);
            order.IsDecreaseStock = true;
        }

        /// <summary>
        /// 恢复库存
        /// </summary>
        private static void RestoreStock(OrderCreating order)
        {
            if (!order.IsDecreaseStock) return;
            var changes = order.SubOrders.SelectMany(subOrder => subOrder.Items.Select(item => new StockChange
            {
                ShopBranchId = subOrder.ShopBranchId,
                ProductId = item.ProductId,
                SkuId = item.SkuId,
                Number = item.Quantity,
            })).ToList();
            GetService<StockService>().Increase(changes);
        }


        /// <summary>
        /// 构建订单,拆单
        /// </summary>
        private static OrderCreating BuildOrder(OrderCreateCommand command, OrderCreateingSource source)
        {
            var order = new OrderCreating
            {
                Member = source.Member,
                Platform = command.PlatformType,
                Address = source.Address,
                RoomId = command.RoomId,
                SubOrders = new List<OrderCreating.SubOrder>()
            };

            foreach (var sub in command.Subs)
            {
                var subOrder = new OrderCreating.SubOrder
                {
                    ShopId = sub.ShopId,
                    ShopBranchId = sub.ShopBranchId,
                    DeliveryType = sub.DeliveryType,
                    Remark = sub.Remark,
                    IsCashOnDelivery = sub.IsCashOnDelivery,
                    Invoice = sub.Invoice.Map<OrderInvoice>(),
                    Items = new List<OrderCreating.ProductItem>()
                };

                if (command.RoomId > 0)
                    subOrder.OrderType = OrderInfo.OrderTypes.Live;

                if (command.IsVirtual)
                    subOrder.OrderType = OrderInfo.OrderTypes.Virtual;

                var business = GetService<ShopService>().GetBusiness(subOrder.ShopId);
                foreach (var item in sub.Items)
                {
                    var product = source.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    var sku = product.Skus.FirstOrDefault(p => p.Id == item.SkuId);
                    var productItem = new OrderCreating.ProductItem
                    {
                        ProductId = item.ProductId,
                        SkuId = item.SkuId,
                        ProductName = product.ProductName,
                        Thumbnails = sku.ShowPic ?? product.ImagePath,
                        SKU = sku.Sku,
                        Quantity = item.Quantity,
                        CostPrice = sku.CostPrice,
                        SalePrice = sku.SalePrice,
                        Color = sku.Color,
                        Size = sku.Size,
                        Version = sku.Version,
                        RoomId = command.RoomId > 0 ? command.RoomId : item.RoomId,
                    };

                    //经营类目费率
                    if (business.TryGetValue(product.CategoryId, out var rate))
                        productItem.CommisRate = FormatMonty(rate / 100);

                    //阶梯价商品
                    if (product.IsOpenLadder)
                    {
                        var productQuantity = sub.Items.Where(p => p.ProductId == item.ProductId).Sum(p => p.Quantity);
                        var ladder = product.LadderPrice.LastOrDefault(p => p.MinBath <= productQuantity);
                        if (ladder == null)
                            throw new HimallException("未满足最低起购量");
                        productItem.SalePrice = ladder.Price;
                    }
                    //虚拟商品
                    if (product.ProductType == 1)
                    {
                        subOrder.IsVirtual = true;
                        productItem.VirtualItems = new List<OrderCreating.VirtualContent>();
                        foreach (var virtualItem in product.VirtualData.Items)
                        {
                            var virtualContent = item.VirtualContents.FirstOrDefault(p => p.Name == virtualItem.Name && p.Type == virtualItem.Type);
                            if (virtualItem.Required && (virtualContent == null || string.IsNullOrEmpty(virtualContent.Content)))
                                throw new HimallException($"请输入{virtualItem.Name}");
                            if (virtualContent == null)
                                continue;

                            productItem.VirtualItems.Add(new OrderCreating.VirtualContent
                            {
                                Name = virtualContent.Name,
                                Content = virtualContent.Content,
                                Type = virtualContent.Type,
                            });
                        }
                    }
                    subOrder.Items.Add(productItem);
                }
                var shop = source.Shops.FirstOrDefault(p => p.Id == subOrder.ShopId);
                subOrder.ShopName = shop.ShopName;
                //分配门店
                if (!subOrder.IsVirtual && subOrder.ShopBranchId == 0 && shop.AutoAllotOrder)
                    AllotOrder(source.Address, subOrder, shop);

                order.SubOrders.Add(subOrder);
            }

            return order;
        }

        /// <summary>
        /// 验证订单提交
        /// </summary>
        private static OrderCreateingSource Verify(OrderCreateCommand command)
        {
            var source = new OrderCreateingSource();

            source.Member = GetService<MemberService>().GetMemberData(command.MemberId);

            if (command.Subs.Sum(p => p.Items.Sum(i => i.Quantity)) <= 0)
                throw new HimallException("购买商品数量不能为零");

            //商品信息
            var items = command.Subs.SelectMany(p => p.Items);
            var products = items.Select(i => i.ProductId).Distinct().ToList();
            source.Products = ProductManagerApplication.GetProductData(products);
            if (source.Products.Any(p => p.SaleStatus != ProductInfo.ProductSaleStatus.OnSale))
                throw new HimallException("部分商品已下架");

            foreach (var sub in command.Subs)
            {
                if (sub.ShopBranchId > 0)
                {
                    var skus = sub.Items.Select(p => p.SkuId).ToList();
                    if (!GetService<ShopBranchService>().CheckOnSale(sub.ShopBranchId, skus))
                        throw new HimallException("部分商品已下架");
                }
            }

            //商品限购
            var limitBuys = source.Products.Where(p => p.MaxBuyCount > 0).ToList();
            if (limitBuys.Count > 0)
            {
                var list = limitBuys.Select(p => p.Id).ToList();
                var buyCounts = orderService.GetBuyCount(command.MemberId, list);
                foreach (var item in limitBuys)
                {
                    var quantity = items.Where(p => p.ProductId == item.Id).Sum(p => p.Quantity);
                    if (quantity > item.MaxBuyCount)
                        throw new HimallException("超过商品最大限购数");
                    if (buyCounts.TryGetValue(item.Id, out var count) && quantity + count > item.MaxBuyCount)
                        throw new HimallException("超过商品最大限购数");
                }
            }

            if (command.IsVirtual)
            {
                //虚拟订单仅一个商品
                var product = source.Products.FirstOrDefault();
                var virtualProduct = product.VirtualData;
                if (virtualProduct.ValidityType && DateTime.Now > virtualProduct.EndDate.Value)
                    throw new HimallException("商品已过期");
                var requires = virtualProduct.Items.Where(p => p.Required).ToList();
                if (requires.Count > 0)
                {
                    var virtualContents = command.Subs[0].Items[0].VirtualContents;
                    foreach (var require in requires)
                        if (!virtualContents.Any(p => p.Name == require.Name))
                            throw new HimallException($"请填写 {require.Name} 信息");
                }
            }
            if (command.Subs.Any(p => p.IsCashOnDelivery && p.ShopId != 1))
                throw new HimallException("仅官方自营店支持货到付款");
            var shops = source.Products.Select(p => p.ShopId).Distinct().ToList();
            source.Shops = shops.Select(p => GetService<ShopService>().GetShopData(p)).ToList();

            //收货地址
            if (command.IsVirtual || command.Subs.All(p => p.DeliveryType == DeliveryType.SelfTake))
            {//虚拟订单 or 到店自提 无需收货地址

            }
            else
            {
                source.Address = ShippingAddressApplication.GetAddress(command.MemberId).FirstOrDefault(p => p.Id == command.AddressId);
                if (source.Address == null)
                    throw new HimallException("请选择收货地址");
            }

            if (command.UseCapital > 0 || command.UseIntegral > 0)
            {
                var assets = GetService<MemberService>().GetAssets(command.MemberId);
                if (command.UseCapital > 0)
                {
                    if (string.IsNullOrEmpty(command.Password))
                        throw new HimallException("请输入支付密码");
                    if (!GetService<MemberService>().VerifyPayPassword(command.Password, assets.PayPassword, assets.PayPasswordSalt))
                        throw new HimallException("支付密码错误");
                    if (command.UseCapital > assets.Balance)
                        throw new HimallException("预存款余额不足");
                }
                if (command.UseIntegral > assets.Integral)
                    throw new HimallException("积分不足");
            }
            return source;
        }

        private static decimal FormatMonty(decimal money) =>
            Math.Floor(money * 100) / 100M;

        public static List<DTO.Market.GeneralRecordChoice> GetSelectedCoupon(string couponIds = "", string platcouponid = "")
        {
            List<DTO.Market.GeneralRecordChoice> records = new List<DTO.Market.GeneralRecordChoice>();
            var coupons = CouponApplication.ConvertUsedCoupon(couponIds);
            var platcoupons = CouponApplication.ConvertUsedCoupon(platcouponid);
            if (coupons != null)
            {
                foreach (string[] item in coupons)
                {
                    long recordId = 0;
                    long.TryParse(item[0], out recordId);
                    GeneralRecordType recordType = GeneralRecordType.Coupon;
                    Enum.TryParse<GeneralRecordType>(item[1], out recordType);
                    //string[0] = id, string[1] = type //部分更新 string[2] = shopId
                    records.Add(new DTO.Market.GeneralRecordChoice()
                    {
                        RecordId = recordId,
                        RecordType = recordType,
                        ShopId = long.Parse(item[2])

                    });
                }
            }
            if (platcoupons != null)
            {
                foreach (string[] item in platcoupons)
                {
                    long recordId = 0;
                    long.TryParse(item[0], out recordId);
                    GeneralRecordType recordType = GeneralRecordType.Coupon;
                    Enum.TryParse<GeneralRecordType>(item[1], out recordType);
                    //string[0] = id, string[1] = type //部分更新 string[2] = shopId
                    records.Add(new DTO.Market.GeneralRecordChoice()
                    {
                        RecordId = recordId,
                        RecordType = recordType,
                        ShopId = long.Parse(item[2])

                    });
                }
            }
            return records;
        }


    }
}
