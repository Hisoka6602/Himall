﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Himall.Core;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.CommonModel;
using Himall.DTO;

namespace Himall.Application
{
    public class MemberIntegralApplication:BaseApplicaion
    {
        private static MemberIntegralService _iMemberIntegralService = ObjectContainer.Current.Resolve<MemberIntegralService>();
        private static MemberIntegralConversionFactoryService _iMemberIntegralConversionFactoryService = ObjectContainer.Current.Resolve<MemberIntegralConversionFactoryService>();

        /// <summary>
        ///  //用户积分记录增加
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conversionMemberIntegralEntity"></param>
        public static void AddMemberIntegral(MemberIntegralRecordInfo model, IConversionMemberIntegralBase conversionMemberIntegralEntity = null)
        {
            _iMemberIntegralService.AddMemberIntegral(model, conversionMemberIntegralEntity);
        }
        /// <summary>
        /// 通过多个RecordAction，增加用户积分
        /// </summary>
        /// <param name="model"></param>
        /// <param name="type"></param>
        public static void AddMemberIntegralByEnum(MemberIntegralRecordInfo model, MemberIntegralInfo.IntegralType type)
        {
            var conversionService= ServiceProvider.Instance<MemberIntegralConversionFactoryService>.Create;
            var conversionMemberIntegralEntity = conversionService.Create(MemberIntegralInfo.IntegralType.Share);
            _iMemberIntegralService.AddMemberIntegralByRecordAction(model, conversionMemberIntegralEntity);
        }
        /// <summary>
        /// 获取用户积分列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<MemberIntegralInfo> GetMemberIntegralList(IntegralQuery query)
        {
            return _iMemberIntegralService.GetMemberIntegralList(query);
        }


        /// <summary>
        /// 根据用户ID获取用户的积分信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static MemberIntegralInfo GetMemberIntegral(long userId)
        {
            var integral = _iMemberIntegralService.GetMemberIntegral(userId);
            if (integral == null) return new MemberIntegralInfo { MemberId = userId };
            return integral;
        }
        public static int GetAvailableIntegral(long userId)
        {
            var integral = _iMemberIntegralService.GetMemberIntegral(userId);
            if (integral == null) return 0;
            return integral.AvailableIntegrals;
        }

        /// <summary>
        /// 根据用户ID获取用户的积分信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<MemberIntegralInfo> GetMemberIntegrals(List<long> userIds)
        {
            return _iMemberIntegralService.GetMemberIntegrals(userIds);
        }

        /// <summary>
        /// 获取单个用户的积分记录
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<MemberIntegralRecordInfo> GetIntegralRecordList(IntegralRecordQuery query)
        {
            return _iMemberIntegralService.GetIntegralRecordList(query);
        }

        /// <summary>
        /// 订单是否已经分享
        /// </summary>
        /// <param name="orderid"></param>
        /// <returns>true:已经分享过</returns>
        public static bool OrderIsShared(IEnumerable<long> orderids)
        {
            var recordAction = _iMemberIntegralService.GetIntegralRecordAction(orderids, MemberIntegralInfo.VirtualItemType.ShareOrder);
            if (recordAction.Count > 0)//有分享记录，就认为已经分享过（不管分享的订单个数）
                return true;
            return false;
        }


        public static QueryPageModel<MemberIntegral> GetMemberIntegrals(IntegralQuery query)
        {
            var data = _iMemberIntegralService.GetMemberIntegralList(query);
            var members = GetService<MemberService>().GetMembers(data.Models.Select(p => (long)p.MemberId).ToList());
            var grades = MemberGradeApplication.GetMemberGrades();
            var result = new List<MemberIntegral>();
            foreach (var item in data.Models)
            {
                var member = members.FirstOrDefault(p => p.Id == item.MemberId);
                result.Add(new MemberIntegral
                {
                    Id = item.Id,
                    AvailableIntegrals = item.AvailableIntegrals,
                    HistoryIntegrals = item.HistoryIntegrals,
                    MemberGrade = MemberGradeApplication.GetMemberGradeByIntegral(grades, item.HistoryIntegrals).GradeName,
                    UserName = member.UserName,
                    MemberId = member.Id,
                    CreateDate = member.CreateDate,
                });
            }

            return new QueryPageModel<MemberIntegral>
            {
                Models = result,
                Total = data.Total
            };
        }

        /// <summary>
        /// 获取积分规则
        /// </summary>
        /// <returns></returns>
        public static List<MemberIntegralRuleInfo> GetIntegralRule()
        {
            return _iMemberIntegralService.GetIntegralRule();
        }

      
        public static void BatchMemberIntegral(string Operation, int Integral, string userids, string reMark) {
            var userIdlist = userids.Split(',').Select(s => long.Parse(s)).ToList<long>();
            var memberslist=MemberApplication.GetMembers(userIdlist);
            if (Operation == "sub")
            {
                Integral = -Integral;
            }
            foreach (var member in memberslist)
            {
                var info = new MemberIntegralRecordInfo();
                info.UserName = member.UserName;
                info.MemberId = member.Id;
                info.RecordDate = DateTime.Now;
                info.TypeId = MemberIntegralInfo.IntegralType.SystemOper;
                info.ReMark = reMark;
              
                var memberIntegral = _iMemberIntegralConversionFactoryService.Create(MemberIntegralInfo.IntegralType.SystemOper, Integral);

                _iMemberIntegralService.AddMemberIntegral(info, memberIntegral);
            }
        }

    }
}
