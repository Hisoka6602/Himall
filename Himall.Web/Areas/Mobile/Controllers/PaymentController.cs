﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Core.Plugins.Payment;
using Himall.Service;
using Himall.Web.App_Code.Common;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class PaymentController : BaseMobileTemplatesController


    {
        OrderService _OrderService;
        MemberService _MemberService;
        MemberCapitalService _MemberCapitalService;
        FightGroupService _FightGroupService;
        public PaymentController(OrderService OrderService, MemberService MemberService
            , MemberCapitalService MemberCapitalService, FightGroupService FightGroupService
            )
        {
            _OrderService = OrderService;
            _MemberService = MemberService;
            _MemberCapitalService = MemberCapitalService;
            _FightGroupService = FightGroupService;

        }
        /// <summary>
        /// 预存款支付
        /// </summary>
        /// <param name="pmtidpmtid"></param>
        /// <param name="ids"></param>
        /// <param name="payPwd"></param>
        /// <returns></returns>
        public JsonResult PayByCapital(string ids, string payPwd)
        {
            OrderApplication.PayByCapital(UserId, ids, payPwd, WebHelper.GetHost());
            return SuccessResult<dynamic>(msg: "支付成功");
        }
        public JsonResult PayByCapitalIsOk(string ids)
        {
            var result = OrderApplication.PayByCapitalIsOk(UserId, ids);
            return Json<dynamic>(success: result);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pmtid"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public JsonResult Pay(string pmtid, string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
            {
                Log.Error("payment/pay 参数异常：IDS为空");
                return ErrorResult<dynamic>("参数异常");
            }
            if (string.IsNullOrWhiteSpace(pmtid))
            {
                Log.Error("payment/pay 参数异常：pmtid为空");
                return ErrorResult<dynamic>("参数异常");
            }
#if DEBUG
            Log.Debug("Pay ids:" + ids);
#endif
            var orderIdArr = ids.Split(',').Select(item => long.Parse(item));
            //获取待支付的所有订单
            var orders = _OrderService.GetOrders(orderIdArr).Where(item => item.OrderStatus == Entities.OrderInfo.OrderOperateStatus.WaitPay).ToList();

            if (orders == null || orders.Count == 0 || orders.Exists(o => o.UserId != CurrentUser.Id)) //订单状态不正确
            {
                Log.Error($"payment/pay 未找到可支付的订单,OrderId:{ids},订单用户编号：{(orders == null || orders.Count == 0 ? "" : string.Join(",", orders.Select(o => o.UserId)))},登录用户编号：{CurrentUser.Id}");
                return ErrorResult<dynamic>($"未找到可支付的订单,OrderId:{ids},订单用户编号：{(orders == null || orders.Count == 0 ? "" : string.Join(", ", orders.Select(o => o.UserId)))},登录用户编号：{CurrentUser.Id}");
            }

            decimal total = orders.Sum(a => a.OrderTotalAmount - a.CapitalAmount);
            if (total == 0)
            {
                Log.Error("payment/pay 支付金额不能为0");
                return ErrorResult<dynamic>("支付金额不能为0");
            }

            foreach (var item in orders)
            {
                if (item.OrderType == Entities.OrderInfo.OrderTypes.FightGroup)
                {
                    if (!_FightGroupService.OrderCanPay(item.Id))
                    {
                        Log.Error("payment/pay 有拼团订单为不可付款状态");
                        return ErrorResult<dynamic>("有拼团订单为不可付款状态");
                        //throw new HimallException("有拼团订单为不可付款状态");
                    }
                }
            }

            //获取所有订单中的商品名称
            var productInfos = GetProductNameDescriptionFromOrders(orders);
            string webRoot = SiteSettingApplication.GetCurDomainUrl();
            string urlPre = webRoot + "/m-" + PlatformType + "/Payment/";
            //获取同步返回地址
            string returnUrl = webRoot + "/Pay/Return/{0}";
            //获取异步通知地址
            string payNotify = webRoot + "/Pay/Notify/{0}";
            string notifyPre = urlPre + "Notify/", returnPre = webRoot + "/m-" + PlatformType + "/Member/PaymentToOrders?ids=" + ids;
            if (pmtid.ToLower().Contains("weixin"))
            {//微信里跳转到分享页面
                //支付成功后晒单地址(source=pay用来区分从哪个页面跳转过去的)
                returnPre = webRoot + "/m-" + PlatformType + "/Order/OrderShare?source=pay&orderids=" + ids;
            }
            var payment = Core.PluginsManagement.GetPlugins<IPaymentPlugin>(true).FirstOrDefault(d => d.PluginInfo.PluginId == pmtid);
            if (payment == null)
            {
                throw new HimallException("错误的支付方式");
            }
            string openId = Core.Helper.WebHelper.GetCookie(CookieKeysCollection.HIMALL_USER_OpenID);
            if (!string.IsNullOrWhiteSpace(openId))
            {
                openId = Core.Helper.SecureHelper.AESDecrypt(openId, "Mobile");
            }
            else
            {
                var openIds = _MemberService.GetOpenIdByUser(CurrentUser.Id);
                var openUserInfo = openIds.FirstOrDefault(item => item.AppIdType == Entities.MemberOpenIdInfo.AppIdTypeEnum.Payment);
                if (openUserInfo != null)
                    openId = openUserInfo.OpenId;
            }

            #region 支付流水获取
            var orderPayModel = orders.Select(item => new Entities.OrderPayInfo
            {
                PayId = 0,
                OrderId = item.Id
            });
            //保存支付订单
            long payid = _OrderService.SaveOrderPayInfo(orderPayModel, PlatformType);
            #endregion
#if DEBUG
            Log.Debug("Pay payid:" + payid);
#endif
            //组织返回Model
            Himall.Web.Models.PayJumpPageModel model = new Himall.Web.Models.PayJumpPageModel();
            model.PaymentId = pmtid;
            model.OrderIds = ids;
            model.TotalPrice = total;
            model.UrlType = payment.Biz.RequestUrlType; ;
            model.PayId = payid;
            try
            {
#if DEBUG
                Core.Log.Info("其他详情 :  returnPre = " + returnPre + " notifyPre = " + notifyPre + payment.PluginInfo.PluginId.Replace(".", "-") + " ids = " + ids + " totalAmount=" + total + " productInfos=" + productInfos + " openId=" + openId);
#endif
                model.RequestUrl = payment.Biz.GetRequestUrl(returnPre, notifyPre + payment.PluginInfo.PluginId.Replace(".", "-") + "/", payid.ToString(), total, productInfos, openId);
                Log.Debug(string.Format("openId:{0} Url:{1}", openId, model.RequestUrl));
            }
            catch (Exception ex)
            {
                Core.Log.Error("支付页面加载支付插件出错：", ex);
                throw new HimallException("错误的支付方式");
            }
#if DEBUG
            Core.Log.Info("支付方式详情 :  id = " + payment.PluginInfo.PluginId + " name = " + payment.PluginInfo.DisplayName + " url = " + model.RequestUrl);
#endif
            if (string.IsNullOrWhiteSpace(model.RequestUrl))
            {
                throw new HimallException("错误的支付方式,获取支付地址为空");
            }
            switch (model.UrlType)
            {
                case UrlType.Page:
                    return SuccessResult<dynamic>(data: new { jumpUrl = model.RequestUrl });
                //return Json(new { success = true, msg = "", jumpUrl = model.RequestUrl });
                //return Redirect(model.RequestUrl);
                case UrlType.QRCode:
                    return SuccessResult<dynamic>(data: new { jumpUrl = "/Pay/QRPay/?id=" + pmtid + "&url=" + model.RequestUrl + "&orderIds=" + ids });
                    //return Json(new { success = true, msg = "", jumpUrl = "/Pay/QRPay/?id=" + pmtid + "&url=" + model.RequestUrl + "&orderIds=" + ids });
                    //return Redirect("/Pay/QRPay/?id=" + pmtid + "&url=" + model.RequestUrl + "&orderIds=" + ids);
            }
            return ErrorResult<dynamic>("调用支付方式异常");
        }
        /// <summary>
        /// 对PaymentId进行加密（因为PaymentId中包含小数点"."，因此进行编码替换）
        /// </summary>
        private string EncodePaymentId(string paymentId)
        {
            return paymentId.Replace(".", "-");
        }

        // GET: Mobile/Payment
        public JsonResult Get(string orderIds)
        {
            var mobilePayments = Core.PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(item => item.Biz.SupportPlatforms.Contains(PlatformType));
            string webRoot = CurrentUrlHelper.CurrentUrlNoPort();
            string urlPre = webRoot + "/m-" + PlatformType + "/Payment/";

            //获取待支付的所有订单
            var orderService = _OrderService;
            var orders = orderService.GetOrders(orderIds.Split(',').Select(t => long.Parse(t))).ToList();
            var waitPayOrders = orders.Where(p => p.OrderStatus == Entities.OrderInfo.OrderOperateStatus.WaitPay);
            var totalAmount = waitPayOrders.Sum(t => t.OrderTotalAmount - t.CapitalAmount);

            /* 移到 Payment/pay实现 lly
            //获取所有订单中的商品名称
            string productInfos = GetProductNameDescriptionFromOrders(orders);
            string openId = Core.Helper.WebHelper.GetCookie(CookieKeysCollection.HIMALL_USER_OpenID);
            if (!string.IsNullOrWhiteSpace(openId))
            {
                openId = Core.Helper.SecureHelper.AESDecrypt(openId, "Mobile");
            }
            else
            {
                var openUserInfo = _MemberService.GetMember(CurrentUser.Id).MemberOpenIdInfo.FirstOrDefault(item => item.AppIdType == MemberOpenIdInfo.AppIdTypeEnum.Payment);
                if (openUserInfo != null)
                    openId = openUserInfo.OpenId;
            }
            string[] strIds = orderIds.Split(',');
            string notifyPre = urlPre + "Notify/", returnPre = webRoot + "/m-" + PlatformType + "/Member/PaymentToOrders?ids=" + orderIds;

            var orderPayModel = waitPayOrders.Select(p => new OrderPayInfo
            {
                PayId = 0,
                OrderId = p.Id
            });

            //保存支付订单
            var payid = orderService.SaveOrderPayInfo(orderPayModel, PlatformType);
            var ids = payid.ToString();
             * */
            var model = mobilePayments.ToArray().Select(item =>
               {
                   string url = string.Empty;
                   return new
                   {
                       id = item.PluginInfo.PluginId,
                       //name = item.PluginInfo.DisplayName,
                       name = PaymentApplication.GetForeGroundPaymentName(item.PluginInfo.DisplayName),
                       logo = item.Biz.Logo,
                       url = url
                   };
               }).OrderByDescending(d => d.id);
            foreach (var item in model)
            {
                Core.Log.Debug(item.id + "   " + item.name);
            }
            return Json(new { data = model, totalAmount = totalAmount });
        }

        [ValidateInput(false)]
        public ContentResult Notify(string id)
        {
            id = DecodePaymentId(id);
            string errorMsg = string.Empty;
            string response = string.Empty;

            try
            {
                var payment = Core.PluginsManagement.GetPlugin<IPaymentPlugin>(id);
                var payInfo = payment.Biz.ProcessNotify(this.HttpContext.Request);
                if (payInfo != null)
                {
                    var orderid = payInfo.OrderIds.FirstOrDefault();
                    var orderIds = OrderApplication.GetOrderPay(orderid).Select(item => item.OrderId).ToList();
                    var payTime = payInfo.TradeTime;
                    OrderApplication.PaySucceed(orderIds, id, payInfo.TradeTime.Value, payInfo.TradNo, payId: orderid);
                    //写入支付状态缓存
                    string payStateKey = CacheKeyCollection.PaymentState(string.Join(",", orderIds));//获取支付状态缓存键
                    Cache.Insert(payStateKey, true, 15);//标记为已支付
                    #region TOD:ZYF 注释，统一在service的方法里实现
                    //PaymentHelper.IncreaseSaleCount(orderIds);
                    #endregion
                    PaymentHelper.GenerateBonus(orderIds, WebHelper.GetHost());
                    response = payment.Biz.ConfirmPayResult();

                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                Core.Log.Error("移动端支付异步通知返回出错，支持方式：" + id, ex);
            }
            return Content(response);
        }

        public ActionResult Return(string id)
        {
            id = DecodePaymentId(id);
            string errorMsg = string.Empty;

            try
            {
                var payment = Core.PluginsManagement.GetPlugin<IPaymentPlugin>(id);
                var payInfo = payment.Biz.ProcessReturn(HttpContext.Request);
                if (payInfo != null)
                {
                    var payTime = payInfo.TradeTime;

                    var orderid = payInfo.OrderIds.FirstOrDefault();
                    var orderIds = OrderApplication.GetOrderPay(orderid).Select(item => item.OrderId).ToList();

                    ViewBag.OrderIds = string.Join(",", orderIds);
                    OrderApplication.PaySucceed(orderIds, id, payInfo.TradeTime.Value, payInfo.TradNo, payId: orderid);

                    string payStateKey = CacheKeyCollection.PaymentState(string.Join(",", orderIds));//获取支付状态缓存键
                    Cache.Insert(payStateKey, true, 15);//标记为已支付

                    var order = OrderApplication.GetOrder(orderid);
                    if (order != null)
                    {
                        if (order.OrderType == Entities.OrderInfo.OrderTypes.FightGroup)
                        {
                            var gpord = FightGroupApplication.GetOrder(orderid);
                            if (gpord != null)
                            {
                                return Redirect(string.Format("/m-{0}/FightGroup/GroupOrderOk?orderid={1}", PlatformType.ToString(), orderid));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                Core.Log.Error("移动端同步返回出错，支持方式：" + id, ex);
            }
            ViewBag.Error = errorMsg;

            return View();
        }



        string DecodePaymentId(string paymentId)
        {
            return paymentId.Replace("-", ".");
        }


        string GetProductNameDescriptionFromOrders(IEnumerable<Entities.OrderInfo> orders)
        {
            var items = _OrderService.GetOrderItemsByOrderId(orders.Select(p => p.Id));
            var productNames = items.Select(p => p.ProductName);
            var productInfos = productNames.Count() > 1 ? (productNames.ElementAt(0) + " 等" + productNames.Count() + "种商品") : productNames.ElementAt(0);
            return productInfos;
        }
        /// <summary>
        /// 判断是否设置支付密码
        /// </summary>
        public JsonResult GetPayPwd()
        {
            bool result = false;
            result = OrderApplication.GetPayPwd(UserId);
            return Json<dynamic>(success: result);
        }
        /// <summary>
        /// 设置密码
        /// </summary>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public JsonResult SetPayPwd(string pwd)
        {
            _MemberCapitalService.SetPayPwd(CurrentUser.Id, pwd);
            return SuccessResult(msg: "设置成功");
        }
        /// <summary>
        /// 判断预存款支付密码
        /// </summary>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public JsonResult ValidPayPwd(string pwd)
        {
            var ret = MemberApplication.VerificationPayPwd(CurrentUser.Id, pwd);
            return Json<dynamic>(success: ret, msg: "密码错误");
        }
        [ActionName("CapitalChargeNotify")]
        [ValidateInput(false)]
        public ContentResult PayNotify_Charge(string id)
        {
            var plugin = Core.PluginsManagement.GetPlugin<IPaymentPlugin>(id.Replace("-", "."));
            var payInfo = plugin.Biz.ProcessNotify(this.HttpContext.Request);
            if (payInfo != null)
            {

                var chargeApplyId = payInfo.OrderIds.FirstOrDefault();
                MemberCapitalApplication.ChargeSuccess(chargeApplyId);
                var response = plugin.Biz.ConfirmPayResult();
                return Content(response);
            }
            return Content(string.Empty);
        }
    }
}