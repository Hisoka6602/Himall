using System;
using System.Collections.Generic;
using System.Text;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.CommonModel;
using Himall.DTO;
using Himall.Entities;
using NetRube.Data;
using Himall.Core.Plugins.Message;
using System.Threading.Tasks;
using Himall.Core;
using System.Linq;
using static Himall.Entities.CapitalDetailInfo;

namespace Himall.Service
{
    public class MemberCapitalService : ServiceBase
    {

        public QueryPageModel<MemberCapital> GetCapitals(CapitalQuery query)
        {
            var db = DbFactory.Default.Get<CapitalInfo>();
            if (query.MemberId > 0)
                db.Where(e => e.MemId == query.MemberId);

            if (!string.IsNullOrEmpty(query.UserName) || !string.IsNullOrEmpty(query.CellPhone) || !string.IsNullOrEmpty(query.Nick) || !string.IsNullOrEmpty(query.RealName))
            {
                var dbmem = DbFactory.Default.Get<MemberInfo>();
                if (!string.IsNullOrEmpty(query.UserName))
                    dbmem.Where(p => p.UserName.Contains(query.UserName) || p.Nick.Contains(query.UserName));
                if (!string.IsNullOrEmpty(query.CellPhone))
                    dbmem.Where(p => p.CellPhone.Contains(query.CellPhone));
                if (!string.IsNullOrEmpty(query.Nick))
                    dbmem.Where(p => p.Nick.Contains(query.Nick));
                if (!string.IsNullOrEmpty(query.RealName))
                    dbmem.Where(p => p.RealName.Contains(query.RealName));

                var idlist = dbmem.Select(t => t.Id).ToList<long>();
                if (idlist != null && idlist.Count > 0)
                    db.Where(p => p.MemId.ExIn(idlist));
                else
                    db.Where(p => p.Id == 0);//说明没找到

                #region 这里是如搜索会员，如会员之前没有创建余额表数据初始一条余额记录
                var membercheck = DbFactory.Default.Get<MemberInfo>().Where(t => t.UserName == query.UserName).Select(t => t.Id).FirstOrDefault();
                if (membercheck != null && membercheck.Id > 0)
                {
                    GetCapitalInfo(membercheck.Id);
                }
                #endregion
            }

            switch (query.Sort.ToLower())
            {
                case "balance":
                    if (query.IsAsc) db.OrderBy<CapitalInfo>(p => p.Balance);
                    else db.OrderByDescending<CapitalInfo>(p => p.Balance);
                    break;
                case "freezeamount":
                    if (query.IsAsc) db.OrderBy<CapitalInfo>(p => p.FreezeAmount);
                    else db.OrderByDescending<CapitalInfo>(p => p.FreezeAmount);
                    break;
                case "chargeamount":
                    if (query.IsAsc) db.OrderBy<CapitalInfo>(p => p.ChargeAmount);
                    else db.OrderByDescending<CapitalInfo>(p => p.ChargeAmount);
                    break;
                case "presentamount":
                    if (query.IsAsc) db.OrderBy<CapitalInfo>(p => p.PresentAmount);
                    else db.OrderByDescending<CapitalInfo>(p => p.PresentAmount);
                    break;
                default:
                    db.OrderByDescending<CapitalInfo>(e => e.Balance);
                    break;
            }

            var page = db.ToPagedList(query.PageNo, query.PageSize);
            if (page == null || page.TotalRecordCount <= 0)
            {
                return new QueryPageModel<MemberCapital>
                {
                    Models = null,
                    Total = 0
                };
            }

            List<MemberCapital> listmem = new List<MemberCapital>();

            #region 之前用表关联查询数据很慢，则只一个表查询，内容下面循环读取出来
            var memIds = page.Select(p => p.MemId).ToList();
            var mempages = DbFactory.Default.Get<MemberInfo>().Where(p => p.Id.ExIn(memIds));
            var mems = mempages.Select(p => new { Id = p.Id, RealName = p.RealName, Nick = p.Nick, UserId = p.Id, UserName = p.UserName, CellPhone = p.CellPhone }).ToList();

            if (mems != null)
            {
                foreach (var item in page)
                {
                    var meminfo = mems.Where(p => p.Id == item.MemId).FirstOrDefault();

                    var memcapital = new MemberCapital();
                    if (meminfo != null)
                    {
                        memcapital.Id = meminfo.Id;
                        memcapital.RealName = meminfo.RealName;
                        memcapital.UserId = meminfo.Id;
                        memcapital.UserName = meminfo.UserName;
                        memcapital.CellPhone = meminfo.CellPhone;
                        memcapital.Nick = meminfo.Nick;
                    }

                    memcapital.Balance = item.Balance;
                    memcapital.ChargeAmount = item.ChargeAmount;
                    memcapital.FreezeAmount = item.FreezeAmount;
                    memcapital.PresentAmount = item.PresentAmount;

                    listmem.Add(memcapital);
                }
            }
            #endregion


            return new QueryPageModel<MemberCapital>
            {
                Models = listmem,
                Total = page.TotalRecordCount
            };
        }

        public QueryPageModel<CapitalDetailInfo> GetCapitalDetails(CapitalDetailQuery query)
        {
            //var capitalDetail = Context.CapitalDetailInfo.Where(item => item.Himall_Capital.MemId == query.memberId);

            //var capital = DbFactory.Default.Get<CapitalInfo>().Where(p => p.MemId == query.memberId).Select(p => p.Id).ToList<long>();
            var capitalDetail = DbFactory.Default
                .Get<CapitalDetailInfo>()
                .InnerJoin<CapitalInfo>((cdi, ci) => cdi.CapitalID == ci.Id && ci.MemId == query.memberId);
            if (query.capitalType.HasValue && query.capitalType.Value != 0)
            {
                capitalDetail.Where(e => e.SourceType == query.capitalType.Value);
            }
            if (query.startTime.HasValue)
            {
                capitalDetail.Where(e => e.CreateTime >= query.startTime);
            }
            if (query.endTime.HasValue)
            {
                capitalDetail.Where(e => e.CreateTime < query.endTime);
            }
            //int total = 0;
            //var model = capitalDetail.GetPage(p => p.OrderByDescending(e => e.CreateTime), out total, query.PageNo, query.PageSize);
            var model = capitalDetail.OrderByDescending(e => e.CreateTime).Select().ToPagedList(query.PageNo, query.PageSize);

            QueryPageModel<CapitalDetailInfo> result = new QueryPageModel<CapitalDetailInfo> { Models = model, Total = model.TotalRecordCount };
            return result;
        }

        public QueryPageModel<ApplyWithDrawInfo> GetApplyWithDraw(ApplyWithDrawQuery query)
        {
            var db = WhereBuilder(query);
            if (string.IsNullOrWhiteSpace(query.Sort))
                db.OrderBy(e => e.ApplyStatus).OrderByDescending(e => e.ApplyTime);
            else
                db.OrderByDescending(e => e.ApplyTime);

            var model = db.ToPagedList(query.PageNo, query.PageSize);
            return new QueryPageModel<ApplyWithDrawInfo> { Models = model, Total = model.TotalRecordCount };
        }

        public int GetApplyWithDrawCount(ApplyWithDrawQuery query)
        {
            var db = WhereBuilder(query);
            return db.Count();
        }

        private GetBuilder<ApplyWithDrawInfo> WhereBuilder(ApplyWithDrawQuery query)
        {
            var db = DbFactory.Default.Get<ApplyWithDrawInfo>();

            if (query.MemberId.HasValue)
                db.Where(e => e.MemId == query.MemberId);

            if (query.WithDrawNo.HasValue)
                db.Where(e => e.Id == query.WithDrawNo);

            if (query.withDrawStatus.HasValue && query.withDrawStatus.Value > 0)
                db.Where(e => e.ApplyStatus == query.withDrawStatus.Value);

            if (query.ApplyType.HasValue)
                db.Where(e => e.ApplyType == query.ApplyType.Value);

            return db;
        }
        /// <summary>
        /// 获取提现记录
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ApplyWithDrawInfo GetApplyWithDrawInfo(long Id)
        {
            return DbFactory.Default.Get<ApplyWithDrawInfo>().Where(r => r.Id == Id).FirstOrDefault();
        }
        public List<ApplyWithDrawInfo> GetApplyWithDrawInfoByIds(IEnumerable<long> Ids)
        {
            return DbFactory.Default.Get<ApplyWithDrawInfo>().Where(e => e.Id.ExIn(Ids)).ToList();
        }
        public void ConfirmApplyWithDraw(ApplyWithDrawInfo info)
        {
            var model = DbFactory.Default.Get<ApplyWithDrawInfo>().Where(e => e.Id == info.Id).FirstOrDefault();

            var flag = DbFactory.Default.InTransaction(() =>
            {
                model.ApplyStatus = info.ApplyStatus;
                model.OpUser = info.OpUser;
                model.Remark = info.Remark;
                model.ConfirmTime = info.ConfirmTime.HasValue ? info.ConfirmTime.Value : DateTime.Now;
                //Context.SaveChanges();
                DbFactory.Default.Update(model);
                if (info.ApplyStatus == ApplyWithDrawInfo.ApplyWithDrawStatus.WithDrawSuccess)
                {
                    //model.PayNo = info.PayNo;
                    //model.PayTime = info.PayTime.HasValue ? info.PayTime.Value : DateTime.Now;
                    //DbFactory.Default.Update(model);
                    DbFactory.Default.Set<ApplyWithDrawInfo>().Set(n => n.PayNo, info.PayNo).Set(n => n.PayTime, info.PayTime.HasValue ? info.PayTime.Value : DateTime.Now).Where(p => p.Id == model.Id).Succeed();
                    CapitalDetailModel capitalDetail = new CapitalDetailModel
                    {
                        Amount = -info.ApplyAmount,
                        UserId = model.MemId,
                        PayWay = info.Remark,
                        SourceType = CapitalDetailInfo.CapitalDetailType.WithDraw,
                        SourceData = info.Id.ToString()
                    };
                    AddCapital(capitalDetail, false);
                }
                //scope.Complete();
                return true;
            });
            if (flag)
            {
                //发送消息
                var member = DbFactory.Default.Get<MemberInfo>(m => m.Id == model.MemId).FirstOrDefault();
                var message = new MessageWithDrawInfo();
                message.UserName = model.NickName;
                message.UserName = member != null ? member.UserName : "";
                message.Amount = model.ApplyAmount;
                message.ApplyType = model.ApplyType.GetHashCode();
                message.ApplyTime = model.ApplyTime;
                message.Remark = model.Remark;
                message.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;

                if (info.ApplyStatus == ApplyWithDrawInfo.ApplyWithDrawStatus.WithDrawSuccess)
                    Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnMemberWithDrawSuccess(model.MemId, message));

                if (info.ApplyStatus == ApplyWithDrawInfo.ApplyWithDrawStatus.Refuse)
                    Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnMemberWithDrawFail(model.MemId, message));
            }
        }

        public void AddWithDrawApply(ApplyWithDrawInfo model)
        {
            DbFactory.Default.Add(model);
            var capital = DbFactory.Default.Get<CapitalInfo>().Where(e => e.MemId == model.MemId).FirstOrDefault();
            capital.Balance -= model.ApplyAmount;
            capital.FreezeAmount = capital.FreezeAmount + model.ApplyAmount;
            var result = DbFactory.Default.Update(capital);
            //发送消息
            if (result > 0)
            {
                var member = DbFactory.Default.Get<MemberInfo>(m => m.Id == model.MemId).FirstOrDefault();
                var message = new MessageWithDrawInfo();
                message.UserName = model.NickName;
                message.UserName = member != null ? member.UserName : "";
                message.Amount = model.ApplyAmount;
                message.ApplyType = model.ApplyType.GetHashCode();
                message.ApplyTime = model.ApplyTime;
                message.Remark = model.Remark;
                message.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnMemberWithDrawApply(model.MemId, message));
            }
        }


        public void AddCapital(CapitalDetailModel model, bool isAccrualRecharge = true)
        {
            var capital = GetCapitalInfo(model.UserId);
            decimal balance = 0;
            decimal presentAmount = 0;
            //充值赠送
            if (model.PresentAmount.HasValue && model.PresentAmount > 0)
            {
                presentAmount = model.PresentAmount.Value;
            }
            decimal chargeAmount = model.Amount;
            decimal freezeAmount = 0;
            StringBuilder strBuilder = new StringBuilder();
            if (presentAmount > 0)
            {
                strBuilder.Append("充" + model.Amount + "元赠送" + presentAmount + "元");
            }
            else
            {
                strBuilder.Append(model.Remark);
            }
            switch (model.SourceType)
            {
                case CapitalDetailInfo.CapitalDetailType.ChargeAmount:
                    balance = chargeAmount + presentAmount;
                    break;
                case CapitalDetailInfo.CapitalDetailType.WithDraw:
                    freezeAmount = model.Amount;
                    break;
                default:
                    balance = chargeAmount + presentAmount;
                    break;
            }

            var capitalDetail = new CapitalDetailInfo()
            {
                Id = CreateCode(model.SourceType),
                Amount = model.Amount,
                PresentAmount = model.PresentAmount.HasValue ? model.PresentAmount.Value : 0,
                CreateTime = DateTime.Parse(model.CreateTime),
                CapitalID = capital.Id,
                SourceType = model.SourceType,
                SourceData = model.SourceData,
                Remark = strBuilder.ToString()
            };
            DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Add(capitalDetail);
                var db = DbFactory.Default.Set<CapitalInfo>()
                    .Where(p => p.Id == capital.Id)
                    .Set(p => p.Balance, p => p.Balance + balance)
                    .Set(p => p.FreezeAmount, p => p.FreezeAmount + freezeAmount);
                if (presentAmount > 0)
                    db.Set(p => p.PresentAmount, p => p.PresentAmount + presentAmount);
                if (isAccrualRecharge && chargeAmount > 0)
                    db.Set(p => p.ChargeAmount, p => p.ChargeAmount + chargeAmount);
                db.Execute();
            });
        }

        public void ConsumeCapital(long memberId, long orderId, decimal amount)
        {
            var capital = GetCapitalInfo(memberId);
            DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Set<CapitalInfo>()
                    .Where(p => p.MemId == memberId)
                    .Set(p => p.Balance, p => p.Balance - amount)
                    .Execute();
                DbFactory.Default.Add(new CapitalDetailInfo
                {
                    Id = CreateCode(CapitalDetailType.Consume),
                    Amount = -amount,
                    CreateTime = DateTime.Now,
                    CapitalID = capital.Id,
                    SourceType = CapitalDetailType.Consume,
                    Remark = orderId.ToString(),
                    SourceData = orderId.ToString(),
                    PresentAmount = 0,
                });
            });
        }

        /// <summary>
        /// 充值成功
        /// </summary>
        /// <param name="chargeDetailId"></param>
        public void ChargeSuccess(long chargeDetailId, string remark = "")
        {
            var chargeDetail = DbFactory.Default.Get<ChargeDetailInfo>().Where(p => p.Id == chargeDetailId).FirstOrDefault();
            if (chargeDetail == null)
                return;

            chargeDetail.ChargeStatus = ChargeDetailInfo.ChargeDetailStatus.ChargeSuccess;
            var capitalDetail = DbFactory.Default.Get<CapitalDetailInfo>().Where(e => e.SourceData == chargeDetailId.ToString() && e.SourceType == CapitalDetailInfo.CapitalDetailType.ChargeAmount).FirstOrDefault();
            if (capitalDetail != null)//已经处理过直接返回
                return;
            DbFactory.Default.Update(chargeDetail);
            CapitalDetailModel detail = new CapitalDetailModel
            {
                Id = CreateCode(CapitalDetailInfo.CapitalDetailType.ChargeAmount),
                UserId = chargeDetail.MemId,
                Amount = chargeDetail.ChargeAmount,
                CreateTime = DateTime.Now.ToString(),
                SourceType = CapitalDetailInfo.CapitalDetailType.ChargeAmount,
                SourceData = chargeDetailId.ToString(),
                Remark = remark,
                PresentAmount = chargeDetail.PresentAmount
            };
            AddCapital(detail);
        }

        public void UpdateCapitalAmount(long memid, decimal amount, decimal freezeAmount, decimal chargeAmount)
        {
            throw new NotImplementedException();
        }


        private static object obj = new object();
        public long CreateCode(CapitalDetailInfo.CapitalDetailType type)
        {
            lock (obj)
            {
                int rand;
                char code;
                string orderId = string.Empty;
                Random random = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                for (int i = 0; i < 4; i++)
                {
                    rand = random.Next();
                    code = (char)('0' + (char)(rand % 10));
                    orderId += code.ToString();
                }
                //17位
                return long.Parse(DateTime.Now.ToString("yyMMddHHmmss") + (int)type + orderId);
            }
        }


        public CapitalDetailInfo GetCapitalDetailInfo(long id)
        {
            return DbFactory.Default.Get<CapitalDetailInfo>().Where(e => e.Id == id).FirstOrDefault();
        }

        public List<CapitalDetailInfo> GetTopCapitalDetailList(long capitalId, int num)
        {
            return DbFactory.Default.Get<CapitalDetailInfo>().Where(e => e.CapitalID == capitalId).OrderByDescending(e => e.CreateTime).Take(num).ToList();
        }


        private object lockInitCapital = new object();
        /// <summary>
        /// 初始化会员资产表数据
        /// </summary>
        public CapitalInfo GetCapitalInfo(long userid)
        {
            var result = DbFactory.Default.Get<CapitalInfo>().Where(e => e.MemId == userid).FirstOrDefault();
            if (result == null)
            {
                lock (lockInitCapital)
                {
                    result = new CapitalInfo()
                    {
                        Balance = 0,
                        ChargeAmount = 0,
                        MemId = userid,
                        FreezeAmount = 0
                    };
                    DbFactory.Default.Add(result);
                }
            }
            return result;
        }


        public void SetPayPwd(long memid, string pwd)
        {
            pwd = pwd.Trim();
            var salt = Guid.NewGuid().ToString("N");
            var pwdmd5 = Himall.Core.Helper.SecureHelper.MD5(Himall.Core.Helper.SecureHelper.MD5(pwd) + salt);
            var member = DbFactory.Default.Get<MemberInfo>().Where(e => e.Id == memid).FirstOrDefault();
            if (member != null)
            {
                DbFactory.Default.Set<MemberInfo>().Set(n => n.PayPwd, pwdmd5).Set(n => n.PayPwdSalt, salt).Where(p => p.Id == member.Id).Succeed();
                CacheManager.ClearMemberData(memid); //清用户缓存

                //消息通知
                var userMessage = new MessageUserInfo();
                userMessage.UserName = member.UserName;
                userMessage.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnEditPayPassWord(member.Id, userMessage));
            }
        }

        public void RefuseApplyWithDraw(long id, ApplyWithDrawInfo.ApplyWithDrawStatus status, string opuser, string remark)
        {
            //var model = Context.ApplyWithDrawInfo.FirstOrDefault(e => e.Id == id);
            var model = DbFactory.Default.Get<ApplyWithDrawInfo>().Where(e => e.Id == id).FirstOrDefault();
            model.ApplyStatus = status;
            model.OpUser = opuser;
            model.Remark = remark;
            model.ConfirmTime = DateTime.Now;
            DbFactory.Default.Update(model);
            //var capital = Context.CapitalInfo.FirstOrDefault(e => e.MemId == model.MemId);
            var capital = DbFactory.Default.Get<CapitalInfo>().Where(e => e.MemId == model.MemId).FirstOrDefault();
            //capital.Balance = capital.Balance.Value + model.ApplyAmount;
            //capital.FreezeAmount = capital.FreezeAmount - model.ApplyAmount;
            DbFactory.Default.Set<CapitalInfo>().Set(n => n.Balance, capital.Balance + model.ApplyAmount)
                .Set(n => n.FreezeAmount, capital.FreezeAmount - model.ApplyAmount).Where(p => p.Id == capital.Id).Succeed();
            //发送消息
            var member = DbFactory.Default.Get<MemberInfo>(m => m.Id == model.MemId).FirstOrDefault();
            var message = new MessageWithDrawInfo();
            message.UserName = model.NickName;
            message.UserName = member != null ? member.UserName : "";
            message.Amount = model.ApplyAmount;
            message.ApplyType = model.ApplyType.GetHashCode();
            message.ApplyTime = model.ApplyTime;
            message.Remark = model.Remark;
            message.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
            Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnMemberWithDrawFail(model.MemId, message));
            //Context.SaveChanges();
        }

        public long AddChargeApply(ChargeDetailInfo model)
        {
            if (model.Id == 0)
            {
                model.Id = CreateCode(CapitalDetailInfo.CapitalDetailType.ChargeAmount);
            }
            //Context.ChargeDetailInfo.Add(model);
            //Context.SaveChanges();
            DbFactory.Default.Add(model);
            return model.Id;
        }

        public ChargeDetailInfo GetChargeDetail(long id)
        {
            //return Context.ChargeDetailInfo.FirstOrDefault(e => e.Id == id);
            return DbFactory.Default.Get<ChargeDetailInfo>().Where(e => e.Id == id).FirstOrDefault();
        }

        public void UpdateChargeDetail(ChargeDetailInfo model)
        {
            //var oldmodel = Context.ChargeDetailInfo.FirstOrDefault(e => e.Id == model.Id);
            var oldmodel = DbFactory.Default.Get<ChargeDetailInfo>().Where(e => e.Id == model.Id).FirstOrDefault();
            //using (TransactionScope scope = new TransactionScope())
            //{
            var flag = DbFactory.Default.InTransaction(() =>
            {
                //oldmodel.ChargeStatus = model.ChargeStatus;
                //oldmodel.ChargeTime = model.ChargeTime.Value;
                //oldmodel.ChargeWay = model.ChargeWay;
                //Context.SaveChanges();
                DbFactory.Default.Set<ChargeDetailInfo>().Set(n => n.ChargeStatus, model.ChargeStatus)
                    .Set(n => n.ChargeTime, model.ChargeTime.Value).Set(n => n.ChargeWay, model.ChargeWay).Where(e => e.Id == oldmodel.Id).Succeed();
                CapitalDetailModel capitalDetail = new CapitalDetailModel
                {
                    Amount = oldmodel.ChargeAmount,
                    UserId = oldmodel.MemId,
                    PayWay = model.ChargeWay,
                    SourceType = CapitalDetailInfo.CapitalDetailType.ChargeAmount,
                    SourceData = oldmodel.Id.ToString(),
                    PresentAmount = oldmodel.PresentAmount
                };
                AddCapital(capitalDetail);
                //scope.Complete();
                return true;
            });

        }

        public QueryPageModel<ChargeDetailInfo> GetChargeLists(ChargeQuery query)
        {
            //var charges = Context.ChargeDetailInfo.AsQueryable();
            var charges = DbFactory.Default.Get<ChargeDetailInfo>();
            if (query.ChargeStatus.HasValue)
            {
                charges = charges.Where(e => e.ChargeStatus == query.ChargeStatus.Value);
            }
            if (query.memberId.HasValue)
            {
                charges = charges.Where(e => e.MemId == query.memberId.Value);
            }
            if (query.ChargeNo.HasValue)
            {
                charges = charges.Where(e => e.Id == query.ChargeNo.Value);
            }
            //int total = 0;
            //var model = charges.GetPage(p => p.OrderByDescending(o => o.CreateTime), out total, query.PageNo, query.PageSize);
            var model = charges.OrderByDescending(o => o.CreateTime).ToPagedList(query.PageNo, query.PageSize);
            QueryPageModel<ChargeDetailInfo> result = new QueryPageModel<ChargeDetailInfo> { Models = model, Total = model.TotalRecordCount };
            return result;
        }



        /// <summary>
        /// 添加店铺充值流水
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public long AddChargeDetailShop(ChargeDetailShopInfo model)
        {
            if (model.Id == 0)
            {
                model.Id = CreateCode(CapitalDetailInfo.CapitalDetailType.ChargeAmount);
            }
            DbFactory.Default.Add(model);
            return model.Id;
        }

        /// <summary>
        /// 修改店铺充值流水
        /// </summary>
        /// <param name="model"></param>
        public void UpdateChargeDetailShop(ChargeDetailShopInfo model)
        {
            DbFactory.Default.Set<ChargeDetailShopInfo>().Set(n => n.ChargeStatus, model.ChargeStatus)
                .Set(n => n.ChargeTime, model.ChargeTime).Set(n => n.ChargeWay, model.ChargeWay).Where(e => e.Id == model.Id).Succeed();
        }

        /// <summary>
        /// 获取店铺充值流水信息
        /// </summary>
        /// <param name="Id">流水ID</param>
        /// <returns></returns>
        public ChargeDetailShopInfo GetChargeDetailShop(long Id)
        {
            return DbFactory.Default.Get<ChargeDetailShopInfo>().Where(e => e.Id == Id).FirstOrDefault();
        }
        /// <summary>
        /// 更新提现支付号
        /// </summary>
        /// <param name="id"></param>
        /// <param name="PayNo"></param>
        public void UpdateApplyWithDrawInfoPayNo(long Id, string PayNo)
        {
            DbFactory.Default.Set<ApplyWithDrawInfo>().Set(n => n.PayNo, PayNo).Where(r => r.Id == Id).Succeed();
        }
        /// <summary>
        /// 取消第三方付款
        /// </summary>
        /// <param name="Id"></param>
        public bool CancelPay(long Id)
        {
            bool result = false;
            var Info = DbFactory.Default.Get<ApplyWithDrawInfo>().Where(r => r.Id == Id).FirstOrDefault();
            if (Info != null && Info.ApplyStatus == ApplyWithDrawInfo.ApplyWithDrawStatus.PayPending)
            {
                result = DbFactory.Default.Set<ApplyWithDrawInfo>().Set(n => n.ApplyStatus, ApplyWithDrawInfo.ApplyWithDrawStatus.WaitConfirm).Where(r => r.Id == Info.Id).Succeed();
            }
            return result;
        }
        /// <summary>
        /// 获取累计收到红包
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public decimal GetSumRedPacket(long Id)
        {
            return DbFactory.Default.Get<CapitalDetailInfo>().Where(d => d.SourceType == CapitalDetailInfo.CapitalDetailType.RedPacket && d.CapitalID == Id).Sum<decimal>(d => d.Amount);
        }

        public void AddCapital(CapitalDetailInfo detail)
        {
            detail.Id = CreateCode(detail.SourceType);
            detail.CreateTime = DateTime.Now;
            DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Add(detail);
                //根据 detail.SourceType 进行处理
                switch (detail.SourceType)
                {
                    case CapitalDetailInfo.CapitalDetailType.ChargeAmount:
                    case CapitalDetailInfo.CapitalDetailType.Brokerage:
                        var change = detail.Amount;
                        DbFactory.Default.Set<CapitalInfo>()
                        .Where(p => p.Id == detail.CapitalID)
                        .Set(p => p.Balance, n => n.Balance + change)//变更数值已包含正负符号
                        .Succeed();
                        break;
                    default:
                        throw new HimallException("未能处理当前业务类型");
                }
            });
        }
    }
}
