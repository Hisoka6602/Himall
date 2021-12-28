using AutoMapper;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Mobile.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    /// <summary>
    /// 拼团
    /// </summary>
    public class MyGiftsController : BaseMobileMemberController
    {
        private ProductService _ProductService;
        private TypeService _iTypeService;
        private GiftsOrderService _iGiftsOrderService;
        private ExpressService _ExpressService;
        public MyGiftsController(ProductService ProductService, TypeService TypeService
            , GiftsOrderService GiftsOrderService, ExpressService ExpressService
            )
        {
            _ProductService = ProductService;
            _iTypeService = TypeService;
            _iGiftsOrderService = GiftsOrderService;
            _ExpressService = ExpressService;
        }

        #region 礼品订单列表
        /// <summary>
        /// 礼品订单列表
        /// </summary>
        /// <returns></returns>
        public ActionResult OrderList(int? status = null)
        {
            ViewBag.status = status;
            return View();
        }

        /// <summary>
        /// 获取礼品订单列表
        /// </summary>
        /// <param name="skey"></param>
        /// <param name="status"></param>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetOrderList(string skey = "", Himall.Entities.GiftOrderInfo.GiftOrderStatus? status = null, int page = 1, int pagesize = 10)
        {
            if (CurrentUser == null)
            {
                throw new HimallException("错误的用户信息");
            }
            int rows = pagesize;
            GiftsOrderQuery query = new GiftsOrderQuery();
            query.Skey = skey;
            if (status != null)
            {
                if ((int)status != 0)
                {
                    query.Status = status;
                }
            }
            query.UserId = CurrentUser.Id;
            query.PageSize = rows;
            query.PageNo = page;
            var orderdata = _iGiftsOrderService.GetOrders(query);
            List<Himall.Entities.GiftOrderInfo> orderlist = orderdata.Models.ToList();
            //_iGiftsOrderService.OrderAddUserInfo(orderlist);
            var result = orderlist.ToList();
            Mapper.CreateMap<GiftOrderInfo, GiftsOrderDtoModel>();
            Mapper.CreateMap<GiftOrderItemInfo, GiftsOrderItemDtoModel>();
            List<GiftsOrderDtoModel> pagedata = new List<GiftsOrderDtoModel>();
            foreach (var order in result)
            {
                order.Address = ClearHtmlString(order.Address);
                order.CloseReason = ClearHtmlString(order.CloseReason);
                order.UserRemark = ClearHtmlString(order.UserRemark);

                var tmpordobj = Mapper.Map<GiftsOrderDtoModel>(order);
                tmpordobj.Items = new List<GiftsOrderItemDtoModel>();
                var orderitems = _iGiftsOrderService.GetOrderItemByOrder(order.Id);
                foreach (var subitem in orderitems)
                {
                    var tmporditemobj = Mapper.Map<GiftsOrderItemDtoModel>(subitem);
                    tmporditemobj.DefaultImage = HimallIO.GetRomoteProductSizeImage(tmporditemobj.ImagePath, 1, ImageSize.Size_150.GetHashCode());
                    tmpordobj.Items.Add(tmporditemobj);
                }
                pagedata.Add(tmpordobj);
            }

            var pageresult = SuccessResult(data: new
            {
                total = orderdata.Total,
                rows = pagedata
            });
            return pageresult;
        }
        /// <summary>
        /// 获取订单综合数据
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetOrderCount()
        {
            if (CurrentUser == null)
            {
                throw new HimallException("错误的用户信息");
            }
            GiftsOrderAggregateDataModel result = new GiftsOrderAggregateDataModel();
            long curid = CurrentUser.Id;
            result.AllCount = _iGiftsOrderService.GetOrderCount(null, curid);
            result.WaitDeliveryCount = _iGiftsOrderService.GetOrderCount(Himall.Entities.GiftOrderInfo.GiftOrderStatus.WaitDelivery, curid);
            result.WaitReceivingCount = _iGiftsOrderService.GetOrderCount(Himall.Entities.GiftOrderInfo.GiftOrderStatus.WaitReceiving, curid);
            result.FinishCount = _iGiftsOrderService.GetOrderCount(Himall.Entities.GiftOrderInfo.GiftOrderStatus.Finish, curid);
            result.success = true;
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 获取订单信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orderid"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetOrder(long id)
        {
            if (CurrentUser == null)
            {
                throw new HimallException("错误的用户信息");
            }
            var orderdata = _iGiftsOrderService.GetOrder(id, CurrentUser.Id);
            if (orderdata == null)
            {
                throw new HimallException("错误的订单编号");
            }
            Mapper.CreateMap<GiftOrderInfo, GiftsOrderDtoModel>();
            Mapper.CreateMap<GiftOrderItemInfo, GiftsOrderItemDtoModel>();
            //_iGiftsOrderService.OrderAddUserInfo(orderlist);
            orderdata.Address = ClearHtmlString(orderdata.Address);
            orderdata.CloseReason = ClearHtmlString(orderdata.CloseReason);
            orderdata.UserRemark = ClearHtmlString(orderdata.UserRemark);

            var result = Mapper.Map<GiftsOrderDtoModel>(orderdata);
            result.Items = new List<GiftsOrderItemDtoModel>();
            var orderitems = _iGiftsOrderService.GetOrderItemByOrder(orderdata.Id);
            foreach (var subitem in orderitems)
            {
                var tmporditemobj = Mapper.Map<GiftsOrderItemDtoModel>(subitem);
                tmporditemobj.DefaultImage = HimallIO.GetRomoteProductSizeImage(tmporditemobj.ImagePath, 1, ImageSize.Size_150.GetHashCode());
                result.Items.Add(tmporditemobj);
            }
            result.success = true;
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 获取物流信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetExpressInfo(long orderId)
        {
            if (CurrentUser == null)
            {
                throw new HimallException("错误的用户信息");
            }
            var order = _iGiftsOrderService.GetOrder(orderId, CurrentUser.Id);
            var expressData = _ExpressService.GetExpressData(order.ExpressCompanyName, order.ShipOrderNumber,order.Id.ToString(),order.CellPhone);

            if (expressData == null)
            {
                return Json(new { success = false, ExpressNum = order.ShipOrderNumber, ExpressCompanyName = order.ExpressCompanyName, Comment = "" }, JsonRequestBehavior.AllowGet);
            }

            if (expressData.Success)
                expressData.ExpressDataItems = expressData.ExpressDataItems.OrderByDescending(item => item.Time);//按时间逆序排列
            var json = new
            {
                Success = expressData.Success,
                Msg = expressData.Message,
                Data = expressData.ExpressDataItems.Select(item => new
                {
                    time = item.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    content = item.Content
                })
            };
            return Json(new { success = true, ExpressNum = order.ShipOrderNumber, ExpressCompanyName = order.ExpressCompanyName, Comment = json }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 确认收货
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult ConfirmOrderOver(long OrderId)
        {
            if (CurrentUser == null)
            {
                throw new HimallException("错误的用户信息");
            }
            if (OrderId < 1)
            {
                throw new HimallException("错误的订单编号");
            }
            Result result = new Result();
            _iGiftsOrderService.ConfirmOrder(OrderId, CurrentUser.Id);
            result.success = true;
            result.code = 1;
            result.msg = "订单完成";
            return Json(result);
        }
        #endregion

        /// <summary>
        /// 礼品订单详情
        /// </summary>
        /// <returns></returns>
        public ActionResult OrderDetail(long id)
        {
            var order = _iGiftsOrderService.GetOrder(id,CurrentUser.Id);
            if (order == null)
            {
                throw new HimallException("错误的参数");
            }
            var orderlist = new GiftOrderInfo[] { order };
            _iGiftsOrderService.OrderAddUserInfo(orderlist);
            order = orderlist.FirstOrDefault();
            var expressData = _ExpressService.GetExpressData(order.ExpressCompanyName, order.ShipOrderNumber,order.Id.ToString(), order.CellPhone);
            if (expressData.Success)
                expressData.ExpressDataItems = expressData.ExpressDataItems.OrderByDescending(item => item.Time);//按时间逆序排列
            MyGiftsOrderDetailModel result = new MyGiftsOrderDetailModel();
            result.OrderData = order;
            result.OrderItems = _iGiftsOrderService.GetOrderItemByOrder(id);
            result.ExpressData = expressData;
            return View(result);
        }


        #region 私有
        /// <summary>
        /// 计算最大页数
        /// </summary>
        /// <param name="total"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        private int GetMaxPage(int total, int pagesize)
        {
            int result = 1;
            if (total > 0 && pagesize > 0)
            {
                result = (int)Math.Ceiling((double)total / (double)pagesize);
            }
            return result;
        }
        /// <summary>
        /// 获取收货地址
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        private Entities.ShippingAddressInfo GetShippingAddress(long? regionId)
        {
            Entities.ShippingAddressInfo result = null;
            var _ShippingAddressService = ObjectContainer.Current.Resolve<ShippingAddressService>();
            if (regionId != null)
            {
                result = _ShippingAddressService.Get((long)regionId);
            }
            else
            {
                if (CurrentUser != null)
                {
                    result = _ShippingAddressService.GetDefault(CurrentUser.Id);
                }
            }
            return result;
        }
        /// <summary>
        /// 清理引号类字符
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string ClearHtmlString(string str)
        {
            string result = str;
            if (!string.IsNullOrWhiteSpace(result))
            {
                result = result.Replace("'", "&#39;");
                result = result.Replace("\"", "&#34;");
                result = result.Replace(">", "&gt;");
                result = result.Replace("<", "&lt;");
            }
            return result;
        }
        #endregion
    }
}