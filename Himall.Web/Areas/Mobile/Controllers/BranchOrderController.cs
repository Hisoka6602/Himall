using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.Market;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Web.Areas.Mobile.Models;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class BranchOrderController : BaseMobileMemberController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (CurrentUser != null && CurrentUser.Disabled)
            {
                filterContext.Result = RedirectToAction("Entrance", "Login", new { returnUrl = WebHelper.GetAbsoluteUri() });
            }
        }
        public ActionResult Index()
        {
            return View();
        }


        /// <summary>
        /// 进入立即购买提交页面
        /// </summary>
        /// <param name="skuIds">库存ID集合</param>
        /// <param name="counts">库存ID对应的数量</param>
        /// <param name="GroupActionId">拼团活动编号</param>
        /// <param name="GroupId">拼团编号</param>
        public ActionResult Submit(string skuIds, string counts, int islimit = 0, long shippingAddressId = 0, string couponIds = "", sbyte productType = 0, long flashSaleId = 0, long shopBranchId = 0, string platcouponid = "")
        {
            if (productType == 0)
                throw new HimallException("门店订单不支持立即购买");
            long productId = 0;
            OrderPreproCommand command = new OrderPreproCommand();
            command.ShopBranchId = shopBranchId;
            command.Items = new List<OrderPreproCommand.ProductItem>();
            if (productId <= 0 && !string.IsNullOrWhiteSpace(skuIds))
            {
                productId = long.Parse(skuIds.Split('_')[0]);
            }

            command.Items.Add(new OrderPreproCommand.ProductItem()
            {
                ProductId = productId,
                Quantity = int.Parse(counts),
                SkuId = skuIds,
            });
            command.FlashSaleId = flashSaleId;

            command.AddressId = shippingAddressId;
            command.Records = OrderProcessApplication.GetSelectedCoupon(couponIds, platcouponid);

            command.IsVirtual = false;
            command.MemberId = CurrentUser.Id;
            GetSubmitOrderInitViewBag(command);
            return View();
        }

        /// <summary>
        /// 判断订单是否已提交
        /// </summary>
        /// <param name="orderTag"></param>
        /// <returns></returns>
        public ActionResult IsSubmited(string orderTag)
        {
            return Json<dynamic>(true, data: object.Equals(Session["OrderTag"], orderTag) == false);
        }

        /// <summary>
        /// 展示门店列表
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="regionId"></param>
        /// <param name="skuIds"></param>
        /// <param name="counts"></param>
        /// <returns></returns>
        public ActionResult ShopBranchs(int shopId, int regionId, string[] skuIds, int[] counts, long shippingAddressId)
        {
            ViewBag.ShippingAddressId = shippingAddressId;
            return View(new ShopBranchModel
            {
                ShopId = shopId,
                RegionId = regionId,
                SkuIds = skuIds,
                Counts = counts
            });
        }

     

        private void GetSubmitOrderInitViewBag(OrderPreproCommand command, IEnumerable<ShoppingCartItem> cartItems = null)
        {

            OrderPreproResult oResult = OrderProcessApplication.Prepro(command);
            ViewBag.ConfirmModel = oResult;
            command.Records = new List<GeneralRecordChoice>();
            if (oResult.Records != null && oResult.Records.Count > 0)
            {
                GeneralRecord record = oResult.Records.FirstOrDefault(r => r.Selected);
                if (record != null)
                {
                        command.Records.Add(new GeneralRecordChoice()
                        {
                            RecordId = record.Id,
                            RecordType = record.RecordType,
                            ShopId = record.ShopId
                        });
                }
            }
            foreach (var shop in oResult.SubOrders)
            {
                if (shop.Records != null && shop.Records.Count > 0)
                {
                    GeneralRecord record = shop.Records.FirstOrDefault(r => r.Selected);
                    if (record != null)
                    {
                        command.Records.Add(new GeneralRecordChoice()
                        {
                            RecordId = record.Id,
                            RecordType = record.RecordType,
                            ShopId = record.ShopId
                        });
                    }
                }
            }

            ViewBag.OrderParam = command;
            ViewBag.InvoiceTitles = OrderApplication.GetInvoiceTitles(CurrentUser.Id, null);
            string cellPhone = "", email = "", invoiceName = "", invoiceCode = "";
            ViewBag.vatInvoice = OrderApplication.GetDefaultInvoiceInfo(CurrentUser.Id, ref cellPhone, ref email, ref invoiceName, ref invoiceCode);

            ViewBag.cellPhone = cellPhone;
            ViewBag.email = email;
            ViewBag.invoiceName = invoiceName;
            ViewBag.invoiceCode = invoiceCode;
            var orderTag = Guid.NewGuid().ToString("N");
            ViewBag.OrderTag = orderTag;
            Session["OrderTag"] = orderTag;
            ViewBag.IsOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            bool canIntegralPerMoney = true, canCapital = true;
            CanDeductible(out canIntegralPerMoney, out canCapital);
            ViewBag.CanIntegralPerMoney = canIntegralPerMoney;
            ViewBag.CanCapital = canCapital;
            ViewBag.IntegralPerMoneyRate = SiteSettingApplication.SiteSettings == null ? 0 : SiteSettingApplication.SiteSettings.IntegralDeductibleRate;
            ViewBag.CartItems = cartItems == null ? "" : string.Join(",", cartItems.Select(c => c.Id));
        }
        /// <summary>
        /// 新的提交订单
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public JsonResult NewSubmit(OrderCreateCommand command)
        {
            command.MemberId = CurrentUser.Id;
            command.PlatformType = PlatformType;
            if (command.Choices == null)
            {
                command.Choices = new List<GeneralRecordChoice>();
            }
            var result = OrderProcessApplication.Submit(command);
            return Json<dynamic>(true, data: result);
        }
        /// <summary>
        /// 进入购物车提交页面
        /// </summary>
        /// <param name="cartItemIds">购物车物品id集合</param>
        public ActionResult SubmiteByCart(string cartItemIds, long shippingAddressId = 0, string couponIds = "", string platcouponid = "", long shopBranchId = 0)
        {
            OrderPreproCommand command = new OrderPreproCommand();
            command.ShopBranchId = shopBranchId;
            command.Items = new List<OrderPreproCommand.ProductItem>();
            IEnumerable<Himall.Entities.ShoppingCartItem> cartItems = null;
            if (string.IsNullOrWhiteSpace(cartItemIds))
                cartItems = OrderApplication.GetCart(CurrentUser.Id, "").Items;
            else
            {
                var cartItemIdsArr = cartItemIds.Split(',').Where(d => !string.IsNullOrWhiteSpace(d)).Select(t => long.Parse(t));
                cartItems = CartApplication.GetCartItems(cartItemIdsArr);
            }
            foreach (ShoppingCartItem item in cartItems)
            {
                command.Items.Add(new OrderPreproCommand.ProductItem()
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    SkuId = item.SkuId,
                });
            }
            command.Records = OrderProcessApplication.GetSelectedCoupon(couponIds, platcouponid);
            command.AddressId = shippingAddressId;
            command.IsVirtual = false;
            command.MemberId = CurrentUser.Id;
            GetSubmitOrderInitViewBag(command, cartItems);
            return View("submit");
        }

        /// <summary>
        /// 设置发票抬头
        /// </summary>
        /// <param name="name">抬头名称</param>
        /// <returns>返回抬头ID</returns>
        [HttpPost]
        public JsonResult SaveInvoiceTitle(string name, string code)
        {
            return SuccessResult<dynamic>(data: OrderApplication.SaveInvoiceTitle(UserId, name, code));
        }
        /// <summary>
        /// 删除发票抬头
        /// </summary>
        /// <param name="id">抬头ID</param>
        /// <returns>是否完成</returns>
        [HttpPost]
        public JsonResult DeleteInvoiceTitle(long id)
        {
            OrderApplication.DeleteInvoiceTitle(id);
            return SuccessResult();
        }



        
        private string MoveImages(string image, long userId)
        {
            List<string> content = new List<string>();
            if (string.IsNullOrWhiteSpace(image))
            {
                return "";
            }
            var list = image.Split(',').ToList();
            if (list != null && list.Count > 0)
            {
                list.ForEach(a =>
                {
                    var oldname = Path.GetFileName(a);
                    string ImageDir = string.Empty;
                    //转移图片
                    string relativeDir = "/Storage/Plat/VirtualProduct/";
                    string fileName = userId + oldname;
                    if (a.Replace("\\", "/").Contains("/temp/"))//只有在临时目录中的图片才需要复制
                    {
                        var de = a.Substring(a.LastIndexOf("/temp/"));
                        Core.HimallIO.CopyFile(de, relativeDir + fileName, true);
                        content.Add(relativeDir + fileName);
                    }  //目标地址
                    else if (a.Contains("/Storage"))
                    {
                        content.Add(a.Substring(a.LastIndexOf("/Storage")));
                    }
                    else
                        content.Add(a);
                });
            }
            return string.Join(",", content);
        }

        /// <summary>
        /// 积分支付
        /// </summary>
        /// <param name="orderIds">订单Id</param>
        [HttpPost]
        public JsonResult PayOrderByIntegral(string orderIds)
        {
            OrderApplication.ConfirmOrder(UserId, orderIds);
            return SuccessResult<dynamic>();
        }

        /// <summary>
        /// 取消积分支付订单
        /// </summary>
        /// <param name="orderIds">订单Id</param>
        [HttpPost]
        public JsonResult CancelOrders(string orderIds)
        {
            OrderApplication.CancelOrder(orderIds, UserId);
            return Json<dynamic>(true);
        }

        /// <summary>
        /// 是否全部抵扣
        /// </summary>
        /// <param name="integral">积分</param>
        /// <param name="total">总价</param>
        [HttpPost]
        public ActionResult IsAllDeductible(int integral, decimal total)
        {
            return Json<dynamic>(true, data: OrderApplication.IsAllDeductible(integral, total, UserId));
        }

        /// <summary>
        /// 获取收货地址界面
        /// </summary>
        /// <param name="returnURL">返回url路径</param>
        public ActionResult ChooseShippingAddress(string returnURL = "", long shopBranchId = 0)
        {
            if (shopBranchId == 0)
                throw new HimallException("获取门店ID失败，不可提交非门店商品");
            ViewBag.shopBranchId = shopBranchId;
            return View(OrderApplication.GetUserAddresses(UserId, shopBranchId));
        }

        /// <summary>
        /// 设置默认收货地址
        /// </summary>
        /// <param name="addId">收货地址Id</param>
        [HttpPost]
        public JsonResult SetDefaultUserShippingAddress(long addId)
        {
            OrderApplication.SetDefaultUserShippingAddress(addId, UserId);
            return Json<dynamic>(true, data: addId);
        }

        /// <summary>
        /// 获得编辑收获地址页面
        /// </summary>
        /// <param name="addressId">收货地址Id</param>
        /// <param name="returnURL">返回url路径</param>
        public ActionResult EditShippingAddress(long addressId = 0, string returnURL = "", long shopBranchId = 0)
        {
            if (shopBranchId == 0)
                throw new HimallException("获取门店ID失败，不可提交非门店商品");
            ViewBag.shopBranchId = shopBranchId;

            var ShipngInfo = OrderApplication.GetUserAddress(addressId);
            ViewBag.addId = addressId;
            if (ShipngInfo != null)
            {
                ViewBag.fullPath = RegionApplication.GetRegionPath(ShipngInfo.RegionId);
                ViewBag.fullName = RegionApplication.GetFullName(ShipngInfo.RegionId);
            }
            return View(ShipngInfo);
        }

        /// <summary>
        /// 删除收货地址
        /// </summary>
        /// <param name="addressId">收货地址Id</param>
        [HttpPost]
        public ActionResult DeleteShippingAddress(long addressId)
        {
            OrderApplication.DeleteShippingAddress(addressId, UserId);
            return SuccessResult();
        }

        /// <summary>
        /// 获得用户的收货地址信息
        /// </summary>
        /// <param name="addressId">收货地址Id</param>
        [HttpPost]
        public JsonResult GetUserShippingAddresses(long addressId)
        {
            var addresses = OrderApplication.GetUserAddress(addressId);
            var json = new
            {
                id = addresses.Id,
                fullRegionName = addresses.RegionFullName,
                address = addresses.Address,
                phone = addresses.Phone,
                shipTo = addresses.ShipTo,
                fullRegionIdPath = addresses.RegionIdPath
            };
            return SuccessResult<dynamic>(data: json);
        }

        /// <summary>
        /// 取消订单
        /// </summary>
        /// <param name="orderId">订单Id</param>
        [HttpPost]
        public JsonResult CloseOrder(long orderId)
        {
            MemberInfo umi = CurrentUser;
            bool isClose = OrderApplication.CloseOrder(orderId, umi.Id, umi.UserName);
            if (isClose)
                return SuccessResult("取消成功");
            else
                return ErrorResult("取消失败，该订单已删除或者不属于当前用户！");
        }

        /// <summary>
        /// 确认订单收货
        /// </summary>
        [HttpPost]
        public JsonResult ConfirmOrder(long orderId)
        {
            var status = OrderApplication.ConfirmOrder(orderId, CurrentUser.Id, CurrentUser.UserName);
            Result result = new Result() { status = status };
            switch (status)
            {
                case 0:
                    result.success = true;
                    result.msg = "操作成功";
                    break;
                case 1:
                    result.success = false;
                    result.msg = "该订单已经确认过!";
                    break;
                case 2:
                    result.success = false;
                    result.msg = "订单状态发生改变，请重新刷页面操作!";
                    break;
            }
            // var data = ObjectContainer.Current.Resolve<OrderService>.Create.GetOrder(orderId);
            //确认收货写入结算表 改LH写在Controller里的
            // ObjectContainer.Current.Resolve<OrderService>.Create.WritePendingSettlnment(data);
            return Json<dynamic>(result.success, result.msg);
        }

        /// <summary>
        /// 订单详细信息页面
        /// </summary>
        /// <param name="id">订单Id</param>
        public ActionResult Detail(long id)
        {
            OrderDetailView view = OrderApplication.Detail(id, UserId, PlatformType, WebHelper.GetHost());
            ViewBag.Detail = view.Detail;
            ViewBag.Bonus = view.Bonus;
            ViewBag.ShareHref = view.ShareHref;
            ViewBag.IsRefundTimeOut = view.IsRefundTimeOut;
            ViewBag.Logo = SiteSettings.Logo;
            view.Order.FightGroupOrderJoinStatus = view.FightGroupJoinStatus;
            view.Order.FightGroupCanRefund = view.FightGroupCanRefund;

            var customerServices = CustomerServiceApplication.GetMobileCustomerServiceAndMQ(view.Order.ShopId, true, CurrentUser);
            ViewBag.CustomerServices = customerServices;
            #region 门店信息
            if (view.Order.ShopBranchId > 0)
            {
                ViewBag.ShopBranchInfo = ShopBranchApplication.GetShopBranchById(view.Order.ShopBranchId);
            }
            #endregion
            return View(view.Order);
        }

        /// <summary>
        /// 快递信息
        /// </summary>
        /// <param name="orderId">订单Id</param>
        public ActionResult ExpressInfo(long orderId)
        {
            string[] result = OrderApplication.GetExpressInfo(orderId);
            ViewBag.ExpressCompanyName = result[0];
            ViewBag.ShipOrderNumber = result[1];
            ViewBag.OrderId = orderId;
            return View();
        }

        /// <summary>
        /// 获取商家分店
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="regionId">街道id</param>
        /// <param name="getParent">是否获取县/区下面所有街道的分店</param>
        /// <param name="skuIds">购买的商品的sku</param>
        /// <param name="counts">商品sku对应的购买数量</param>
        /// <param name="shippingAddressesId">订单收货地址ID</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetShopBranchs(long shopId, long regionId, bool getParent, string[] skuIds, int[] counts, int page, int rows, long shippingAddressId)
        {
            var shippingAddressInfo = ShippingAddressApplication.GetUserShippingAddress(shippingAddressId);
            int streetId = 0, districtId = 0;//收货地址的街道、区域

            var query = new ShopBranchQuery()
            {
                ShopId = shopId,
                PageNo = page,
                PageSize = rows,
                Status = ShopBranchStatus.Normal,
                ShopBranchProductStatus = ShopBranchSkuStatus.Normal
            };
            if (shippingAddressInfo != null)
            {
                query.FromLatLng = string.Format("{0},{1}", shippingAddressInfo.Latitude, shippingAddressInfo.Longitude);//需要收货地址的经纬度
                streetId = shippingAddressInfo.RegionId;
                var parentAreaInfo = RegionApplication.GetRegion(shippingAddressInfo.RegionId, Region.RegionLevel.Town);//判断当前区域是否为第四级
                if (parentAreaInfo != null && parentAreaInfo.ParentId > 0) districtId = parentAreaInfo.ParentId;
                else { districtId = streetId; streetId = 0; }
            }
            bool hasLatLng = false;
            if (!string.IsNullOrWhiteSpace(query.FromLatLng)) hasLatLng = query.FromLatLng.Split(',').Length == 2;

            var region = RegionApplication.GetRegion(regionId, getParent ? Region.RegionLevel.City : Region.RegionLevel.County);
            if (region != null) query.AddressPath = region.GetIdPath();

            #region 旧排序规则
            //var skuInfos = ProductManagerApplication.GetSKUs(skuIds);

            //query.ProductIds = skuInfos.Select(p => p.ProductId).ToArray();
            //var data = ShopBranchApplication.GetShopBranchs(query);

            //var shopBranchSkus = ShopBranchApplication.GetSkus(shopId, data.Models.Select(p => p.Id));

            //var models = new
            //{
            //    Rows = data.Models.Select(sb => new
            //    {
            //        sb.ContactUser,
            //        sb.ContactPhone,
            //        sb.AddressDetail,
            //        sb.ShopBranchName,
            //        sb.Id,
            //        Enabled = skuInfos.All(skuInfo => shopBranchSkus.Any(sbSku => sbSku.ShopBranchId == sb.Id && sbSku.Stock >= counts[skuInfos.IndexOf(skuInfo)] && sbSku.SkuId == skuInfo.Id))
            //    }).OrderByDescending(p => p.Enabled).ToArray(),
            //    data.Total
            //};
            #endregion
            #region 3.0版本排序规则
            var skuInfos = ProductManagerApplication.GetSKUs(skuIds);
            query.ProductIds = skuInfos.Select(p => p.ProductId).ToArray();
            var data = ShopBranchApplication.GetShopBranchsAll(query);
            var shopBranchSkus = ShopBranchApplication.GetSkus(shopId, data.Models.Select(p => p.Id).ToList());//获取该商家下具有订单内所有商品的门店状态正常数据,不考虑库存
            data.Models.ForEach(p =>
            {
                p.Enabled = skuInfos.All(skuInfo => shopBranchSkus.Any(sbSku => sbSku.ShopBranchId == p.Id && sbSku.Stock >= counts[skuInfos.IndexOf(skuInfo)] && sbSku.SkuId == skuInfo.Id));
            });

            List<ShopBranch> newList = new List<ShopBranch>();
            List<long> fillterIds = new List<long>();
            var currentList = data.Models.Where(p => hasLatLng && p.Enabled && (p.Latitude > 0 && p.Longitude > 0)).OrderBy(p => p.Distance).ToList();
            if (currentList != null && currentList.Count() > 0)
            {
                fillterIds.AddRange(currentList.Select(p => p.Id));
                newList.AddRange(currentList);
            }
            var currentList2 = data.Models.Where(p => !fillterIds.Contains(p.Id) && p.Enabled && p.AddressPath.Contains(CommonConst.ADDRESS_PATH_SPLIT + streetId + CommonConst.ADDRESS_PATH_SPLIT)).ToList();
            if (currentList2 != null && currentList2.Count() > 0)
            {
                fillterIds.AddRange(currentList2.Select(p => p.Id));
                newList.AddRange(currentList2);
            }
            var currentList3 = data.Models.Where(p => !fillterIds.Contains(p.Id) && p.Enabled && p.AddressPath.Contains(CommonConst.ADDRESS_PATH_SPLIT + districtId + CommonConst.ADDRESS_PATH_SPLIT)).ToList();
            if (currentList3 != null && currentList3.Count() > 0)
            {
                fillterIds.AddRange(currentList3.Select(p => p.Id));
                newList.AddRange(currentList3);
            }
            var currentList4 = data.Models.Where(p => !fillterIds.Contains(p.Id) && p.Enabled).ToList();//非同街、非同区，但一定会同市
            if (currentList4 != null && currentList4.Count() > 0)
            {
                fillterIds.AddRange(currentList4.Select(p => p.Id));
                newList.AddRange(currentList4);
            }
            var currentList5 = data.Models.Where(p => !fillterIds.Contains(p.Id)).ToList();//库存不足的排最后
            if (currentList5 != null && currentList5.Count() > 0)
            {
                newList.AddRange(currentList5);
            }
            if (newList.Count() != data.Models.Count())//如果新组合的数据与原数据数量不一致，则异常
            {
                return Json<dynamic>(true, data: new { Rows = "" }, camelCase: true);
            }
            var needDistance = false;
            if (shippingAddressInfo != null && shippingAddressInfo.Latitude != 0 && shippingAddressInfo.Longitude != 0)
            {
                needDistance = true;
            }
            var models = new
            {
                Rows = newList.Select(sb => new
                {
                    sb.ContactUser,
                    sb.ContactPhone,
                    sb.AddressDetail,
                    sb.ShopBranchName,
                    sb.Id,
                    Enabled = sb.Enabled,
                    Distance = needDistance ? RegionApplication.GetDistance(sb.Latitude, sb.Longitude, shippingAddressInfo.Latitude, shippingAddressInfo.Longitude) : 0
                }).ToArray(),
                Total = newList.Count
            };
            #endregion
            return SuccessResult<dynamic>(data: models, camelCase: true);
        }

        public JsonResult ExistShopBranch(int shopId, int regionId, long[] productIds)
        {
            var query = new ShopBranchQuery();
            query.Status = ShopBranchStatus.Normal;
            query.ShopId = shopId;

            var region = RegionApplication.GetRegion(regionId, Region.RegionLevel.City);
            query.AddressPath = region.GetIdPath();
            query.ProductIds = productIds;
            query.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
            var existShopBranch = ShopBranchApplication.Exists(query);
            return SuccessResult(data: existShopBranch);
        }

        /// <summary>
        /// 获取运费
        /// </summary>
        /// <param name="addressId">地址ID</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CalcFreight(int addressId, CalcFreightparameter[] parameters)
        {
            var result = OrderApplication.CalcFreight(addressId, parameters.GroupBy(p => p.ShopId).ToDictionary(p => p.Key, p => p.GroupBy(pp => pp.ProductId).ToDictionary(pp => pp.Key, pp => string.Format("{0}${1}", pp.Sum(ppp => ppp.Count), pp.Sum(ppp => ppp.Amount)))));
            if (result.Count == 0)
                return ErrorResult("计算运费失败");
            else
                return SuccessResult<dynamic>(data: result.Select(p => new { shopId = p.Key, freight = p.Value }).ToArray());
        }
        [HttpPost]
        public JsonResult GetOrderPayStatus(string orderids)
        {
            var isPaied = OrderApplication.AllOrderIsPaied(orderids);
            return Json<dynamic>(isPaied);
        }

        public ActionResult OrderShare(string orderids, string source)
        {
            if (string.IsNullOrWhiteSpace(orderids))
            {
                throw new HimallException("订单号不能为空！");
            }
            long orderId = 0;
            var ids = orderids.Split(',').Select(e =>
            {
                if (long.TryParse(e, out orderId))
                {
                    return orderId;
                }
                else
                {
                    return 0;
                }
            }
            );
            if (MemberIntegralApplication.OrderIsShared(ids))
            {
                ViewBag.IsShared = true;
            }
            ViewBag.Source = source;
            ViewBag.OrderIds = orderids;
            var orders = OrderApplication.GetOrderDetailViews(ids);
            return View(orders);
        }


        [HttpPost]
        public JsonResult OrderShareAddIntegral(string orderids)
        {
            if (string.IsNullOrWhiteSpace(orderids))
            {
                throw new HimallException("订单号不能为空！");
            }
            long orderId = 0;
            var ids = orderids.Split(',').Select(e =>
            {
                if (long.TryParse(e, out orderId))
                    return orderId;
                else
                    throw new HimallException("订单分享增加积分时，订单号异常！");
            }
            );
            if (MemberIntegralApplication.OrderIsShared(ids))
            {
                throw new HimallException("订单已经分享过！");
            }
            Himall.Entities.MemberIntegralRecordInfo record = new Himall.Entities.MemberIntegralRecordInfo();
            record.MemberId = CurrentUser.Id;
            record.UserName = CurrentUser.UserName;
            record.RecordDate = DateTime.Now;
            record.TypeId = Himall.Entities.MemberIntegralInfo.IntegralType.Share;
            record.ReMark = string.Format("订单号:{0}", orderids);
            List<Himall.Entities.MemberIntegralRecordActionInfo> recordAction = new List<Himall.Entities.MemberIntegralRecordActionInfo>();

            foreach (var id in ids)
            {
                recordAction.Add(new Himall.Entities.MemberIntegralRecordActionInfo
                {
                    VirtualItemId = id,
                    VirtualItemTypeId = Himall.Entities.MemberIntegralInfo.VirtualItemType.ShareOrder
                });
            }
            record.MemberIntegralRecordActionInfo = recordAction;
            MemberIntegralApplication.AddMemberIntegralByEnum(record, Himall.Entities.MemberIntegralInfo.IntegralType.Share);
            return SuccessResult("晒单添加积分成功！");
        }

        [HttpGet]
        public ActionResult InitRegion(string fromLatLng)
        {
            string address = string.Empty, province = string.Empty, city = string.Empty, district = string.Empty, street = string.Empty, newStreet = string.Empty;
            ShopbranchHelper.GetAddressByLatLng(fromLatLng, ref address, ref province, ref city, ref district, ref street);
            if (district == "" && street != "")
            {
                district = street;
                street = "";
            }
            string fullPath = RegionApplication.GetAddress_Components(city, district, street, out newStreet);
            if (fullPath.Split(',').Length <= 3) newStreet = string.Empty;//如果无法匹配街道，则置为空
            return SuccessResult<dynamic>(data: new { fullPath = fullPath, showCity = string.Format("{0} {1} {2} {3}", province, city, district, newStreet), street = newStreet });
        }

        private void CanDeductible(out bool canIntegralPerMoney, out bool canCapital)
        {
            //授权模块控制积分抵扣、余额抵扣功能是否开放
            canIntegralPerMoney = true;
            canCapital = true;

            if (!(SiteSettings.IsOpenPC || SiteSettings.IsOpenH5 || SiteSettings.IsOpenApp || SiteSettings.IsOpenMallSmallProg))
            {
                canIntegralPerMoney = false;
            }

            if (!(SiteSettings.IsOpenPC || SiteSettings.IsOpenH5 || SiteSettings.IsOpenApp))
            {
                canCapital = false;
            }

        }

        #region 私有方法
        private void InitOrderSubmitModel(MobileOrderDetailConfirmModel model)
        {
            if (model.Address != null)
            {
                var query = new ShopBranchQuery();
                query.Status = ShopBranchStatus.Normal;

                var region = RegionApplication.GetRegion(model.Address.RegionId, Region.RegionLevel.City);
                query.AddressPath = region.GetIdPath();

                foreach (var item in model.products)
                {
                    query.ShopId = item.shopId;
                    query.ProductIds = item.CartItemModels.Select(p => p.id).ToArray();
                    query.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
                    item.ExistShopBranch = ShopBranchApplication.Exists(query);
                }
            }
        }

        /// <summary>
        /// 是否超出限购数
        /// </summary>
        /// <param name="products"></param>
        /// <param name="buyCounts">buyCounts</param>
        /// <returns></returns>
        private bool IsOutMaxBuyCount(IEnumerable<ProductInfo> products, Dictionary<long, int> buyCounts)
        {
            var buyedCounts = OrderApplication.GetProductBuyCount(CurrentUser.Id, products.Select(pp => pp.Id));
            var isOutMaxBuyCount = products.Any(pp => pp.MaxBuyCount > 0 && pp.MaxBuyCount < (buyedCounts.ContainsKey(pp.Id) ? buyedCounts[pp.Id] : 0) + buyCounts[pp.Id]);

            return isOutMaxBuyCount;
        }
        #endregion
    }

}