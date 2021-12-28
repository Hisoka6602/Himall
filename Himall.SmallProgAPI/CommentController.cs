using Himall.Application;
using Himall.DTO;
using Himall.Entities;
using Himall.SmallProgAPI.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Transactions;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    public class CommentController : BaseApiController
    {
        /// <summary>
        /// 根据订单ID获取评价
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetComment(long orderId)
        {
            CheckUserLogin();
            var order = OrderApplication.GetOrderInfo(orderId);
            var comment = OrderApplication.GetOrderCommentCount(order.Id);
            if (order != null && comment == 0)
            {
                var model = CommentApplication.GetProductEvaluationByOrderId(orderId, CurrentUser.Id).Select(item => new
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Image = item.ThumbnailsUrl.Contains("skus") ? Core.HimallIO.GetRomoteImagePath(item.ThumbnailsUrl) : Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_220) //商城App评论时获取商品图片
                });
                var orderitems = OrderApplication.GetOrderItems(order.Id);
                var orderEvaluation = TradeCommentApplication.GetOrderCommentInfo(orderId, CurrentUser.Id);
                var isVirtual = order.OrderType == Himall.Entities.OrderInfo.OrderTypes.Virtual ? 1 : 0;
                return JsonResult<dynamic>(new { Product = model, orderItemIds = orderitems.Select(item => item.Id), isVirtual = isVirtual });
            }
            else
                return Json(ErrorResult<dynamic>("该订单不存在或者已评论过"));
        }

        //发布评论
        public JsonResult<Result<int>> PostAddComment(CommentAddCommentModel value)
        {
            CheckUserLogin();
            try
            {
                string Jsonstr = value.Jsonstr;
                bool result = false;
                var orderComment = Newtonsoft.Json.JsonConvert.DeserializeObject<OrderCommentModel>(Jsonstr);
                if (orderComment != null)
                {
                    AddOrderComment(orderComment, orderComment.ProductComments.Count());//添加订单评价
                    AddProductsComment(orderComment.OrderId, orderComment.ProductComments);//添加商品评论
                    result = true;
                }
                return Json(ApiResult<int>(result));
            }
            catch (Exception ex)
            {
                Core.Log.Error(ex);
                return Json(ErrorResult<int>(ex.Message));
            }
        }

        void AddOrderComment(OrderCommentModel comment, int productNum)
        {
            TradeCommentApplication.Add(new DTO.OrderComment()
            {
                OrderId = comment.OrderId,
                DeliveryMark = comment.DeliveryMark,
                ServiceMark = comment.ServiceMark,
                PackMark = comment.PackMark,
                UserId = CurrentUser.Id
            }, productNum);
        }

        void AddProductsComment(long orderId, IEnumerable<ProductCommentModel> productComments)
        {
            foreach (var productComment in productComments)
            {
                Entities.ProductCommentInfo model = new Entities.ProductCommentInfo();
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
                    model.ProductCommentImageInfo = productComment.Images.Select(item => new Entities.ProductCommentImageInfo
                    {
                        CommentType = 0,//0代表默认的表示评论的图片
                        CommentImage = MoveImages(item, CurrentUser.Id)
                    }).ToList();
                }
                CommentApplication.AddComment(model);
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
                var de = image.Substring(image.LastIndexOf("/temp/"));
                Core.Log.Error("image:" + image + ",de:" + de + ",filename:" + fileName);
                Core.HimallIO.CopyFile(de, relativeDir + fileName, true);
                return relativeDir + fileName;
            }  //目标地址
            else if (image.Contains("/Storage"))
            {
                return image.Substring(image.LastIndexOf("/Storage"));
            }
            return image;
        }
        /// <summary>
        /// 获取追加评论
        /// </summary>
        /// <param name="orderid"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetAppendComment(long orderId)
        {
            CheckUserLogin();
            var model = CommentApplication.GetProductEvaluationByOrderIdNew(orderId, CurrentUser.Id);

            if (model.Count() > 0 && model.FirstOrDefault().AppendTime.HasValue)
                return Json(ErrorResult<dynamic>("追加评论时，获取数据异常", new int[0]));
            else
            {
                var listResult = model.Select(item => new
                {
                    Id = item.Id,
                    CommentId = item.CommentId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    //ThumbnailsUrl = item.ThumbnailsUrl,
                    ThumbnailsUrl = item.ThumbnailsUrl.Contains("skus") ? Core.HimallIO.GetRomoteImagePath(item.ThumbnailsUrl) : Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_220), //商城App追加评论时获取商品图片
                    BuyTime = item.BuyTime,
                    EvaluationStatus = item.EvaluationStatus,
                    EvaluationContent = item.EvaluationContent,
                    AppendContent = item.AppendContent,
                    AppendTime = item.AppendTime,
                    EvaluationTime = item.EvaluationTime,
                    ReplyTime = item.ReplyTime,
                    ReplyContent = item.ReplyContent,
                    ReplyAppendTime = item.ReplyAppendTime,
                    ReplyAppendContent = item.ReplyAppendContent,
                    EvaluationRank = item.EvaluationRank,
                    OrderId = item.OrderId,
                    CommentImages = item.CommentImages.Select(r => new
                    {
                        CommentImage = r.CommentImage,
                        CommentId = r.CommentId,
                        CommentType = r.CommentType
                    }).ToList(),
                    Color = item.Color,
                    Size = item.Size,
                    Version = item.Version
                }).ToList();
                return JsonResult<dynamic>(listResult);
            }
        }
        /// <summary>
        /// 追加评价
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public JsonResult<Result<int>> PostAppendComment(CommentAppendCommentModel value)
        {
            CheckUserLogin();
            string productCommentsJSON = value.productCommentsJSON;
            //var commentService = ServiceProvider.Instance<CommentService>.Create;
            var productComments = JsonConvert.DeserializeObject<List<AppendCommentModel>>(productCommentsJSON);

            foreach (var m in productComments)
            {
                m.UserId = CurrentUser.Id;
            }
            CommentApplication.Append(productComments);
            return JsonResult<int>();
        }

        /// <summary>
        /// 获取小程序微信模板消息列表
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetTemplateByAppletlist()
        {
            var list = WXMsgTemplateApplication.GetTemplateByAppletlist();
            var result = list.Select(a => new
            {
                a.Id,
                a.TemplateId,
                a.MessageType
            });
            return JsonResult<dynamic>(result);
        }
        /// <summary>
        /// 授权订阅消息
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="templateIds">订阅消息模板Ids</param>
        public JsonResult<Result<int>> GetAuthorizedSubscribeMessage(string orderId, string templateIds)
        {
            WXMsgTemplateApplication.AddAuthorizedSubscribeMessage(orderId, templateIds);
            return JsonResult<int>(msg: "成功订阅");
        }


        /// <summary>
        /// 获取普通商品分享的小程序码
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <param name="pid"></param>
        /// <param name="distributorId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetProductDetailAppletCode(int shopBranchId, int pid, int distributorId)
        {
            var path = string.Format("pages/productdetail/productdetail?id={0}{1}{2}", pid, shopBranchId > 0 ? "&shopBranchId=" + shopBranchId : "", distributorId > 0 ? "&distributorId=" + distributorId : "");
            var fileName = string.Format(@"/Storage/Applet/Codes/shop{0}-pid{1}_disid{2}.jpg", shopBranchId, pid, distributorId);
            if (Core.HimallIO.ExistFile(fileName))
                return JsonResult<dynamic>(data: new { url = Core.HimallIO.GetRomoteImagePath(fileName) });

            //从微信获取二维码
            var stream = GetQRCode(new
            {
                path,
                width = 600
            });
            Core.HimallIO.CreateFile(fileName, stream, Core.FileCreateType.Create);
            return JsonResult<dynamic>(data: new { url = Core.HimallIO.GetRomoteImagePath(fileName) });
        }
        private MemoryStream GetQRCode(object data)
        {
            var setting = SiteSettingApplication.SiteSettings;
            var helper = new Service.Weixin.WXHelper();
            var token = helper.GetAccessToken(setting.WeixinAppletId, setting.WeixinAppletSecret);
            var response = GetResponse("https://api.weixin.qq.com/wxa/getwxacode?access_token=" + token, data);
            if (response.ContentType.Contains("application/json"))
            {
                var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                var json = JObject.Parse(reader.ReadToEnd());
                if (json["errcode"].Value<int>() == 42001 || json["errcode"].Value<int>() == 40001)
                {
                    token = helper.GetAccessToken(setting.WeixinAppletId, setting.WeixinAppletSecret, true);
                    response = GetResponse("https://api.weixin.qq.com/wxa/getwxacode?access_token=" + token, data);
                }
                if (response.ContentType != "image/jpeg")
                {
                    Core.Log.Error($"微信二维码生成失败:data:{JsonConvert.SerializeObject(data)},result:{json}");
                    throw new Core.HimallException("微信二维码生成失败");
                }
            }
            var ms = new MemoryStream();
            var stream = response.GetResponseStream();
            var buffer = new byte[1024];
            int length;
            while ((length = stream.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, length);
            ms.Position = 0;
            return ms;
        }
        private HttpWebResponse GetResponse(string url, object postData)
        {
            var request = WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(postData, Formatting.None));
            request.ContentLength = payload.Length;
            request.GetRequestStream().Write(payload, 0, payload.Length);
            return (HttpWebResponse)request.GetResponse();
        }



        private static byte[] StreamToBytes(Stream stream)
        {

            List<byte> bytes = new List<byte>();
            int temp = stream.ReadByte();
            while (temp != -1)
            {
                bytes.Add((byte)temp);
                temp = stream.ReadByte();
            }
            return bytes.ToArray();

        }

        /// <summary>
        /// 是否显示绑定手机号页面
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<int>> GetIsShowBindCellPhone(long userId)
        {
            var isShow = CommentApplication.IsShowBindCellPhone(userId);
            return JsonResult<int>(data: isShow ? 1 : 0, msg: "");
        }
        /// <summary>
        /// 晒单增加积分
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetShareAddMemberIntegral(long orderId)
        {
            var list = MemberIntegralApplication.GetIntegralRule();
            var share = list.FirstOrDefault(a => a.TypeId == (int)Entities.MemberIntegralInfo.IntegralType.Share);
            var integral = share == null ? 0 : share.Integral;
            if (integral > 0 && CurrentUser != null)
            {
                Entities.MemberIntegralRecordInfo mirInfo = new Entities.MemberIntegralRecordInfo()
                {
                    MemberId = CurrentUser.Id,
                    Integral = integral,
                    RecordDate = DateTime.Now,
                    TypeId = Entities.MemberIntegralInfo.IntegralType.Share,
                    UserName = CurrentUser.UserName
                };

                List<long> orderIds = new List<long>();
                orderIds.Add(orderId);
                if (!MemberIntegralApplication.OrderIsShared(orderIds))
                {
                    var model = new MemberIntegralRecordActionInfo()
                    {
                        VirtualItemId = orderId,
                        VirtualItemTypeId = MemberIntegralInfo.VirtualItemType.ShareOrder,
                    };
                    List<MemberIntegralRecordActionInfo> actionlist = new List<MemberIntegralRecordActionInfo>
                    {
                        model
                    };
                    mirInfo.MemberIntegralRecordActionInfo = actionlist;
                    MemberIntegralApplication.AddMemberIntegralByEnum(mirInfo, Entities.MemberIntegralInfo.IntegralType.Share);
                    return JsonResult<int>(data: 1, msg: "晒单获取积分成功");
                }
                else
                {
                    return JsonResult<int>(data: 1, msg: "当前订单已获取积分成功");
                }
            }
            else
                return JsonResult<int>(data: 0, msg: "未开启晒单得积分规则");
        }
    }
}
