using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Framework;
using Newtonsoft.Json;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class ScratchCardController : BaseMobileTemplatesController
    {
        private BonusService _BonusService;
        private MemberIntegralService _iMemberIntegralService;
        private long curUserId;
        private WeiActivityWinModel activityWinModel;
        private int consumePoint;

        public ScratchCardController(BonusService BonusService, MemberIntegralService MemberIntegralService)
        {
            _BonusService = BonusService;
            _iMemberIntegralService = MemberIntegralService;
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (siteSetting.IsOpenH5) {

                if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppId) || string.IsNullOrWhiteSpace(siteSetting.WeixinAppSecret))
                {
                    throw new HimallException("未配置公众号参数");
                }

            }
      
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (CurrentUser != null)
            {
                curUserId = CurrentUser.Id;
            }
        }

        public ActionResult Index(long id)
        {
            //TODO:改成统一的方式取 Token
            var settings = SiteSettingApplication.SiteSettings;
            if (settings.IsOpenH5)
            {
                var token = WXApiApplication.TryGetToken(settings.WeixinAppId, settings.WeixinAppSecret);

                SceneHelper helper = new SceneHelper();
                var qrTicket = QrCodeApi.Create(token, 86400, 7758258, Senparc.Weixin.MP.QrCode_ActionName.QR_SCENE).ticket;

                ViewBag.QRTicket = qrTicket;
            }

            WeiActivityModel activityModel = WeiActivityApplication.GetActivityModel(id);
            activityModel.userId = curUserId;
            activityModel.winModel = AddWinInfo(activityModel);
            activityModel.winModel.integrals = MemberIntegralApplication.GetAvailableIntegral(curUserId);
            activityModel.participationCount = GetParticipationCount(activityModel);
            consumePoint = activityModel.consumePoint;
            return View(activityModel);
        }
        /// <summary>
        /// 计算用户可抽奖次数
        /// </summary>
        /// <param name="activityModel"></param>
        /// <returns></returns>
        public int GetParticipationCount(WeiActivityModel activityModel)
        {
            int participationCount = WeiActivityApplication.GetWinModel(activityModel);
            if (participationCount != -1)
            {
                return activityModel.participationCount - participationCount;
            }
            else
            {
                return participationCount;
            }
        }

        /// <summary>
        /// 获取中奖概率
        /// </summary>
        /// <param name="id"></param>
        /// <param name="awardId"></param>
        /// <returns></returns>
        public double GetCount(long id, long awardId)
        {
            var item = WeiActivityApplication.GetActivityModel(id);
            var proportions = 0.0;//抽取完成的概率总和
            float sum = 0;
            List<WeiActivityAwardModel> listModel = item.awards;
            var model = listModel.Where(p => p.Id == awardId).ToList();
            foreach (var t in item.awards)
            {
                bool falg = WeiActivityApplication.GetWinNumberByAward(id, t.Id);//查询当前奖等是否有剩余数量
                if (!falg)//无剩余数量
                {

                    proportions += t.proportion;
                    continue;
                }
                if (t.awardLevel < model[0].awardLevel)
                    sum += t.proportion;
            }

            var isOver = WeiActivityApplication.GetWinNumberByAward(id, awardId);//是否还有奖品
            if (!isOver)
            {
                return 0;
            }
            else
            {
                return proportions + sum + model[0].proportion;
            }

        }
        public WeiActivityWinModel AddWinInfo(WeiActivityModel activityModel)
        {
            activityWinModel = new WeiActivityWinModel();
            Random r = new Random();
            activityWinModel.activityId = activityModel.Id;
            activityWinModel.addDate = DateTime.Now;
            activityWinModel.userId = curUserId;

            activityWinModel.isWin = false;
            activityWinModel.awardId = 0;
            activityWinModel.awardName = "未中奖";
            activityWinModel.awardType = WeiActivityAwardType.Integral;
            // var activityModel=WeiActivityWinApplication.GetWinModel()
            #region 重新计算中奖
            if (activityModel.awards != null && activityModel.awards.Count > 0)
            {
                var winCounts = WeiActivityApplication.GetActivityWin(activityModel.Id);
                int maxProportion = 0;
                float nullAwardProportion = 0;
                Dictionary<long, int> dicAwards = new Dictionary<long, int>();
                foreach (var item in activityModel.awards)
                {
                    var winCount = winCounts.Where(x => x.AwardId == item.Id).Count();
                    if (winCount < item.awardCount)
                    {
                        maxProportion += Convert.ToInt32(item.proportion * 100);
                        dicAwards.Add(item.Id, maxProportion);
                    }
                    else
                    {
                        nullAwardProportion += item.proportion;
                    }
                }
                nullAwardProportion += 100 - activityModel.awards.Sum(x => x.proportion);
                maxProportion += Convert.ToInt32(nullAwardProportion * 100);
                dicAwards.Add(0, maxProportion);


                int num = r.Next(1, maxProportion);//获取随机数做为中奖信息

                Log.Info(dicAwards.ToJSON() + "+" + num);
                var winAward = dicAwards.OrderBy(x => x.Value).FirstOrDefault(x => x.Value >= num);
                if (winAward.Key > 0)
                {
                    var item = activityModel.awards.FirstOrDefault(x => x.Id == winAward.Key);
                    if (item.awardType == WeiActivityAwardType.Integral)
                    {
                        activityWinModel.awardName = item.integral.ToString();
                        activityWinModel.awardId = item.Id;
                        activityWinModel.awardType = item.awardType;
                        activityWinModel.isWin = true;
                        return activityWinModel;
                    }
                    else if (item.awardType == WeiActivityAwardType.Bonus)
                    {
                        Entities.BonusInfo bonusInfo = _BonusService.Get(item.bonusId);
                        string Surplus = _BonusService.GetBonusSurplus(item.bonusId);
                        if (bonusInfo.IsInvalid)//红包已经失效 返回 未中奖
                        {
                            return activityWinModel;
                        }
                        if (Convert.ToInt32(Surplus) <= 0)//当前红包已经领取完 设置未中奖
                        {
                            return activityWinModel;
                        }

                        //获取红包名称

                        activityWinModel.awardName = BonusApplication.GetFirstReceivePriceByBonus(bonusInfo.Id) + "元红包";
                        activityWinModel.awardId = item.Id;
                        activityWinModel.awardType = item.awardType;
                        activityWinModel.bonusId = bonusInfo.Id;
                        activityWinModel.isWin = true;
                        return activityWinModel;
                    }
                    else if (item.awardType == WeiActivityAwardType.Coupon)
                    {
                        CouponModel couponModel = CouponApplication.Get(item.couponId);
                        int perMax = WeiActivityApplication.GetCouPonMax(activityWinModel.userId, activityWinModel.activityId, item.Id);

                        if (couponModel == null)
                        {
                            return activityWinModel;
                        }
                        if (couponModel.Num <= 0)//无库存 返回 未中奖
                        {
                            return activityWinModel;
                        }
                        if (couponModel.EndTime < DateTime.Now)//优惠券失效
                        {
                            return activityWinModel;
                        }
                        if (couponModel.perMax != 0 && perMax >= couponModel.perMax)
                        {
                            return activityWinModel;
                        }

                        //获取优惠券名称
                        string awardName = couponModel.CouponName;
                        if (couponModel.ShopName != "")
                        {
                            awardName = awardName + "(" + couponModel.ShopName + ")";
                        }
                        if (couponModel.OrderAmount != "")
                        {
                            activityWinModel.amount = "(" + couponModel.OrderAmount + ")";
                        }
                        activityWinModel.awardName = awardName;
                        activityWinModel.awardId = item.Id;
                        activityWinModel.awardType = item.awardType;
                        activityWinModel.couponId = couponModel.Id;
                        activityWinModel.isWin = true;
                        return activityWinModel;
                    }

                }
            }
            #endregion
            return activityWinModel;
        }
        public ActionResult Add(long activityId)
        {

            WeiActivityWinModel productComments = new WeiActivityWinModel();
            //用于提交时 积分参数为抽奖需消耗积分
            if (activityId > 0)
            {
                //重新抓取奖项
                WeiActivityModel activityModel = WeiActivityApplication.GetActivityModel(activityId);
                productComments = AddWinInfo(activityModel);
                productComments.integrals = activityModel.consumePoint;
                int participationCount = activityModel != null ? GetParticipationCount(activityModel) : 0;
                if (participationCount > 0 || participationCount == -1)
                    WeiActivityApplication.WinnerSubmit(productComments);
            }
            return Json<dynamic>(success: true, msg: "成功", data: productComments);
        }

    }
}