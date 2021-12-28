using AutoMapper;
using Himall.CommonModel;
using Himall.CommonModel.WeiXin;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Core.Plugins.Message;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Himall.Application
{
    public class WXMsgTemplateApplication
    {

        private static WXMsgTemplateService _WXMsgTemplateService = ObjectContainer.Current.Resolve<WXMsgTemplateService>();

        /// <summary>
        /// 新增图文
        /// </summary>
        /// <param name="info"></param>
        /// 
        public static Entities.WXUploadNewsResult Add(IEnumerable<Entities.WXMaterialInfo> info, string appid, string appsecret)
        {
            return _WXMsgTemplateService.Add(info, appid, appsecret);
        }
        /// <summary>
        /// 更新单条图文消息
        /// </summary>
        /// <param name="mediaid"></param>
        /// <param name="news"></param>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static IEnumerable<Entities.WxJsonResult> UpdateMedia(string mediaid, IEnumerable<Entities.WXMaterialInfo> news, string appid, string appsecret)
        {
            return _WXMsgTemplateService.UpdateMedia(mediaid, news, appid, appsecret);
        }
        /// <summary>
        /// 删除素材
        /// </summary>
        /// <param name="mediaid"></param>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static Entities.WxJsonResult DeleteMedia(string mediaid, string appid, string appsecret)
        {
            return _WXMsgTemplateService.DeleteMedia(mediaid, appid, appsecret);
        }
        /// <summary>
        /// 添加图片
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns>media_id</returns>
        public static string AddImage(string filename, string appid, string appsecret)
        {
            return _WXMsgTemplateService.AddImage(filename, appid, appsecret);
        }
        /// <summary>
        /// 获取图文素材列表
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Entities.MediaNewsItemList GetMediaMsgTemplateList(string appid, string appsecret, int offset, int count)
        {
            return _WXMsgTemplateService.GetMediaMsgTemplateList(appid, appsecret, offset, count);
        }
        /// <summary>
        /// 取素材总数
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static Entities.MediaItemCount GetMediaItemCount(string appid, string appsecret)
        {
            return _WXMsgTemplateService.GetMediaItemCount(appid, appsecret);
        }
        /// <summary>
        /// 群发送消息
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static Entities.SendInfoResult SendWXMsg(Entities.SendMsgInfo info)
        {
            return _WXMsgTemplateService.SendWXMsg(info);
        }
        /// <summary>
        /// 取图文素材
        /// </summary>
        /// <param name="mediaid"></param>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static IEnumerable<Entities.WXMaterialInfo> GetMedia(string mediaid, string appid, string appsecret)
        {
            return _WXMsgTemplateService.GetMedia(mediaid, appid, appsecret);
        }
        /// <summary>
        /// 取非图文素材
        /// </summary>
        /// <param name="mediaid"></param>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <param name="stream"></param>
        public static void GetMedia(string mediaid, string appid, string appsecret, Stream stream)
        {
            _WXMsgTemplateService.GetMedia(mediaid, appid, appsecret, stream);
        }
        /// <summary>
        /// 添加发送记录
        /// </summary>
        /// <param name="info"></param>
        public static void AddSendRecord(SendMessageRecordInfo info, List<SendmessagerecordCouponInfo> coupons = null)
        {
            _WXMsgTemplateService.AddSendRecord(info, coupons);
        }
        /// <summary>
        /// 取发送记录
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<SendMessageRecord> GetSendRecords(SendRecordQuery query)
        {
            var data = _WXMsgTemplateService.GetSendRecords(query);
            QueryPageModel<SendMessageRecord> item = new QueryPageModel<SendMessageRecord>();
            item.Total = data.Total;
            var list = data.Models.ToList();
            var dataList = Mapper.Map<List<SendMessageRecord>>(list);
            foreach (var info in dataList)
            {
                var record = _WXMsgTemplateService.GetSendrecordCouponSnById(info.Id);
                info.CurrentCouponCount = record.Count;
                foreach (var items in record)
                {
                    var result = _WXMsgTemplateService.GetCouponRecordBySn(items.CouponSN);
                    long orderId = 0;
                    if (result.OrderId != null)
                    {
                        if (result.OrderId.IndexOf(',') > -1)
                        {
                            orderId = long.Parse(result.OrderId.Split(',')[0]);
                        }
                        else
                        {
                            orderId = long.Parse(result.OrderId);
                        }
                    }
                    var orderResult = result.OrderId == null ? null : OrderApplication.GetOrder(orderId);
                    if (result != null && orderResult != null)
                        info.CurrentUseCouponCount++;
                }
            }
            item.Models = dataList;
            return item;
        }
        public static List<Entities.SendmessagerecordCouponSNInfo> GetSendrecordCouponSnById(long id)
        {
            return _WXMsgTemplateService.GetSendrecordCouponSnById(id);
        }
        /// <summary>
        /// 指定openIds发送微信消息
        /// </summary>
        /// <param name="openIds">发送openId集合</param>
        /// <param name="msgType">类型</param>
        /// <param name="content">文本内容</param>
        /// <param name="mediaId">模板ID</param>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <returns></returns>
        public static Entities.SendInfoResult SendWXMsg(IEnumerable<string> openIds, WXMsgType msgType, string content, string mediaId, string appId, string appSecret)
        {
            return _WXMsgTemplateService.SendWXMsg(openIds, msgType, content, mediaId, appId, appSecret);
        }



        #region 模板消息
        /// <summary>
        /// 获取微信模板消息列表
        /// </summary>
        /// <returns></returns>
        public static List<Entities.WeiXinMsgTemplateInfo> GetWeiXinMsgTemplateList()
        {
            return _WXMsgTemplateService.GetWeiXinMsgTemplateList();
        }
        /// <summary>
        /// 获取微信模板信息
        /// </summary>
        /// <returns></returns>
        public static Entities.WeiXinMsgTemplateInfo GetWeiXinMsgTemplate(Himall.Core.Plugins.Message.MessageTypeEnum type)
        {
            return _WXMsgTemplateService.GetWeiXinMsgTemplate(type);
        }
        /// <summary>
        /// 设置微信模板消息配置
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static Entities.WeiXinMsgTemplateInfo UpdateWeiXinMsgTemplate(Entities.WeiXinMsgTemplateInfo info)
        {
            return _WXMsgTemplateService.UpdateWeiXinMsgTemplate(info);
        }
        /// <summary>
        /// 设置微信消息开启状态
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isOpen"></param>
        public static void UpdateWeiXinMsgOpenState(Himall.Core.Plugins.Message.MessageTypeEnum type, bool isOpen)
        {
            _WXMsgTemplateService.UpdateWeiXinMsgOpenState(type, isOpen);
        }
        /// <summary>
        /// 发送模板消息
        /// </summary>
        /// <param name="type"></param>
        /// <param name="userId">为0时使用openid</param>
        /// <param name="data">信息数据</param>
        /// <param name="url"></param>
        /// <param name="openid">与userid配合使用，userid为0时使用此字段</param>
        public static void SendMessageByTemplate(Himall.Core.Plugins.Message.MessageTypeEnum type, long userId, WX_MsgTemplateSendDataModel data, string url = "", string wxopenid = "")
        {
            _WXMsgTemplateService.SendMessageByTemplate(type, userId, data, url, wxopenid);
        }
        /// <summary>
        /// 获取模板消息跳转URL
        /// </summary>
        /// <param name="type"></param>
        public static string GetMessageTemplateShowUrl(Himall.Core.Plugins.Message.MessageTypeEnum type)
        {
            return _WXMsgTemplateService.GetMessageTemplateShowUrl(type);
        }
        /// <summary>
        /// 添加消息模板
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <param name="type">null表示所有都重置</param>
        public static void AddMessageTemplate(Himall.Core.Plugins.Message.MessageTypeEnum? type = null)
        {
            _WXMsgTemplateService.AddMessageTemplate(type);
        }
        #endregion

        /// <summary>
        /// 新增小程序表单提交数据
        /// </summary>
        /// <param name="mWXSmallChoiceProductsInfo"></param>
        public static void AddWXAppletFromData(Entities.WXAppletFormDataInfo wxapplet)
        {
            _WXMsgTemplateService.AddWXAppletFromData(wxapplet);
        }

        /// <summary>
        /// 获取发送消息记录
        /// </summary>
        /// <param name="messageId">消息记录Id</param>
        public static SendMessageRecordModel GetSendMessageRecordById(long messageId)
        {
            var record = _WXMsgTemplateService.GetSendMessageRecordById(messageId);
            Mapper.CreateMap<Entities.SendMessageRecordInfo, SendMessageRecordModel>();
            SendMessageRecordModel sendModel = Mapper.Map<SendMessageRecordInfo, SendMessageRecordModel>(record);

            if (sendModel == null)
                return sendModel;

            #region 群发对象拆简化（之前数据已保存了，则下面拆简）
            if (!string.IsNullOrEmpty(sendModel.ToUserLabel))
            {
                sendModel.ToUserObject = sendModel.ToUserLabel;
                string[] s1 = sendModel.ToUserLabel.Trim().Split(new string[] { "性别" }, System.StringSplitOptions.RemoveEmptyEntries);
                if(s1!=null && s1.Length > 1)
                {
                    sendModel.ToUserObject= s1[0];
                    string[] s2 = s1[1].Split(' ');
                    sendModel.ToUserSex = s2[0].Replace("：", "").Replace(":", "");
                }

                string[] sq = sendModel.ToUserLabel.Trim().Split(new string[] { "地区" }, System.StringSplitOptions.RemoveEmptyEntries);
                if (sq != null && sq.Length > 1)
                {
                    string[] s2 = sq[1].Split(' ');
                    sendModel.ToUserRegion = s2[0].Replace("：", "").Replace(":", "");
                }
                sendModel.ToUserObject = sendModel.ToUserObject.Replace("标签：", "").Replace("标签:", "");
            }
            #endregion

            #region 群发是内容是存在一起的拆简分出来
            if (!string.IsNullOrEmpty(sendModel.SendContent))
            {
                if (sendModel.MessageType == MsgType.Email)
                {
                    #region //邮件内容和标题是存在一个字段，现拆分出来
                    string[] s1 = sendModel.SendContent.Trim().Split(new string[] { "####" }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (string str in s1)
                    {
                        if (string.IsNullOrEmpty(str.Trim()))
                            continue;
                        string[] s2 = str.Split(new string[] { "■■" }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (s2.Length > 1)
                        {
                            sendModel.SendEmailTitle = s2[1];
                            break;
                        }
                    }
                    sendModel.SendContent = s1[s1.Length - 1];//内容是最后
                    #endregion
                }
                else if (sendModel.MessageType == MsgType.WeiXin && sendModel.ContentType == WXMsgType.mpnews)
                {
                    #region 微信群发图文，链接MediaId与内容是存在一个字段，现拆分出来
                    string[] s1 = sendModel.SendContent.Trim().Split(new string[] { "####" }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (string str in s1)
                    {
                        if (string.IsNullOrEmpty(str.Trim()))
                            continue;
                        string[] s2 = str.Split(new string[] { "■■" }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (s2.Length > 1)
                        {
                            sendModel.SendWXMediaId = s2[1];
                            break;
                        }
                    }
                    sendModel.SendContent = s1[s1.Length - 1];//内容是最后
                    #endregion
                }
            }
            #endregion

            if (sendModel.MessageType == MsgType.Coupon)
            {
                #region 加载它优惠列表
                CouponService _CouponService = ObjectContainer.Current.Resolve<CouponService>();

                List<Entities.CouponInfo> CList = _CouponService.GetCouponBySendmessagerecordId(messageId);

                if (CList != null && CList.Count() > 0)
                {
                    sendModel.CouponList = new List<CouponModel>();
                    foreach (var cinfo in CList)
                    {
                        Mapper.CreateMap<Entities.CouponInfo, CouponModel>();
                        CouponModel couponModel = Mapper.Map<CouponInfo, CouponModel>(cinfo);
                        sendModel.CouponList.Add(couponModel);
                    }
                }
                #endregion
            }

            return sendModel;
        }


        /// <summary>
        /// 获取小程序微信模板消息列表
        /// </summary>
        /// <returns></returns>
        public static List<Entities.WeiXinMsgTemplateInfo> GetTemplateByAppletlist()
        {
           return _WXMsgTemplateService.GetTemplateByAppletlist();
        }
        /// <summary>
        /// 授权订阅消息
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="templateIds">订阅消息模板Ids</param>
        /// <param name="messageType">消息类型</param>
        public static void AddAuthorizedSubscribeMessage(string orderIds, string templateIds)
        {
            _WXMsgTemplateService.AuthorizedSubscribeMessage(orderIds, templateIds);
        }

        #region 获取小程序订阅消息
        public static bool GetAppletSubscribeTmplate()
        {
            bool isSuccess = false;
            string appletaccesstoken = GetAppletResetToken();
            var appletTemps = _WXMsgTemplateService.GetTemplateByAppletlist();

            Dictionary<string, string> dics = new Dictionary<string, string>();
            dics.Add("退款审核后通知会员", "1435");//退款通知
            dics.Add("自提订单付款成功后通知", "2306");//订单提货通知
            dics.Add("快递配送订单发货后通知", "855");//订单发货通知
            dics.Add("门店配送订单发货后通知", "855");//订单发货通知

            foreach (var item in dics)
            {
                string tempId = "";
                var lst = new int[] { };
                var value = item.Value;
                var messageType = 0;
                switch (value)
                {
                    case "1435":
                        lst = new int[] { 6, 4, 5 };
                        break;
                    case "2306":
                        lst = new int[] { 1, 2, 4, 5, 3 };
                        break;
                    case "855":
                        if (item.Key.Contains("门店配送"))
                        {
                            lst = new int[] { 1, 2, 5, 8 };
                            messageType = (int)MessageTypeEnum.ShopOrderShipping;
                        }
                        else
                        {
                            lst = new int[] { 1, 2, 7, 4, 6 };
                            messageType = (int)MessageTypeEnum.OrderShipping;
                        }
                        break;
                    default:
                        break;
                }
                try
                {
                    //先删除旧的模板再添加新模板内容
                    if (appletTemps != null)
                    {
                        var firstTemp = (messageType != 0) ? appletTemps.Where(t => t.TemplateNum == item.Value && t.MessageType == messageType).FirstOrDefault() : appletTemps.Where(t => t.TemplateNum == item.Value).FirstOrDefault();
                        if (firstTemp != null && !string.IsNullOrEmpty(firstTemp.TemplateId))
                        {
                            RemoveTemplate(firstTemp.TemplateId, appletaccesstoken);
                        }
                    }

                    tempId = AddSubscribeTemplate(value, lst, item.Key, appletaccesstoken);
                    if (value == "855")
                    {
                        _WXMsgTemplateService.UpdateWXsmallTemplateId(value, tempId, messageType);
                    }
                    else
                    {
                        _WXMsgTemplateService.UpdateWXsmallTemplateId(value, tempId);
                    }
                    isSuccess = true;
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("获取小程序订阅消息模板ID {0}：" + ex.Message, value));
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 删除订阅消息模板
        /// </summary>
        public static void RemoveTemplate(string template_id, string accesstoken)
        {
            var postData = $"priTmplId={template_id}";
            var content = PostForm("https://api.weixin.qq.com/wxaapi/newtmpl/deltemplate", postData, accesstoken);
            var json = JObject.Parse(content);
            if (json["errcode"].Value<int>() > 0)
                Log.Debug("删除订阅消息模板异常：" + json["errcode"].Value<string>() + ";" + json["errmsg"].Value<string>());
        }

        /// <summary>
        /// 添加订阅消息模板，返回模板ID
        /// </summary>
        /// <param name="number"></param>
        /// <param name="keywords"></param>
        /// <param name="sceneDesc">场景描述</param>
        /// <returns></returns>
        public static string AddSubscribeTemplate(string number, int[] keywords, string sceneDesc, string accesstoken)
        {
            var postData = $"tid={number}&sceneDesc={sceneDesc}";
            foreach (var key in keywords)
                postData += $"&kidList={key}";

            var content = PostForm("https://api.weixin.qq.com/wxaapi/newtmpl/addtemplate", postData, accesstoken);
            var json = JObject.Parse(content);
            if (json["errcode"].Value<int>() != 0)
                Log.Debug("添加订阅消息模板异常：" + json["errcode"].Value<string>() + ";" + json["errmsg"].Value<string>());
            return json["priTmplId"].Value<string>();
        }

        /// <summary>
        /// postForm提交
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string PostForm(string url, string formData, string accesstoken)
        {
            var postUrl = url + $"?access_token={accesstoken}";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(postUrl);
            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(formData);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = requestBytes.Length;
            Stream requestStream = req.GetRequestStream();
            requestStream.Write(requestBytes, 0, requestBytes.Length);
            requestStream.Close();
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            using (StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// 获取Token
        /// </summary>
        /// <returns></returns>
        private static string GetAppletResetToken()
        {
            var api = "https://" + $"api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={SiteSettingApplication.SiteSettings.WeixinAppletId}&secret={SiteSettingApplication.SiteSettings.WeixinAppletSecret}";
            var content = HttpHelper.Get(api);
            var json = JObject.Parse(content);
            return json["access_token"].Value<string>();
        }
        #endregion
    }
}
