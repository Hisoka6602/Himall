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
using static Himall.Entities.LiveProductLibraryInfo;
using Himall.Entities;
using Himall.DTO.QueryModel;
using Himall.DTO.Live;
using Himall.CommonModel.Model;
using Himall.Core.Helper;
using Himall.Core.Extends;
using static Himall.Entities.ProductInfo;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class LiveController : BaseSellerController
    {

        /// <summary>
        /// 新增直播间
        /// </summary>
        /// <returns></returns>
        public ActionResult AddLiveRoom()
        {
            AnchorQuery query = new AnchorQuery();
            query.ShopId = CurrentShop.Id;
            QueryPageModel<AnchorModel> anchors = LiveApplication.GetAnchorList(query);
            ViewBag.AchorList = anchors;
            return View();
        }
        /// <summary>
        /// 保存直播间数据
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        public JsonResult SaveLiveRoom(LiveRoomInfo roomInfo)
        {
            if (roomInfo == null)
            {
                throw new HimallException("缺少必填参数。");
            }
            if (roomInfo.Name == "" || roomInfo.Name.GetChineseLen() < 3 || roomInfo.Name.GetChineseLen() > 17)
            {
                throw new HimallException("直播标题长度3-17个汉字(一个汉字等于两个英文字符或特殊字符）");
            }

            DateTime nowtime = DateTime.Now;

            if (!roomInfo.EndTime.HasValue || roomInfo.EndTime.Value < roomInfo.StartTime)
            {
                throw new HimallException("请选选择正确的直播开始和结束时间,结束时间必须大于开始时间。");

            }
            if ((roomInfo.StartTime - nowtime).TotalMinutes < 10 || (roomInfo.StartTime - nowtime).TotalDays > 180)
            {
                throw new HimallException("开播时间必须在6个月之内，且在当前时间10分钟之后。");

            }
            if ((roomInfo.EndTime.Value - roomInfo.StartTime).TotalMinutes < 30 || (roomInfo.EndTime.Value - roomInfo.StartTime).TotalHours > 24)
            {
                throw new HimallException("开播开始和结束时间不能短于30分钟，不超过24小时。");
            }
            if (roomInfo.AnchorName.Trim().Length < 2 || roomInfo.AnchorName.Trim().Length > 15)
            {
                throw new HimallException("主播昵称不能为空，长度在2至15个汉字之间(一个汉字等于两个英文字符或特殊字符）");
            }
            if (roomInfo.AnchorWechat == "")
            {
                throw new HimallException("请选择主播微信帐号。");
            }
            if (string.IsNullOrEmpty(roomInfo.ShareImg))
            {
                throw new HimallException("请上传分享卡片封面图片。");
            }
            if (string.IsNullOrEmpty(roomInfo.CoverImg))
            {
                throw new HimallException("请上传直播间背景墙图片。");
            }
            string uploadMsg = "";
            //roomInfo.ShareImg= LiveApplication.SaveLiveRoomImage(roomInfo.ShareImg,roomInfo.RoomId)
            AppletUploadResult uploadresult = LiveApplication.AppletMeidaUpload(Request.MapPath(roomInfo.ShareImg), MediaType.image, out uploadMsg);
            if (uploadresult == null)
            {
                throw new HimallException("分享卡片封面图片上传至小程序服务器失败，原因：" + uploadMsg);
            }
            roomInfo.AnchorImg = "";
            roomInfo.ShareImgMediaId = uploadresult.media_id;
            roomInfo.Type = 0;
            roomInfo.Status = LiveRoomStatus.NoSubmit;
            roomInfo.ShopId = CurrentShop.Id;
            roomInfo.Sequence = LiveApplication.GetMaxSequence().ToInt() + 1;
            roomInfo.CreateTime = DateTime.Now;
            uploadMsg = "";
            uploadresult = LiveApplication.AppletMeidaUpload(Request.MapPath(roomInfo.CoverImg), MediaType.image, out uploadMsg);
            if (uploadresult == null)
            {
                throw new HimallException("上传直播间背景墙图片上传至小程序服务器失败，原因：" + uploadMsg);
            }
            roomInfo.CoverImgMediaId = uploadresult.media_id;
            if (LiveApplication.AddLiveRoom(roomInfo))
            {
                roomInfo = LiveApplication.GetLiveRoomInfoBaseId(roomInfo.Id);
                //将图片从临时文件夹移走
                if (roomInfo != null)
                {
                    roomInfo.CoverImg = LiveApplication.SaveLiveRoomImage(roomInfo.CoverImg, roomInfo.Id, "CoverImg");
                    roomInfo.ShareImg = LiveApplication.SaveLiveRoomImage(roomInfo.ShareImg, roomInfo.Id);
                    LiveApplication.UpdateLiveRoom(roomInfo);
                }
                return SuccessResult();
            }
            else
            {
                throw new HimallException("保存直播间数据失败");
            }

        }
        /// <summary>
        /// 将商品提交到直播商品库
        /// </summary>
        /// <param name="productIds"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult ProductToLiveProductLibrary(string productIds)
        {
            List<long> productIdList = productIds.ToLongList();
            if (productIdList == null || productIdList.Count == 0)
            {
                throw new HimallException("请选择要导入的商品");
            }
            List<LiveProductLibraryInfo> list = new List<LiveProductLibraryInfo>();
            ProductQuery query = new ProductQuery();
            query.Ids = productIdList;
            query.InLiveProductLibaray = false;
            query.ShopId = CurrentShop.Id;
            List<ProductInfo> products = ProductManagerApplication.GetNotInLiveLibraryProductByIds(productIdList);
            if (products != null && products.Count > 0)
            {
                string msg = "";
                if (LiveApplication.AddProductToLiveProductLibary(products))
                {
                    LiveProductLibaryQuery query1 = new LiveProductLibaryQuery();
                    query1.ShopId = CurrentShop.Id;
                    var result = LiveApplication.GetLiveProductLibrarys(query1);
                    return Json(new { success = true, msg = "导入成功", rows = result.Models, total = result.Total });
                }
                else
                {
                    throw new HimallException("导入失败");
                }
            }
            else
            {
                throw new HimallException("请选择要导入的商品");
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
            query.ShopId = CurrentShop.Id;
            var result = LiveApplication.GetLiveProductLibrarys(query);
            return Json(new { rows = result.Models, total = result.Total });
        }

        /// <summary>
        /// 获取直播商品库数据
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult GetInLibarayProductIds()
        {

            LiveProductLibaryQuery query = new LiveProductLibaryQuery();

            query.ShopId = CurrentShop.Id;
            var result = LiveApplication.GetLiveProductLibrarys(query);

            return Json(result.Models.Select(p => p.ProductId).ToList());
        }

        /// <summary>
        /// 获取直播商品库数据
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult GetShopNotInLiveLibaryProducts(ProductQuery query)
        {
            query.InLiveProductLibaray = false;
            query.ShopId = CurrentShop.Id;
            query.ShopCategoryId = query.CategoryId;
            query.CategoryId = 0;
            var data = ProductManagerApplication.GetProducts(query);

            var shops = ShopApplication.GetShops(data.Models.Select(p => p.ShopId));
            var brands = BrandApplication.GetBrands(data.Models.Select(p => p.BrandId));
            var skus = ProductManagerApplication.GetSKUByProducts(data.Models.Select(p => p.Id));

            var products = data.Models.Select(item =>
            {
                var brand = brands.FirstOrDefault(p => p.Id == item.BrandId);
                var shop = shops.FirstOrDefault(p => p.Id == item.ShopId);
                var cate = CategoryApplication.GetCategory(item.CategoryId);
                var sku = skus.Where(p => p.ProductId == item.Id);
                var limitAdd = LimitTimeApplication.IsAdd(item.Id);
                return new
                {
                    name = item.ProductName,
                    brandName = brand?.Name ?? string.Empty,
                    categoryName = brand == null ? "" : cate.Name,
                    id = item.Id,
                    imgUrl = item.GetImage(ImageSize.Size_50),
                    price = item.MinSalePrice,
                    Stock = sku.Sum(s => s.Stock),
                    shopName = shop.ShopName,
                    isOpenLadder = item.IsOpenLadder,
                    isLimit = limitAdd
                };
            });

            var dataGrid = new
            {
                rows = products,
                total = data.Total
            };
            return Json(dataGrid);
        }

        /// <summary>
        /// 获取直播商品库数据
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult GetShopAuditedLiveProducts(long roomId, long categoryId = 0, string keywords = "", int page = 1, int rows = 7)
        {
            LiveProductLibaryQuery query = new LiveProductLibaryQuery();
            ProductAuditStatus[] AuditStatus = { ProductAuditStatus.Audited };
            query.LiveAuditStatus.Add(LiveProductAuditStatus.Audited.GetHashCode());
            query.ShopId = CurrentShop.Id;
            query.PageNo = page;
            query.PageSize = rows;
            query.CategoryId = categoryId;
            query.Keywords = keywords;
            query.FilterRoomId = roomId;
            var data = LiveApplication.GetLiveProductLibrarys(query);

            var shops = ShopApplication.GetShops(data.Models.Select(p => p.ShopId));
            var skus = ProductManagerApplication.GetSKUByProducts(data.Models.Select(p => p.ProductId));
            var products = ProductManagerApplication.GetProductByIds(data.Models.Select(p => p.ProductId));
            var liveproducts = data.Models.Select(item =>
            {

                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                var shop = shops.FirstOrDefault(p => p.Id == item.ShopId);
                var sku = skus.Where(p => p.ProductId == item.Id);
                var limitAdd = LimitTimeApplication.IsAdd(item.Id);

                return new
                {
                    name = item.Name,
                    id = item.Id,
                    ProductId = item.ProductId,
                    imgUrl = product.GetImage(ImageSize.Size_50),
                    price = item.MinSalePrice,
                    item.Price,
                    item.Price2,
                    stock = sku.Sum(s => s.Stock),
                    item.PriceType,
                    shopName = shop.ShopName,
                    isOpenLadder = product.IsOpenLadder,
                    isLimit = limitAdd
                };

            });

            var dataGrid = new
            {
                rows = liveproducts,
                total = data.Total
            };
            return Json(dataGrid);
        }
        /// <summary>
        /// 导入商品到直播间
        /// </summary>
        [HttpPost]
        [UnAuthorize]
        public JsonResult ImportProductToLiveRoom(long roomId, string productIds)
        {
            List<long> productIdList = productIds.ToLongList();
            if (productIdList == null || productIdList.Count == 0)
            {
                throw new HimallException("请选择要导入的商品");
            }
            if (roomId <= 0)
            {
                throw new HimallException("直播间ID错误");
            }
            LiveRoomInfo roomInfo = LiveApplication.GetLiveRoomInfo(roomId);
            if (roomInfo == null || roomInfo.ShopId != CurrentShop.Id)
            {
                throw new HimallException("直播间信息错误,或者直播间不属于当前店铺");
            }
            LiveProductLibaryQuery query = new LiveProductLibaryQuery();
            ProductAuditStatus[] AuditStatus = { ProductAuditStatus.Audited };
            query.LiveAuditStatus.Add(LiveProductAuditStatus.Audited.GetHashCode());
            query.ShopId = CurrentShop.Id;
            query.ProductIds = productIds;
            query.HasPage = false;
            var data = LiveApplication.GetLiveProductLibrarys(query);
            if (data.Total == 0)
            {
                throw new HimallException("请选择要导入的商品");
            }
            string msg = "";
            if (LiveApplication.ImportProductToLiveRoom(data.Models, roomId, out msg))
            {
                return SuccessResult("导入成功");
            }
            else
            {
                throw new HimallException("导入失败,失败原因：" + msg);
            }
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
            //ViewBag.Shops = ShopApplication.getAll();
            return View();
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
            LiveRoomInfo roomInfo = LiveApplication.GetLiveDetail(roomId);
            if (roomInfo == null)
            {
                throw new HimallException("直播间ID错误");
            }
            ViewBag.roomId = roomId;
            ViewBag.ShowImportProduct = (roomInfo.Status != LiveRoomStatus.End && roomInfo.Status != LiveRoomStatus.Exception && roomInfo.Status != LiveRoomStatus.Expire && roomInfo.Status != LiveRoomStatus.forbid);
            return View();
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult LiveProductList(LiveQuery query)
        {
            query.HasPage = true;
            var result = LiveApplication.GetLiveProducts(query);
            return Json(new { rows = result.Models, total = result.Total });
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult List(LiveQuery query)
        {
            query.ShopId = CurrentSellerManager.ShopId;
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

        [HttpPost]
        [UnAuthorize]
        public ActionResult ExportToExcel(LiveQuery query)
        {
            query.HasPage = false;
            query.ShopId = CurrentSellerManager.ShopId;
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
        /// 添加主播页面
        /// </summary>
        /// <returns></returns>
        public ActionResult AddAnchor(int anchorId = 0)
        {
            AnchorModel anchor = new AnchorModel();
            if (anchorId > 0)
            {
                anchor = LiveApplication.GetAnchorInfo(anchorId);
                if (anchor == null)
                {
                    throw new HimallException("参数错误");
                }
            }
            if (anchor == null)
            {
                anchor = new AnchorModel();
            }
            return View(anchor);
        }

        /// <summary> 
        /// 获取直播数据列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [UnAuthorize]
        public JsonResult GetAnchorMembers(string keyWords, long userId = 0)
        {
            var result = LiveApplication.GetAnchorMemberList(keyWords, userId, CurrentShop.Id, 1, 20);
            var data = result.Models.Select(m =>
                new
                {
                    key = m.Id,
                    value = m.Nick.IsEmptyString() ? m.UserName : m.Nick + "|" + m.CellPhone
                }
            );

            return Json(data);
        }
        /// <summary>
        /// 获取直播数据列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult SaveAnchor(AnchorModel anchor)
        {
            Result result = new Result();
            if (anchor.UserId <= 0)
            {
                throw new HimallException("请选择关联主播的会员");
            }
            if (string.IsNullOrEmpty(anchor.WeChat) || anchor.WeChat.Trim().Length > 100)
            {
                throw new HimallException("请输入主播微信号，字符长度在1-100之间");
            }
            AnchorModel anchor1 = LiveApplication.GetAnchorInfo(anchor.WeChat);
            if (anchor1 != null && anchor1.Id != anchor.Id)
            {
                throw new HimallException("主播微信号：" + anchor.WeChat + " 已绑定主播, 请重新输入");
            }
            if (string.IsNullOrEmpty(anchor.CellPhone) || !DataHelper.IsMobile(anchor.CellPhone))
            {
                throw new HimallException("请输入正确的手机号码");
            }
            if (string.IsNullOrEmpty(anchor.AnchorName) || anchor.AnchorName.Trim().GetChineseLen() > 15 || anchor.AnchorName.Trim().GetChineseLen() < 2)
            {
                throw new HimallException("请输入主播昵称，必须为2-15个汉字（一个字等于两个英文字符或特殊字符）");
            }
            if (anchor.Id > 0 && anchor.ShopId != CurrentShop.Id)
            {
                throw new HimallException("当前主播不属于当前店铺，无法编辑");
            }
            anchor.ShopId = CurrentShop.Id;
            AnchorInfo anchor2 = AutoMapper.Mapper.Map<AnchorModel, AnchorInfo>(anchor);
            if (anchor.Id > 0)
            {

                if (LiveApplication.UpdateAnchorInfo(anchor2))
                {

                    result.success = true;
                    result.msg = "更新主播信息成功！";
                }
                else
                {
                    throw new HimallException("更新主播信息失败");
                }
            }
            else
            {
                if (LiveApplication.AddAnchorInfo(anchor2, out string url, out bool isRealNameVerify))
                {
                    result.success = true;
                    result.msg = "添加主播信息成功！";
                }
                else
                {
                    if (!isRealNameVerify)
                    { //存在实名未认证
                        result.success = false;
                        result.msg = "微信未实名认证！|" + url;
                    }
                    else
                    {
                        throw new HimallException("添加主播信息失败," + url);
                    }
                }
            }
            return Json(result);
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
            query.ShopId = CurrentShop.Id;
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
        /// 获取主播列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult GetAnchors(AnchorQuery query)
        {
            query.ShopId = CurrentShop.Id;
            var result = LiveApplication.GetAnchorList(query);
            if (DemoAuthorityHelper.IsDemo())
            {
                result.Models.ForEach(item =>
                {
                    item.CellPhone = item.CellPhone.Substring(0, 3).PadRight(item.CellPhone.Length, '*');
                    item.WeChat = item.WeChat.Substring(0, 3).PadRight(item.WeChat.Length, '*');
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
        public JsonResult GetAllAnchors()
        {
            AnchorQuery query = new AnchorQuery();
            query.PageNo = 1;
            query.PageSize = int.MaxValue;
            query.ShopId = CurrentShop.Id;
            var result = LiveApplication.GetAnchorList(query);
            return Json(new { rows = result.Models, total = result.Total });

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
            else if (info.ShopId != CurrentShop.Id)
            {
                throw new HimallException("不是自己店铺的主播不能删除");
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
        /// <summary>
        /// 删除直播回放数据
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult DelLiveReplay(long roomId)
        {
            LiveRoomInfo liveRoom = LiveApplication.GetLiveRoomInfo(roomId);
            if (liveRoom == null || liveRoom.ShopId != CurrentShop.Id)
            {
                throw new HimallException("直播间不属于当前店铺,不能删除");
            }
            LiveApplication.DelLiveReplay(roomId);
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
            if (result == null || result.Count == 0)
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
    }
}