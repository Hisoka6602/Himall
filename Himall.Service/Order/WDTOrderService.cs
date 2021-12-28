using Hidistro.Core;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using NetRube.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Himall.Entities.OrderInfo;

namespace Himall.Service
{
    public class WDTOrderService : ServiceBase
    {

        public void PushWangDianTongOrder(WDTConfigModel setting, long shopId, OrderInfo order = null)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (!setting.OpenErp)
                {
                    Log.Info($"旺店通没有开启");
                    return;
                }

                string apiUrl = setting.ErpUrl;
                string sid = setting.ErpSid;
                string appkey = setting.ErpAppkey;
                string appsecret = setting.ErpAppsecret;
                if (!string.IsNullOrEmpty(apiUrl) && !string.IsNullOrEmpty(sid)
                    && !string.IsNullOrEmpty(appkey) && !string.IsNullOrEmpty(appsecret))
                {
                    //获取线上已支付，线下已支付且未推送完成的订单或者需要推送的订单（一次最多50单）
                    //推送开始时间
                    List<OrderInfo> orders = new List<OrderInfo>();
                    if (order != null)
                    {
                        orders.Add(order);
                    }
                    else
                    {
                        orders = new OrderService().GetPushOrders(50, shopId);
                    }
                    if (orders == null || orders.Count <= 0)
                    {
                        Log.Info(DateTime.Now.ToString() + "-没有获取到可推送的订单");
                        return;
                    }
                    string message = "";
                    var pushResult = BatchPushOrderToWangdiantong(orders, setting, out message);
                    if (pushResult == pushState.Success)
                    {
                        //更新订单推送状态
                        new OrderService().UpdateOrderPushState(orders.Select(o => o.Id));
                        Log.Info(DateTime.Now.ToString() + "旺店通订单推送成功：" + string.Join(",", orders.Select(o => o.Id)));
                    }
                    else if (pushResult == pushState.PartSuccess && !string.IsNullOrEmpty(message))
                    {
                        List<errorPush> errordata = JsonConvert.DeserializeObject<List<errorPush>>(message);
                        //推送失败的tid
                        List<long> errortid = errordata.Select(t => long.Parse(t.tid)).ToList();
                        //需要更新推送状态的订单
                        var updateIds = orders.Where(t => !errortid.Contains(t.Id)).Select(t => t.Id);
                        new OrderService().UpdateOrderPushState(updateIds);
                        Log.Info(DateTime.Now.ToString() + "-推送订单：" + message);
                    }
                    else
                    {
                        Log.Info(DateTime.Now.ToString() + "-推送订单：" + message);
                    }
                }
                else
                {
                    Log.Error($"旺店通参数配置验证错误1，是否开启：{setting.OpenErp}，库存同步：{setting.OpenErpStock},ErpAppkey:{setting.ErpAppkey},ErpAppsecret:{setting.ErpAppsecret},ErpPlateId:{setting.ErpPlateId},ErpSid:{setting.ErpSid},ErpStoreNumber:{setting.ErpStoreNumber},ErpUrl:{setting.ErpUrl}");

                }
            }
            catch (Exception ex)
            {
                Log.Error("推送订单异常：" + ex);
            }
        }


        /// <returns></returns>
        public pushState BatchPushOrderToWangdiantong(List<OrderInfo> orders, WDTConfigModel setting, out string message)
        {
            try
            {
                message = "";
                if (!setting.OpenErp)
                {
                    return pushState.Fail;
                }


                WdtClient client = new WdtClient();
                client.sid = setting.ErpSid;
                client.appkey = setting.ErpAppkey;
                client.appsecret = setting.ErpAppsecret;
                client.gatewayUrl = setting.ErpUrl + "/openapi2/trade_push.php";
                client.putParams("shop_no", setting.ErpStoreNumber);

                List<OrderInfo> unCanPush = new List<OrderInfo>();
                foreach (OrderInfo order in orders)
                {
                    //未支付或关闭的不推送到旺店通
                    if (order.OrderStatus == OrderOperateStatus.WaitPay || order.OrderStatus == OrderOperateStatus.Close || order.OrderStatus == OrderOperateStatus.History)
                    {
                        unCanPush.Add(order);
                        //return pushState.Fail;
                    }
                    //到店自提的订单不推送到旺店通
                    else if (order.DeliveryType == CommonModel.DeliveryType.ShopStore && order.DeliveryType == CommonModel.DeliveryType.SelfTake)
                    {
                        unCanPush.Add(order);
                    }
                    //拼团未完成的不推送到旺店通
                    else if (order.OrderType == OrderTypes.FightGroup)
                    {
                        if (order.FightGroupOrderJoinStatus != CommonModel.FightGroupOrderJoinStatus.BuildFailed
                            && order.FightGroupOrderJoinStatus == CommonModel.FightGroupOrderJoinStatus.JoinSuccess)
                        {
                            unCanPush.Add(order);
                        }
                    }
                }

                orders.RemoveAll(t => unCanPush.Contains(t));


                List<long> UserIds = orders.Select(e => e.UserId).Distinct().ToList();
                List<MemberInfo> members = new MemberService().GetMembers(UserIds).ToList();
                List<dynamic> wdtorders = new List<dynamic>();
                foreach (var item in orders)
                {
                    wdtorders.Add(PushWdtOrders(item, members.FirstOrDefault(m => m.Id == item.UserId)));
                }




                string json = JsonConvert.SerializeObject(wdtorders);

                client.putParams("trade_list", json);
                client.putParams("switch", "0");
                string result = client.wdtOpenapi();

                //获取推送结果
                baseResponse resultModel = JsonConvert.DeserializeObject<baseResponse>(result);
                Log.Info("请求数据：" + json);
                Log.Info(Newtonsoft.Json.JsonConvert.SerializeObject(resultModel));
                message = resultModel.message;
                if (resultModel.code == 0 && string.IsNullOrEmpty(resultModel.message))
                {
                    return pushState.Success;
                }
                else if (resultModel.code == 0 && !string.IsNullOrEmpty(resultModel.message))
                {
                    return pushState.PartSuccess;
                }
                else
                {
                    Log.Error("推送失败，结果：" + result);
                    return pushState.Fail;
                }

            }
            catch (Exception ex)
            {
                Log.Error("推送订单:" + ex.ToString());
                message = ex.ToString();
                return pushState.Error;
            }
        }

        /// <summary>
        /// 获取旺店通发票类型
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private int GetInvoiceKind(OrderInvoiceInfo info)
        {
            if (info == null || info.Id <= 0) { return 0; }
            switch (info.InvoiceType)
            {
                case InvoiceType.ElectronicInvoice:
                    return 1;
                    break;
                case InvoiceType.OrdinaryInvoices:
                    return 2;
                    break;
                case InvoiceType.VATInvoice:
                    return 3;
                    break;
            }
            return 0;
        }

        private string GetInvoiceContent(OrderInvoiceInfo info)
        {
            if (info == null || info.Id <= 0) { return ""; }
            string content = "";
            if (!string.IsNullOrEmpty(info.InvoiceCode))
            {
                content = "纳税人识别号:" + info.InvoiceCode + ";";
            }
            if (!string.IsNullOrEmpty(info.Address))
            {
                content += "地址:" + info.Address + " " + info.CellPhone + ";";
            }
            if (!string.IsNullOrEmpty(info.BankName))
            {
                content += "开户银行:" + info.BankName + " " + info.BankNo + ";";
            }
            return content;
        }

        public dynamic PushWdtOrders(OrderInfo order, MemberInfo currentmember)
        {

            var refunde = new RefundService().GetOrderRefundList(order.Id);
            List<ProductInfo> products = new ProductService().GetAllProductByIds(order.OrderItemInfo.Select(i => i.ProductId));
            var trade_list = new

            {
                tid = order.Id,
                trade_status = getTrade_status(order.OrderStatus),
                pay_status = getPay_status(order.OrderStatus),
                delivery_term = getPay_type(order.PaymentType),
                trade_time = order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                pay_time = order.PayDate.HasValue ? order.PayDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "0000-00-00 00:00:00",
                buyer_nick = string.IsNullOrEmpty(currentmember.Nick) ? currentmember.Nick : currentmember.UserName,
                pay_id = order.GatewayOrderId,
                receiver_name = order.ShipTo,
                receiver_province = splitShippingRegion(order.RegionFullName, 0),
                receiver_city = splitShippingRegion(order.RegionFullName, 1),
                receiver_district = splitShippingRegion(order.RegionFullName, 2),
                receiver_address = order.Address,
                receiver_mobile = order.CellPhone,
                logistics_type = logisticCompany(order.ExpressCompanyName),
                invoice_kind = GetInvoiceKind(order.OrderInvoice),//是否发票类别
                invoice_title = order.OrderInvoice == null ? "" : order.OrderInvoice.InvoiceTitle,//发票抬头
                invoice_content = GetInvoiceContent(order.OrderInvoice),//发票内容
                buyer_message = order.UserRemark,
                remark_flag = remarkFlag(order.SellerRemarkFlag),
                seller_memo = order.SellerRemark,
                post_amount = order.Freight,
                cod_amount = getPay_type(order.PaymentType) == 2 ? order.OrderTotalAmount : 0,//货到付款才有金额，否则为0
                ext_cod_fee = 0,
                other_amount = 0,
                paid = order.ActualPayAmount,
                order_list = order.OrderItemInfo.Select(n => new
                {
                    oid = n.OrderId + n.Id,  //子订单编号要加字段
                    num = n.Quantity,
                    price = n.SalePrice,
                    status = getItemTrade_status(n, order, refunde),
                    refund_status = getRefund_status(n, order, refunde),
                    goods_id = n.ProductId.ToString(),
                    spec_id = n.SkuId,
                    goods_no = getProductCode(products, n.ProductId, n.Id),
                    spec_no = string.IsNullOrEmpty(n.SKU) ? getProductCode(products, n.ProductId, n.Id) : n.SKU,
                    goods_name = n.ProductName,
                    spec_name = n.Color + ";" + n.Size + ";" + n.Version,
                    adjust_amount = n.DiscountAmount,
                    discount = n.CouponDiscount + n.PlatCouponDiscount,
                    share_discount = n.FullDiscount
                })
            };

            return trade_list;
        }

        private string getProductCode(List<ProductInfo> products, long productId, long itemId)
        {
            var productInfo = products.FirstOrDefault(p => p.Id == productId);
            if (productInfo != null)
            {
                return productInfo.ProductCode;
            }
            else
            {
                return productId.ToString() + itemId.ToString();
            }
        }

        public int getTrade_status(OrderOperateStatus status)
        {
            int trade_status = -1;
            if (status == OrderOperateStatus.WaitPay)
            {
                trade_status = 10;
            }
            if (status == OrderOperateStatus.WaitReceiving)
            {
                trade_status = 50;
            }
            if (status == OrderOperateStatus.Close)
            {
                trade_status = 90;
            }
            if (status == OrderOperateStatus.Finish)
            {
                trade_status = 70;
            }
            if (status == OrderOperateStatus.WaitDelivery)
            {
                trade_status = 30;
            }
            if (trade_status < 0)
            {
                trade_status = status == OrderOperateStatus.WaitPay ? 10 : 30;
            }
            return trade_status;
        }

        public static int getPay_status(OrderOperateStatus status)
        {

            if (status == OrderOperateStatus.WaitPay || status == OrderOperateStatus.Close)
            {
                return 0;
            }
            else
            {
                return 2;
            }
        }

        public int getPay_type(PaymentTypes paytype)
        {
            int delivery_term = 1;
            if (paytype == PaymentTypes.CashOnDelivery)
            {
                delivery_term = 2;
            }
            return delivery_term;
        }

        public string getBuyer_nick(long userId)
        {
            string nick = "";
            var memberinfo = new MemberService().GetMember(userId);
            if (memberinfo != null)
            {
                nick = string.IsNullOrEmpty(memberinfo.Nick) ? memberinfo.UserName : memberinfo.Nick;
            }
            return nick;
        }

        public string splitShippingRegion(string address, int index)
        {
            string result = "";
            string[] str = address.Split(' ');
            if (index < str.Length)
            {
                result = str[index];
            }
            return result;
        }

        //物流方式
        public string logisticCompany(string expressname)
        {
            string result = "";
            switch (expressname)
            {
                case "圆通速递":
                    result = "4";
                    break;
                case "顺丰速运":
                    result = "8";
                    break;
                case "中通速递":
                    result = "5";
                    break;
                case "申通物流":
                    result = "6";
                    break;
                case "全峰快递":
                    result = "7";
                    break;
                case "韵达快递":
                    result = "9";
                    break;
                case "汇通快运":
                    result = "10";
                    break;
                case "中铁快运":
                    result = "11";
                    break;
                case "中远":
                    result = "12";
                    break;
                case "龙邦速递":
                    result = "13";
                    break;
                case "快捷速递":
                    result = "14";
                    break;
                case "全日通快递":
                    result = "15";
                    break;
                case "海航天天快递":
                    result = "16";
                    break;
                case "发网":
                    result = "17";
                    break;
                case "联昊通":
                    result = "18";
                    break;
                case "宅急送":
                    result = "19";
                    break;
                case "百世物流":
                    result = "20";
                    break;
                case "联邦快递":
                    result = "21";
                    break;
                case "德邦物流":
                    result = "22";
                    break;
                case "邮政国内小包":
                    result = "51";
                    break;
                case "同城快递":
                    result = "54";
                    break;
                default:
                    result = "-1";
                    break;
            }
            return result;
        }

        //客户标记
        public static string remarkFlag(int? flag)
        {
            string result = "0";
            if (flag.HasValue)
            {
                switch (flag.Value)
                {
                    case 1://绿
                        result = "3";
                        break;
                    case 2://蓝
                        result = "4";
                        break;
                    case 3://黄
                        result = "2";
                        break;
                    case 4://红
                        result = "1";
                        break;
                    default:
                        result = "0";
                        break;
                };
            }
            return result;
        }


        public int getItemTrade_status(OrderItemInfo item, OrderInfo order, List<OrderRefundInfo> refundelist)
        {
            int trade_status = -1;
            var status = order.OrderStatus;
            if (status == OrderOperateStatus.WaitPay)
            {
                trade_status = 10;
            }
            if (status == OrderOperateStatus.WaitReceiving)
            {
                trade_status = 50;
            }
            if (status == OrderOperateStatus.Close)
            {
                trade_status = 90;
            }
            if (status == OrderOperateStatus.Finish)
            {
                trade_status = 70;
            }
            if (status == OrderOperateStatus.WaitDelivery)
            {
                trade_status = 30;
            }
            if (refundelist.Count > 0)
            {
                var skurefund = refundelist.Where(r => r.OrderItemId == item.Id).FirstOrDefault();
                if (skurefund.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                {//申请退款
                    trade_status = 2;
                }
                if (skurefund.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.UnAudit)
                {
                    trade_status = 1;
                }
                if (skurefund.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed)
                {
                    trade_status = 80;
                }
            }

            return trade_status;
        }

        public int getRefund_status(OrderItemInfo item, OrderInfo order, List<OrderRefundInfo> refundelist)
        {
            int trade_status = 0;

            if (refundelist.Count > 0)
            { //存在售后
                var skurefund = refundelist.Where(r => r.OrderItemId == item.Id).FirstOrDefault();
                if (skurefund.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                {//申请退款
                    trade_status = 2;
                }
                if (skurefund.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.UnAudit)
                {
                    trade_status = 1;
                }
                if (skurefund.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery)
                {
                    trade_status = 3;
                }
                if (skurefund.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving)
                {
                    trade_status = 4;
                }
                if (skurefund.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed)
                {
                    trade_status = 5;
                }
            }
            return trade_status;

        }
        /// <summary>
        /// 查询物流同步  ERP销售订单的发货状态、物流单号等同步给其他系统，注：”查询物流同步”与“物流同步回写”两个接口配合使用，完成“销售订单发货同步”
        /// </summary>
        /// <param name="setting"></param>
        public void SyncOrderSendGoodsStatus(WDTConfigModel setting)
        {
            string result = "";
            WdtClient client = new WdtClient();
            client.sid = setting.ErpSid;
            client.appkey = setting.ErpAppkey;
            client.appsecret = setting.ErpAppsecret;
            client.gatewayUrl = setting.ErpUrl + "/openapi2/logistics_sync_query.php";
            client.putParams("shop_no", setting.ErpStoreNumber);
            client.putParams("limit", "100");
            QueryWDTOrderResponse response = new QueryWDTOrderResponse();
            try
            {
                result = client.wdtOpenapi();
                response = Newtonsoft.Json.JsonConvert.DeserializeObject<QueryWDTOrderResponse>(result);
                if (response != null && response.code == 0)
                {

                    List<LogisticsSyncTrades> changeLists = response.trades;

                    List<LogisticsListInfo> logisticslist = SyncSendGoods(changeLists);
                    LogisticsSyncBack(setting, logisticslist);
                }

                else
                {
                    Log.Error("旺店通同步商品库存异常" + result);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

        }
        /// <summary>
        /// 是否可以发货
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        private bool CanSendGood(OrderInfo ordobj)
        {
            bool result = false;
            if (ordobj.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                var fgord = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.OrderId == ordobj.Id).FirstOrDefault();
                if (fgord.CanSendGood)
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }
            return result;
        }
        // 同步发货状态和信息,发送失败则记录错误信息
        public List<LogisticsListInfo> SyncSendGoods(List<LogisticsSyncTrades> changeLists)
        {
            List<LogisticsListInfo> errors = new List<LogisticsListInfo>();
            List<long> orderIds = changeLists.Select(c => long.Parse(c.tid)).ToList();
            List<OrderInfo> orders = DbFactory.Default.Get<OrderInfo>().Where(o => o.Id.ExIn(orderIds.ToList<long>())).ToList();
            if (orders == null) { orders = new List<OrderInfo>(); }
            foreach (LogisticsSyncTrades syncTrade in changeLists)
            {
                var order = orders.Where(o => o.Id == long.Parse(syncTrade.tid)).FirstOrDefault();
                if (order == null || order.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery && order.OrderStatus != OrderInfo.OrderOperateStatus.WaitReceiving)
                {
                    errors.Add(new LogisticsListInfo()
                    {
                        rec_id = syncTrade.rec_id,
                        status = 1,
                        message = "订单不存在或者订单状态不正确",
                    });
                }
                else if (!CanSendGood(order))
                {
                    errors.Add(new LogisticsListInfo()
                    {
                        rec_id = syncTrade.rec_id,
                        status = 1,
                        message = "拼团还没有完成不能发货",
                    });
                }
                else
                {
                    order.OrderStatus = OrderInfo.OrderOperateStatus.WaitReceiving;
                    order.ExpressCompanyName = syncTrade.logistics_name;
                    order.ShipOrderNumber = syncTrade.logistics_no;
                    order.ShippingDate = DateTime.Now;
                    order.LastModifyTime = DateTime.Now;
                    //处理订单退款
                    var refund = DbFactory.Default
                        .Get<OrderRefundInfo>()
                        .Where(d => d.OrderId == order.Id && d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund
                            && d.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                        .FirstOrDefault();
                    if (refund != null)
                    {
                        //自动拒绝退款申请
                        ServiceProvider.Instance<RefundService>.Create.SellerDealRefund(refund.Id, OrderRefundInfo.OrderRefundAuditStatus.UnAudit, "旺店通已发货", "旺店通接口");
                    }
                    DbFactory.Default.Update(order);

                    AddOrderOperationLog(order.Id, "旺店通发货状态同步", "旺店通发货");
                    errors.Add(new LogisticsListInfo()
                    {
                        rec_id = syncTrade.rec_id,
                        status = 0,
                        message = "",
                    });
                }

            }
            return errors;
        }



        // 添加订单操作日志
        private void AddOrderOperationLog(long orderId, string userName, string operateContent)
        {
            OrderOperationLogInfo orderOperationLog = new OrderOperationLogInfo();
            orderOperationLog.Operator = userName;
            orderOperationLog.OrderId = orderId;
            orderOperationLog.OperateDate = DateTime.Now;
            orderOperationLog.OperateContent = operateContent;

            DbFactory.Default.Add(orderOperationLog);
        }
        /// <summary>
        /// 物流同步回写  将物流同步（发货状态、物流单号等）是否成功的结果批量回传给ERP。
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="logistics_list"></param>
        public void LogisticsSyncBack(WDTConfigModel setting, List<LogisticsListInfo> logistics_list)
        {
            WdtClient client = new WdtClient();
            client.sid = setting.ErpSid;
            client.appkey = setting.ErpAppkey;
            client.appsecret = setting.ErpAppsecret;
            client.gatewayUrl = setting.ErpUrl + "/openapi2/logistics_sync_ack.php";


            String json = logistics_list.ToJsonString();

            client.putParams("logistics_list", json);

            string result = client.wdtOpenapi();
            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<LogisticsSyncErrors>(result);
            if (!string.IsNullOrEmpty(response.error))
            {
                Log.Error("回写订单发货结果到旺店通失败：" + result);
            }
        }
    }
}
