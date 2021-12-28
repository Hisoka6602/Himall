﻿using Himall.CommonModel;
using Himall.CommonModel.WeiXin;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Core.Plugins.Message;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Service.Weixin;
using NetRube.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Senparc.Weixin.Entities.TemplateMessage;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.GroupMessage;
using Senparc.Weixin.MP.AdvancedAPIs.Media;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.WxOpen.AdvancedAPIs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Himall.Service
{
    public class WXMsgTemplateService : ServiceBase
    {
        #region 素材管理
        public Entities.WXUploadNewsResult Add(IEnumerable<Entities.WXMaterialInfo> info, string appid, string appsecret)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            var models = info.Select(e => new NewsModel
            {
                author = e.author,
                content = GetContentHandler(e.content, appid, appsecret),
                content_source_url = e.content_source_url,
                digest = e.digest,
                show_cover_pic = e.show_cover_pic,
                thumb_media_id = e.thumb_media_id,
                title = e.title
            }).ToArray();
            var uploadNewsResult = MediaApi.UploadNews(token, news: models);
            return new Entities.WXUploadNewsResult { errmsg = uploadNewsResult.errmsg, media_id = uploadNewsResult.media_id };
        }

        public Entities.WxJsonResult DeleteMedia(string mediaid, string appid, string appsecret)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            var result = MediaApi.DeleteForeverMedia(token, mediaid);
            return new Entities.WxJsonResult() { errmsg = result.errmsg };
        }

        public List<Entities.WxJsonResult> UpdateMedia(string mediaid, IEnumerable<Entities.WXMaterialInfo> news, string appid, string appsecret)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            int idx = 0;
            List<Entities.WxJsonResult> resultList = new List<Entities.WxJsonResult>();
            foreach (var model in news)
            {
                var result = MediaApi.UpdateForeverNews(token, mediaid, idx, new NewsModel()
                {
                    author = model.author,
                    content = GetContentHandler(model.content, appid, appsecret),
                    content_source_url = model.content_source_url,
                    digest = model.digest,
                    show_cover_pic = model.show_cover_pic,
                    thumb_media_id = model.thumb_media_id,
                    title = model.title
                });
                resultList.Add(new Entities.WxJsonResult { errmsg = result.errmsg });
            }
            return resultList;
        }
        public string AddImage(string filename, string appid, string appsecret)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            var uploadResult = MediaApi.UploadForeverMedia(token, filename);
            if (uploadResult.errcode != 0)
            {
                Log.Error($"新增其他类型永久素材,出错,错误代码：{uploadResult.errcode},错误信息：{uploadResult.errmsg},filename:{filename}");
            }
            return uploadResult.media_id;
        }
        /// <summary>
        /// 上传图片返回图片路径
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public string AddImageGetUrl(string filename, string appid, string appsecret)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            var uploadResult = MediaApi.UploadForeverMedia(token, filename);
            return uploadResult.url;
        }
        public IEnumerable<Entities.WXMaterialInfo> GetMedia(string mediaid, string appid, string appsecret)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            var mediaNews = MediaApi.GetForeverNews(token, mediaid).news_item.Select(e => new Entities.WXMaterialInfo
            {
                author = e.author,
                title = e.title,
                thumb_media_id = e.thumb_media_id,
                show_cover_pic = e.show_cover_pic,
                digest = e.digest,
                content_source_url = e.content_source_url,
                content = e.content,
                url = e.url
            });

            return mediaNews;
        }
        public void GetMedia(string mediaid, string appid, string appsecret, Stream stream)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            MediaApi.GetForeverMedia(token, mediaid, stream);
        }
        public Entities.MediaNewsItemList GetMediaMsgTemplateList(string appid, string appsecret, int offset, int count)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            var mediaList = MediaApi.GetNewsMediaList(token, offset, count);
            if (mediaList == null || mediaList.total_count <= 0)
            {
                token = wxHelper.GetAccessToken(appid, appsecret, true);
            }

            var tempList = new Entities.MediaNewsItemList
            {
                count = mediaList.item_count,
                total_count = mediaList.total_count,
                content = mediaList.item == null ? null : mediaList.item.Select(e => new Entities.MediaNewsItem
                {
                    media_id = e.media_id,
                    items = e.content.news_item.Select(item => new Entities.WXMaterialInfo
                    {
                        author = item.author,
                        title = item.title,
                        thumb_media_id = item.thumb_media_id,
                        show_cover_pic = item.show_cover_pic,
                        digest = item.digest,
                        content_source_url = item.content_source_url,
                        content = item.content,
                        url = item.url
                    }),
                    update_time = DateTime.Parse("1970-01-01").AddSeconds(e.update_time).ToString()
                }),
                errCode = mediaList.errcode.ToString(),
                errMsg = mediaList.errmsg
            };
            return tempList;
        }

        public Entities.MediaItemCount GetMediaItemCount(string appid, string appsecret)
        {
            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appid, appsecret);
            var result = MediaApi.GetMediaCount(token);
            var itemcount = new Entities.MediaItemCount
            {
                image_count = result.image_count,
                news_count = result.news_count,
                video_count = result.video_count,
                voice_count = result.voice_count,
                errMsg = result.errmsg,
                errCode = result.errcode.ToString()
            };
            return itemcount;
        }
        #region 正文内容里外部图片上传处理
        /// <summary>
        /// 对素材正文如上传了图片进行上传处理
        /// </summary>
        /// <param name="strcontent"></param>
        /// <returns></returns>
        private string GetContentHandler(string strcontent, string appid, string appsecret)
        {
            string strNew = strcontent;
            if (string.IsNullOrEmpty(strcontent))
                return strNew;

            ArrayList listurl = new ArrayList();
            foreach (var strUrl in GetHtmlImageUrlList(strcontent))
            {
                //外部网址不需处理；"https://mmbiz.qpic.cn"已是微信公众号的网址不需处理,或已上传过一次不需处理
                if (string.IsNullOrEmpty(strUrl) || strUrl.IndexOf("http://") != -1 || strUrl.IndexOf("https://") != -1 || listurl.IndexOf(strUrl) != -1)
                    continue;

                var filename = System.Web.HttpContext.Current.Server.MapPath(strUrl);
                if (!File.Exists(filename))
                    continue;//图片不存在不需执行下面的

                var newurl = AddImageGetUrl(filename, appid, appsecret);
                if (!string.IsNullOrEmpty(newurl))
                {
                    strNew = strNew.Replace(strUrl, newurl);//原图片网址换成新的图片地址
                    listurl.Add(strUrl);
                }
            }
            return strNew;
        }

        /// <summary>   
        /// 取得HTML中所有图片的 URL。   
        /// </summary>   
        /// <param name="sHtmlText">HTML代码</param>   
        /// <returns>图片的URL列表</returns>   
        private string[] GetHtmlImageUrlList(string sHtmlText)
        {
            // 定义正则表达式用来匹配 img 标签   
            Regex regImg = new Regex(@"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<imgUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);

            // 搜索匹配的字符串   
            MatchCollection matches = regImg.Matches(sHtmlText);
            int i = 0;
            string[] sUrlList = new string[matches.Count];

            // 取得匹配项列表   
            foreach (Match match in matches)
                sUrlList[i++] = match.Groups["imgUrl"].Value;
            return sUrlList;
        }
        #endregion
        #endregion 素材管理

        #region 群发消息
        public Entities.SendInfoResult SendWXMsg(Entities.SendMsgInfo info)
        {
            var toUsers = GetToUser(info.ToUserLabel);
            if (toUsers.Length > 0)
            {
                if (toUsers.Length == 1)
                {
                    return new Entities.SendInfoResult { errCode = "群发微信消息，至少需要2个发送对象！", errMsg = "" };
                }
                var wxHelper = new WXHelper();
                var token = wxHelper.GetAccessToken(info.AppId, info.AppSecret);
                var sendResult = GroupMessageApi.SendGroupMessageByOpenId(token, (GroupMessageType)info.MsgType, info.MsgType == WXMsgType.text ? info.Content : info.MediaId, openIds: toUsers);
                if (!string.IsNullOrWhiteSpace(sendResult.msg_id))
                {
                    string strContent = info.Content;
                    if (info.MsgType == WXMsgType.mpnews)
                    {
                        #region 如是图文列表，内容把链接和标题保存进去
                        strContent = "####MediaId■■" + info.MediaId + "####";
                        var remodel = GetMedia(info.MediaId, info.AppId, info.AppSecret);
                        if (remodel != null)
                        {
                            WXMaterialInfo reinfo = remodel.FirstOrDefault();
                            if (reinfo != null)
                                strContent += reinfo.title;
                        }
                        #endregion
                    }
                    Entities.SendMessageRecordInfo model = new Entities.SendMessageRecordInfo()
                    {
                        ContentType = info.MsgType,
                        MessageType = MsgType.WeiXin,
                        SendContent = strContent,
                        SendTime = DateTime.Now,
                        ToUserLabel = info.ToUserDesc,
                        SendState = 1
                    };
                    DbFactory.Default.Add(model);
                }
                return new Entities.SendInfoResult { errCode = sendResult.errcode.ToString(), errMsg = sendResult.errmsg };
            }
            else
            {
                return new Entities.SendInfoResult { errCode = "未找到符合条件的发送对象！", errMsg = "" };
            }
        }


        public Entities.SendInfoResult SendWXMsg(IEnumerable<string> openIds, WXMsgType msgType, string content, string mediaId, string appId, string appSecret)
        {
            if (openIds.Count() <= 1)
            {
                return new Entities.SendInfoResult { errCode = "群发微信消息，至少需要2个发送对象！", errMsg = "" };
            }

            var wxHelper = new WXHelper();
            var token = wxHelper.GetAccessToken(appId, appSecret);
            var sendResult = GroupMessageApi.SendGroupMessageByOpenId(token, (GroupMessageType)msgType, msgType == WXMsgType.text ? content : mediaId, openIds: openIds.ToArray());
            return new Entities.SendInfoResult { errCode = sendResult.errcode.ToString(), errMsg = sendResult.errmsg };
        }

        private string[] GetToUser(Entities.SendToUserLabel labelinfo)
        {
            //var memOpenid = (from m in Context.MemberOpenIdInfo
            //                 join o in Context.OpenIdsInfo on m.OpenId equals o.OpenId
            //                 join u in Context.UserMemberInfo on m.UserId equals u.Id
            //                 where o.IsSubscribe
            //                 select new
            //                 {
            //                     userid = m.UserId,
            //                     openid = m.OpenId,
            //                     regionid = u.TopRegionId,
            //                     sex = u.Sex
            //                 });
            var memOpenid = DbFactory.Default
                .Get<MemberOpenIdInfo>()
                .InnerJoin<OpenIdInfo>((moii, oii) => moii.OpenId == oii.OpenId)
                .InnerJoin<MemberInfo>((moii, mi) => moii.UserId == mi.Id)
                .Where<OpenIdInfo>(n => n.IsSubscribe == true);
            //.Select(n => new { n.UserId, n.OpenId })
            //.Select<MemberInfo>(n => new { n.TopRegionId, n.Sex });
            if (labelinfo.ProvinceId.HasValue)
            {
                memOpenid.Where<MemberInfo>(e => e.TopRegionId == labelinfo.ProvinceId.Value);
            }
            if (labelinfo.Sex.HasValue)
            {
                memOpenid.Where<MemberInfo>(e => e.Sex == labelinfo.Sex.Value);
            }
            if (labelinfo.LabelIds != null && labelinfo.LabelIds.Length > 0)
            {
                memOpenid.InnerJoin<MemberLabelInfo>((moii, mli) => moii.UserId == mli.MemId)
                    .Where<MemberLabelInfo>(n => n.LabelId.ExIn(labelinfo.LabelIds));
            }

            return memOpenid.Select(e => e.OpenId).Distinct().ToList<string>().ToArray();
        }
        public void AddSendRecord(SendMessageRecordInfo info, List<SendmessagerecordCouponInfo> coupons = null)
        {
            DbFactory.Default.Add(info);
            if (coupons != null)
            {
                coupons.ForEach(p => p.MessageId = info.Id);
                DbFactory.Default.AddRange(coupons);
            }
        }

        public QueryPageModel<Entities.SendMessageRecordInfo> GetSendRecords(SendRecordQuery query)
        {
            var sendRecords = DbFactory.Default.Get<Entities.SendMessageRecordInfo>();
            if (query.msgType.HasValue)
            {
                var msgType = query.msgType.Value;
                sendRecords.Where(p => p.MessageType == (MsgType)msgType);
            }
            if (query.sendState.HasValue)
            {
                sendRecords.Where(e => e.SendState == query.sendState.Value);
            }
            if (query.startDate.HasValue)
            {
                DateTime sdt = query.startDate.Value;
                sendRecords.Where(d => d.SendTime >= sdt);
            }
            if (query.endDate.HasValue)
            {
                DateTime edt = query.endDate.Value.AddDays(1);
                sendRecords.Where(d => d.SendTime < edt);
            }

            var models = sendRecords.OrderByDescending(a => a.Id).ToPagedList(query.PageNo, query.PageSize);
            QueryPageModel<Entities.SendMessageRecordInfo> pageModel = new QueryPageModel<Entities.SendMessageRecordInfo>() { Models = sendRecords.ToList(), Total = models.TotalRecordCount };
            return pageModel;

            //IQueryable<SendMessageRecordInfo> sendRecords = Context.SendMessageRecordInfo.AsQueryable();
            //if (query.msgType.HasValue)
            //{
            //    var msgType = query.msgType.Value;
            //    sendRecords = sendRecords.Where(p => p.MessageType == (MsgType)msgType);
            //}
            //if (query.sendState.HasValue)
            //{
            //    sendRecords = sendRecords.Where(e => e.SendState == query.sendState.Value);
            //}
            //if (query.startDate.HasValue)
            //{
            //    DateTime sdt = query.startDate.Value;
            //    sendRecords = sendRecords.Where(d => d.SendTime >= sdt);
            //}
            //if (query.endDate.HasValue)
            //{
            //    DateTime edt = query.endDate.Value.AddDays(1);
            //    sendRecords = sendRecords.Where(d => d.SendTime < edt);
            //}
            //int total = 0;
            //sendRecords = sendRecords.GetPage(d => d.OrderByDescending(o => o.Id), out total, query.PageNo, query.PageSize);
            //QueryPageModel<SendMessageRecordInfo> pageModel = new QueryPageModel<SendMessageRecordInfo>() { Models = sendRecords.ToList(), Total = total };
            //return pageModel;
        }
        public List<Entities.SendmessagerecordCouponSNInfo> GetSendrecordCouponSnById(long id)
        {
            var datalist = new List<Entities.FightGroupOrderInfo>();
            var data = DbFactory.Default.Get<Entities.SendmessagerecordCouponSNInfo>();
            //var data = Context.SendmessagerecordCouponSNInfo.AsQueryable();
            if (id > 0)
            {
                data.Where(p => p.MessageId == id);
            }
            return data.ToList();
        }
        public Entities.CouponRecordInfo GetCouponRecordBySn(string CouponSn)
        {
            var data = DbFactory.Default.Get<Entities.CouponRecordInfo>().Where(p => p.CounponSN == CouponSn).FirstOrDefault();
            //var data = Context.CouponRecordInfo.AsNoTracking().FirstOrDefault(p => p.CounponSN == CouponSn);
            return data;
        }

        /// <summary>
        /// 获取发送消息记录
        /// </summary>
        /// <param name="messageId">消息记录Id</param>
        /// <returns></returns>
        public Entities.SendMessageRecordInfo GetSendMessageRecordById(long messageId)
        {
            var result = DbFactory.Default.Get<Entities.SendMessageRecordInfo>().Where(p => p.Id == messageId).FirstOrDefault();
            return result;
        }
        #endregion  群发消息

        #region 模板消息
        /// <summary>
        /// 获取微信模板消息列表
        /// </summary>
        /// <returns></returns>
        public List<Entities.WeiXinMsgTemplateInfo> GetWeiXinMsgTemplateList()
        {
            List<WeiXinMsgTemplateInfo> needChange = new List<WeiXinMsgTemplateInfo>();
            List<Entities.WeiXinMsgTemplateInfo> result = new List<Entities.WeiXinMsgTemplateInfo>();
            result = DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().Where(p => p.UserInWxApplet == WXMsgTemplateType.WeiXinShop).ToList();
            //result = Context.WeiXinMsgTemplateInfo.ToList();
            bool isHaveSave = false;
            //初始表
            var _wxmsglist = WX_MsgTemplateLinkData.GetList();
            foreach (var item in _wxmsglist)
            {
                int _tmptype = (int)item.MsgType;
                var _tmpmsg = result.FirstOrDefault(d => d.MessageType == _tmptype);
                if (_tmpmsg == null)
                {
                    isHaveSave = true;
                    result.Add(GetWeiXinMsgTemplate(item.MsgType));
                }
                else
                {
                    if (_tmpmsg.TemplateNum != item.MsgTemplateShortId)
                    {
                        isHaveSave = true;
                        //数据修正
                        _tmpmsg.TemplateNum = item.MsgTemplateShortId;
                        _tmpmsg.TemplateId = "";   //如果模板对应出错，需重置
                        needChange.Add(_tmpmsg);
                        //DbFactory.Default.Update(_tmpmsg);
                    }
                }
            }
            if (isHaveSave)
            {
                foreach (var item in needChange)
                {
                    DbFactory.Default.Update(item);
                }
                result = DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().ToList();
                //result = Context.WeiXinMsgTemplateInfo.ToList();
            }
            return result;
        }


        /// <summary>
        /// 获取微信模板信息
        /// </summary>
        /// <returns></returns>
        public Entities.WeiXinMsgTemplateInfo GetWeiXinMsgTemplate(Himall.Core.Plugins.Message.MessageTypeEnum type)
        {
            int msgtype = (int)type;
            Entities.WeiXinMsgTemplateInfo result = DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().Where(d => d.MessageType == msgtype && (d.UserInWxApplet.ExIsNull() || d.UserInWxApplet == WXMsgTemplateType.WeiXinShop)).FirstOrDefault();
            //WeiXinMsgTemplateInfo result = Context.WeiXinMsgTemplateInfo.FirstOrDefault(d => d.MessageType == msgtype && (!d.UserInWxApplet.HasValue || d.UserInWxApplet == WXMsgTemplateType.WeiXinShop));
            var _tmp = WX_MsgTemplateLinkData.GetList().FirstOrDefault(d => d.MsgType == type);
            if (result == null)
            {
                result = new Entities.WeiXinMsgTemplateInfo();
                result.MessageType = msgtype;
                if (_tmp != null)
                {
                    result.TemplateNum = _tmp.MsgTemplateShortId;
                }
                result.UpdateDate = DateTime.Now;
                result.IsOpen = false;
                DbFactory.Default.Add(result);
            }
            return result;
        }


        /// <summary>
        /// 获取小程序微信模板消息列表，初始化
        /// </summary>
        /// <returns></returns>
        public List<Entities.WeiXinMsgTemplateInfo> GetWeiXinMsgTemplateListByApplet()
        {
            List<Entities.WeiXinMsgTemplateInfo> result = new List<Entities.WeiXinMsgTemplateInfo>();
            result = DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().Where(d => d.UserInWxApplet == WXMsgTemplateType.Applet).ToList();
            bool isHaveSave = false;
            //初始表
            var _wxmsglist = WXApplet_MsgTemplateLinkData.GetList();
            foreach (var item in _wxmsglist)
            {
                int _tmptype = (int)item.MsgType;
                var _tmpmsg = result.FirstOrDefault(d => d.MessageType == _tmptype);
                if (_tmpmsg == null)
                {
                    isHaveSave = true;
                    result.Add(GetWeiXinMsgTemplateByApplet(item.MsgType));
                }
                else
                {
                    if (_tmpmsg.TemplateNum != item.MsgTemplateShortId)
                    {
                        isHaveSave = true;
                        //数据修正
                        _tmpmsg.TemplateNum = item.MsgTemplateShortId;
                        _tmpmsg.TemplateId = "";   //如果模板对应出错，需重置
                        DbFactory.Default.Update(_tmpmsg);
                    }
                }
            }
            if (isHaveSave)
            {
                result = DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().Where(d => d.UserInWxApplet == WXMsgTemplateType.Applet).ToList();
            }
            return result;
        }
        /// <summary>
        /// 获取小程序消息模板
        /// </summary>
        /// <returns></returns>
        public List<Entities.WeiXinMsgTemplateInfo> GetTemplateByAppletlist()
        {
            return DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().Where(d => d.UserInWxApplet == WXMsgTemplateType.Applet).ToList();
        }
        /// <summary>
        /// 获取小程序微信模版
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Entities.WeiXinMsgTemplateInfo GetWeiXinMsgTemplateByApplet(Himall.Core.Plugins.Message.MessageTypeEnum type)
        {
            int msgtype = (int)type;
            Entities.WeiXinMsgTemplateInfo result = DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().Where(d => d.UserInWxApplet == WXMsgTemplateType.Applet && d.MessageType == msgtype).FirstOrDefault();
            if (result == null)
            {
                result = new Entities.WeiXinMsgTemplateInfo();
                result.MessageType = msgtype;
                var _tmp = WXApplet_MsgTemplateLinkData.GetList().FirstOrDefault(d => d.MsgType == type);
                if (_tmp != null)
                {
                    result.TemplateNum = _tmp.MsgTemplateShortId;
                }
                result.UpdateDate = DateTime.Now;
                result.IsOpen = true;
                result.UserInWxApplet = WXMsgTemplateType.Applet;
                DbFactory.Default.Add(result);
            }
            return result;
        }
        /// <summary>
        /// 设置微信模板消息配置
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Entities.WeiXinMsgTemplateInfo UpdateWeiXinMsgTemplate(Entities.WeiXinMsgTemplateInfo info)
        {
            Himall.Core.Plugins.Message.MessageTypeEnum _msgtype = (Himall.Core.Plugins.Message.MessageTypeEnum)info.MessageType;
            Entities.WeiXinMsgTemplateInfo data = GetWeiXinMsgTemplate(_msgtype);
            if (data != null)
            {
                info.Id = data.Id;
                data.TemplateId = info.TemplateId;
                data.MessageType = info.MessageType;
                data.UpdateDate = info.UpdateDate;
                data.IsOpen = info.IsOpen;
                DbFactory.Default.Update(data);
            }
            else
            {
                DbFactory.Default.Add(info);
            }

            return info;
        }
        /// <summary>
        /// 设置微信消息开启状态
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isOpen"></param>
        public void UpdateWeiXinMsgOpenState(Himall.Core.Plugins.Message.MessageTypeEnum type, bool isOpen)
        {
            Entities.WeiXinMsgTemplateInfo data = GetWeiXinMsgTemplate(type);
            data.MessageType = (int)type;
            data.IsOpen = isOpen;
            DbFactory.Default.Update(data);
        }
        /// <summary>
        /// 发送模板消息
        /// </summary>
        /// <param name="type"></param>
        /// <param name="userId">为0时使用openid</param>
        /// <param name="data">信息数据</param>
        /// <param name="url"></param>
        /// <param name="openid">与userid配合使用，userid为0时使用此字段</param>
        public void SendMessageByTemplate(MessageTypeEnum type, long userId, WX_MsgTemplateSendDataModel data, string url = "", string wxopenid = "")
        {
            var siteSetting = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings;
            string appid = siteSetting.WeixinAppId;
            string appsecret = siteSetting.WeixinAppSecret;
            if (string.IsNullOrWhiteSpace(appid) || string.IsNullOrWhiteSpace(appsecret))
            {
                throw new HimallException("未配置微信公众信息");
            }
            string dataerr = "";
#if DEBUG
            Core.Log.Info("[模板消息]开始准备数据：" + userId.ToString() + "[" + type.ToDescription() + "]");
#endif
            bool isdataok = true;
            string openId = wxopenid;
            if (userId == 0)
            {
                if (string.IsNullOrWhiteSpace(wxopenid))
                {
                    throw new HimallException("错误的OpenId");
                }
                openId = wxopenid;
            }
            else
            {
                openId = GetPlatformOpenIdByUserId(userId);
            }
            if (string.IsNullOrWhiteSpace(openId))
            {
                dataerr = "openid为空";
                isdataok = false;
            }
            var userinfo = DbFactory.Default.Get<Entities.MemberInfo>().Where(d => d.Id == userId).FirstOrDefault();
            if (userId != 0)
            {
                if (userinfo == null)
                {
                    dataerr = "用户信息未取到" + userId;
                    isdataok = false;
                }
            }
            var _msgtmplinfo = GetWeiXinMsgTemplate(type);
            if (_msgtmplinfo == null)
            {
                dataerr = "消息模板未取到";
                isdataok = false;
            }
            string templateId = "";
            string topcolor = "#000000";
            if (isdataok)
            {
                templateId = _msgtmplinfo.TemplateId;
                if (string.IsNullOrWhiteSpace(templateId))
                {
                    dataerr = "消息模板未取到";
                    isdataok = false;
                }
            }
            if (!_msgtmplinfo.IsOpen)
            {
                dataerr = "未开启";
                isdataok = false;
            }
            Log.Info("消息类型=" + type.ToDescription() + " userId=" + userId + " openId=" + openId + " dataerr=" + dataerr);
            if (isdataok)
            {
#if DEBUG
                Core.Log.Info("[模板消息]开始发送前");
#endif
                object msgdata;
                switch (type)
                {
                    case Core.Plugins.Message.MessageTypeEnum.EditPayPassWord:
                        #region 修改交易密码
                        var _paymsgdata = new WX_MsgTemplateKey2DataModel();
                        _paymsgdata.first.value = data.first.value;
                        _paymsgdata.first.color = data.first.color;
                        _paymsgdata.keyword1.value = data.keyword1.value;
                        _paymsgdata.keyword1.color = data.keyword1.color;
                        _paymsgdata.keyword2.value = data.keyword2.value;
                        _paymsgdata.keyword2.color = data.keyword2.color;
                        _paymsgdata.remark.value = data.remark.value;
                        _paymsgdata.remark.color = data.remark.color;
                        msgdata = _paymsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.EditLoginPassWord:
                        #region 修改登录密码
                        var _loginmsgdata = new WX_MsgTemplateKey2DataModel();
                        _loginmsgdata.first.value = data.first.value;
                        _loginmsgdata.first.color = data.first.color;
                        _loginmsgdata.keyword1.value = data.keyword1.value;
                        _loginmsgdata.keyword1.color = data.keyword1.color;
                        _loginmsgdata.keyword2.value = data.keyword2.value;
                        _loginmsgdata.keyword2.color = data.keyword2.color;
                        _loginmsgdata.remark.value = data.remark.value;
                        _loginmsgdata.remark.color = data.remark.color;
                        msgdata = _loginmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderCreated:
                        #region 创建订单(买家)
                        var _ocmsgdata = new WX_MsgTemplateKey3DataModel();
                        _ocmsgdata.first.value = data.first.value;
                        _ocmsgdata.first.color = data.first.color;
                        _ocmsgdata.keyword1.value = data.keyword1.value;
                        _ocmsgdata.keyword1.color = data.keyword1.color;
                        _ocmsgdata.keyword2.value = data.keyword2.value;
                        _ocmsgdata.keyword2.color = data.keyword2.color;
                        _ocmsgdata.keyword3.value = data.keyword3.value;
                        _ocmsgdata.keyword3.color = data.keyword3.color;
                        _ocmsgdata.remark.value = data.remark.value;
                        _ocmsgdata.remark.color = data.remark.color;
                        msgdata = _ocmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderPay:
                        #region 订单支付(买家)
                        var _opmsgdata = new WX_MsgTemplateKey2DataModel();
                        _opmsgdata.first.value = data.first.value;
                        _opmsgdata.first.color = data.first.color;
                        _opmsgdata.keyword1.value = data.keyword1.value;
                        _opmsgdata.keyword1.color = data.keyword1.color;
                        _opmsgdata.keyword2.value = data.keyword2.value;
                        _opmsgdata.keyword2.color = data.keyword2.color;
                        _opmsgdata.remark.value = data.remark.value;
                        _opmsgdata.remark.color = data.remark.color;
                        msgdata = _opmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.ShopOrderShipping:
                        #region 发货提醒(商家)
                        var _spmsgdata = new WX_MsgTemplateKey4DataModel();
                        _spmsgdata.first.value = data.first.value;
                        _spmsgdata.first.color = data.first.color;
                        _spmsgdata.keyword1.value = data.keyword1.value;
                        _spmsgdata.keyword1.color = data.keyword1.color;
                        _spmsgdata.keyword2.value = data.keyword2.value;
                        _spmsgdata.keyword2.color = data.keyword2.color;
                        _spmsgdata.keyword3.value = data.keyword3.value;
                        _spmsgdata.keyword3.color = data.keyword3.color;
                        _spmsgdata.keyword4.value = data.keyword4.value;
                        _spmsgdata.keyword4.color = data.keyword4.color;
                        _spmsgdata.remark.value = data.remark.value;
                        _spmsgdata.remark.color = data.remark.color;
                        msgdata = _spmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderShipping:
                        #region 订单发货(买家)
                        var _osmsgdata = new WX_MsgTemplateKey3DataModel();
                        _osmsgdata.first.value = data.first.value;
                        _osmsgdata.first.color = data.first.color;
                        _osmsgdata.keyword1.value = data.keyword1.value;
                        _osmsgdata.keyword1.color = data.keyword1.color;
                        _osmsgdata.keyword2.value = data.keyword2.value;
                        _osmsgdata.keyword2.color = data.keyword2.color;
                        _osmsgdata.keyword3.value = data.keyword3.value;
                        _osmsgdata.keyword3.color = data.keyword3.color;
                        _osmsgdata.remark.value = data.remark.value;
                        _osmsgdata.remark.color = data.remark.color;
                        msgdata = _osmsgdata;
                        break;
                    #endregion
                    //case Core.Plugins.Message.MessageTypeEnum.OrderRefundApply:
                    //    #region 退款申请(买家)
                    //    var _oraemsgdata = new WX_MsgTemplateKey3DataModel();
                    //    _oraemsgdata.first.value = data.first.value;
                    //    _oraemsgdata.first.color = data.first.color;
                    //    _oraemsgdata.keyword1.value = data.keyword1.value;
                    //    _oraemsgdata.keyword1.color = data.keyword1.color;
                    //    _oraemsgdata.keyword2.value = data.keyword2.value;
                    //    _oraemsgdata.keyword2.color = data.keyword2.color;
                    //    _oraemsgdata.keyword3.value = data.keyword3.value;
                    //    _oraemsgdata.keyword3.color = data.keyword3.color;
                    //    _oraemsgdata.remark.value = data.remark.value;
                    //    _oraemsgdata.remark.color = data.remark.color;
                    //    msgdata = _oraemsgdata;
                    //    break;
                    //#endregion
                    //case Core.Plugins.Message.MessageTypeEnum.OrderReturnApply:
                    //    #region 退货申请(买家)
                    //    var _oramsgdata = new WX_MsgTemplateKey4DataModel();
                    //    _oramsgdata.first.value = data.first.value;
                    //    _oramsgdata.first.color = data.first.color;
                    //    _oramsgdata.keyword1.value = data.keyword1.value;
                    //    _oramsgdata.keyword1.color = data.keyword1.color;
                    //    _oramsgdata.keyword2.value = data.keyword2.value;
                    //    _oramsgdata.keyword2.color = data.keyword2.color;
                    //    _oramsgdata.keyword3.value = data.keyword3.value;
                    //    _oramsgdata.keyword3.color = data.keyword3.color;
                    //    _oramsgdata.keyword4.value = data.keyword4.value;
                    //    _oramsgdata.keyword4.color = data.keyword4.color;
                    //    _oramsgdata.remark.value = data.remark.value;
                    //    _oramsgdata.remark.color = data.remark.color;
                    //    msgdata = _oramsgdata;
                    //    break;
                    //#endregion                        
                    case Core.Plugins.Message.MessageTypeEnum.OrderRefundSuccess:
                        #region 退款成功(买家)
                        var _ormsgdata = new WX_MsgTemplateKey3DataModel();
                        _ormsgdata.first.value = data.first.value;
                        _ormsgdata.first.color = data.first.color;
                        _ormsgdata.keyword1.value = data.keyword1.value;
                        _ormsgdata.keyword1.color = data.keyword1.color;
                        _ormsgdata.keyword2.value = data.keyword2.value;
                        _ormsgdata.keyword2.color = data.keyword2.color;
                        _ormsgdata.keyword3.value = data.keyword3.value;
                        _ormsgdata.keyword3.color = data.keyword3.color;
                        _ormsgdata.remark.value = data.remark.value;
                        _ormsgdata.remark.color = data.remark.color;
                        msgdata = _ormsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderReturnSuccess:
                        #region 退货成功(买家)
                        var _oremsgdata = new WX_MsgTemplateKey4DataModel();
                        _oremsgdata.first.value = data.first.value;
                        _oremsgdata.first.color = data.first.color;
                        _oremsgdata.keyword1.value = data.keyword1.value;
                        _oremsgdata.keyword1.color = data.keyword1.color;
                        _oremsgdata.keyword2.value = data.keyword2.value;
                        _oremsgdata.keyword2.color = data.keyword2.color;
                        _oremsgdata.keyword3.value = data.keyword3.value;
                        _oremsgdata.keyword3.color = data.keyword3.color;
                        _oremsgdata.keyword4.value = data.keyword4.value;
                        _oremsgdata.keyword4.color = data.keyword4.color;
                        _oremsgdata.remark.value = data.remark.value;
                        _oremsgdata.remark.color = data.remark.color;
                        msgdata = _oremsgdata;
                        break;
                    #endregion                  
                    case Core.Plugins.Message.MessageTypeEnum.OrderRefundFail:
                        #region 退款失败通知(买家)
                        var _RefundFaildata = new WX_MsgTemplateKey4DataModel();
                        _RefundFaildata.first.value = data.first.value;
                        _RefundFaildata.first.color = data.first.color;
                        _RefundFaildata.keyword1.value = data.keyword1.value;//交易单号
                        _RefundFaildata.keyword1.color = data.keyword1.color;
                        _RefundFaildata.keyword2.value = data.keyword2.value;//商品名称
                        _RefundFaildata.keyword2.color = data.keyword2.color;
                        _RefundFaildata.keyword3.value = data.keyword3.value;//退款金额
                        _RefundFaildata.keyword3.color = data.keyword3.color;
                        _RefundFaildata.keyword4.value = data.keyword4.value;//退款时间
                        _RefundFaildata.keyword4.color = data.keyword4.color;
                        _RefundFaildata.remark.value = "";
                        _RefundFaildata.remark.color = data.remark.color;
                        msgdata = _RefundFaildata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderReturnFail:
                        #region 退货失败通知(买家)
                        var _ReturnFaildata = new WX_MsgTemplateKey4DataModel();
                        _ReturnFaildata.first.value = data.first.value;
                        _ReturnFaildata.first.color = data.first.color;
                        _ReturnFaildata.keyword1.value = data.keyword1.value;//交易单号
                        _ReturnFaildata.keyword1.color = data.keyword1.color;
                        _ReturnFaildata.keyword2.value = data.keyword2.value;//交易单号
                        _ReturnFaildata.keyword2.color = data.keyword2.color;
                        _ReturnFaildata.keyword3.value = data.keyword3.value;
                        _ReturnFaildata.keyword3.color = data.keyword3.color;
                        _ReturnFaildata.keyword4.value = data.keyword4.value;
                        _ReturnFaildata.keyword4.color = data.keyword4.color;
                        _ReturnFaildata.remark.value = data.remark.value;
                        _ReturnFaildata.remark.color = data.remark.color;
                        msgdata = _ReturnFaildata;
                        break;
                    #endregion                  
                    case Core.Plugins.Message.MessageTypeEnum.FightGroupOpenSuccess:
                        #region 开团成功
                        var _fgosmsgdata = new WX_MsgTemplateKey3DataModel();
                        _fgosmsgdata.first.value = data.first.value;
                        _fgosmsgdata.first.color = data.first.color;
                        _fgosmsgdata.keyword1.value = data.keyword1.value;
                        _fgosmsgdata.keyword1.color = data.keyword1.color;
                        _fgosmsgdata.keyword2.value = data.keyword2.value;
                        _fgosmsgdata.keyword2.color = data.keyword2.color;
                        _fgosmsgdata.keyword3.value = data.keyword3.value;
                        _fgosmsgdata.keyword3.color = data.keyword3.color;
                        _fgosmsgdata.remark.value = data.remark.value;
                        _fgosmsgdata.remark.color = data.remark.color;
                        msgdata = _fgosmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.FightGroupJoinSuccess:
                        #region 参团成功
                        var _fgjsmsgdata = new WX_MsgTemplateKey3DataModel();
                        _fgjsmsgdata.first.value = data.first.value;
                        _fgjsmsgdata.first.color = data.first.color;
                        _fgjsmsgdata.keyword1.value = data.keyword1.value;
                        _fgjsmsgdata.keyword1.color = data.keyword1.color;
                        _fgjsmsgdata.keyword2.value = data.keyword2.value;
                        _fgjsmsgdata.keyword2.color = data.keyword2.color;
                        _fgjsmsgdata.keyword3.value = data.keyword3.value;
                        _fgjsmsgdata.keyword3.color = data.keyword3.color;
                        _fgjsmsgdata.remark.value = data.remark.value;
                        _fgjsmsgdata.remark.color = data.remark.color;
                        msgdata = _fgjsmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.FightGroupFailed:
                        #region 拼团失败
                        var _fgfmsgdata = new WX_MsgTemplateKey3DataModel();
                        _fgfmsgdata.first.value = data.first.value;
                        _fgfmsgdata.first.color = data.first.color;
                        _fgfmsgdata.keyword1.value = data.keyword1.value;
                        _fgfmsgdata.keyword1.color = data.keyword1.color;
                        _fgfmsgdata.keyword2.value = data.keyword2.value;
                        _fgfmsgdata.keyword2.color = data.keyword2.color;
                        _fgfmsgdata.remark.value = data.remark.value;
                        _fgfmsgdata.remark.color = data.remark.color;
                        msgdata = _fgfmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.FightGroupSuccess:
                        #region 拼团成功
                        var _fgsmsgdata = new WX_MsgTemplateKey2DataModel();
                        _fgsmsgdata.first.value = data.first.value;
                        _fgsmsgdata.first.color = data.first.color;
                        _fgsmsgdata.keyword1.value = data.keyword1.value;
                        _fgsmsgdata.keyword1.color = data.keyword1.color;
                        _fgsmsgdata.keyword2.value = data.keyword2.value;
                        _fgsmsgdata.keyword2.color = data.keyword2.color;
                        _fgsmsgdata.remark.value = data.remark.value;
                        _fgsmsgdata.remark.color = data.remark.color;
                        msgdata = _fgsmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.SelfTakeOrderPay:
                        #region 自提订单支付成功
                        var _selftakedata = new WX_MsgTemplateKey4DataModel();
                        _selftakedata.first.value = data.first.value;
                        _selftakedata.first.color = data.first.color;
                        _selftakedata.keyword1.value = data.keyword1.value;
                        _selftakedata.keyword1.color = data.keyword1.color;
                        _selftakedata.keyword2.value = data.keyword2.value;
                        _selftakedata.keyword2.color = data.keyword2.color;
                        _selftakedata.keyword3.value = data.keyword3.value;
                        _selftakedata.keyword3.color = data.keyword3.color;
                        _selftakedata.keyword4.value = data.keyword4.value;
                        _selftakedata.keyword4.color = data.keyword4.color;
                        _selftakedata.remark.value = data.remark.value;
                        _selftakedata.remark.color = data.remark.color;
                        msgdata = _selftakedata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.AlreadyVerification:
                        #region 自提订单核销成功(买家)
                        var _verdata = new WX_MsgTemplateKey4DataModel();
                        _verdata.first.value = data.first.value;
                        _verdata.first.color = data.first.color;
                        _verdata.keyword1.value = data.keyword1.value;
                        _verdata.keyword1.color = data.keyword1.color;
                        _verdata.keyword2.value = data.keyword2.value;
                        _verdata.keyword2.color = data.keyword2.color;
                        _verdata.keyword3.value = data.keyword3.value;
                        _verdata.keyword3.color = data.keyword3.color;
                        _verdata.keyword4.value = data.keyword4.value;
                        _verdata.keyword4.color = data.keyword4.color;
                        _verdata.remark.value = data.remark.value;
                        _verdata.remark.color = data.remark.color;
                        msgdata = _verdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.MemberWithDrawApply:
                        #region 会员提现申请
                        var _wdmsgdata = new WX_MsgTemplateKey3DataModel();
                        _wdmsgdata.first.value = data.first.value;
                        _wdmsgdata.first.color = data.first.color;
                        _wdmsgdata.keyword1.value = data.keyword1.value;
                        _wdmsgdata.keyword1.color = data.keyword1.color;
                        _wdmsgdata.keyword2.value = data.keyword2.value;
                        _wdmsgdata.keyword2.color = data.keyword2.color;
                        _wdmsgdata.keyword3.value = data.keyword3.value;
                        _wdmsgdata.keyword3.color = data.keyword3.color;
                        _wdmsgdata.remark.value = data.remark.value;
                        _wdmsgdata.remark.color = data.remark.color;
                        msgdata = _wdmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.MemberWithDrawSuccess:
                        #region 会员提现成功
                        var _wdsmsgdata = new WX_MsgTemplateKey3DataModel();
                        _wdsmsgdata.first.value = data.first.value;
                        _wdsmsgdata.first.color = data.first.color;
                        _wdsmsgdata.keyword1.value = data.keyword1.value;
                        _wdsmsgdata.keyword1.color = data.keyword1.color;
                        _wdsmsgdata.keyword2.value = data.keyword2.value;
                        _wdsmsgdata.keyword2.color = data.keyword2.color;
                        _wdsmsgdata.keyword3.value = data.keyword3.value;
                        _wdsmsgdata.keyword3.color = data.keyword3.color;
                        _wdsmsgdata.remark.value = "";
                        _wdsmsgdata.remark.color = data.remark.color;
                        msgdata = _wdsmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.MemberWithDrawFail:
                        #region 会员提现失败
                        var _wdfmsgdata = new WX_MsgTemplateKey3DataModel();
                        _wdfmsgdata.first.value = data.first.value;
                        _wdfmsgdata.first.color = data.first.color;
                        _wdfmsgdata.keyword1.value = data.keyword1.value;
                        _wdfmsgdata.keyword1.color = data.keyword1.color;
                        _wdfmsgdata.keyword2.value = data.keyword2.value;
                        _wdfmsgdata.keyword2.color = data.keyword2.color;
                        _wdfmsgdata.keyword3.value = data.keyword3.value;
                        _wdfmsgdata.keyword3.color = data.keyword3.color;
                        _wdfmsgdata.remark.value = "";
                        _wdfmsgdata.remark.color = data.remark.color;
                        msgdata = _wdfmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.VirtualOrderPay:
                        #region 虚拟商品购买成功
                        var _ovcmsgdata = new WX_MsgTemplateKey5DataModel();
                        _ovcmsgdata.first.value = data.first.value;
                        _ovcmsgdata.first.color = data.first.color;
                        _ovcmsgdata.keyword1.value = data.keyword1.value;
                        _ovcmsgdata.keyword1.color = data.keyword1.color;
                        _ovcmsgdata.keyword2.value = data.keyword2.value;
                        _ovcmsgdata.keyword2.color = data.keyword2.color;
                        _ovcmsgdata.keyword3.value = data.keyword3.value;
                        _ovcmsgdata.keyword3.color = data.keyword3.color;
                        _ovcmsgdata.keyword4.value = data.keyword4.value;
                        _ovcmsgdata.keyword4.color = data.keyword4.color;
                        _ovcmsgdata.keyword5.value = data.keyword5.value;
                        _ovcmsgdata.keyword5.color = data.keyword5.color;
                        _ovcmsgdata.remark.value = data.remark.value;
                        _ovcmsgdata.remark.color = data.remark.color;
                        msgdata = _ovcmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.VirtualAlreadyVerification:
                        #region 虚拟订单核销成功
                        var _ovscmsgdata = new WX_MsgTemplateKey4DataModel();
                        _ovscmsgdata.first.value = data.first.value;
                        _ovscmsgdata.first.color = data.first.color;
                        _ovscmsgdata.keyword1.value = data.keyword1.value;
                        _ovscmsgdata.keyword1.color = data.keyword1.color;
                        _ovscmsgdata.keyword2.value = data.keyword2.value;
                        _ovscmsgdata.keyword2.color = data.keyword2.color;
                        _ovscmsgdata.keyword3.value = data.keyword3.value;
                        _ovscmsgdata.keyword3.color = data.keyword3.color;
                        _ovscmsgdata.keyword4.value = data.keyword4.value;
                        _ovscmsgdata.keyword4.color = data.keyword4.color;
                        _ovscmsgdata.remark.value = data.remark.value;
                        _ovscmsgdata.remark.color = data.remark.color;
                        msgdata = _ovscmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.ShopDelivering:
                        #region 售后申请(卖家)
                        var _sdmsgdata = new WX_MsgTemplateKey2DataModel();
                        _sdmsgdata.first.value = data.first.value;
                        _sdmsgdata.first.color = data.first.color;
                        _sdmsgdata.keyword1.value = data.keyword1.value;
                        _sdmsgdata.keyword1.color = data.keyword1.color;
                        _sdmsgdata.keyword2.value = data.keyword2.value;
                        _sdmsgdata.keyword2.color = data.keyword2.color;
                        _sdmsgdata.remark.value = data.remark.value;
                        _sdmsgdata.remark.color = data.remark.color;
                        msgdata = _sdmsgdata;
                        break;
                    #endregion
                    #region TDO:ZYF 去掉不需要的微信消息模版
                    //case Core.Plugins.Message.MessageTypeEnum.RefundDeliver:
                    //    #region 退款退货(买家)
                    //    var _rdmsgdata = new WX_MsgTemplateKey5DataModel();
                    //    _rdmsgdata.first.value = data.first.value;
                    //    _rdmsgdata.first.color = data.first.color;
                    //    _rdmsgdata.keyword1.value = data.keyword1.value;
                    //    _rdmsgdata.keyword1.color = data.keyword1.color;
                    //    _rdmsgdata.keyword2.value = data.keyword2.value;
                    //    _rdmsgdata.keyword2.color = data.keyword2.color;
                    //    _rdmsgdata.keyword3.value = data.keyword3.value;
                    //    _rdmsgdata.keyword3.color = data.keyword3.color;
                    //    _rdmsgdata.keyword4.value = data.keyword4.value;
                    //    _rdmsgdata.keyword4.color = data.keyword4.color;
                    //    _rdmsgdata.keyword5.value = data.keyword5.value;
                    //    _rdmsgdata.keyword5.color = data.keyword5.color;
                    //    _rdmsgdata.remark.value = data.remark.value;
                    //    _rdmsgdata.remark.color = data.remark.color;
                    //    msgdata = _rdmsgdata;
                    //    break;
                    //#endregion
                    //case Core.Plugins.Message.MessageTypeEnum.ShopHaveNewOrder:
                    //    #region 店铺有新订单(卖家)
                    //    var _shnomsgdata = new WX_MsgTemplateKey5DataModel();
                    //    _shnomsgdata.first.value = data.first.value;
                    //    _shnomsgdata.first.color = data.first.color;
                    //    _shnomsgdata.keyword1.value = data.keyword1.value;
                    //    _shnomsgdata.keyword1.color = data.keyword1.color;
                    //    _shnomsgdata.keyword2.value = data.keyword2.value;
                    //    _shnomsgdata.keyword2.color = data.keyword2.color;
                    //    _shnomsgdata.keyword3.value = data.keyword3.value;
                    //    _shnomsgdata.keyword3.color = data.keyword3.color;
                    //    _shnomsgdata.keyword4.value = data.keyword4.value;
                    //    _shnomsgdata.keyword4.color = data.keyword4.color;
                    //    _shnomsgdata.keyword5.value = data.keyword5.value;
                    //    _shnomsgdata.keyword5.color = data.keyword5.color;
                    //    _shnomsgdata.remark.value = data.remark.value;
                    //    _shnomsgdata.remark.color = data.remark.color;
                    //    msgdata = _shnomsgdata;
                    //    break;
                    //#endregion
                    //case Core.Plugins.Message.MessageTypeEnum.ReceiveBonus:
                    //    #region 领取现金红包
                    //    var _rbmsgdata = new WX_MsgTemplateKey3DataModel();
                    //    _rbmsgdata.first.value = data.first.value;
                    //    _rbmsgdata.first.color = data.first.color;
                    //    _rbmsgdata.keyword1.value = data.keyword1.value;
                    //    _rbmsgdata.keyword1.color = data.keyword1.color;
                    //    _rbmsgdata.keyword2.value = data.keyword2.value;
                    //    _rbmsgdata.keyword2.color = data.keyword2.color;
                    //    _rbmsgdata.keyword3.value = data.keyword3.value;
                    //    _rbmsgdata.keyword3.color = data.keyword3.color;
                    //    _rbmsgdata.remark.value = data.remark.value;
                    //    _rbmsgdata.remark.color = data.remark.color;
                    //    msgdata = _rbmsgdata;
                    //    break;
                    //#endregion
                    //case Core.Plugins.Message.MessageTypeEnum.LimitTimeBuy:
                    //    #region 限时购开始
                    //    var _ltbmsgdata = new WX_MsgTemplateKey2DataModel();
                    //    //_ltbmsgdata.first.value = data.first.value;
                    //    //_ltbmsgdata.first.color = data.first.color;
                    //    //_ltbmsgdata.keyword1.value = data.keyword1.value;
                    //    //_ltbmsgdata.keyword1.color = data.keyword1.color;
                    //    //_ltbmsgdata.keyword2.value = data.keyword2.value;
                    //    //_ltbmsgdata.keyword2.color = data.keyword2.color;
                    //    //_ltbmsgdata.keyword3.value = data.keyword3.value;
                    //    //_ltbmsgdata.keyword3.color = data.keyword3.color;
                    //    //_ltbmsgdata.remark.value = data.remark.value;
                    //    //_ltbmsgdata.remark.color = data.remark.color;

                    //    _ltbmsgdata.first.value = data.first.value;
                    //    _ltbmsgdata.first.color = data.first.color;
                    //    _ltbmsgdata.keyword2.value = data.keyword2.value;
                    //    _ltbmsgdata.keyword2.color = data.keyword2.color;
                    //    _ltbmsgdata.remark.value = data.remark.value;
                    //    _ltbmsgdata.remark.color = data.remark.color;

                    //    msgdata = _ltbmsgdata;
                    //    break;
                    //#endregion
                    //case Core.Plugins.Message.MessageTypeEnum.SubscribeLimitTimeBuy:
                    //    #region 订阅限时购
                    //    var _sltbmsgdata = new WX_MsgTemplateKey3DataModel();
                    //    _sltbmsgdata.first.value = data.first.value;
                    //    _sltbmsgdata.first.color = data.first.color;
                    //    _sltbmsgdata.keyword1.value = data.keyword1.value;
                    //    _sltbmsgdata.keyword1.color = data.keyword1.color;
                    //    _sltbmsgdata.keyword2.value = data.keyword2.value;
                    //    _sltbmsgdata.keyword2.color = data.keyword2.color;
                    //    _sltbmsgdata.keyword3.value = data.keyword3.value;
                    //    _sltbmsgdata.keyword3.color = data.keyword3.color;
                    //    _sltbmsgdata.remark.value = data.remark.value;
                    //    _sltbmsgdata.remark.color = data.remark.color;
                    //    msgdata = _sltbmsgdata;
                    //    break;
                    //#endregion
                    //case Core.Plugins.Message.MessageTypeEnum.FightGroupNewJoin:
                    //    #region 新成员参团成功
                    //    //var _fgnjmsgdata = new WX_MsgFightGroupNewJoinDataModel();
                    //    //_fgnjmsgdata.first.value = data.first.value;
                    //    //_fgnjmsgdata.first.color = data.first.color;
                    //    //_fgnjmsgdata.time.value = data.keyword1.value;
                    //    //_fgnjmsgdata.time.color = data.keyword1.color;
                    //    //_fgnjmsgdata.number.value = data.keyword2.value;
                    //    //_fgnjmsgdata.number.color = data.keyword2.color;
                    //    //_fgnjmsgdata.remark.value = data.remark.value;
                    //    //_fgnjmsgdata.remark.color = data.remark.color;

                    //    var _fgnjmsgdata = new WX_MsgTemplateKey5DataModel();
                    //    _fgnjmsgdata.first.value = data.first.value;
                    //    _fgnjmsgdata.first.color = data.first.color;
                    //    _fgnjmsgdata.keyword1.value = data.keyword1.value;
                    //    _fgnjmsgdata.keyword1.color = data.keyword1.color;
                    //    _fgnjmsgdata.keyword2.value = data.keyword2.value;
                    //    _fgnjmsgdata.keyword2.color = data.keyword2.color;
                    //    _fgnjmsgdata.keyword3.value = data.keyword3.value;
                    //    _fgnjmsgdata.keyword3.color = data.keyword3.color;
                    //    //_fgnjmsgdata.keyword4.value = data.keyword4.value;
                    //    //_fgnjmsgdata.keyword4.color = data.keyword4.color;
                    //    _fgnjmsgdata.keyword5.value = data.keyword5.value;
                    //    _fgnjmsgdata.keyword5.color = data.keyword5.color;
                    //    _fgnjmsgdata.remark.value = data.remark.value;
                    //    _fgnjmsgdata.remark.color = data.remark.color;

                    //    msgdata = _fgnjmsgdata;
                    //    #endregion
                    //    break;
                    //case Core.Plugins.Message.MessageTypeEnum.GetBrokerage:
                    //    #region 获得佣金
                    //    var _bkdata = new WX_MsgTemplateKey4DataModel();
                    //    _bkdata.first.value = data.first.value;
                    //    _bkdata.first.color = data.first.color;
                    //    _bkdata.keyword1.value = data.keyword1.value;
                    //    _bkdata.keyword1.color = data.keyword1.color;
                    //    _bkdata.keyword2.value = data.keyword2.value;
                    //    _bkdata.keyword2.color = data.keyword2.color;
                    //    //_bkdata.keyword3.value = data.keyword3.value;
                    //    //_bkdata.keyword3.color = data.keyword3.color;
                    //    //_bkdata.keyword4.value = data.keyword4.value;
                    //    //_bkdata.keyword4.color = data.keyword4.color;
                    //    _bkdata.remark.value = data.remark.value;
                    //    _bkdata.remark.color = data.remark.color;
                    //    msgdata = _bkdata;
                    //    break;
                    //#endregion
                    #endregion
                    #region 分销
                    case Core.Plugins.Message.MessageTypeEnum.DistributorApply:
                        #region 分销：申请成为销售员
                        var _damsgdata = new WX_MsgTemplateKey2DataModel();
                        _damsgdata.first.value = data.first.value;
                        _damsgdata.first.color = data.first.color;
                        _damsgdata.keyword1.value = data.keyword1.value;
                        _damsgdata.keyword1.color = data.keyword1.color;
                        _damsgdata.keyword2.value = data.keyword2.value;
                        _damsgdata.keyword2.color = data.keyword2.color;
                        _damsgdata.remark.value = data.remark.value;
                        _damsgdata.remark.color = data.remark.color;
                        msgdata = _damsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.DistributorAuditSuccess:
                        #region 分销：申请审核通过
                        var _daasmsgdata = new WX_MsgTemplateKey2DataModel();
                        _daasmsgdata.first.value = data.first.value;
                        _daasmsgdata.first.color = data.first.color;
                        _daasmsgdata.keyword1.value = data.keyword1.value;
                        _daasmsgdata.keyword1.color = data.keyword1.color;
                        _daasmsgdata.keyword2.value = data.keyword2.value;
                        _daasmsgdata.keyword2.color = data.keyword2.color;
                        _daasmsgdata.remark.value = data.remark.value;
                        _daasmsgdata.remark.color = data.remark.color;
                        msgdata = _daasmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.DistributorAuditFail:
                        #region 分销：申请审核拒绝
                        var _daafmsgdata = new WX_MsgTemplateKey2DataModel();
                        _daafmsgdata.first.value = data.first.value;
                        _daafmsgdata.first.color = data.first.color;
                        _daafmsgdata.keyword1.value = data.keyword1.value;
                        _daafmsgdata.keyword1.color = data.keyword1.color;
                        _daafmsgdata.keyword2.value = data.keyword2.value;
                        _daafmsgdata.keyword2.color = data.keyword2.color;
                        _daafmsgdata.remark.value = data.remark.value;
                        _daafmsgdata.remark.color = data.remark.color;
                        msgdata = _daafmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.DistributorNewJoin:
                        #region 分销：会员发展成功
                        var _dnjmsgdata = new WX_MsgTemplateKey2DataModel();
                        _dnjmsgdata.first.value = data.first.value;
                        _dnjmsgdata.first.color = data.first.color;
                        _dnjmsgdata.keyword1.value = data.keyword1.value;
                        _dnjmsgdata.keyword1.color = data.keyword1.color;
                        _dnjmsgdata.keyword2.value = data.keyword2.value;
                        _dnjmsgdata.keyword2.color = data.keyword2.color;
                        _dnjmsgdata.remark.value = data.remark.value;
                        _dnjmsgdata.remark.color = data.remark.color;
                        msgdata = _dnjmsgdata;
                        #endregion
                        break;
                    case Core.Plugins.Message.MessageTypeEnum.DistributorCommissionSettled:
                        #region 分销：有已结算佣金时
                        var _dcsmsgdata = new WX_MsgTemplateKey2DataModel();
                        _dcsmsgdata.first.value = data.first.value;
                        _dcsmsgdata.first.color = data.first.color;
                        _dcsmsgdata.keyword1.value = data.keyword1.value;
                        _dcsmsgdata.keyword1.color = data.keyword1.color;
                        _dcsmsgdata.keyword2.value = data.keyword2.value;
                        _dcsmsgdata.keyword2.color = data.keyword2.color;
                        _dcsmsgdata.remark.value = data.remark.value;
                        _dcsmsgdata.remark.color = data.remark.color;
                        msgdata = _dcsmsgdata;
                        #endregion
                        break;
                    #endregion
                    default:
                        throw new HimallException("无此模板消息，不能完成消息推送");
                        break;
                }
#if DEBUG
                Core.Log.Info("[模板消息]开始发送到openid:" + openId + "||type:" + type.GetHashCode());
#endif
                var wxhelper = new Weixin.WXHelper();
                var accessToken = wxhelper.GetAccessToken(appid, appsecret);
#if DEBUG
                Core.Log.Info("[模板消息]取到Token");
#endif
                var _result = TemplateApi.SendTemplateMessage(accessToken, openId, templateId, url, msgdata);
                Log.Debug("发消息返回的数据" + _result.ToJSON());
                //小程序发送消息接口
                // Senparc.Weixin.WxOpen.AdvancedAPIs.Template.TemplateApi.SendTemplateMessage(accessToken, openId, templateId,  msgdata,formId:"");
                Log.Info("[模板消息]发送返回");
                if (_result.errcode != Senparc.Weixin.ReturnCode.请求成功)
                {
                    Core.Log.Info(_result.errcode.ToString() + ":" + _result.errmsg);
                }

#if DEBUG
                Core.Log.Info("[模板消息]发送结束");
#endif
            }
            else
            {

#if DEBUG
                Core.Log.Info("[模板消息]发送失败：数据验证未通过-" + dataerr + "[" + type.ToDescription() + "]");
#endif
            }
        }

        /// <summary>
        /// 获取模板消息跳转URL
        /// </summary>
        /// <param name="type"></param>
        public string GetMessageTemplateShowUrl(Himall.Core.Plugins.Message.MessageTypeEnum type)
        {
            string result = "";
            var _tmplinkdata = WX_MsgTemplateLinkData.GetList().FirstOrDefault(d => d.MsgType == type);
            if (_tmplinkdata != null)
            {
                if (!string.IsNullOrWhiteSpace(_tmplinkdata.ReturnUrl))
                {
                    result = Himall.ServiceProvider.Instance<SiteSettingService>.Create.GetCurDomainUrl();
                    result += _tmplinkdata.ReturnUrl;
                }
            }
            return result;
        }
        /// <summary>
        /// 取当前用户对应平台的OpenId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private string GetPlatformOpenIdByUserId(long userId)
        {
            string result = "";
            if (userId > 0)
            {
                var _tmp = DbFactory.Default.Get<Entities.MemberOpenIdInfo>().Where(mo =>
                mo.ServiceProvider == "Himall.Plugin.OAuth.WeiXin" &&
                mo.AppIdType == Entities.MemberOpenIdInfo.AppIdTypeEnum.Payment && mo.UserId == userId).FirstOrDefault();
                //var _tmp = (from mo in Context.MemberOpenIdInfo
                //            where mo.ServiceProvider == "Himall.Plugin.OAuth.WeiXin" && mo.AppIdType == MemberOpenIdInfo.AppIdTypeEnum.Payment
                //            select mo).FirstOrDefault(d => d.UserId == userId);
                if (_tmp != null)
                {
                    result = _tmp.OpenId;
                }
            }
            return result;
        }
        /// <summary>
        /// 添加消息模板
        /// </summary>
        /// <param name="type">null表示所有都重置</param>
        public void AddMessageTemplate(Himall.Core.Plugins.Message.MessageTypeEnum? type = null)
        {
            var siteSetting = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings;
            string appid = siteSetting.WeixinAppId;
            string appsecret = siteSetting.WeixinAppSecret;
            var wxhelper = new Weixin.WXHelper();
            var accessToken = wxhelper.GetAccessToken(appid, appsecret);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new HimallException("获取Token失败");
            }
            List<Entities.WeiXinMsgTemplateInfo> msgtmplist = new List<Entities.WeiXinMsgTemplateInfo>();
            if (type != null)
            {
                Entities.WeiXinMsgTemplateInfo _tmp = GetWeiXinMsgTemplate(type.Value);
                if (_tmp != null)
                {
                    msgtmplist.Add(_tmp);
                }
            }
            else
            {
                msgtmplist = GetWeiXinMsgTemplateList();
            }
            foreach (var item in msgtmplist)
            {
                var rdata = TemplateApi.Addtemplate(accessToken, item.TemplateNum);
                if (rdata.errcode == Senparc.Weixin.ReturnCode.请求成功)
                {
                    item.TemplateId = rdata.template_id;
                }
                else
                {
                    item.TemplateId = "";   //重置失败会清理之前的值
                    Core.Log.Info("[重置消息模板]" + item.MessageType.ToString() + ":" + rdata.errcode.ToString() + " - " + rdata.errmsg);
                }
                item.UpdateDate = DateTime.Now;
                DbFactory.Default.Update(item);
            }
        }
        #endregion

        public void UpdateWXsmallMessage(IEnumerable<KeyValuePair<string, string>> items)
        {
            foreach (var model in items)
            {
                Entities.WeiXinMsgTemplateInfo data = GetWeiXinMsgTemplateById(long.Parse(model.Key));
                data.TemplateId = model.Value;
                DbFactory.Default.Update(data);
            }
        }

        /// <summary>
        /// 更新小程序模板ID
        /// </summary>
        /// <param name="tempNum"></param>
        /// <param name="tempId"></param>
        /// <param name="messageType"></param>
        public void UpdateWXsmallTemplateId(string tempNum, string tempId, int messageType = 0)
        {
            if (messageType == 0)
                DbFactory.Default.Set<WeiXinMsgTemplateInfo>().Set(e => e.TemplateId, tempId).Where(e => e.TemplateNum == tempNum).Succeed();
            else
                DbFactory.Default.Set<WeiXinMsgTemplateInfo>().Set(e => e.TemplateId, tempId).Where(e => e.TemplateNum == tempNum && e.MessageType == messageType).Succeed();
        }

        /// <summary>
        /// 获取微信模板信息
        /// </summary>
        /// <returns></returns>
        public Entities.WeiXinMsgTemplateInfo GetWeiXinMsgTemplateById(long Id)
        {
            Entities.WeiXinMsgTemplateInfo result = DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().Where(d => d.Id == Id).FirstOrDefault();
            return result;
        }


        #region 小程序消息模版
        public string GetWXAppletMessageTemplateShowUrl(Himall.Core.Plugins.Message.MessageTypeEnum type)
        {
            string result = "";
            var _tmplinkdata = WXApplet_MsgTemplateLinkData.GetList().FirstOrDefault(d => d.MsgType == type);
            if (_tmplinkdata != null)
            {
                result = _tmplinkdata.ReturnUrl;
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 获取微信模板信息
        /// </summary>
        /// <returns></returns>
        public Entities.WeiXinMsgTemplateInfo GetWXAppletMsgTemplate(Himall.Core.Plugins.Message.MessageTypeEnum type)
        {
            int msgtype = (int)type;
            var applet = WXMsgTemplateType.Applet;
            Entities.WeiXinMsgTemplateInfo result = DbFactory.Default.Get<Entities.WeiXinMsgTemplateInfo>().Where(d => d.MessageType == msgtype && d.UserInWxApplet == applet).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// 小程序发送模板消息
        /// </summary>
        /// <param name="type"></param>
        /// <param name="userId">为0时使用openid</param>
        /// <param name="data">信息数据</param>
        /// <param name="url"></param>
        /// <param name="openid">与userid配合使用，userid为0时使用此字段</param>
        public void SendAppletMessageByTemplate(MessageTypeEnum type, long userId, WX_MsgTemplateSendDataModel data, string url = "", string wxopenid = "", string formId = "")
        {
            var siteSetting = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings;
            string appid = siteSetting.WeixinAppletId;
            string appsecret = siteSetting.WeixinAppletSecret;

            if (string.IsNullOrWhiteSpace(appid) || string.IsNullOrWhiteSpace(appsecret))
            {
                throw new HimallException("未配置微信公众信息");
            }
            string dataerr = "";
            bool isdataok = true;
            string openId = wxopenid;
            if (userId == 0)
            {
                if (string.IsNullOrWhiteSpace(wxopenid))
                {
                    throw new HimallException("小程序：错误的OpenId");
                }
                openId = wxopenid;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(wxopenid))
                {
                    openId = wxopenid;
                }
            }
            if (string.IsNullOrWhiteSpace(openId))
            {
                dataerr = "openid为空";
                isdataok = false;
            }
            var userinfo = DbFactory.Default.Get<Entities.MemberInfo>().Where(d => d.Id == userId).FirstOrDefault();
            //var userinfo = Context.UserMemberInfo.FirstOrDefault(d => d.Id == userId);
            if (userId != 0)
            {
                if (userinfo == null)
                {
                    dataerr = "用户信息未取到" + userId;
                    isdataok = false;
                }
            }
            var _msgtmplinfo = GetWeiXinMsgTemplateByApplet(type);
            if (_msgtmplinfo == null)
            {
                dataerr = "消息模板未取到";
                isdataok = false;
            }
            string templateId = "";
            string topcolor = "#000000";
            if (isdataok)
            {
                templateId = _msgtmplinfo.TemplateId;
                if (string.IsNullOrWhiteSpace(templateId))
                {
                    dataerr = "消息模板未取到";
                    isdataok = false;
                }
            }
            if (!_msgtmplinfo.IsOpen)
            {
                dataerr = "未开启";
                isdataok = false;
            }

            if (isdataok)
            {
                //TemplateMessageData msgdata;
                object msgdata;
                switch (type)
                {
                    case Core.Plugins.Message.MessageTypeEnum.OrderCreated:
                        #region 创建订单(买家)
                        var _ocmsgdata = new WX_MsgTemplateKey5DataModel();
                        _ocmsgdata.keyword1.value = data.keyword1.value;
                        _ocmsgdata.keyword1.color = data.keyword1.color;
                        _ocmsgdata.keyword2.value = data.keyword2.value;
                        _ocmsgdata.keyword2.color = data.keyword2.color;
                        _ocmsgdata.keyword3.value = data.keyword3.value;
                        _ocmsgdata.keyword3.color = data.keyword3.color;
                        _ocmsgdata.keyword4.value = data.keyword4.value;
                        _ocmsgdata.keyword4.color = data.keyword4.color;
                        _ocmsgdata.keyword5.value = data.keyword5.value;
                        _ocmsgdata.keyword5.color = data.keyword5.color;
                        msgdata = _ocmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderPay:
                        #region 订单支付(买家)
                        var _opmsgdata = new WX_MsgTemplateKey5DataModel();
                        _opmsgdata.keyword1.value = data.keyword1.value;
                        _opmsgdata.keyword1.color = data.keyword1.color;
                        _opmsgdata.keyword2.value = data.keyword2.value;
                        _opmsgdata.keyword2.color = data.keyword2.color;
                        _opmsgdata.keyword3.value = data.keyword3.value;
                        _opmsgdata.keyword3.color = data.keyword3.color;
                        _opmsgdata.keyword4.value = data.keyword4.value;
                        _opmsgdata.keyword4.color = data.keyword4.color;
                        _opmsgdata.keyword5.value = data.keyword5.value;
                        _opmsgdata.keyword5.color = data.keyword5.color;
                        msgdata = _opmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderShipping:
                        #region 订单发货(买家)
                        var _osmsgdata = new WX_MsgTemplateKey6DataModel();
                        _osmsgdata.keyword1.value = data.keyword1.value;
                        _osmsgdata.keyword1.color = data.keyword1.color;
                        _osmsgdata.keyword2.value = data.keyword2.value;
                        _osmsgdata.keyword2.color = data.keyword2.color;
                        _osmsgdata.keyword3.value = data.keyword3.value;
                        _osmsgdata.keyword3.color = data.keyword3.color;
                        _osmsgdata.keyword4.value = data.keyword4.value;
                        _osmsgdata.keyword4.color = data.keyword4.color;
                        _osmsgdata.keyword5.value = data.keyword5.value;
                        _osmsgdata.keyword5.color = data.keyword5.color;
                        _osmsgdata.keyword6.value = data.keyword6.value;
                        _osmsgdata.keyword6.color = data.keyword6.color;
                        msgdata = _osmsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderRefundSuccess:
                        #region 退款退货(买家)
                        var _ormsgdata = new WX_MsgTemplateKey6DataModel();
                        _ormsgdata.keyword1.value = data.keyword1.value;
                        _ormsgdata.keyword1.color = data.keyword1.color;
                        _ormsgdata.keyword2.value = data.keyword2.value;
                        _ormsgdata.keyword2.color = data.keyword2.color;
                        _ormsgdata.keyword3.value = data.keyword3.value;
                        _ormsgdata.keyword3.color = data.keyword3.color;
                        _ormsgdata.keyword4.value = data.keyword4.value;
                        _ormsgdata.keyword4.color = data.keyword4.color;
                        _ormsgdata.keyword5.value = data.keyword5.value;
                        _ormsgdata.keyword5.color = data.keyword5.color;
                        _ormsgdata.keyword6.value = data.keyword6.value;
                        _ormsgdata.keyword6.color = data.keyword6.color;
                        msgdata = _ormsgdata;
                        break;
                    #endregion
                    case Core.Plugins.Message.MessageTypeEnum.OrderRefundFail:
                        #region 退款退货失败通知(买家)
                        var _rdmsgdata = new WX_MsgTemplateKey5DataModel();
                        _rdmsgdata.keyword1.value = data.keyword1.value;
                        _rdmsgdata.keyword1.color = data.keyword1.color;
                        _rdmsgdata.keyword2.value = data.keyword2.value;
                        _rdmsgdata.keyword2.color = data.keyword2.color;
                        _rdmsgdata.keyword3.value = data.keyword3.value;
                        _rdmsgdata.keyword3.color = data.keyword3.color;
                        _rdmsgdata.keyword4.value = data.keyword4.value;
                        _rdmsgdata.keyword4.color = data.keyword4.color;
                        msgdata = _rdmsgdata;
                        break;
                    #endregion
                    default:
                        throw new HimallException("小程序：无此模板消息，不能完成消息推送");
                        break;
                }
                var wxhelper = new Weixin.WXHelper();
                Log.Info("小程序：appid:" + appid + "，appsecret:" + appsecret);
                var accessToken = wxhelper.GetAccessToken(appid, appsecret);
                Core.Log.Info("[小程序：模版消息]发送开始:" + " openId<" + openId + "> Url=<" + url + "> formId<" + formId + ">");
                var _result = Senparc.Weixin.WxOpen.AdvancedAPIs.Template.TemplateApi.SendTemplateMessage(accessToken, openId, templateId, msgdata, formId, url);
                if (_result.errcode != Senparc.Weixin.ReturnCode.请求成功)
                {
                    Core.Log.Info("第一次不成功：" + _result.errcode.ToString() + ":" + _result.errmsg);
                    //不成功再发一次
                    _result = Senparc.Weixin.WxOpen.AdvancedAPIs.Template.TemplateApi.SendTemplateMessage(accessToken, openId, templateId, msgdata, formId, url);
                    if (_result.errcode != Senparc.Weixin.ReturnCode.请求成功)
                    {
                        Core.Log.Info("第二次不成功：" + _result.errcode.ToString() + ":" + _result.errmsg);
                        _result = Senparc.Weixin.WxOpen.AdvancedAPIs.Template.TemplateApi.SendTemplateMessage(accessToken, openId, templateId, msgdata, formId, url);
                    }
                }
#if DEBUG
                Core.Log.Info(_result.errcode == Senparc.Weixin.ReturnCode.请求成功 ? "成功" : "失败" + "[小程序：模板消息]发送结束");
#endif
            }
            else
            {
#if DEBUG
                Core.Log.Info("[小程序：模板消息]发送失败：数据验证未通过-" + dataerr + "[" + type.ToDescription() + "]");
#endif
            }
        }


        /// <summary>
        /// 新增小程序表单提交数据
        /// </summary>
        /// <param name="mWXSmallChoiceProductsInfo"></param>
        public void AddWXAppletFromData(Entities.WXAppletFormDataInfo mWxAppletFromDateInfo)
        {
            DbFactory.Default.Add(mWxAppletFromDateInfo);
            //Context.WXAppletFormDatasInfo.Add(mWxAppletFromDateInfo);
            //Context.SaveChanges();
        }
        /// <summary>
        /// 获取表单保存数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        public Entities.WXAppletFormDataInfo GetWXAppletFromDataById(MessageTypeEnum type, string OrderId)
        {
            var model = DbFactory.Default.Get<Entities.WXAppletFormDataInfo>().Where(d => d.EventId == (long)type && d.EventValue == OrderId).OrderByDescending(p => p.EventTime).FirstOrDefault();
            //var model = Context.WXAppletFormDatasInfo.Where(d => d.EventId == (long)type && d.EventValue == OrderId).OrderByDescending(p => p.EventTime).FirstOrDefault();
            return model;
        }


        #region 小程序订阅消息功能
        /// <summary>
        /// 授权订阅消息
        /// </summary>
        /// <param name="orderIds"></param>
        /// <param name="templateIds">订阅消息模板Id</param>
        /// <param name="messageType">消息类型</param>
        public void AuthorizedSubscribeMessage(string orderIds, string templateIds)
        {
            //使用原来表结构记录订阅消息授权情况，订阅成功才允许调用小程序发送订阅消息接口
            if (!string.IsNullOrEmpty(orderIds) && !string.IsNullOrEmpty(templateIds))
            {
                bool IsSendMessage = false;
                var templates = GetTemplateByAppletlist();
                var templatelist = templateIds.Split(',');
                var orderList = orderIds.Split(',');
                foreach (var orderId in orderList)
                {
                    foreach (var item in templatelist)
                    {
                        var messageType = 0;
                        var info = templates.FirstOrDefault(t => t.TemplateId == item);
                        if (info != null)
                        {
                            messageType = info.MessageType;
                            //如果订阅消息为自提订单则发送自提订阅消息
                            if (info.MessageType == (int)MessageTypeEnum.SelfTakeOrderPay)
                            {
                                IsSendMessage = true;
                            }
                            WXAppletFormDataInfo wxInfo = new WXAppletFormDataInfo
                            {
                                EventId = messageType,
                                EventTime = DateTime.Now,
                                EventValue = orderId,
                                ExpireTime = DateTime.Now.AddYears(1),//订阅消息不存在过期日期，可忽略
                                FormId = item
                            };
                            AddWXAppletFromData(wxInfo);
                        }
                    }
                    var ser_order = Himall.ServiceProvider.Instance<OrderService>.Create;
                    var orderInfo = ser_order.GetOrder(long.Parse(orderId));
                    if (orderInfo != null)
                    {
                        //到店自提
                        if (orderInfo.DeliveryType == DeliveryType.SelfTake && IsSendMessage)
                        {
                            var list = GetTemplateByAppletlist();
                            var formInfo = list.FirstOrDefault(f => f.MessageType == (int)MessageTypeEnum.SelfTakeOrderPay);
                            var page = GetWXAppletMessageTemplateShowUrl(MessageTypeEnum.SelfTakeOrderPay);//小程序跳转地址
                            page = page.Replace("{id}", orderId);

                            string openId = "";
                            try
                            {
                                var _iwxmember = Himall.ServiceProvider.Instance<MemberService>.Create;
                                openId = _iwxmember.GetMemberOpenIdInfoByuserIdAndType(orderInfo.UserId, "WeiXinSmallProg").OpenId;//登录小程序的OpenId
                            }
                            catch
                            {
                            }

                            var productName = "";
                            var orderitems = ser_order.GetOrderItemsByOrderId(orderInfo.Id);
                            if (orderitems.Count > 0)
                            {
                                var productInfo = Himall.ServiceProvider.Instance<ProductService>.Create.GetProduct(orderitems[0].ProductId);
                                productName = productInfo.ProductName;
                            }
                            var shopbranch = DbFactory.Default.Get<ShopBranchInfo>().Where(s => s.Id == orderInfo.ShopBranchId).FirstOrDefault();
                            var shopBranchAddress = "店铺地址";
                            var shopName = "店铺名称：";
                            if (shopbranch != null)
                            {
                                shopBranchAddress = shopbranch.AddressDetail.Replace(" ", "");
                                shopBranchAddress = shopBranchAddress.Length > 20 ? shopBranchAddress.Substring(0, 17) + "..." : shopBranchAddress;
                                shopName = shopbranch.ShopBranchName.Length > 15 ? shopbranch.ShopBranchName.Length <= 20 ? shopbranch.ShopBranchName : shopbranch.ShopBranchName.Substring(0, 17) + "..." : string.Format("{0}{1}", shopName, shopbranch.ShopBranchName);
                            }

                            var data = new Dictionary<string, string>
                            {
                                { "character_string1", orderId },
                                { "thing2", productName.Length>20?productName.Substring(0,17)+"...":productName },
                                { "thing4",  shopName },
                                { "thing5",  shopBranchAddress },
                                { "character_string3", orderInfo.PickupCode }
                            };
                            string templateId = formInfo.TemplateId;
                            if (!string.IsNullOrEmpty(templateId))
                            {
                                Log.Info("自提订单支付成功 订阅消息模板ID为：" + templateId);
                                SendSubscribeMessage(openId, templateId, page, data);
                                RemoveSubscribeMessage(orderId, templateId, MessageTypeEnum.SelfTakeOrderPay);
                            }
                            IsSendMessage = false;
                        }
                    }
                }

            }
        }

        /// <summary>
        /// 移除订阅消息
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="templateId">订阅消息模板Id</param>
        /// <param name="messageType">消息类型</param>
        public void RemoveSubscribeMessage(string orderId, string templateId, MessageTypeEnum messageType)
        {
            //使用原来表结构记录订阅消息授权情况，订阅成功才允许调用小程序发送订阅消息接口
            if (!string.IsNullOrEmpty(orderId) && !string.IsNullOrEmpty(templateId))
            {
                DbFactory.Default.Del<Entities.WXAppletFormDataInfo>(d => d.EventId == (long)messageType && d.EventValue == orderId && d.FormId == templateId);
            }
        }

        /// <summary>
        /// 小程序发送订阅消息
        /// </summary>
        /// <param name="touser">openId</param>
        /// <param name="templateId"></param>
        /// <param name="toPage"></param>
        /// <param name="data"></param>
        public void SendSubscribeMessage(string touser, string template_id, string toPage, Dictionary<string, string> data)
        {
            var param = new JObject
            {
                ["touser"] = touser,
                ["template_id"] = template_id,
                ["page"] = toPage,
                ["data"] = new JObject()
            };
            foreach (var item in data)
            {
                param["data"][item.Key] = new JObject();
                param["data"][item.Key]["value"] = item.Value;
            }
            var content = PostJson("https://api.weixin.qq.com/cgi-bin/message/subscribe/send", param);
            //MessageApi.SendSubscribe(GetAppletResetToken(), touser, template_id, new TemplateMessageData { }, toPage);
#if DEBUG
            Core.Log.Info("小程序订阅消息返回值：" + content);
#endif
            var json = JObject.Parse(content);
            if (json["errcode"].Value<int>() > 0)
                Log.Debug("发送订阅消息异常：" + json["errcode"].Value<string>() + ";" + json["errmsg"].Value<string>());
        }
        /// <summary>
        /// PostJson提交
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private string PostJson(string url, object data)
        {
            var jsonData = JsonConvert.SerializeObject(data);
            Log.Info("PostJson发送订阅消息参数：" + jsonData);

            var AccessToken = GetAppletResetToken();
            var postUrl = url + $"?access_token={AccessToken}";
            return HttpHelper.HttpPost(postUrl, jsonData, System.Text.Encoding.UTF8, true);
        }

        /// <summary>
        /// 获取Token
        /// </summary>
        /// <returns></returns>
        private string GetAppletResetToken()
        {
            var siteconfig = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings;
            var api = "https://" + $"api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={siteconfig.WeixinAppletId}&secret={siteconfig.WeixinAppletSecret}";
            var content = HttpHelper.Get(api);
            var json = JObject.Parse(content);
            return json["access_token"].Value<string>();
        }
        #endregion


        #region 视频号功能


        public List<string> GetWeiXinOpenId(string appid, string secreate, out string msg)
        {
            var wxHelper = new WXHelper();

            var token = wxHelper.GetAccessToken(appid, secreate);
            msg = "";
            var api = "https://" + $"api.weixin.qq.com/cgi-bin/user/get?access_token={token}";

            var content = HttpHelper.Get(api);

            var json = JObject.Parse(content);

            if (json["errcode"] != null && json["errcode"].Value<int>() > 0)
            {
                if (json["errcode"].Value<int>() == 45009)
                    msg = "今日次数超过限制无法继续生成！";
                else
                    msg = json["errmsg"].Value<string>();
                return null;
            }
            return json["data"]["openid"].Values<string>().ToList();
        }
        /// <summary>
        /// 查询微信文章URL是否存在（不存在则返回null）
        /// </summary>
        public string ExistWeiXinArticleUrl(long productId, long superiorId)
        {
            var db = DbFactory.Default.Get<WeiXinArticleUrlInfo>()
                .Where(p => p.ProductId == productId)
                .Where(p => p.SuperiorId == superiorId);
            var entity = db.FirstOrDefault();
            return entity?.ArticleUrl;
        }

        /// <summary>
        /// 添加微信文章URL
        /// </summary>
        public void AddWeiXinArticleUrl(long productId, long superiorId, string articleUrl)
        {
            WeiXinArticleUrlInfo urlInfo = new WeiXinArticleUrlInfo()
            {
                ProductId = productId,
                SuperiorId = superiorId,
                ArticleUrl = articleUrl
            };
            DbFactory.Default.Add(urlInfo);
        }

        /// <summary>
        /// 群发消息
        /// </summary>
        public string SendMessage(string appId, string appsecreate, WXSendMessage message)
        {
            var setting = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };

            WXHelper wxhelper = new WXHelper();
            var weixtoken = wxhelper.GetAccessToken(appId, appsecreate, true);

            var api = "https://" + $"api.weixin.qq.com/cgi-bin/message/mass/send?access_token=" + weixtoken;

            var content = PostWeixinJson(api, message);

            var json = JObject.Parse(content);
            if (json["errcode"] != null && json["errcode"].Value<int>() != 0)
                throw new HimallException(json["errmsg"].Value<string>());
            return json["msg_id"].Value<string>();
        }

        private string PostWeixinJson(string url, object data)
        {
            var setting = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            var jsonData = JsonConvert.SerializeObject(data, Formatting.None, setting);
            Log.Info("PostWeixinJson发送订阅消息参数：" + jsonData);
            return HttpHelper.HttpPost(url, jsonData, System.Text.Encoding.UTF8, true);
        }


        #endregion



    }
}
