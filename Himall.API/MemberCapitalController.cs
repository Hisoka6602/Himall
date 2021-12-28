using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using System;
using System.Linq;

namespace Himall.API
{
    public class MemberCapitalController : BaseApiController
    {
        private const string PLUGIN_OAUTH_WEIXIN = "Himall.Plugin.OAuth.WeiXin";
        public object Get()
        {
            CheckUserLogin();
            var capital = MemberCapitalApplication.GetCapitalInfo(CurrentUserId);
            var sitesetting = SiteSettingApplication.SiteSettings;
            var redPacketAmount = 0M;
            if (capital != null)
            {
                //redPacketAmount = capital.Himall_CapitalDetail.Where(e => e.SourceType == Himall.Model.CapitalDetailInfo.CapitalDetailType.RedPacket).Sum(e => e.Amount);
                redPacketAmount = MemberCapitalApplication.GetSumRedPacket(capital.Id);
            }
            else
            {
                capital = new CapitalInfo();
            }
            bool canWithDraw = MemberApplication.CanWithdraw(CurrentUser.Id);
            return new
            {
                success = true,
                Balance = capital.Balance,
                RedPacketAmount = redPacketAmount,
                PresentAmount =  capital.PresentAmount,
                ChargeAmount = capital.ChargeAmount,
                WithDrawMinimum = sitesetting.WithDrawMinimum,
                WithDrawMaximum = sitesetting.WithDrawMaximum,
                canWithDraw= canWithDraw,
                isOpen = sitesetting.IsOpenRechargePresent,
                rule = RechargePresentRuleApplication.GetRules()
            };
        }

        public object GetList(int pageNo = 1, int pageSize = 10)
        {
            CheckUserLogin();
            var capitalService = ObjectContainer.Current.Resolve<MemberCapitalService>();

            var query = new CapitalDetailQuery
            {
                memberId = CurrentUser.Id,
                PageSize = pageSize,
                PageNo = pageNo
            };
            var pageMode = capitalService.GetCapitalDetails(query);
            var model = pageMode.Models.ToList().Select(e => new CapitalDetailModel
            {
                Id = e.Id,
                Amount = e.Amount +  e.PresentAmount,
                PresentAmount = e.PresentAmount,
                CapitalID = e.CapitalID,
                CreateTime = e.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                SourceData = e.SourceData,
                SourceType = e.SourceType,
                Remark = e.SourceType == CapitalDetailInfo.CapitalDetailType.Brokerage ? GetBrokerageRemark(e) : e.Remark,
                PayWay = e.Remark
            });
            dynamic result = SuccessResult();
            result.rows = model;
            result.total = pageMode.Total;

            return result;
        }
        /// <summary>
        /// 获取分销备注
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        string GetBrokerageRemark(CapitalDetailInfo data)
        {
            var remark = data.Remark;
            if (remark.IndexOf(',') > -1)
            {
                remark = remark.Replace("Id", "号").Split(',')[1];
            }
            return data.SourceType.ToDescription() + " " + remark;
        }
        private string GetRemark(CapitalDetailInfo data)
        {
            string result = "";
            if (data == null) return result;
            result = data.SourceType.ToDescription() + "(单号 " + (string.IsNullOrWhiteSpace(data.SourceData) ? data.Id.ToString() : data.SourceData) + ")";
            switch (data.SourceType)
            {
                case CapitalDetailInfo.CapitalDetailType.ChargeAmount:
                    if (data.PresentAmount > 0)
                    {
                        result = data.SourceType.ToDescription() + "，充" + data.Amount + "送" + data.PresentAmount + "元";
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// 是否可以提现
        /// </summary>
        /// <returns></returns>
        public object GetCanWithDraw()
        {
            CheckUserLogin();
            bool canWeiXin = false;
            bool canAlipay = false;
            var sitesetting = SiteSettingApplication.SiteSettings;
            //判断是否有微信openid
            var mo = MemberApplication.GetMemberOpenIdInfoByuserId(CurrentUser.Id, Entities.MemberOpenIdInfo.AppIdTypeEnum.Payment, PLUGIN_OAUTH_WEIXIN);
            if (mo != null && !string.IsNullOrWhiteSpace(mo.OpenId)) { canWeiXin = true; }
            //判断是否开启支付宝
            if (sitesetting.Withdraw_AlipayEnable)
            {
                canAlipay = true;
            }
            bool canWithDraw = MemberApplication.CanWithdraw(CurrentUser.Id);
            dynamic result = new Result();
            result.success = canWithDraw && (canWeiXin || canAlipay);
            result.canWeiXin = canWeiXin;
            result.canAlipay = canAlipay;
            return result;
        }
        /// <summary>
        /// 申请提现
        /// </summary>
        /// <param name="openid"></param>
        /// <param name="nickname"></param>
        /// <param name="amount"></param>
        /// <param name="pwd"></param>
        /// <param name="applyType"></param>
        /// <returns></returns>
        public object PostApplyWithDraw(MemberCapitalApplyWithDrawModel para)
        {
            CheckUserLogin();
            if (para == null)
            {
                para = new MemberCapitalApplyWithDrawModel();
            }
            if (string.IsNullOrEmpty(para.pwd))
            {
                throw new HimallException("请输入密码！");
            }
            if (para.amount <= 0)
                throw new HimallException("提现金额不能小于等于0！");
            var success = MemberApplication.VerificationPayPwd(CurrentUser.Id, para.pwd);
            var sitesetting = SiteSettingApplication.SiteSettings;
            if (para.applyType == CommonModel.UserWithdrawType.ALiPay.GetHashCode() && !sitesetting.Withdraw_AlipayEnable)
            {
                throw new HimallException("不支持支付宝提现方式！");
            }
            var _MemberCapitalService = ObjectContainer.Current.Resolve<MemberCapitalService>();
            if (!success)
            {
                throw new HimallException("支付密码不对，请重新输入！");
            }
            var balance = MemberCapitalApplication.GetBalanceByUserId(CurrentUser.Id);
            if (para.amount > balance)
                throw new HimallException("提现金额不能超出可用金额！");

            if (string.IsNullOrWhiteSpace(para.openid) && para.applyType == CommonModel.UserWithdrawType.WeiChat.GetHashCode())
            {
                var mo = MemberApplication.GetMemberOpenIdInfoByuserId(CurrentUser.Id, Entities.MemberOpenIdInfo.AppIdTypeEnum.Payment, PLUGIN_OAUTH_WEIXIN);
                if (mo != null && !string.IsNullOrWhiteSpace(mo.OpenId))
                {
                    para.openid = mo.OpenId;
                }
            }
            if (string.IsNullOrWhiteSpace(para.nickname) && para.applyType == CommonModel.UserWithdrawType.ALiPay.GetHashCode())
            {
                throw new HimallException("数据异常,真实姓名不可为空！");
            }
            if (!string.IsNullOrWhiteSpace(para.openid) && para.applyType == CommonModel.UserWithdrawType.WeiChat.GetHashCode())
            {
                //para.openid = Core.Helper.SecureHelper.AESDecrypt(para.openid, "Mobile");
                if (!(string.IsNullOrWhiteSpace(sitesetting.WeixinAppId) || string.IsNullOrWhiteSpace(sitesetting.WeixinAppSecret)))
                {
                    string token = WXApiApplication.TryGetToken(sitesetting.WeixinAppId, sitesetting.WeixinAppSecret);
                    var userinfo = Senparc.Weixin.MP.CommonAPIs.CommonApi.GetUserInfo(token, para.openid);
                    if (userinfo != null)
                    {
                        para.nickname = userinfo.nickname;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(para.openid))
            {
                throw new HimallException("数据异常,OpenId或收款账号不可为空！");
            }

            Himall.Entities.ApplyWithDrawInfo model = new Himall.Entities.ApplyWithDrawInfo()
            {
                ApplyAmount = para.amount,
                ApplyStatus = Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.WaitConfirm,
                ApplyTime = DateTime.Now,
                MemId = CurrentUser.Id,
                OpenId = para.openid,
                NickName = para.nickname,
                ApplyType = (CommonModel.UserWithdrawType)para.applyType
            };
            _MemberCapitalService.AddWithDrawApply(model);
            return SuccessResult();
        }
    }
}