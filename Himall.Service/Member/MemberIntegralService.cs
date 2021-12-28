using Himall.CommonModel;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using NetRube.Data;

namespace Himall.Service
{
    public class MemberIntegralService : ServiceBase
    {
        public void AddMemberIntegral(MemberIntegralRecordInfo model, IConversionMemberIntegralBase conversionMemberIntegralEntity = null)
        {
            if (null == model) { throw new NullReferenceException("添加会员积分记录时，会员积分Model为空."); }
            if (0 == model.MemberId) { throw new NullReferenceException("添加会员积分记录时，会员Id为空."); }
            if (!DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == model.MemberId).Exist())
            {
                throw new Himall.Core.HimallException("不存在此会员");
            }
            if (null != conversionMemberIntegralEntity)
            {
                model.Integral = conversionMemberIntegralEntity.ConversionIntegral();
            }
            if (model.Integral == 0)
            {
                return;
            }
            var userIntegral = DbFactory.Default.Get<MemberIntegralInfo>().Where(a => a.MemberId == model.MemberId).FirstOrDefault();
            if (userIntegral == null)
            {
                userIntegral = new MemberIntegralInfo();
                userIntegral.MemberId = model.MemberId;
                userIntegral.UserName = model.UserName;
                if (model.Integral > 0)
                {
                    userIntegral.HistoryIntegrals += model.Integral;
                }
                else
                {
                    throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Add(userIntegral);
            }
            else
            {
                if (model.Integral > 0)
                {
                    userIntegral.HistoryIntegrals += model.Integral;
                }
                else
                {
                    if (userIntegral.AvailableIntegrals < Math.Abs(model.Integral))
                        throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Update(userIntegral);
            }
          
            DbFactory.Default.Add(model);

            if (model.MemberIntegralRecordActionInfo != null)
            {
                model.MemberIntegralRecordActionInfo.ForEach(p => p.IntegralRecordId = model.Id);
                DbFactory.Default.AddRange(model.MemberIntegralRecordActionInfo);
            }
            CacheManager.ClearMemberData(model.MemberId); //清用户缓存
        }

        public void SettlementOrder(DateTime expireTime, int rule)
        {
            //取过了售后维权期且完成的订单
            var records = DbFactory.Default.Get<MemberIntegralRecordActionInfo>()
                .Where(t => t.VirtualItemTypeId == MemberIntegralInfo.VirtualItemType.Consumption)
                .Select(p => p.VirtualItemId);

            var refundsql = DbFactory.Default.Get<OrderRefundInfo>()
                .Where(d => d.ManagerConfirmStatus != OrderRefundInfo.OrderRefundConfirmStatus.Confirmed && d.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.UnAudit)
                .Select(d => d.OrderId);

            var startFinishDate = DateTime.Now.AddDays(-365);//FinishDate>1年(这个是特加的，完成1年之前的数据没必要查太久的数据)

            var orders = DbFactory.Default.Get<OrderInfo>()
                .Where(a => a.FinishDate > startFinishDate && a.FinishDate < expireTime
                && a.OrderStatus == OrderInfo.OrderOperateStatus.Finish && a.Id.ExNotIn(records) && a.Id.ExNotIn(refundsql)).ToList();

            if (orders != null)
            {
                foreach (var order in orders)
                {
                    try
                    {
                        AddIntegral(order, rule);
                    }
                    catch (Exception ex) {
                        throw new Himall.Core.HimallException("积分异常数据订单号:" + order.Id+"错误："+ex);
                    }
                }
            }
        }
        private void AddIntegral(OrderInfo order, int moneyPerIntegral)
        {
            var orderTotal = order.ActualPayAmount - order.Freight - order.Tax; //order.ProductTotal - order.IntegralDiscount - order.RefundTotalAmount;
            orderTotal = orderTotal < 0 ? 0 : orderTotal;


            var integral = moneyPerIntegral <= 0 ? 0 : Convert.ToInt32(Math.Floor(orderTotal / moneyPerIntegral));
            Himall.Entities.MemberIntegralRecordInfo record = new Himall.Entities.MemberIntegralRecordInfo();
            record.UserName = order.UserName;
            record.MemberId = order.UserId;
            record.RecordDate = DateTime.Now;
            record.TypeId = Himall.Entities.MemberIntegralInfo.IntegralType.Consumption;
            record.ReMark = "使用订单号(" + order.Id.ToString() + ")";
            Himall.Entities.MemberIntegralRecordActionInfo action = new Himall.Entities.MemberIntegralRecordActionInfo();
            action.VirtualItemTypeId = Himall.Entities.MemberIntegralInfo.VirtualItemType.Consumption;
            action.VirtualItemId = order.Id;
            DbFactory.Default.Add(action);
            AddMemberIntegral(record, integral);
        }

        public void AddMemberIntegral(MemberIntegralRecordInfo model, int integral)
        {
            if (null == model) { throw new NullReferenceException("添加会员积分记录时，会员积分Model为空."); }
            if (0 == model.MemberId) { throw new NullReferenceException("添加会员积分记录时，会员Id为空."); }
            if (!DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == model.MemberId && a.UserName == model.UserName).Exist())
            {
                throw new Himall.Core.HimallException("不存在此会员!userId:" + model.MemberId + "--UserName：" + model.UserName);
            }
            model.Integral = integral;
            if (model.Integral == 0)
            {
                return;
            }
            //var userIntegral = Context.MemberIntegral.FirstOrDefault(a => a.MemberId == model.MemberId);
            var userIntegral = DbFactory.Default.Get<MemberIntegralInfo>().Where(a => a.MemberId == model.MemberId).FirstOrDefault();
            if (userIntegral == null)
            {
                userIntegral = new MemberIntegralInfo();
                userIntegral.MemberId = model.MemberId;
                userIntegral.UserName = model.UserName;
                if (model.Integral > 0)
                {
                    userIntegral.HistoryIntegrals += model.Integral;
                }
                else
                {
                    throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                //Context.MemberIntegral.Add(userIntegral);
                DbFactory.Default.Add(userIntegral);
            }
            else
            {
                if (model.Integral > 0)
                {
                    userIntegral.HistoryIntegrals += model.Integral;
                }
                else
                {
                    if (userIntegral.AvailableIntegrals < Math.Abs(model.Integral))
                        throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Update(userIntegral);
            }
            DbFactory.Default.Add(model);
            if (model.MemberIntegralRecordActionInfo.Count() > 0)
            {
                foreach (var item in model.MemberIntegralRecordActionInfo)
                {
                    item.IntegralRecordId = model.Id;
                    DbFactory.Default.Add(item);
                }
            }
        }

        public void AddMemberIntegralNotAddHistoryIntegrals(MemberIntegralRecordInfo model, IConversionMemberIntegralBase conversionMemberIntegralEntity = null)
        {
            if (null == model) { throw new NullReferenceException("添加会员积分记录时，会员积分Model为空."); }
            if (0 == model.MemberId) { throw new NullReferenceException("添加会员积分记录时，会员Id为空."); }
            if (!DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == model.MemberId && a.UserName == model.UserName).Exist())
            {
                throw new Himall.Core.HimallException("不存在此会员");
            }
            if (null != conversionMemberIntegralEntity)
            {
                model.Integral = conversionMemberIntegralEntity.ConversionIntegral();
            }
            if (model.Integral == 0)
            {
                return;
            }
            var userIntegral = DbFactory.Default.Get<MemberIntegralInfo>().Where(a => a.MemberId == model.MemberId).FirstOrDefault();
            if (userIntegral == null)
            {
                userIntegral = new MemberIntegralInfo();
                userIntegral.MemberId = model.MemberId;
                userIntegral.UserName = model.UserName;
                if (model.Integral <= 0)
                {
                    throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                //Context.MemberIntegral.Add(userIntegral);
                DbFactory.Default.Add(userIntegral);
            }
            else
            {
                if (model.Integral <= 0)
                {
                    if (userIntegral.AvailableIntegrals < Math.Abs(model.Integral))
                        throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Update(userIntegral);
            }
          
            DbFactory.Default.Add(model);
            if (model.MemberIntegralRecordActionInfo != null)
            {
                model.MemberIntegralRecordActionInfo.ForEach(p => p.IntegralRecordId = model.Id);
                DbFactory.Default.AddRange(model.MemberIntegralRecordActionInfo);
            }
        }
        public void AddMemberIntegralByRecordAction(MemberIntegralRecordInfo model, IConversionMemberIntegralBase conversionMemberIntegralEntity = null)
        {
            if (null == model) { throw new NullReferenceException("添加会员积分记录时，会员积分Model为空."); }
            if (0 == model.MemberId) { throw new NullReferenceException("添加会员积分记录时，会员Id为空."); }
            //if (!Context.UserMemberInfo.Any(a => a.Id == model.MemberId && a.UserName == model.UserName))
            if (!DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == model.MemberId && a.UserName == model.UserName).Exist())
            {
                throw new Himall.Core.HimallException("不存在此会员");
            }
            if (null != conversionMemberIntegralEntity)
            {
                var conversionIntegral = conversionMemberIntegralEntity.ConversionIntegral();
                if (model.Id <= 0)
                    model.Integral = conversionIntegral;
                else
                {
                    var actions = DbFactory.Default.Get<MemberIntegralRecordActionInfo>(p => p.IntegralRecordId == model.Id).ToList();

                    if (actions.Count > 0)//多个明细记录时，每个记录都需计算
                        model.Integral = conversionIntegral * actions.Count;
                    else
                        model.Integral = conversionIntegral;
                }
            }
            if (model.Integral == 0)
                return;

            var userIntegral = DbFactory.Default.Get<MemberIntegralInfo>().Where(a => a.MemberId == model.MemberId).FirstOrDefault();
            if (userIntegral == null)
            {
                userIntegral = new MemberIntegralInfo();
                userIntegral.MemberId = model.MemberId;
                userIntegral.UserName = model.UserName;
                if (model.Integral > 0)
                {
                    userIntegral.HistoryIntegrals += model.Integral;
                }
                else
                {
                    throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Add(userIntegral);
            }
            else
            {
                if (model.Integral > 0)
                {
                    userIntegral.HistoryIntegrals += model.Integral;
                }
                else
                {
                    if (userIntegral.AvailableIntegrals < Math.Abs(model.Integral))
                        throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Update(userIntegral);
            }
           
            DbFactory.Default.Add(model);

            //加会员积分记录明细详情
            if (model.MemberIntegralRecordActionInfo != null)
            {
                model.MemberIntegralRecordActionInfo.ForEach(p => p.IntegralRecordId = model.Id);
                DbFactory.Default.AddRange(model.MemberIntegralRecordActionInfo);
            }
        }

        public QueryPageModel<MemberIntegralInfo> GetMemberIntegralList(IntegralQuery query)
        {
            var db = DbFactory.Default.Get<MemberIntegralInfo>()
                .InnerJoin<MemberInfo>((mii, mi) => mii.MemberId == mi.Id);
            if (!string.IsNullOrWhiteSpace(query.UserName))
            {
                db.Where(item => item.UserName.Contains(query.UserName));
            }
            if (query.StartDate.HasValue)
            {
                var members = DbFactory.Default.Get<MemberInfo>().Where(p => p.CreateDate >= query.StartDate).Select(p => p.Id).ToList<long>();
                db.Where(item => item.MemberId.ExIn(members));
            }
            if (query.EndDate.HasValue)
            {
                var end = query.EndDate.Value.Date.AddDays(1);
                var members = DbFactory.Default.Get<MemberInfo>().Where(p => p.CreateDate < end).Select(p => p.Id).ToList<long>();
                db.Where(item => item.MemberId.ExIn(members));
            }
            switch (query.Sort.ToLower())
            {
                case "availableintegrals":
                    if (query.IsAsc) db.OrderBy(p => p.AvailableIntegrals);
                    else db.OrderByDescending(p => p.AvailableIntegrals);
                    break;
                case "historyintegrals":
                    if (query.IsAsc) db.OrderBy(p => p.HistoryIntegrals);
                    else db.OrderByDescending(p => p.HistoryIntegrals);
                    break;
                case "createdate":
                    if (query.IsAsc) db.OrderBy<MemberInfo>(p => p.CreateDate);
                    else db.OrderByDescending<MemberInfo>(p => p.CreateDate);
                    break;
                default:
                    db.OrderByDescending(item => item.HistoryIntegrals);
                    break;

            }

            var model = db.ToPagedList(query.PageNo, query.PageSize);
            return new QueryPageModel<MemberIntegralInfo>
            {
                Models = model,
                Total = model.TotalRecordCount
            };
        }

        public QueryPageModel<MemberIntegralRecordInfo> GetIntegralRecordList(IntegralRecordQuery query)
        {
            //int total = 0;
            //IQueryable<MemberIntegralRecord> list = Context.MemberIntegralRecord.AsQueryable();
            var list = DbFactory.Default.Get<MemberIntegralRecordInfo>();
            if (query.UserId.HasValue)
            {
                list.Where(item => item.MemberId == query.UserId.Value);
            }
            if (query.StartDate.HasValue)
            {
                list.Where(item => query.StartDate <= item.RecordDate);
            }
            if (query.IntegralType.HasValue)
            {
                list.Where(item => item.TypeId == query.IntegralType.Value);
            }
            if (query.EndDate.HasValue)
            {
                list.Where(item => query.EndDate >= item.RecordDate);
            }
            //list = list.GetPage(d => d.OrderByDescending(o => o.Id), out total, query.PageNo, query.PageSize);
            var model = list.OrderByDescending(o => o.Id).ToPagedList(query.PageNo, query.PageSize);
            QueryPageModel<MemberIntegralRecordInfo> pageModel = new QueryPageModel<MemberIntegralRecordInfo>() { Models = model, Total = model.TotalRecordCount };
            return pageModel;
        }

        public QueryPageModel<MemberIntegralRecordInfo> GetIntegralRecordListForWeb(IntegralRecordQuery query)
        {
            //int total = 0;
            //IQueryable<MemberIntegralRecord> list = Context.MemberIntegralRecord.AsQueryable();
            var list = DbFactory.Default.Get<MemberIntegralRecordInfo>();
            if (query.UserId.HasValue)
            {
                list.Where(item => item.MemberId == query.UserId.Value);
            }
            if (query.StartDate.HasValue)
            {
                list.Where(item => query.StartDate <= item.RecordDate);
            }
            if (query.IntegralType.HasValue)
            {
                if ((int)query.IntegralType.Value == 0)
                {
                    //list.Where(item => true);
                }
                else if ((int)query.IntegralType.Value == 1)
                {
                    list.Where(item => item.Integral >= 0);
                }
                else if ((int)query.IntegralType.Value == 2)
                {
                    list.Where(item => item.Integral < 0);
                }
            }
            if (query.EndDate.HasValue)
            {
                list.Where(item => query.EndDate >= item.RecordDate);
            }
            //list = list.GetPage(d => d.OrderByDescending(o => o.Id), out total, query.PageNo, query.PageSize);
            var model = list.OrderByDescending(o => o.Id).ToPagedList(query.PageNo, query.PageSize);
            QueryPageModel<MemberIntegralRecordInfo> pageModel = new QueryPageModel<MemberIntegralRecordInfo>() { Models = model, Total = model.TotalRecordCount };
            return pageModel;
        }

        public void SetIntegralRule(IEnumerable<MemberIntegralRuleInfo> info)
        {
            var list = DbFactory.Default.Get<MemberIntegralRuleInfo>().ToList();
            foreach (var s in info)
            {
                var t = list.FirstOrDefault(a => a.TypeId == s.TypeId);
                if (t != null)
                    DbFactory.Default.Set<MemberIntegralRuleInfo>().Set(n => n.Integral, s.Integral).Where(a => a.TypeId == s.TypeId).Succeed();
                else
                    DbFactory.Default.Add(s);
            }
        }

        public void SetMoneyPerIntegral(int moneyPerIntegral)
        {
            DbFactory.Default.Set<MemberIntegralExchangeRuleInfo>()
                .Set(p => p.MoneyPerIntegral, moneyPerIntegral)
                .Execute();
            CacheManager.Clear("integral:rule");
        }

        public void SetIntegralPerMoney(int integralPerMoney)
        {
            DbFactory.Default.Set<MemberIntegralExchangeRuleInfo>()
                .Set(p => p.IntegralPerMoney, integralPerMoney)
                .Execute();
            CacheManager.Clear("integral:rule");
        }

        public MemberIntegralExchangeRuleInfo GetIntegralChangeRule() =>
            CacheManager.GetOrCreate("integral:rule", () =>
            {
                var model = DbFactory.Default.Get<MemberIntegralExchangeRuleInfo>().FirstOrDefault();
                if (model == null)
                {
                    model = new MemberIntegralExchangeRuleInfo
                    {
                        IntegralPerMoney = 0,
                        MoneyPerIntegral = 0
                    };
                    DbFactory.Default.Add(model);
                }
                return model;
            });
            

        public List<MemberIntegralRuleInfo> GetIntegralRule()
        {
            return DbFactory.Default.Get<MemberIntegralRuleInfo>().ToList();
        }

        public bool HasLoginIntegralRecord(long userId)
        {
            var Date = DateTime.Now.Date;
            var Date2 = Date.AddDays(1);
            return DbFactory.Default.Get<MemberIntegralRecordInfo>().Where(a => a.MemberId == userId && a.RecordDate >= Date && a.RecordDate < Date2 && a.TypeId == MemberIntegralInfo.IntegralType.Login).Exist();
        }

        public UserIntegralGroupModel GetUserHistroyIntegralGroup(long userId)
        {
            var data = DbFactory.Default.Get<MemberIntegralRecordInfo>()
                .Where(a => a.MemberId == userId)
                .GroupBy(p => p.TypeId)
                .Select(p => new { Item1 = p.TypeId, Item2 = p.Integral.ExSum() })
                .ToList<SimpItem<MemberIntegralInfo.IntegralType, int>>();
            return new UserIntegralGroupModel
            {
                BindWxIntegral = data.FirstOrDefault(a => a.Item1 == MemberIntegralInfo.IntegralType.BindWX)?.Item2 ?? 0,
                CommentIntegral = data.FirstOrDefault(a => a.Item1 == MemberIntegralInfo.IntegralType.Comment)?.Item2 ?? 0,
                ConsumptionIntegral = data.FirstOrDefault(a => a.Item1 == MemberIntegralInfo.IntegralType.Consumption)?.Item2 ?? 0,
                LoginIntegral = data.FirstOrDefault(a => a.Item1 == MemberIntegralInfo.IntegralType.Login)?.Item2 ?? 0,
                RegIntegral = data.FirstOrDefault(a => a.Item1 == MemberIntegralInfo.IntegralType.Reg)?.Item2 ?? 0,
                SignIn = data.FirstOrDefault(a => a.Item1 == MemberIntegralInfo.IntegralType.SignIn)?.Item2 ?? 0,
                InviteIntegral = data.FirstOrDefault(a => a.Item1 == MemberIntegralInfo.IntegralType.InvitationMemberRegiste)?.Item2 ?? 0
            };
        }

        public MemberIntegralInfo GetMemberIntegral(long userId)
        {
            return DbFactory.Default.Get<MemberIntegralInfo>().Where(a => a.MemberId == userId).FirstOrDefault();
        }

        public List<MemberIntegralInfo> GetMemberIntegrals(IEnumerable<long> userIds)
        {
            return DbFactory.Default.Get<MemberIntegralInfo>().Where(a => a.MemberId.ExIn(userIds)).ToList();
        }

        public List<MemberIntegralRecordActionInfo> GetIntegralRecordAction(IEnumerable<long> virtualItemIds, MemberIntegralInfo.VirtualItemType type)
        {
            return DbFactory.Default.Get<MemberIntegralRecordActionInfo>().Where(e => e.VirtualItemId.ExIn(virtualItemIds) && e.VirtualItemTypeId == type).ToList();
        }

        public List<MemberIntegralRecordActionInfo> GetIntegralRecordAction(long record)
        {
            return DbFactory.Default.Get<MemberIntegralRecordActionInfo>().Where(p => p.IntegralRecordId == record).ToList();
        }


    }
}