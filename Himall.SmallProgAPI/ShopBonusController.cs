using Himall.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Results;
using Himall.Core;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Himall.DTO;
using Senparc.Weixin.MP.AdvancedAPIs;

namespace Himall.SmallProgAPI
{
    public class ShopBonusController : BaseApiController
    {
        /// <summary>
        /// 根据带金红包Id查询带金红包详情
        /// </summary>
        /// <param name="ganId"></param>
        /// <returns></returns>
        public JsonResult<Result<SmallShopBonusView>> GetShopBounus(long  Id)
        {
            var bounus = ShopBonusApplication.GetByGrantId(Id);
            if (bounus == null)
            {
                throw new HimallException("带金红包不存在");
            }
            SmallShopBonusView shopbounusview = new SmallShopBonusView(bounus);
            shopbounusview.ShareImg = HimallIO.GetRomoteImagePath(shopbounusview.ShareImg);
            return JsonResult(shopbounusview);
        }

        /// <summary>
        /// 获取指定带金红包的领取记录
        /// </summary>
        /// <param name="ganId"></param>
        /// <returns></returns>
        public List<SmallShopBonusOtherReceiveView> GetOtherReceive(long ganId)
        {
            List<SmallShopBonusOtherReceiveView> models = new List<SmallShopBonusOtherReceiveView>();
            var result = ShopBonusApplication.GetDetailByGrantId(ganId);
            int randomIndex = 0;
            foreach (var re in result)
            {
                models.Add(new SmallShopBonusOtherReceiveView
                {
                    Name = re.WXName,
                    HeadImg = (!string.IsNullOrEmpty(re.WXHead) && re.WXHead.IndexOf("http://") == -1 && re.WXHead.IndexOf("https://") == -1) ? HimallIO.GetRomoteImagePath(re.WXHead) : re.WXHead,
                    Copywriter = RandomStr(randomIndex),
                    Price = (decimal)re.Price,
                    ReceiveTime = ((DateTime)re.ReceiveTime).ToString("yyyy-MM-dd HH:mm")
                });
                randomIndex++;
                if (randomIndex > 4)
                {
                    randomIndex = 0;
                }
            }

            return models;
        }

        /// <summary>
        /// 添加领取记录
        /// </summary>
        /// <returns></returns>

        public JsonResult<Result<dynamic>> GetAddShopBonusReceive(long id)
        {
            var bouns=ShopBonusApplication.GetByGrantId(id);
            if (bouns == null)
            {
                throw new HimallException("红包不存在！");
            }
            string openId = CurrentUserOpenId;
            string headImg = "", nickname = "";
            if (CurrentUserId > 0)//如果已存在的客户
            {
                headImg = CurrentUser.PhotoUrl;
                nickname = CurrentUser.ShowNick;
            }
            else
            {
                try
                {
                    var settings = SiteSettingApplication.SiteSettings;
                    var token = WXApiApplication.TryGetToken(settings.WeixinAppletId, settings.WeixinAppletSecret, true);
                    var wxuseInfo = OAuthApi.GetUserInfo(token, openId);//获取微信用户信息
                    headImg = wxuseInfo.headimgurl;
                    nickname = wxuseInfo.nickname;

                }
                catch (Exception e)
                {
                    Exception innerEx = e.InnerException == null ? e : e.InnerException;
                    Log.Error(innerEx.Message);
                    if (innerEx.Message.IndexOf("code been used") != -1)
                    {
                        throw new HimallException("该红包已被领取过");
                    }
                    else
                    {
                        throw new HimallException(innerEx.Message);
                    }
                }

            }
            ShopReceiveModel receiveobj = new ShopReceiveModel();
          
            SmallShopBonusView model = new SmallShopBonusView(bouns);

            if (model.DateEnd <= DateTime.Now || model.IsInvalid || model.BonusDateEnd <= DateTime.Now)  //过期、失效
            {
                receiveobj = new ShopReceiveModel { State = ShopReceiveStatus.Invalid, Price = 0 };
            }
            else if (model.DateStart > DateTime.Now) // 未开始
            {
                receiveobj = new ShopReceiveModel { State = ShopReceiveStatus.NoStart, Price = 0 };
            }
            else
            {
                receiveobj = (ShopReceiveModel)ShopBonusApplication.Receive(id, openId, headImg, nickname);
            }
            var Recevielist = GetOtherReceive(id);
            var result = new
            {
                ShopReceiveModel = receiveobj,
                RedEnvelopeGetRecords = Recevielist
            };

            return JsonResult<dynamic>(result);
        }
        private string RandomStr(int index)
        {
            string[] strs =
            {
                "手气不错，以后购物就来这家店了",
                "抢红包，姿势我最帅",
                "人品攒的好，红包来的早",
                "这个发红包的老板好帅",
                "多谢，老板和宝贝一样靠谱"
            };
            return strs[index];
        }
    }
}
