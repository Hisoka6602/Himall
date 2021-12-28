using Himall.CommonModel;
using Himall.CommonModel.Model;
using Himall.Core;
using Himall.Core.Extends;
using Himall.Core.Helper;
using Himall.DTO.Live;
using Himall.DTO.Product;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using Senparc.Weixin.MP.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static Himall.Entities.LiveProductInfo;
using static Himall.Entities.LiveProductLibraryInfo;

namespace Himall.Service
{
    public class WXApiService : ServiceBase
    {
        /// <summary>
        /// 获取Api中文错误信息描述
        /// </summary>
        /// <param name="errcode"></param>
        /// <returns></returns>
        public string GetApiErrorMsg(int errcode, string errmsg)
        {
            Dictionary<int, string> ApiErrDict = new Dictionary<int, string>();
            ApiErrDict.Add(-1, "系统错误");
            ApiErrDict.Add(1, "未创建直播间");
            ApiErrDict.Add(1003, "商品id不存在");
            ApiErrDict.Add(47001, "入参格式不符合规范");
            ApiErrDict.Add(200002, "入参错误");
            ApiErrDict.Add(300001, "禁止创建/更新商品 或 禁止编辑&更新房间");
            ApiErrDict.Add(300002, "名称长度不符合规则");
            ApiErrDict.Add(300003, "价格输入不合规（如：现价比原价大、传入价格非数字等）");
            ApiErrDict.Add(300004, "商品名称存在违规违法内容");
            ApiErrDict.Add(300005, "商品图片存在违规违法内容");
            ApiErrDict.Add(300006, "图片上传失败（如：mediaID过期）");
            ApiErrDict.Add(300007, "线上小程序版本不存在该链接");
            ApiErrDict.Add(300008, "添加商品失败");
            ApiErrDict.Add(300009, "商品审核撤回失败");
            ApiErrDict.Add(300010, "商品审核状态不对（如：商品审核中）");
            ApiErrDict.Add(300011, "操作非法（API不允许操作非API创建的商品）");
            ApiErrDict.Add(300012, "没有提审额度（每天500次提审额度）");
            ApiErrDict.Add(300013, "提审失败");
            ApiErrDict.Add(300014, "审核中，无法删除（非零代表失败）");
            ApiErrDict.Add(300015, "商品不存在");
            ApiErrDict.Add(300017, "商品未提审");
            ApiErrDict.Add(300021, "商品添加成功，审核失败");
            ApiErrDict.Add(300022, "此房间号不存在");
            ApiErrDict.Add(300023, "房间状态 拦截（当前房间状态不允许此操作）");
            ApiErrDict.Add(300024, "商品不存在");
            ApiErrDict.Add(300025, "商品审核未通过");
            ApiErrDict.Add(300026, "房间商品数量已经满额");
            ApiErrDict.Add(300027, "导入商品失败");
            ApiErrDict.Add(300028, "房间名称违规");
            ApiErrDict.Add(300029, "主播昵称违规");
            ApiErrDict.Add(300030, "主播微信号不合法");
            ApiErrDict.Add(300031, "直播间封面图不合规");
            ApiErrDict.Add(300032, "直播间分享图违规");
            ApiErrDict.Add(300033, "添加商品超过直播间上限");
            ApiErrDict.Add(300034, "主播微信昵称长度不符合要求");
            ApiErrDict.Add(300035, "主播微信号不存在");
            ApiErrDict.Add(300036, "主播微信号未实名认证");
            ApiErrDict.Add(400001, "主播微信号不正确");
            if (ApiErrDict.ContainsKey(errcode))
            {
                return ApiErrDict[errcode];
            }
            else
            {
                return errmsg.IsEmptyString() ? "未知原因" : errmsg;
            }
        }
        private static object _locker = new object();

        public void Subscribe(string openId)
        {
            var model = DbFactory.Default.Get<OpenIdInfo>().Where(p => p.OpenId == openId).FirstOrDefault();
            if (model == null)
            {
                model = new OpenIdInfo();
                model.OpenId = openId;
                model.SubscribeTime = DateTime.Now;
                model.IsSubscribe = true;
                DbFactory.Default.Add(model);
            }
            else
            {
                if (!model.IsSubscribe)
                {
                    model.IsSubscribe = true;
                    DbFactory.Default.Update(model);
                }
            }

        }

        public void UnSubscribe(string openId)
        {
            var model = DbFactory.Default.Get<OpenIdInfo>().Where(p => p.OpenId == openId).FirstOrDefault();
            if (model != null)
            {
                model.IsSubscribe = false;
                DbFactory.Default.Update(model);
            }
            else
            {
                model = new OpenIdInfo();
                model.OpenId = openId;
                model.SubscribeTime = DateTime.Now;
                model.IsSubscribe = false;
                DbFactory.Default.Add(model);
            }
        }

        public string GetTicket(string appid, string appsecret)
        {
            var model = DbFactory.Default.Get<WeiXinBasicInfo>().FirstOrDefault();
            if (model != null && model.TicketOutTime > DateTime.Now && !string.IsNullOrEmpty(model.Ticket))
                return model.Ticket;

            lock (_locker)
            {
                model = DbFactory.Default.Get<WeiXinBasicInfo>().FirstOrDefault();
                if (model != null && model.TicketOutTime > DateTime.Now && !string.IsNullOrEmpty(model.Ticket))
                    return model.Ticket;

                if (model == null)
                {
                    model = new WeiXinBasicInfo();
                }

                var ticketRequest = new Senparc.Weixin.MP.Entities.JsApiTicketResult();
                ticketRequest.errcode = Senparc.Weixin.ReturnCode.系统繁忙此时请开发者稍候再试;
                try
                {
                    var accessToken = this.TryGetToken(appid, appsecret);
                    model.AccessToken = accessToken;
                    ticketRequest = CommonApi.GetTicketByAccessToken(accessToken);
                }
                catch (Exception e)
                {
                    Log.Error("请求Ticket出错，强制刷新acess_token", e);
                    var accessToken = this.TryGetToken(appid, appsecret, true);
                    model.AccessToken = accessToken;
                    ticketRequest = CommonApi.GetTicketByAccessToken(accessToken);
                }
                if (ticketRequest.errcode == Senparc.Weixin.ReturnCode.请求成功 && !string.IsNullOrEmpty(ticketRequest.ticket))
                {
                    if (ticketRequest.expires_in > 3600)
                    {
                        ticketRequest.expires_in = 3600;
                    }
                    model.AppId = appid;
                    model.TicketOutTime = DateTime.Now.AddSeconds(ticketRequest.expires_in);
                    model.Ticket = ticketRequest.ticket;

                    DbFactory.Default.Save(model);

                    return model.Ticket;
                }
                else
                {
                    throw new HimallException("请求微信接口出错");
                }
            }
        }

        /// <summary>
        /// 获取AccessToken
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <param name="getNewToken">是否刷新缓存</param>
        /// <returns></returns>
        public string TryGetToken(string appId, string appSecret, bool getNewToken = false)
        {
            if (!AccessTokenContainer.CheckRegistered(appId))
            {
                lock (_locker)
                {
                    if (!AccessTokenContainer.CheckRegistered(appId))
                    {
                        var _task = AccessTokenContainer.RegisterAsync(appId, appSecret);
                        Task.WaitAll(new Task[] { _task }, 1000);
                    }
                }
            }
            try
            {
                if (getNewToken)
                {
                    Log.Error("获取微信Token:" + GetStackTraceModelName() + ",原token");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return AccessTokenContainer.GetAccessToken(appId, getNewToken);
        }
        static string GetStackTraceModelName()
        {
            //当前堆栈信息
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame[] sfs = st.GetFrames();
            //过虑的方法名称,以下方法将不会出现在返回的方法调用列表中
            string _filterdName = "ResponseWrite,ResponseWriteError,";
            string _fullName = string.Empty, _methodName = string.Empty;
            for (int i = 1; i < sfs.Length; ++i)
            {
                //非用户代码,系统方法及后面的都是系统调用，不获取用户代码调用结束
                if (System.Diagnostics.StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset()) break;
                _methodName = sfs[i].GetMethod().Name;//方法名称
                                                      //sfs[i].GetFileLineNumber();//没有PDB文件的情况下将始终返回0
                if (_filterdName.Contains(_methodName)) continue;
                _fullName = _methodName + "()->" + _fullName;
            }
            st = null;
            sfs = null;
            _filterdName = _methodName = null;
            return _fullName.TrimEnd('-', '>');
        }
        /// <summary>
        /// 发送模板消息
        /// </summary>
        /// <param name="accessTokenOrAppId"></param>
        /// <param name="openId"></param>
        /// <param name="templateId"></param>
        /// <param name="topcolor"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void SendMessageByTemplate(string appid, string appsecret, string openId, string templateId, string topcolor, string url, object data)
        {
            if (!string.IsNullOrWhiteSpace(templateId))
            {
                var accessToken = this.TryGetToken(appid, appsecret);
                TemplateApi.SendTemplateMessage(accessToken, openId, templateId, url, data);
            }
        }

        /// <summary>
        /// 获取微信分享参数
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public WeiXinShareArgs GetWeiXinShareArgs(string appid, string appsecret, string url)
        {
            string ticket = "";
            try
            {
                ticket = GetTicket(appid, appsecret);
            }
            catch { }
            string timestamp = JSSDKHelper.GetTimestamp();
            string nonceStr = JSSDKHelper.GetNoncestr();
            string signature = JSSDKHelper.GetSignature(ticket, nonceStr, timestamp, url);
            return new WeiXinShareArgs(appid, timestamp, nonceStr, signature, ticket);
        }


        /// <summary>
        /// 获取直播间信息
        /// </summary>
        public List<LiveRoom> GetLive(string appid, string appsecret, int startIndex = 0, int size = 100)
        {

            var data = new
            {
                start = startIndex,
                limit = size,
            };

            var url = $"https://api.weixin.qq.com/wxa/business/getliveinfo";
            var content = PostJson(url, JsonConvert.SerializeObject(data), appid, appsecret);

            var json = JObject.Parse(content);
            if (json["errcode"].Value<int>() != 0)
                throw new HimallException(content);

            var result = new List<LiveRoom>();
            var rooms = json["room_info"];
            foreach (var item in rooms)
            {
                var room = new LiveRoom
                {
                    Name = item["name"].Value<string>(),
                    RoomId = item["roomid"].Value<long>(),
                    ShopId = 1,
                    CoverImg = item["cover_img"].Value<string>(),
                    Status = (LiveRoomStatus)item["live_status"].Value<int>(),
                    StartTime = new DateTime(1970, 1, 1, 8, 0, 0).AddSeconds(item["start_time"].Value<long>()),
                    EndTime = new DateTime(1970, 1, 1, 8, 0, 0).AddSeconds(item["end_time"].Value<long>()),
                    AnchorName = item["anchor_name"].Value<string>(),
                    AnchorImg = item["share_img"] != null ? item["share_img"].Value<string>() : string.Empty,
                    Products = new List<LiveProduct>(),
                };
                var products = item["goods"];
                foreach (var i in products)
                {
                    var product = new LiveProduct
                    {
                        Name = i["name"].Value<string>(),
                        Url = i["url"].Value<string>(),//pages/productdetail/productdetail?id=952
                        Price = i["price"].Value<int>() / 100M,
                        Image = i["cover_img"].Value<string>(),
                    };
                    product.ProductId = long.Parse(Regex.Match(product.Url, @"\d+").Value);
                    room.Products.Add(product);
                }
                result.Add(room);
            }
            return result;
        }

        /// <summary>
        /// 获取直播回放
        /// </summary>
        public List<LiveReply> GetLiveReplay(string appid, string appsecret, long roomId, int startIndex = 0, int size = 100)
        {
            var data = new
            {
                action = "get_replay",
                room_id = roomId,
                start = startIndex,
                limit = size
            };

            var url = "https://api.weixin.qq.com/wxa/business/getliveinfo";
            var content = PostJson(url, JsonConvert.SerializeObject(data), appid, appsecret);
            //Log.Info($"回放数据:{content}");
            var json = JObject.Parse(content);
            if (json["errcode"].Value<int>() != 0)
                throw new HimallException(json["errmsg"].Value<string>());
            var result = new List<LiveReply>();
            foreach (var item in json["live_replay"])
            {
                result.Add(new LiveReply
                {
                    MediaUrl = item["media_url"].Value<string>(),
                    CreateTime = item["create_time"].Value<DateTime>(),
                    ExpireTime = item["expire_time"].Value<DateTime>(),
                });
            }
            return result;
        }

        /// <summary>
        /// 保存直播间信息
        /// </summary>
        /// <param name="liveRoom"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public long SaveLiveRoom(string appid, string appsecret, LiveRoomInfo liveRoom, out string msg)
        {
            msg = "";
            if (string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(appsecret))
            {
                msg = "请配置小程序appId和appSecrect";
                return -1;
            }
            string apiUrl = $"https://api.weixin.qq.com/wxaapi/broadcast/room/create";
            AppletLiveRoomInfo appletLive = CopyAppletLiveRoomInfo(liveRoom);
            var result = PostJson(apiUrl, JsonConvert.SerializeObject(appletLive), appid, appsecret);
            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add("appId", appid);
            param.Add("appSecrect", appsecret);

            param.Add("PostUrl", apiUrl);
            param.Add("Content", result);
            var json = JObject.Parse(result);
            int errcode = json["errcode"].Value<int>();
            if (errcode != 0)
            {
                if (errcode == 300006)//图片上传失败（如：mediaID过期），重新上传图片
                {
                    bool reupload = false;
                    AppletUploadResult uploadResult = AppletMeidaUpload(appid, appsecret, liveRoom.ShareImg, MediaType.image, out msg);
                    if (uploadResult != null)
                    {
                        liveRoom.ShareImgMediaId = uploadResult.media_id;
                        reupload = true;
                    }
                    uploadResult = AppletMeidaUpload(appid, appsecret, liveRoom.CoverImg, MediaType.image, out msg);
                    if (uploadResult != null)
                    {
                        liveRoom.CoverImgMediaId = uploadResult.media_id;
                        reupload = true;
                    }
                    if (reupload)
                    {
                        SaveLiveRoom(appid, appsecret, liveRoom, out msg);
                    }
                }
                msg = "直播间：" + liveRoom.Name + "," + GetApiErrorMsg(errcode, json["errmsg"].Value<string>());
                Log.Info(param);
                if (errcode == 300033)
                {
                    return -1;
                }
                return 0;
            }
            liveRoom.Status = LiveRoomStatus.Audting;
            liveRoom.RoomId = json["roomId"].Value<int>();
            DbFactory.Default.Update(liveRoom);
            return liveRoom.RoomId;

        }

        /// <summary>
        /// 复制商城房间信息到小程序房间信息
        /// </summary>
        /// <param name="liveRoomInfo"></param>
        /// <returns></returns>
        public AppletLiveRoomInfo CopyAppletLiveRoomInfo(LiveRoomInfo liveRoomInfo)
        {
            AppletLiveRoomInfo appletLive = new AppletLiveRoomInfo
            {
                anchorName = liveRoomInfo.AnchorName,
                anchorWechat = liveRoomInfo.AnchorWechat,
                closeComment = liveRoomInfo.CloseComment,
                closeGoods = liveRoomInfo.CloseGoods,
                closeLike = liveRoomInfo.CloseLike,
                coverImg = liveRoomInfo.CoverImgMediaId,
                endTime = liveRoomInfo.EndTime.Value.AddHours(-8).DateTimeToUnixTimestamp(),
                name = liveRoomInfo.Name,
                screenType = liveRoomInfo.ScreenType,
                shareImg = liveRoomInfo.ShareImgMediaId,
                feedsImg = liveRoomInfo.ShareImgMediaId,
                startTime = liveRoomInfo.StartTime.AddHours(-8).DateTimeToUnixTimestamp(),
                type = liveRoomInfo.Type,
            };

            return appletLive;
        }

        /// <summary>
        /// 小程序上传文件（后端上传）
        /// </summary>
        /// <param name="fileUrl">文件路径（已在服务器中的图片）</param>
        /// <param name="mediaType">文件类型（默认为图片）</param>
        /// <param name="msg">输出错误信息</param>
        /// <param name="appId">appId</param>
        /// <param name="appSecrect">appSecrect</param>
        /// <returns></returns>
        public AppletUploadResult AppletMeidaUpload(string appId, string appSecrect, string fileUrl, MediaType mediaType, out string msg)
        {
            msg = "";
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecrect))
            {
                msg = "请配置小程序appId和appSecrect";
                return null;
            }

            string meidaTypeStr = mediaType.ToString();
            string apiUrl = $"https://api.weixin.qq.com/cgi-bin/media/upload?type={meidaTypeStr}";
            string result = HttpFilePost(apiUrl, fileUrl, appId, appSecrect);
            if (result.Contains("errcode"))//上传错误
            {

                var json = JObject.Parse(result);
                msg = json["errmsg"].Value<string>();
                return null;
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AppletUploadResult>(result);
            }
        }
        /// <summary>
        /// 导入商品到直播间
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool ImportProductToLiveRoom(string appId, string appSecrect, List<LiveProductLibaryModel> productInfos, long roomId, out string msg)
        {
            msg = "";
            productInfos = productInfos.Where(p => p.GoodsId > 0 && p.LiveAuditStatus == LiveProductAuditStatus.Audited).ToList();
            if (productInfos == null || productInfos.Count <= 0)
            {
                msg = "要导入的数据为空";
                return false;
            }
            List<long> goodsIds = productInfos.Select(p => p.GoodsId).ToList();
            AppletLiveProductImportInfo importInfo = new AppletLiveProductImportInfo();
            importInfo.ids = goodsIds;
            importInfo.roomId = roomId;
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecrect))
            {
                msg = "请配置小程序appId和appSecrect";
                return false; ;
            }
            string apiUrl = $"https://api.weixin.qq.com/wxaapi/broadcast/room/addgoods";
            string postdata = Newtonsoft.Json.JsonConvert.SerializeObject(importInfo);
            string result = PostJson(apiUrl, postdata, appId, appSecrect);
            var json = JObject.Parse(result);
            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add("apiUrl", apiUrl);
            param.Add("postdata", postdata);
            param.Add("result", result);
            if (json["errcode"].Value<int>() > 0)
            {

                msg = GetApiErrorMsg(json["errcode"].Value<int>(), json["errmsg"].Value<string>());
                Log.Info(param);
                return false;
            }

            else
            {
                foreach (LiveProductLibaryModel liveProduct in productInfos)
                {
                    LiveProductInfo productInfo = new LiveProductInfo();
                    productInfo.Image = liveProduct.Image;
                    productInfo.Name = liveProduct.Name;
                    productInfo.Price = liveProduct.Price;
                    productInfo.Price2 = liveProduct.Price2;
                    productInfo.PriceType = liveProduct.PriceType.GetHashCode();
                    productInfo.ProductId = liveProduct.ProductId;
                    productInfo.RoomId = roomId;
                    productInfo.SaleAmount = 0;
                    productInfo.SaleCount = 0;
                    productInfo.Url = "pages/productdetail/productdetail ? id = " + liveProduct.ProductId;
                    DbFactory.Default.Add<LiveProductInfo>(productInfo);
                }
            }
            return true;
        }


        /// <summary>
        /// 商品添加并提审到直播商品库
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool AddProductToLiveProductLibary(string appId, string appSecrect, List<LiveProductLibaryModel> productInfos, out string msg)
        {
            List<string> msglist = new List<string>();
            int successCount = 0;
            msg = "";
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecrect))
            {
                msg = "请配置小程序appId和appSecrect";
                return false; ;
            }
            List<long> successProductIds = new List<long>();
            foreach (LiveProductLibaryModel productInfo in productInfos)
            {
                if (productInfo.Name.Length > 14)
                {
                    productInfo.Name = productInfo.Name.Substring(0, 14);
                }
                productInfo.ApplyLiveTime = DateTime.Now;
                AppletApiProductInfo appletProduct = new AppletApiProductInfo();
                productInfo.LiveAuditStatus = LiveProductAuditStatus.NoAudit;
                GoodsInfo goods = new GoodsInfo();
                //已存在图片，并且没有过期，则不需要上传
                if (!productInfo.ImageMediaId.IsEmptyString() && (DateTime.Now - productInfo.ApplyLiveTime).TotalDays < 3)
                {
                    goods.coverImgUrl = productInfo.ImageMediaId;
                }
                else
                {
                    string productImage = productInfo.Image.ToNullString();
                    //if (productImage.IndexOf("?") > -1)
                    //{
                    //    productImage = productImage.Substring(0, productImage.IndexOf("?"));
                    //}
                    //如果是远程服务器的图片，则先下载下来，然后再上传
                    if (!productImage.IsEmptyString() && (productImage.StartsWith("http://") || productImage.StartsWith("https://")))
                    {
                        string tempProductImage = IOHelper.GetPhysicalPath("/Storage/master/temp/" + productInfo.Id + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
                        if (WebHelper.DonwLoadFile(productImage, tempProductImage))
                        {
                            productImage = tempProductImage;
                        }
                    }
                    else if (productImage.IsEmptyString())
                    {
                        productImage = IOHelper.GetPhysicalPath("/utility/pics/none.gif");
                    }
                    else
                    {
                        productImage = IOHelper.GetPhysicalPath(productImage);
                    }
                    AppletUploadResult uploadResult = AppletMeidaUpload(appId, appSecrect, productImage, MediaType.image, out msg);
                    if (uploadResult != null)
                    {
                        goods.coverImgUrl = uploadResult.media_id;
                        productInfo.ImageMediaId = goods.coverImgUrl;
                    }
                    else
                    {
                        msglist.Add("商品图片上传失败:" + msg);
                        continue;
                    }
                }
                goods.name = productInfo.Name;
                goods.price = productInfo.Price;
                goods.price2 = productInfo.Price2;
                goods.priceType = productInfo.PriceType.GetHashCode();
                goods.url = "pages/productdetail/productdetail?id=" + productInfo.ProductId;
                appletProduct.goodsInfo = goods;

                string apiUrl = $"https://api.weixin.qq.com/wxaapi/broadcast/goods/add";
                string postdata = Newtonsoft.Json.JsonConvert.SerializeObject(appletProduct);
                string result = PostJson(apiUrl, postdata, appId, appSecrect);
                var json = JObject.Parse(result);
                IDictionary<string, string> param = new Dictionary<string, string>();
                param.Add("apiUrl", apiUrl);
                param.Add("postdata", postdata);
                param.Add("result", result);
                if (json["errcode"].Value<int>() == 0)
                {
                    successCount += 1;
                    successProductIds.Add(productInfo.ProductId);
                    productInfo.GoodsId = json["goodsId"].Value<long>();
                    productInfo.AuditId = json["auditId"].Value<long>();
                    productInfo.ApplyLiveTime = DateTime.Now;
                    productInfo.LiveAuditStatus = LiveProductAuditStatus.NoAudit;
                    //DbFactory.Default.Delete(productInfo);
                    DbFactory.Default.Update(productInfo);

                }
                else
                {

                    msg = GetApiErrorMsg(json["errcode"].Value<int>(), json["errmsg"].Value<string>());
                    Log.Info(param);
                    msglist.Add(msg);
                }
            }
            msg = string.Join(",", msglist);
            if (successCount == 0)
            {

                return false;
            }
            else
            {
                return true;
            }
        }


        /// <summary>
        /// 撤回审核
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool ReCallAudit(string appId, string appSecrect, List<LiveProductLibraryInfo> productInfos, out string msg)
        {
            List<string> msglist = new List<string>();
            int successCount = 0;
            msg = "";
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecrect))
            {
                msg = "请配置小程序appId和appSecrect";
                return false; ;
            }
            if (productInfos.Count == 0)
            {
                msg = "没有可撤回的商品";
                return false; ;
            }
            List<long> successProductIds = new List<long>();
            foreach (LiveProductLibraryInfo productInfo in productInfos)
            {
                if (productInfo.AuditId == 0 || productInfo.GoodsId <= 0)
                {
                    msglist.Add("非提交至小程序的商品,不能撤销审核");
                    continue;
                }
                string apiUrl = $"https://api.weixin.qq.com/wxaapi/broadcast/goods/resetaudit";
                string postdata = Newtonsoft.Json.JsonConvert.SerializeObject(new { auditId = productInfo.AuditId, goodsId = productInfo.GoodsId });
                string result = PostJson(apiUrl, postdata, appId, appSecrect);
                IDictionary<string, string> param = new Dictionary<string, string>();
                param.Add("apiUrl", apiUrl);
                param.Add("postdata", postdata);
                param.Add("result", result);
                var json = JObject.Parse(result);
                if (json["errcode"].Value<int>() == 0)
                {
                    successCount += 1;
                    successProductIds.Add(productInfo.ProductId);
                    productInfo.LiveAuditStatus = LiveProductAuditStatus.NoAudit;
                    productInfo.LiveAuditMsg = "撤回审核";
                    DbFactory.Default.Update(productInfo);
                }
                else
                {
                    int errcode = json["errcode"].Value<int>();
                    //商品不存在，从数据库中删除
                    if (errcode == 300024 || errcode == 300015)
                    {
                        DbFactory.Default.Delete(productInfo);
                    }
                    Log.Info(param);
                    msg = GetApiErrorMsg(json["errcode"].Value<int>(), json["errmsg"].Value<string>());
                    msglist.Add(msg);

                }
            }
            msg = string.Join(",", msglist);
            if (successCount == 0)
            {
                if (msg.IsEmptyString())
                {
                    msg = "没有可撤回的商品";
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// 重新审核
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool ReApplyAudit(string appId, string appSecrect, List<LiveProductLibraryInfo> productInfos, out string msg)
        {
            List<string> msglist = new List<string>();
            int successCount = 0;
            msg = "";
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecrect))
            {
                msg = "请配置小程序appId和appSecrect";
                return false; ;
            }

            List<long> successProductIds = new List<long>();
            foreach (LiveProductLibraryInfo productInfo in productInfos)
            {
                msg = "";
                //只有审核失败的商品才可以重新提交审核
                if (productInfo.LiveAuditMsg != "撤回审核" && (productInfo.AuditId.IsEmptyString() || productInfo.GoodsId <= 0 || productInfo.LiveAuditStatus != LiveProductAuditStatus.NoAudit))
                {
                    continue;
                }

                string apiUrl = $"https://api.weixin.qq.com/wxaapi/broadcast/goods/audit";
                string postdata = Newtonsoft.Json.JsonConvert.SerializeObject(new { goodsId = productInfo.GoodsId });
                string result = PostJson(apiUrl, postdata, appId, appSecrect);
                IDictionary<string, string> param = new Dictionary<string, string>();
                param.Add("apiUrl", apiUrl);
                param.Add("postdata", postdata);
                param.Add("result", result);
                var json = JObject.Parse(result);

                if (json["errcode"].Value<int>() == 0)
                {
                    successCount += 1;
                    successProductIds.Add(productInfo.ProductId);
                    productInfo.LiveAuditStatus = LiveProductAuditStatus.NoAudit;
                    productInfo.LiveAuditMsg = "";
                    DbFactory.Default.Update(productInfo);
                }
                else
                {
                    int errcode = json["errcode"].Value<int>();
                    //商品不存在，从数据库中删除
                    if (errcode == 300024 || errcode == 300015)
                    {
                        DbFactory.Default.Delete(productInfo);
                    }
                    Log.Info(param);
                    msg = GetApiErrorMsg(json["errcode"].Value<int>(), json["errmsg"].Value<string>());
                    msglist.Add(msg);
                }
            }
            msg = string.Join(",", msglist);
            if (successCount == 0)
            {

                return false;
            }
            else
            {
                return true;
            }

        }
        /// <summary>
        /// 移除商品（从商品库中）
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool RemoveProduct(List<LiveProductLibraryInfo> productInfos)
        {

            List<string> msglist = new List<string>();
            int successCount = 0;
            List<int> successProductIds = new List<int>();
            foreach (LiveProductLibraryInfo productInfo in productInfos)
            {
                if (DbFactory.Default.Delete<LiveProductLibraryInfo>(productInfo.ProductId) > 0)
                {
                    successCount += 1;
                }
            }
            if (successCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 删除商品
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool DeleteProduct(string appId, string appSecrect, List<LiveProductLibraryInfo> productInfos, out string msg)
        {
            msg = "";
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecrect))
            {
                msg = "请配置小程序appId和appSecrect";
                return false; ;
            }
            List<string> msglist = new List<string>();
            int successCount = 0;
            List<long> successProductIds = new List<long>();
            foreach (LiveProductLibraryInfo productInfo in productInfos)
            {
                //直播商品ID必须大于0才可以删除,审核中无法删除
                if (productInfo.GoodsId <= 0 || productInfo.LiveAuditStatus == LiveProductAuditStatus.Auditing)
                {
                    continue;
                }

                string apiUrl = $"https://api.weixin.qq.com/wxaapi/broadcast/goods/delete";
                string postdata = Newtonsoft.Json.JsonConvert.SerializeObject(new { goodsId = productInfo.GoodsId });
                string result = PostJson(apiUrl, postdata, appId, appSecrect);
                IDictionary<string, string> param = new Dictionary<string, string>();
                param.Add("apiUrl", apiUrl);
                param.Add("postdata", postdata);
                param.Add("result", result);
                var json = JObject.Parse(result);
                //Log.Info(result);
                if (json["errcode"].Value<int>() == 0)
                {
                    successCount += 1;
                    successProductIds.Add(productInfo.ProductId);
                    DbFactory.Default.Delete(productInfo);
                    DbFactory.Default.Del<LiveProductInfo>(p => p.ProductId == productInfo.ProductId);
                }
                else
                {
                    int errcode = json["errcode"].Value<int>();
                    //商品不存在，从数据库中删除
                    if (errcode == 300024 || errcode == 300015)
                    {
                        DbFactory.Default.Delete(productInfo);
                        DbFactory.Default.Del<LiveProductInfo>(p => p.ProductId == productInfo.ProductId);
                    }
                    Log.Info(param);
                    msglist.Add(GetApiErrorMsg(json["errcode"].Value<int>(), json["errmsg"].Value<string>()));
                }
            }
            msg = string.Join(",", msglist);
            if (successCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 更新商品信息
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool UpdateAppletLiveProduct(string appId, string appSecrect, LiveProductLibaryModel product, out string msg)
        {
            msg = "";
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecrect))
            {
                msg = "请配置小程序appId和appSecrect";
                return false; ;
            }
            AppletApiUpdateProductInfo appletProduct = new AppletApiUpdateProductInfo();
            //审核通过的商品仅允许更新价格类型与价格，审核中的商品不允许更新，未审核的商品允许更新所有字段， 只传入需要更新的字段。
            if ((product.LiveAuditStatus != LiveProductAuditStatus.NoAudit && product.LiveAuditStatus != LiveProductAuditStatus.Audited) || product.GoodsId <= 0)
            {
                msg = "直播商品状态错误";
                return false;
            }
            UpdateGoodsInfo goods = new UpdateGoodsInfo();
            //已存在图片，并且没有过期，则不需要上传
            if (product.LiveAuditStatus == LiveProductAuditStatus.NoAudit)
            {
                if (!product.ImageMediaId.IsEmptyString() && (DateTime.Now - product.ApplyLiveTime).TotalDays < 3)
                {
                    goods.coverImgUrl = product.ImageMediaId;
                }
                else
                {
                    AppletUploadResult uploadResult = AppletMeidaUpload(appId, appSecrect, IOHelper.GetPhysicalPath(product.Image), MediaType.image, out msg);
                    if (uploadResult != null)
                    {
                        goods.coverImgUrl = uploadResult.media_id;
                        product.ImageMediaId = goods.coverImgUrl;
                    }
                    else
                    {
                        msg = "图片上传失败";
                        return false;
                    }
                }
                goods.url = "/pages/productdetail/productdetail?id=" + product.ProductId;
                goods.name = product.Name.Length > 14 ? product.Name.Substring(0, 14) : product.Name;

            }

            goods.price = product.Price;
            goods.price2 = product.Price2;
            goods.priceType = product.PriceType.GetHashCode();

            goods.goodsId = product.GoodsId;
            appletProduct.goodsInfo = goods;
            string apiUrl = $"https://api.weixin.qq.com/wxaapi/broadcast/goods/update";
            var jSetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string postdata = Newtonsoft.Json.JsonConvert.SerializeObject(appletProduct, Formatting.Indented, jSetting);
            string result = PostJson(apiUrl, postdata, appId, appSecrect);
            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add("apiUrl", apiUrl);
            param.Add("postdata", postdata);
            param.Add("result", result);
            var json = JObject.Parse(result);

            if (json["errcode"].Value<int>() == 0)
            {
                DbFactory.Default.Update(product);
            }
            else
            {
                Log.Info(param);
                msg = GetApiErrorMsg(json["errcode"].Value<int>(), json["errmsg"].Value<string>());
            }
            return true;
        }
        /// <summary>
        /// 更新商品状态
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool UpdateLiveProductStatus(string appId, string appSecrect, List<LiveProductLibraryInfo> list, out string msg)
        {
            msg = "";
            if (list == null && list.Count == 0)
            {
                msg = "没有商品数据";
                return false;
            }

            string apiUrl = $"https://api.weixin.qq.com/wxa/business/getgoodswarehouse";
            string postdata = Newtonsoft.Json.JsonConvert.SerializeObject(new { goods_ids = list.Select(p => p.GoodsId).ToArray() });
            string result = PostJson(apiUrl, postdata, appId, appSecrect);
            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add("apiUrl", apiUrl);
            param.Add("postdata", postdata);
            param.Add("result", result);

            var json = JObject.Parse(result);

            if (json["errcode"].Value<int>() == 0)
            {
                Log.Info(param);
                var products = json["goods"];

                foreach (var i in products)
                {
                    long goodsId = i["goods_id"].Value<long>();
                    LiveProductLibraryInfo item = list.Where(p => p.GoodsId == goodsId).FirstOrDefault();
                    if (item != null)
                    {
                        item.GoodsId = goodsId;
                        item.LiveAuditStatus = (LiveProductAuditStatus)i["audit_status"].Value<int>();
                        DbFactory.Default.Update(item, item.Id, new List<string>() { "LiveAuditStatus" });
                    }
                }
            }
            else
            {
                Log.Info(param);
                msg = GetApiErrorMsg(json["errcode"].Value<int>(), json["errmsg"].Value<string>());
                return false;
            }
            return true;
        }
        /// <summary>
        /// 从小程序获取商品列表
        /// </summary>
        /// <returns></returns>
        public AppletApiLiveProductList GetLivePrdouctsFromApplet(string appId, string appSecrect, int start = 0, int limit = 100, int status = 0)
        {
            AppletApiLiveProductList list = new AppletApiLiveProductList();
            var result = new List<LiveRoomInfo>();

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecrect))
            {
                return null;
            }
            var postdata = Newtonsoft.Json.JsonConvert.SerializeObject(new { offset = start, limit = limit, status = status });
            var url = "https://api.weixin.qq.com/wxaapi/broadcast/goods/getapproved";
            var content = PostJson(url, postdata, appId, appSecrect);

            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add("appId", appId);
            param.Add("appSecrect", appSecrect);
            param.Add("PostData", postdata);
            param.Add("PostUrl", url);
            param.Add("Content", content);
            try
            {

                var json = JObject.Parse(content);
                if (json["errcode"].Value<int>() != 0)
                {
                    Log.Info(param);
                    return list;
                }
                else
                {
                    list = Newtonsoft.Json.JsonConvert.DeserializeObject<AppletApiLiveProductList>(content);
                }
                BatchUpdateLiveProductStatus(list.goods, status);
                return list;
            }
            catch (Exception ex)
            {
                Log.Error(param, ex);
            }
            return list;
        }
        /// <summary>
        ///批量同步商品状态
        /// </summary>
        /// <param name="goods"></param>
        /// <param name="status"></param>
        public void BatchUpdateLiveProductStatus(List<ListGoodsInfo> goods, int status)
        {
            List<LiveProductLibraryInfo> list = new List<LiveProductLibraryInfo>();
            if (goods != null && goods.Count > 0)
            {
                foreach (ListGoodsInfo goodsInfo in goods)
                {
                    LiveProductLibraryInfo productInfo = new LiveProductLibraryInfo();
                    productInfo.GoodsId = goodsInfo.goodsId;
                    productInfo.ProductId = Regex.Match(goodsInfo.url, @"\d+").Value.ToInt();
                    productInfo.LiveAuditStatus = (LiveProductAuditStatus)status;
                    DbFactory.Default.Update(productInfo, productInfo.ProductId, new List<string>() { "LiveAuditStatus" });
                }
            }

        }

        /// <summary>
        /// 请求数据
        /// </summary>
        public bool AddAnchorRole(string appid, string appsecret, string weChat, out string msg, out bool isRealNameVerify)
        {
            isRealNameVerify = true;
            bool tag = true;
            string reuslt = "";
            // [1-管理员，2-主播，3-运营者] 这里只需要主播权限
            var postdata = JsonConvert.SerializeObject(new { username = weChat, role = 2 });
            var url = "https://api.weixin.qq.com/wxaapi/broadcast/role/addrole";
            var content = PostJson(url, postdata, appid, appsecret);
            IDictionary<string, string> param = new Dictionary<string, string>();
            param.Add("appId", appid);
            param.Add("appSecrect", appsecret);
            param.Add("PostData", postdata);
            param.Add("PostUrl", url);
            param.Add("Content", content);
            int errcode = 0;
            try
            {
                var json = JObject.Parse(content);
                if (json["errcode"].Value<int>() > 0)
                {
                    tag = false;
                    Log.Info(param);
                    errcode = json["errcode"].Value<int>();
                    if (errcode == 400002)
                    {//未实名认证，返回二维码图片
                        reuslt = json["codeurl"].Value<string>();
                        isRealNameVerify = false;
                    }
                    else
                    {
                        reuslt = GetApiErrorMsg(errcode, json["errmsg"].Value<string>());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(param, ex);
            }

            msg = reuslt;
            return tag;
        }
        /// <summary>
        /// PostJson提交
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private string PostJson(string url, string data, string appId, string appSecret)
        {
            string result = "";
            try
            {
                bool isInvalid = false;
                var token = this.TryGetToken(appId, appSecret);
                string pretoken = token;

                var postUrl = url + $"?access_token={token}";
                result = HttpHelper.HttpPost(postUrl, data, System.Text.Encoding.UTF8, true, null, false);
                if (result.Contains("access_token is invalid"))
                {
                    isInvalid = true;
                    token = this.TryGetToken(appId, appSecret, true);
                    postUrl = url + $"?access_token={token}";
                    result = HttpHelper.HttpPost(postUrl, data, System.Text.Encoding.UTF8, true, null, false);
                }
                // Log.Info($"token获取，pretoken:{pretoken},newtoken:{token},result:{result},IsInvalid:{isInvalid}");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return result;
        }

        /// <summary>
        /// 文件上传PostJson提交
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private string HttpFilePost(string apiUrl, string fileUrl, string appId, string appSecret)
        {
            var token = this.TryGetToken(appId, appSecret, true);
            var postUrl = apiUrl + $"?access_token={token}";
            if (apiUrl.IndexOf("?") > -1)
            {
                postUrl = apiUrl + $"&access_token={token}";
            }

            return WebHelper.HttpUploadFile(postUrl, new string[] { fileUrl });
        }
    }
}
