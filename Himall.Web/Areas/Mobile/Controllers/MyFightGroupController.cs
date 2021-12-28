using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Areas.Mobile.Models;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    /// <summary>
    /// 用户拼团
    /// </summary>
    public class MyFightGroupController : BaseMobileMemberController
    {
        private ProductService _ProductService;
        private long curUserId = 0;
        public MyFightGroupController(ProductService ProductService
            )
        {
            _ProductService = ProductService;
        }

        #region 我的拼团
        /// <summary>
        /// 我的拼团
        /// </summary>
        /// <returns></returns>
        public ActionResult MyGroups()
        {
            return View();
        }
        [HttpPost]
        public JsonResult PostJoinGroups(int page)
        {
            var data = FightGroupApplication.GetFightGroupOrderByUser(UserId, 5, page);
            return Json(new { success = true, data = data.Models, total = data.Total });
        }
        #endregion

        #region 拼团详情
        /// <summary>
        /// 我的拼团详情
        /// </summary>
        /// <param name="id"></param>
        /// <param name="aid"></param>
        /// <returns></returns>
        public ActionResult GroupDetail(long id, long aid, long orderId = 0)
        {
            FightGroupActiveModel gpact = FightGroupApplication.GetActive(aid, false);
            if (gpact == null)
            {
                throw new HimallException("错误的活动信息");
            }
            FightGroupsModel groupsdata = FightGroupApplication.GetGroup(aid, id);
            if (groupsdata == null)
            {
                throw new HimallException("错误的拼团信息");
            }
            if (groupsdata.BuildStatus == FightGroupBuildStatus.Opening)
            {
                //throw new HimallException("开团未成功，等待团长付款中");
                return Redirect(string.Format("/m-{0}/Member/Center/", PlatformType.ToString()));
            }
            //groupsdata.ProductDefaultImage = gpact.ProductDefaultImage;

            var orderDetail = FightGroupApplication.GetFightGroupOrderStatusByOrderId(orderId);
            if (orderDetail != null)
            {
                var currentsku = ProductManagerApplication.GetSKUInfo(orderDetail.SkuId);
                if (currentsku != null)
                {
                    if (!string.IsNullOrEmpty(currentsku.ShowPic))
                        groupsdata.ProductDefaultImage = HimallIO.GetRomoteImagePath(currentsku.ShowPic);
                }
            }

            MyFightGroupDetailModel model = new MyFightGroupDetailModel();
            model.ActiveData = gpact;
            model.GroupsData = groupsdata;

            model.ShareUrl = string.Format("{0}/m-{1}/FightGroup/GroupDetail/{2}?aid={3}", CurrentUrlHelper.CurrentUrlNoPort(), "WeiXin", groupsdata.Id, groupsdata.ActiveId);
            model.ShareTitle = "我参加了(" + groupsdata.ProductName + ")的拼团";
            model.ShareImage = gpact.ProductDefaultImage;
            if (!string.IsNullOrWhiteSpace(model.ShareImage))
            {
                if (model.ShareImage.Substring(0, 4) != "http")
                {
                    model.ShareImage = HimallIO.GetRomoteImagePath(model.ShareImage);
                }
            }

            int neednum = groupsdata.LimitedNumber - groupsdata.JoinedNumber;
            neednum = neednum < 0 ? 0 : neednum;
            if (neednum > 0)
            {
                model.ShareDesc = "还差" + neednum + "人即可成团";
            }
            if (!string.IsNullOrWhiteSpace(gpact.ProductShortDescription))
            {
                if (!string.IsNullOrWhiteSpace(model.ShareDesc))
                {
                    model.ShareDesc += "，(" + gpact.ProductShortDescription + ")";
                }
                else
                {
                    model.ShareDesc += gpact.ProductShortDescription;
                }
            }
            return View(model);
        }
        #endregion
    }
}