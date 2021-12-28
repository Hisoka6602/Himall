﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using Hishop.Open.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.OpenApi
{
    public class TradeHelper : _BaseHelper
    {
        private RegionService regionService;
        private OrderService _OrderService;
        private MemberService _MemberService;
        private FightGroupService _FightGroupService;
        private TypeService _iTypeService;
        private RefundService _RefundService;

        public TradeHelper()
        {
            _RefundService = ServiceProvider.Instance<RefundService>.Create;
            regionService = Himall.ServiceProvider.Instance<RegionService>.Create;
            _OrderService = Himall.ServiceProvider.Instance<OrderService>.Create;
            _MemberService = Himall.ServiceProvider.Instance<MemberService>.Create;
            _FightGroupService = Himall.ServiceProvider.Instance<FightGroupService>.Create;
            _iTypeService = Himall.ServiceProvider.Instance<TypeService>.Create;
        }

        /// <summary>
        /// 查询订单的增量交易数据（根据修改时间）
        /// <para>返回的数据结果是以订单的修改时间倒序排列的，通过从后往前翻页的方式可以避免漏单问题</para>
        /// <para>一次请求只能查询时间跨度为一天的增量交易记录，即end_modified - start_modified小于等于1天。</para>
        /// </summary>
        /// <param name="app_key"></param>
        /// <param name="start_modified"></param>
        /// <param name="end_modified"></param>
        /// <param name="status"></param>
        /// <param name="buyer_uname"></param>
        /// <param name="page_no"></param>
        /// <param name="page_size"></param>
        /// <returns></returns>
        public QueryPageModel<trade_list_model> GetIncrementSoldTrades(string app_key, DateTime start_modified, DateTime end_modified, string status, string buyer_uname, int page_no, int page_size)
        {

            InitShopInfo(app_key);

            #region 参数检查
            if (!string.IsNullOrEmpty(buyer_uname))
            {
                var member = _MemberService.GetMemberByName(buyer_uname);
                if (member == null)
                {
                    throw new HimallApiException(OpenApiErrorCode.User_not_Exists, "buyer_uname");
                }
            }
            #endregion

            return GetSoldTrades(app_key, start_modified, end_modified, status, buyer_uname, page_no, page_size, true);
        }
        /// <summary>
        /// 获取当前商家的订单列表（根据创建时间）
        /// <para>返回的数据结果是以订单的创建时间倒序排列的</para>
        /// </summary>
        /// <param name="app_key"></param>
        /// <param name="start_created"></param>
        /// <param name="end_created"></param>
        /// <param name="status"></param>
        /// <param name="buyer_uname"></param>
        /// <param name="page_no"></param>
        /// <param name="page_size"></param>
        /// <param name="isIncrementGet">是否增量获取</param>
        /// <returns></returns>
        public QueryPageModel<trade_list_model> GetSoldTrades(string app_key, DateTime? start_created, DateTime? end_created, string status, string buyer_uname, int page_no, int page_size, bool isIncrementGet = false)
        {
            InitShopInfo(app_key);

            QueryPageModel<trade_list_model> result = new QueryPageModel<trade_list_model>()
            {
                Models = null,
                Total = 0
            };
            List<trade_list_model> resultdata = new List<trade_list_model>();

            #region 参数检查

            if (!string.IsNullOrEmpty(buyer_uname))
            {
                var member = _MemberService.GetMemberByName(buyer_uname);
                if (member == null)
                {
                    throw new HimallApiException(OpenApiErrorCode.User_not_Exists, "buyer_uname");
                }
            }

            #endregion

            #region 构建查询条件
            OrderQuery ordque = new OrderQuery()
            {
                PageSize = page_size,
                PageNo = page_no,
            };
            ordque.ShopId = shopId;
            if (start_created.HasValue)
            {
                ordque.StartDate = start_created.Value;
            }
            if (end_created.HasValue)
            {
                ordque.EndDate = end_created.Value;
            }
            if (!string.IsNullOrEmpty(buyer_uname))
            {
                ordque.UserName = buyer_uname;
            }
            ordque.IgnoreSelfPickUp = true;

            #region 订单状态
            //请不要使用MoreStatus
            TradeStatus queryts = TradeStatus.Trade_NORMAL;
            if (!string.IsNullOrWhiteSpace(status))
            {
                List<ProductInfo.ProductAuditStatus> _pas = new List<ProductInfo.ProductAuditStatus>();
                if (Enum.TryParse(status, true, out queryts))
                {
                    switch (queryts)
                    {
                        case TradeStatus.WAIT_BUYER_PAY:
                            ordque.Status = Entities.OrderInfo.OrderOperateStatus.WaitPay;
                            break;
                        case TradeStatus.WAIT_SELLER_SEND_GOODS:
                            ordque.Status = Entities.OrderInfo.OrderOperateStatus.WaitDelivery;
                            break;
                        case TradeStatus.WAIT_BUYER_CONFIRM_GOODS:
                            ordque.Status = Entities.OrderInfo.OrderOperateStatus.WaitReceiving;
                            break;
                        case TradeStatus.TRADE_CLOSED:
                            ordque.Status = Entities.OrderInfo.OrderOperateStatus.Close;
                            break;
                        case TradeStatus.TRADE_FINISHED:
                            ordque.Status = Entities.OrderInfo.OrderOperateStatus.Finish;
                            break;
                        default:
                            throw new HimallApiException(OpenApiErrorCode.Trade_Status_is_Invalid, "status");
                    }
                }
                else
                {
                    throw new HimallApiException(OpenApiErrorCode.Trade_Status_is_Invalid, "status");
                }
            }
            #endregion
            #endregion

            //获取数据
            QueryPageModel<Entities.OrderInfo> orddata = new QueryPageModel<Entities.OrderInfo>();
            if (!isIncrementGet)
            {
                orddata = _OrderService.GetOrders<OrderInfo>(ordque);
            }
            else
            {
                orddata = _OrderService.GetOrdersByLastModifyTime(ordque);
            }
            //转换数据
            result.Total = orddata.Total;
            if (orddata.Total > 0)
            {
                var datalist = orddata.Models.ToList();
                resultdata = OrderInfoListMapChange(datalist);
                result.Models = resultdata;
            }
            return result;
        }
        /// <summary>
        /// 获取单笔交易的详细信息
        /// </summary>
        /// <param name="app_key"></param>
        /// <param name="tid"></param>
        /// <returns></returns>
        public trade_list_model GetTrade(string app_key, long orderId)
        {
            InitShopInfo(app_key);

            #region 参数检查
            var trade = _OrderService.GetOrder(orderId);
            CheckOrderInfo(trade);
            #endregion

            trade_list_model result = OrderInfoMapChange(trade);

            return result;
        }
        /// <summary>
        /// 订单发货
        /// </summary>
        /// <param name="app_key"></param>
        /// <param name="tid"></param>
        /// <param name="company_name"></param>
        /// <param name="out_sid"></param>
        /// <returns></returns>
        public bool SendLogistic(string app_key, long orderId, string company_name, string out_sid)
        {
            InitShopInfo(app_key);

            #region 参数检查
            var trade = _OrderService.GetOrder(orderId);

            CheckOrderInfo(trade);

            RefundQuery refquery = new RefundQuery();
            //refquery.OrderId = ordidl[0];
            refquery.OrderId = orderId;
            refquery.PageNo = 1;
            refquery.PageSize =20;
            var reflist = _RefundService.GetOrderRefunds(refquery).Models.Where(d => d.RefundMode == Entities.OrderRefundInfo.OrderRefundMode.OrderRefund && d.SellerAuditStatus != Entities.OrderRefundInfo.OrderRefundAuditStatus.UnAudit).ToList();

            bool canSendCood = true;
            if (reflist.Count() > 0)
            {
                var _tmpobj = reflist.FirstOrDefault(d => d.OrderId == trade.Id);
                if (_tmpobj != null && trade.OrderStatus != OrderInfo.OrderOperateStatus.Close)
                {
                    canSendCood = false;
                }
            }

            if (trade.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitDelivery|| !canSendCood)
            {
                throw new HimallApiException(OpenApiErrorCode.Trade_Status_is_Invalid, "tid");
            }
            CheckExpressCompanyName(company_name);
            #endregion

            #region 商家发货
            Application.OrderApplication.SellerSendGood(orderId, sellerName, company_name, out_sid);
            #endregion

            return true;
        }
        /// <summary>
        /// 更新物流信息
        /// </summary>
        /// <param name="app_key"></param>
        /// <param name="tid"></param>
        /// <param name="company_name"></param>
        /// <param name="out_sid"></param>
        /// <returns></returns>
        public bool ChangLogistics(string app_key, long orderId, string company_name, string out_sid)
        {
            InitShopInfo(app_key);

            #region 参数检查
            var orderdata = _OrderService.GetOrder(orderId);

            CheckOrderInfo(orderdata);

            //if (orderdata.OrderStatus != OrderInfo.OrderOperateStatus.WaitReceiving)
            //{
            //    //只有是已经发货且待收货的订单才能修改物流信息
            //    throw new HimallOpenApiException(OpenApiErrorCode.Trade_Status_is_Invalid, "tid");
            //}
            CheckExpressCompanyName(company_name);
            #endregion

            #region 修改物流信息
            List<long> _ordlist = new List<long>();
            _ordlist.Add(orderId);
            _OrderService.SetOrderExpressInfo(shopId, company_name, out_sid, _ordlist);
            #endregion

            return true;
        }
        /// <summary>
        /// 修改交易备注
        /// </summary>
        /// <param name="app_key"></param>
        /// <param name="tid"></param>
        /// <param name="memo"></param>
        /// <returns></returns>
        public DateTime UpdateTradeMemo(string app_key, long orderId, string memo)
        {
            InitShopInfo(app_key);

            #region 参数检查
            var trade = _OrderService.GetOrder(orderId);
            CheckOrderInfo(trade);
            if (memo.Length > 1000)
            {
                throw new HimallApiException(OpenApiErrorCode.Trade_Memo_Too_Long, "memo");
            }
            #endregion

            _OrderService.SetOrderSellerRemark(orderId, memo);

            return DateTime.Now;
        }


        #region 私有
        /// <summary>
        /// 检测快递公司名称是否正确
        /// </summary>
        /// <param name="companyName"></param>
        private void CheckExpressCompanyName(string companyName)
        {
            var express = Himall.Core.PluginsManagement.GetInstalledPlugins<IExpress>(PluginType.Express).FirstOrDefault(item => item.Name.Contains(companyName));
            if (express == null)
            {
                throw new HimallApiException(OpenApiErrorCode.Company_not_Exists, "company_name");
            }
        }
        /// <summary>
        /// 检测订单信息
        /// </summary>
        /// <param name="data"></param>
        private void CheckOrderInfo(Entities.OrderInfo data)
        {
            if (data == null)
            {
                throw new HimallApiException(OpenApiErrorCode.Trade_not_Exists, "tid");
            }
            if (data.ShopId != shopId)
            {
                throw new HimallApiException(OpenApiErrorCode.Trade_is_Invalid, "app_key");
            }
        }
        /// <summary>
        /// 订单信息转换
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private trade_list_model OrderInfoMapChange(Entities.OrderInfo data,List<MemberInfo> users=null)
        {
            #region 基础信息初始化
            var UserInfo = new MemberInfo();
            if (users != null)
            {
                UserInfo = users.Where(t => t.Id == data.UserId).FirstOrDefault();
            }
            else
            {
                UserInfo = MemberApplication.GetMember(data.UserId);
            }
            string state="", city="", district="", town="";
            if (!string.IsNullOrWhiteSpace(data.RegionFullName))
            {
                string[] adressArray = data.RegionFullName.Split(new string[] { " ", ",", "，" }, StringSplitOptions.RemoveEmptyEntries);
                if (adressArray != null && adressArray.Length > 1)
                {
                    state = adressArray[0];
                    city = adressArray[1];
                    district = adressArray.Length > 2 ? adressArray[2] : string.Empty;
                    town = adressArray.Length > 3 ? adressArray[3] : string.Empty;
                }
            }
            string status = string.Empty;
            switch (data.OrderStatus)
            {
                case Entities.OrderInfo.OrderOperateStatus.Close:
                    status = "TRADE_CLOSED";
                    break;
                case Entities.OrderInfo.OrderOperateStatus.Finish:
                    status = "TRADE_FINISHED";
                    break;
                case Entities.OrderInfo.OrderOperateStatus.WaitDelivery:
                    status = "WAIT_SELLER_SEND_GOODS";
                    //拼团订单信息修正
                    if (data.OrderType == Entities.OrderInfo.OrderTypes.FightGroup)
                    {
                        var fgord = _FightGroupService.GetOrder(data.Id);
                        if (fgord != null)
                        {
                            if (fgord.GetJoinStatus != FightGroupOrderJoinStatus.BuildSuccess)
                            {
                                //未拼团成功的订单为待付款状态返回
                                status = "WAIT_BUYER_PAY";
                            }
                        }
                    }
                    break;
                case Entities.OrderInfo.OrderOperateStatus.WaitPay:
                    status = "WAIT_BUYER_PAY";
                    break;
                case Entities.OrderInfo.OrderOperateStatus.WaitReceiving:
                    status = "WAIT_BUYER_CONFIRM_GOODS";
                    break;
                default:
                    status = string.Empty;
                    break;
            }
            #endregion

            trade_list_model result = new trade_list_model();
            result.tid = data.Id.ToString();
            result.buyer_memo = data.PayRemark;
            result.seller_memo = data.SellerRemark;
            result.seller_flag = "";
            result.discount_fee = data.DiscountAmount;
            result.status = status;
            result.close_memo = data.CloseReason;
            result.created = data.OrderDate;
            result.modified = data.OrderDate;
            result.pay_time = data.PayDate;
            result.consign_time = data.ShippingDate;
            result.end_time = data.FinishDate;
            result.buyer_uname = data.UserName;
            result.buyer_email = UserInfo != null ? UserInfo.Email : string.Empty;
            result.buyer_nick = UserInfo != null ? UserInfo.Nick : string.Empty;
            result.buyer_area = UserInfo != null ? regionService.GetFullName(UserInfo.RegionId, "") : string.Empty;
            result.receiver_name = data.ShipTo;
            result.receiver_state = state;
            result.receiver_city = city;
            result.receiver_district = district;
            result.receiver_town = town;
            result.receiver_address = data.Address;
            result.receiver_zip = ""; //无邮编
            result.receiver_mobile = data.CellPhone;
            result.seller_id = data.ShopId.ToString();
            result.seller_name = data.ShopName;
            result.seller_mobile = data.SellerPhone;
            result.invoice_fee = data.Tax;
            result.invoice_title = "";//data.InvoiceTitle;
            result.payment = data.ProductTotalAmount;
            result.storeId = "0";  //无门店功能,云商城有
            result.orders = new List<trade_itme_model>();

            #region 子订单装配
            var orderitems = _OrderService.GetOrderItemsByOrderId(data.Id);
            foreach (var orderitem in orderitems)
            {
                Entities.TypeInfo typeInfo = _iTypeService.GetTypeByProductId(orderitem.ProductId);
                var productInfo = Himall.Application.ProductManagerApplication.GetProduct(orderitem.ProductId);
                string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                if (productInfo != null)
                {
                    colorAlias = !string.IsNullOrWhiteSpace(productInfo.ColorAlias) ? productInfo.ColorAlias : colorAlias;
                    sizeAlias = !string.IsNullOrWhiteSpace(productInfo.SizeAlias) ? productInfo.SizeAlias : sizeAlias;
                    versionAlias = !string.IsNullOrWhiteSpace(productInfo.VersionAlias) ? productInfo.VersionAlias : versionAlias;
                }
                trade_itme_model tradesItem = new trade_itme_model();
                tradesItem.sku_id = orderitem.SkuId;
                //tradesItem.num_id = orderitem.SKU;
                tradesItem.outer_sku_id = orderitem.SKU;
                tradesItem.title = orderitem.ProductName;
                string skuFullName = "";
                if (!string.IsNullOrEmpty(orderitem.Color))
                    skuFullName = colorAlias + ":" + orderitem.Color + ";";
                //skuFullName = "颜色:" + orderitem.Color + ";";
                if (!string.IsNullOrEmpty(orderitem.Size))
                    skuFullName = sizeAlias + ":" + orderitem.Size + ";";
                //skuFullName = "尺码:" + orderitem.Size + ";";
                if (!string.IsNullOrEmpty(orderitem.Version))
                    skuFullName = versionAlias + ":" + orderitem.Version + ";";
                //skuFullName = "版本:" + orderitem.Version + ";";
                if (!string.IsNullOrEmpty(skuFullName))
                    skuFullName = skuFullName.TrimEnd(';');
                tradesItem.sku_properties_name = skuFullName;
                tradesItem.price = orderitem.SalePrice;
                tradesItem.num = (int)orderitem.Quantity;
                tradesItem.pic_path = OpenAPIHelper.HostUrl + orderitem.ThumbnailsUrl;

                tradesItem.refund_status = status;
                result.orders.Add(tradesItem);
            }
            #endregion

            return result;
        }
        /// <summary>
        /// 订单列表信息转换
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<trade_list_model> OrderInfoListMapChange(List<Entities.OrderInfo> datalist)
        {
            List<trade_list_model> result = new List<trade_list_model>();
            if (datalist != null)
            {
                var users = MemberApplication.GetMembersByIds(datalist.Select(t => t.UserId).Distinct().ToList());
                foreach (var item in datalist)
                {
                    trade_list_model model = OrderInfoMapChange(item, users);
                    result.Add(model);
                }
            }
            return result;
        }
        #endregion
    }
}
