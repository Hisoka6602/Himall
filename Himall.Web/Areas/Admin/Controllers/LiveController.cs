using Himall.Core;
using Himall.Core.Plugins.Payment;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;
using Himall.DTO;
using Himall.Application;
using System.ComponentModel;
using Himall.CommonModel;
using System;
using System.IO;
using Himall.Entities;
using Himall.DTO.Live;
using Himall.DTO.QueryModel;
using static Himall.Entities.LiveProductLibraryInfo;
using Himall.Core.Extends;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class LiveController : BaseAdminController
    {
        [HttpPost]
        public JsonResult Sync()
        {
            LiveApplication.SyncLiveRoom();
            LiveApplication.SyncLiveReply();
            return SuccessResult();
        }

        public ActionResult Management()
        {
            Type t = typeof(LiveRoomStatus);
            List<SelectListItem> selectList = new List<SelectListItem>();
            Array arrays = Enum.GetValues(t);
            for (int i = 0; i < arrays.LongLength; i++)
            {
                LiveRoomStatus pdt = (LiveRoomStatus)arrays.GetValue(i);
                selectList.Add(new SelectListItem() { Text = pdt.ToDescription(), Value = pdt.GetHashCode().ToString() });
            }
            ViewBag.RoomStatusList = selectList;
            //ViewBag.Shops = ShopApplication.getAll();
            return View();
        }

        [UnAuthorize]
        public ActionResult LiveProduct(long roomId)
        {
            ViewBag.roomId = roomId;
            return View();
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult LiveProductList(LiveQuery query)
        {
            var result = LiveApplication.GetLiveProducts(query);
            return Json(new { rows = result.Models, total = result.Total });
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult List(LiveQuery query)
        {
            var result = LiveApplication.GetLiveList(query);
            return Json(new { rows = result.Models, total = result.Total });
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult SetShop(long roomId, long shopId)
        {
            LiveApplication.SetShop(roomId, shopId);
            return SuccessResult();
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult SetSequence(long roomId, long sequence)
        {
            LiveApplication.SetSequence(roomId, sequence);
            return SuccessResult();
        }
        /// <summary>
        /// 删除直播间数据
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult DelLiveRoom(long id)
        {
            LiveRoomInfo roomInfo = LiveApplication.GetLiveRoomInfoBaseId(id);
            if (roomInfo == null)
            {
                throw new HimallException("参数错误");
            }
            LiveApplication.DelLiveRoom(roomInfo.RoomId, id);
            return SuccessResult();
        }
        /// <summary>
        /// 删除直播回放数据
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult DelLiveReplay(long roomId)
        {
            LiveApplication.DelLiveReplay(roomId);
            return SuccessResult();
        }
        [HttpPost]
        [UnAuthorize]
        public ActionResult ExportToExcel(LiveQuery query)
        {
            query.HasPage = false;
            var result = LiveApplication.GetLiveList(query);
            return ExcelView("ExportRoom", "直播间信息", result.Models);
        }

        [HttpPost]
        [UnAuthorize]
        public ActionResult ExportProductToExcel(LiveQuery query)
        {
            query.HasPage = false;
            var result = LiveApplication.GetLiveProducts(query);
            return ExcelView("ExportRoomProduct", "直播商品信息", result.Models);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult SetBanner(long roomId, string banner)
        {
            var savePath = string.Empty;
            if (!string.IsNullOrWhiteSpace(banner))
            {
                //转移图片
                if (banner.Contains("/temp/"))
                {
                    string source = banner.Substring(banner.LastIndexOf("/temp"));
                    string dest = @"/Storage/Live/";
                    savePath = Path.Combine(dest, Path.GetFileName(source));
                    Core.HimallIO.CopyFile(source, savePath, true);
                }
                else if (banner.Contains("/Storage/"))
                {
                    savePath = banner.Substring(banner.LastIndexOf("/Storage/"));
                }

                LiveApplication.SetLiveBanner(roomId, savePath);
            }

            return SuccessResult();
        }
        /// <summary>
        /// 从商品库中移除
        /// </summary>
        /// <param name="context"></param>
        public JsonResult RemoveProduct(string productIds)
        {
            if (productIds.IsEmptyString())
            {
                throw new HimallException("参数错误");
            }
            LiveProductLibaryQuery query = new LiveProductLibaryQuery();
            query.ProductIds = productIds;
            query.IsCanMoveProduct = true;//只有撤回的商品可以移除
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            List<LiveProductLibraryInfo> result = LiveApplication.GetLiveProductLibaryList(query);
            if (result == null && result.Count == 0)
            {
                throw new HimallException("没有可以移除的直播商品");
            }
            else
            {
                string msg = "";
                if (LiveApplication.RemoveProduct(result))
                {
                    return SuccessResult("从商品库中移除成功");
                }
                else
                {
                    throw new HimallException("没有可以移除的直播商品");
                }
            }
            //LiveRoomHelper.()
        }
        /// <summary>
        /// 删除直播商品
        /// </summary>
        /// <param name="context"></param>
        [HttpPost]
        public JsonResult DelLiveProduct(string productIds)
        {
            if (productIds.IsEmptyString())
            {
                throw new HimallException("参数错误");
            }
            LiveProductLibaryQuery query = new LiveProductLibaryQuery();
            query.ProductIds = productIds;
            query.LiveAuditStatus.Add(LiveProductAuditStatus.Audited.GetHashCode());
            query.LiveAuditStatus.Add(LiveProductAuditStatus.AuditFailed.GetHashCode());
            query.LiveAuditStatus.Add(LiveProductAuditStatus.Auditing.GetHashCode());
            query.LiveAuditStatus.Add(LiveProductAuditStatus.NoAudit.GetHashCode());
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            List<LiveProductLibraryInfo> result = LiveApplication.GetLiveProductLibaryList(query);
            if (result == null && result.Count == 0)
            {
                throw new HimallException("没有可以删除的直播商品");
            }
            else
            {
                string msg = "";
                if (LiveApplication.DeleteProduct(result, out msg))
                {
                    return SuccessResult("删除成功" + (msg.IsEmptyString() ? "" : ",部分失败：" + msg));
                }
                else
                {
                    throw new HimallException("删除失败");
                }
            }
        }

        /// <summary>
        /// 直播间列表页面
        /// </summary>
        /// <returns></returns>
        public ActionResult LiveRoom()
        {
            Type t = typeof(LiveRoomStatus);
            List<SelectListItem> selectList = new List<SelectListItem>();
            Array arrays = Enum.GetValues(t);
            for (int i = 0; i < arrays.LongLength; i++)
            {
                LiveRoomStatus pdt = (LiveRoomStatus)arrays.GetValue(i);
                selectList.Add(new SelectListItem() { Text = pdt.ToDescription(), Value = pdt.GetHashCode().ToString() });
            }
            ViewBag.LiveRoomStatusList = selectList;
            return View();
        }
        /// <summary>
        /// 同步商品状态
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SynchorLiveProductStatus(string productIds)
        {

            LiveProductLibaryQuery query = new LiveProductLibaryQuery();
            if (!productIds.IsEmptyString())
            {
                query.ProductIds = productIds;
            }
            query.LiveAuditStatus.Add(LiveProductAuditStatus.NoAudit.GetHashCode());
            query.LiveAuditStatus.Add(LiveProductAuditStatus.Auditing.GetHashCode());
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            List<LiveProductLibraryInfo> result = LiveApplication.GetLiveProductLibaryList(query);
            if (result != null && result.Count > 0)
            {
                string msg = "";
                if (LiveApplication.UpdateLiveProductStatus(result, out msg))
                {
                    return SuccessResult("同步成功");
                }
                else
                {
                    throw new HimallException("获取失败,失败原因:" + msg);
                }

            }
            else
            {
                return SuccessResult("同步成功");
            }

        }
        /// <summary>
        /// 提交直播间审核
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AuditLiveRoom(string ids)
        {
            if (ids == null || ids == "")
            {
                throw new HimallException("请选择要提交审核的直播间");
            }
            List<long> idlist = ids.ToLongList();
            LiveQuery query = new LiveQuery();
            query.ids = idlist;
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            query.Status = LiveRoomStatus.NoSubmit;
            QueryPageModel<LiveViewModel> result = LiveApplication.GetLiveList(query);
            if (result.Total > 0)
            {
                List<string> msgs = new List<string>();
                var nowtime = DateTime.Now;
                foreach (LiveViewModel liveView in result.Models)
                {
                    if ((liveView.StartTime - nowtime).TotalMinutes < 10 || (liveView.StartTime - nowtime).TotalDays > 180)
                    {
                        throw new HimallException("直播间:" + liveView.Name + ",开播时间必须在6个月之内，且在当前时间10分钟之后。");

                    }
                    if ((liveView.EndTime.Value - liveView.StartTime).TotalMinutes < 30 || (liveView.EndTime.Value - liveView.StartTime).TotalHours > 24)
                    {
                        throw new HimallException("直播间:" + liveView.Name + ",开播开始和结束时间不能短于30分钟，不超过24小时。");
                    }
                    if (liveView.AnchorName.Trim().Length < 2 || liveView.AnchorName.Trim().Length > 15)
                    {
                        throw new HimallException("直播间:" + liveView.Name + ",主播昵称不能为空，长度在2至15个汉字之间(一个汉字等于两个英文字符或特殊字符）");
                    }
                    if (liveView.AnchorWechat == "")
                    {
                        throw new HimallException("直播间:" + liveView.Name + ",请选择主播微信帐号。");
                    }
                    if (string.IsNullOrEmpty(liveView.ShareImgMediaId))
                    {
                        throw new HimallException("直播间:" + liveView.Name + ",请上传分享卡片封面图片。");
                    }
                    if (string.IsNullOrEmpty(liveView.CoverImgMediaId))
                    {
                        throw new HimallException("请上传直播间背景墙图片。");
                    }
                    string msg = "";
                    var liveroom = AutoMapper.Mapper.Map<LiveViewModel, LiveRoomInfo>(liveView); 
                    long roomId = LiveApplication.AuditLiveRoom(liveroom, out msg);
                    if (roomId <= 0)
                    {
                        msgs.Add(msg);
                        if (roomId == -1)//没有配置小程序appId,appSecrect或者提交次数已满不在继续
                        {
                            break;
                        }
                    }
                }

                if (msgs.Count < result.Total && msgs.Count > 0)
                {
                    return SuccessResult("部分提交成功,部分失败原因" + string.Join(",", msgs));
                }
                else if (msgs.Count == result.Total)
                {
                    throw new HimallException("提交审核失败,失败原因:" + string.Join(",", msgs));
                }
                return SuccessResult("提交成功");
            }
            else
            {
                throw new HimallException("请选择要提交审核的直播间");
            }

        }
        /// <summary>
        /// 批量撤回审核 
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ReCallAudit(string productIds)
        {

            LiveProductLibaryQuery query = new LiveProductLibaryQuery();
            if (!productIds.IsEmptyString())
            {
                query.ProductIds = productIds;
            }
            query.LiveAuditStatus.Add(LiveProductAuditStatus.NoAudit.GetHashCode());
            query.LiveAuditStatus.Add(LiveProductAuditStatus.Auditing.GetHashCode());
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            List<LiveProductLibraryInfo> result = LiveApplication.GetLiveProductLibaryList(query);
            if (result != null && result.Count > 0)
            {
                string msg = "";
                if (LiveApplication.ReCallAudit(result, out msg))
                {
                    return SuccessResult("撤回审核成功");
                }
                else
                {
                    throw new HimallException("撤回失败,失败原因:" + msg);
                }

            }
            else
            {
                throw new HimallException("没有需要撤回审核的商品");
            }

        }

        /// <summary>
        /// 批量重新提交审核 
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ReApplyAudit(string productIds)
        {


            LiveProductLibaryQuery query = new LiveProductLibaryQuery();
            if (productIds.IsEmptyString())
            {
                throw new HimallException("请选择要重新提交审核的商品");
            }

            query.ProductIds = productIds;
            query.LiveAuditStatus.Add(LiveProductAuditStatus.NoAudit.GetHashCode());
            query.LiveAuditStatus.Add(LiveProductAuditStatus.Auditing.GetHashCode());
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            List<LiveProductLibraryInfo> result = LiveApplication.GetLiveProductLibaryList(query);
            if (result != null && result.Count > 0)
            {
                string msg = "";
                if (LiveApplication.ReApplyAudit(result, out msg))
                {
                    return SuccessResult("重新提交审核成功" + (msg.IsEmptyString() ? "" : ",部分失败：" + msg));
                }
                else
                {
                    throw new HimallException("重新提交审核失败,原因：" + msg);
                }

            }
            else
            {
                throw new HimallException("请选择要重新提交审核的商品");
            }

        }
        /// <summary>
        /// 将商品提交到直播商品库
        /// </summary>
        /// <param name="productIds"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult SubmitToAudit(string productIds)
        {

            List<LiveProductLibaryModel> list = new List<LiveProductLibaryModel>();
            LiveProductLibaryQuery query = new LiveProductLibaryQuery();
            query.ProductIds = productIds;
            query.LiveAuditStatus.Add(LiveProductAuditStatus.NoSubmit.GetHashCode());
            QueryPageModel<LiveProductLibaryModel> products = LiveApplication.GetLiveProductLibrarys(query);
            if (products.Total > 0)
            {
                string msg = "";
                if (LiveApplication.AddProductToLiveProductLibary(products.Models, out msg))
                {
                    return SuccessResult("提交审核成功");
                }
                else
                {
                    throw new HimallException("提交审核失败，失败原因：" + msg);
                }
            }
            else
            {
                throw new HimallException("没有可以提交审核的商品");
            }

            //LiveApplication.AddProductToLiveProductLibary();
        }
        /// <summary>
        /// 获取直播商品库数据
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult LiveProductLibaray(LiveProductLibaryQuery query)
        {

            var result = LiveApplication.GetLiveProductLibrarys(query);
            return Json(new { rows = result.Models, total = result.Total });

        }
        /// <summary>
        /// 直播商品库页面
        /// </summary>
        /// <returns></returns>
        public ActionResult LiveProductLibaray()
        {
            Type t = typeof(LiveProductAuditStatus);
            List<SelectListItem> selectList = new List<SelectListItem>();
            Array arrays = Enum.GetValues(t);
            for (int i = 0; i < arrays.LongLength; i++)
            {
                LiveProductAuditStatus pdt = (LiveProductAuditStatus)arrays.GetValue(i);
                selectList.Add(new SelectListItem() { Text = pdt.ToDescription(), Value = pdt.GetHashCode().ToString() });
            }
            ViewBag.LiveProductAuditStatusList = selectList;
            ShopQuery query = new ShopQuery();
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            QueryPageModel<Shop> shops = ShopApplication.GetShops(query);
            ViewBag.ShopList = shops.Models;
            return View();
        }

        /// <summary>
        /// 主播列表页面
        /// </summary>
        /// <returns></returns>
        public ActionResult AnchorList()
        {
            ShopQuery query = new ShopQuery();
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            QueryPageModel<Shop> shops = ShopApplication.GetShops(query);
            //LiveApplication.GetAnchorMembers(0);
            ViewBag.Shops = shops.Models;
            return View();
        }

        /// <summary>
        /// 获取直播数据列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult GetAnchors(AnchorQuery query)
        {

            var result = LiveApplication.GetAnchorList(query);
            if (DemoAuthorityHelper.IsDemo()) {
                result.Models.ForEach(item => {
                    item.CellPhone = item.CellPhone.Substring(0, 3).PadRight(item.CellPhone.Length, '*');
                    item.WeChat = item.WeChat.Substring(0, 3).PadRight(item.WeChat.Length,'*');
                });
            }
               
            return Json(new { rows = result.Models, total = result.Total });

        }

        /// <summary>
        /// 获取主播列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult DeleteAnchor(int anchorId)
        {
            AnchorModel info = LiveApplication.GetAnchorInfo(anchorId);
            if (info == null)
            {
                throw new HimallException("参数错误");
            }

            var result = LiveApplication.DelAnchorInfo(anchorId);
            if (result)
            {
                return SuccessResult();
            }
            else
            {
                throw new HimallException("删除失败");
            }

        }

    }
}