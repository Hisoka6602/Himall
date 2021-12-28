﻿using Himall.Application;
using Himall.Core;
using Himall.DTO;
using Himall.Web.Framework;
using Newtonsoft.Json;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class CommentController : BaseMobileMemberController
    {
        // GET: Mobile/Comment
        public ActionResult Index(long orderId)
        {
            var order = OrderApplication.GetOrder(orderId);
            var orderComments = OrderApplication.GetOrderCommentCount(new[] { orderId });

            bool valid = false;
            if (order != null && (!orderComments.ContainsKey(orderId) || orderComments[orderId] == 0))
            {
                // 订单还未被评价过，有效
                valid = true;
                var model = CommentApplication.GetProductEvaluationByOrderId(orderId, CurrentUser.Id);
                var orderEvaluation = TradeCommentApplication.GetOrderComment(orderId, CurrentUser.Id);

                ViewBag.Products = model;
                var orderItems = OrderApplication.GetOrderItemsByOrderId(orderId);
                ViewBag.OrderItemIds = orderItems.Select(item => item.Id);
                ViewBag.IsVirtual = order.OrderType == Himall.Entities.OrderInfo.OrderTypes.Virtual ? 1 : 0;
            }
            ViewBag.Valid = valid;

            return View();
        }
        [HttpGet]
        public JsonResult CheckIsComment(long orderId)
        {
            var order = OrderApplication.GetOrder(orderId);
            var orderComments = OrderApplication.GetOrderCommentCount(new[] { orderId });

            bool valid = false;
            if (order != null && (!orderComments.ContainsKey(orderId) || orderComments[orderId] == 0))
            {
                // 订单还未被评价过，有效
                valid = true;
            }
            return SuccessResult<dynamic>(data: new { valid = valid });
        }

        [HttpPost]
        public JsonResult AddComment(string comment)
        {
            bool result = false;
            var orderComment = Newtonsoft.Json.JsonConvert.DeserializeObject<OrderCommentModel>(comment);
            if (orderComment != null)
            {
                AddOrderComment(orderComment, orderComment.ProductComments.Count());//添加订单评价
                AddProductsComment(orderComment.OrderId, orderComment.ProductComments);//添加商品评价
                result = true;
            }
            return Json(new { success = result });
        }

        void AddOrderComment(OrderCommentModel comment, int productNum)
        {
            TradeCommentApplication.Add(new OrderComment()
            {
                OrderId = comment.OrderId,
                DeliveryMark = comment.DeliveryMark,
                ServiceMark = comment.ServiceMark,
                PackMark = comment.PackMark,
                UserId = CurrentUser.Id,

            }, productNum);
        }


        /// <summary>
        /// 下载微信图片
        /// </summary>
        /// <param name="link">下载地址</param>
        /// <param name="filePath">保存相对路径</param>
        /// <param name="fileName">保存地址</param>
        /// <returns></returns>
        public string DownloadWxImage(string mediaId)
        {
            var token = WXApiApplication.TryGetToken(SiteSettings.WeixinAppId, SiteSettings.WeixinAppSecret);
            var address = string.Format("https://file.api.weixin.qq.com/cgi-bin/media/get?access_token={0}&media_id={1}", token, mediaId);
            Random ra = new Random();
            var fileName = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + ra.Next(10) + ".jpg";
            var ImageDir = "/Storage/Plat/Comment/";
            WebClient wc = new WebClient();
            try
            {
                string fullPath = Path.Combine(ImageDir, fileName);
                var data = wc.DownloadData(address);
                MemoryStream stream = new MemoryStream(data);
                Core.HimallIO.CreateFile(fullPath, stream, FileCreateType.Create);
                return "/Storage/Plat/Comment/" + fileName;
            }
            catch (Exception ex)
            {
                Log.Error("下载图片发生异常" + ex.Message);
                return string.Empty;
            }
        }

        private string MoveImages(string image, long userId)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                return "";
            }
            var oldname = Path.GetFileName(image);
            string ImageDir = string.Empty;

            //转移图片
            string relativeDir = "/Storage/Plat/Comment/";
            string fileName = userId + oldname;
            if (image.Replace("\\", "/").Contains("/temp/"))//只有在临时目录中的图片才需要复制
            {
                Core.Log.Error("image:" + image + ",filename:" + fileName);
                Core.HimallIO.CopyFile(image, relativeDir + fileName, true);
                return relativeDir + fileName;
            }  //目标地址
            else if (image.Contains("/Storage"))
            {
                return image.Substring(image.LastIndexOf("/Storage"));
            }
            return image;
        }
        void AddProductsComment(long orderId, IEnumerable<ProductCommentModel> productComments)
        {
            var list = new List<ProductComment>();
            foreach (var productComment in productComments)
            {
                var model = new ProductComment();
                model.ReviewDate = DateTime.Now;
                model.ReviewContent = productComment.Content;
                model.UserId = CurrentUser.Id;
                model.UserName = CurrentUser.UserName;
                model.Email = CurrentUser.Email;
                model.SubOrderId = productComment.OrderItemId;
                model.ReviewMark = productComment.Mark;
                model.ProductId = productComment.ProductId;
                if (productComment.Images != null && productComment.Images.Length > 0)
                {
                    model.Images = new List<ProductCommentImage>();
                    foreach (var img in productComment.Images)
                    {
                        var p = new ProductCommentImage();
                        p.CommentType = 0;//0代表默认的表示评论的图片
                        p.CommentImage = MoveImages(img, CurrentUser.Id);
                        model.Images.Add(p);
                    }
                }
                else if (productComment.WXmediaId != null && productComment.WXmediaId.Length > 0)
                {
                    model.Images = new List<ProductCommentImage>();
                    foreach (var img in productComment.WXmediaId)
                    {
                        var p = new ProductCommentImage();
                        p.CommentType = 0;//0代表默认的表示评论的图片
                        p.CommentImage = DownloadWxImage(img);
                        if (!string.IsNullOrEmpty(p.CommentImage))
                        {
                            model.Images.Add(p);
                        }
                    }
                }
                list.Add(model);
            }
            CommentApplication.Add(list);
        }

        /// <summary>
        /// 追加评论
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public ActionResult AppendComment(long orderId)
        {

            var model = CommentApplication.GetProductEvaluationByOrderIdNew(orderId, CurrentUser.Id);
            if (model!=null && model.Count>0 && model.FirstOrDefault().AppendTime.HasValue)
            {
                return RedirectToAction("orders", "member");
            }
            long shopId = 0;
            if (model.Count > 0) {
                long pid = model[0].ProductId;
                shopId=ProductManagerApplication.GetProduct(pid).ShopId;
                ViewBag.shopId = shopId.ToString();
            }

            return View(model);
        }


        public ActionResult AppendProductComment(string productCommentsJSON)
        {
            var productComments = JsonConvert.DeserializeObject<List<AppendCommentModel>>(productCommentsJSON);

            foreach (var m in productComments)
            {
                m.UserId = CurrentUser.Id;

            }
            CommentApplication.Append(productComments);
            return Json(new Result() { success = true, msg = "追加成功" });
        }
    }
}