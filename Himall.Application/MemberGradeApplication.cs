using System;
using System.Collections.Generic;
using System.Linq;
using Himall.Service;
using Himall.DTO;
using Himall.CommonModel;
using AutoMapper;
using Himall.Core;
using Himall.Entities;
namespace Himall.Application
{
    public class MemberGradeApplication:BaseApplicaion<MemberGradeService>
    {
        /// <summary>
        /// 默认会员等级名称
        /// </summary>
        const string DEFAULT_GRADE_NAME = "vip0";

        private static MemberGradeService _iMemberGradeService = ObjectContainer.Current.Resolve<MemberGradeService>();


        /// <summary>
        /// 获取等级列表 
        /// </summary>
        /// <returns></returns>
        public static List<MemberGradeInfo> GetMemberGrades()
        {
            return _iMemberGradeService.GetMemberGradeList().ToList();
        }
        public static MemberGradeInfo GetMemberGrade(long id) {
            return Service.GetMemberGrade(id);
        }

        /// <summary>
        /// 根据会员积分获取会员等级，获取单个会员的会员等级（循环调用时禁用)
        /// </summary>
        /// <param name="integral"></param>
        /// <returns></returns>
        public static MemberGradeInfo GetMemberGradeByUserIntegral(int integral)
        {
            List<MemberGradeInfo> memberGrade = GetMemberGrades();
            return GetMemberGradeByIntegral(memberGrade, integral);
        }



        /// <summary>
        //根据会员积分获取会员等级(批量获取时先取出所有会员等级)
        /// </summary>
        /// <param name="historyIntegrals"></param>
        /// <returns></returns>
        public static MemberGradeInfo GetMemberGradeByIntegral(List<MemberGradeInfo> memberGrade, int integral)
        {
            var defaultGrade = new MemberGradeInfo() { GradeName = DEFAULT_GRADE_NAME };
            var grade = memberGrade.Where(a => a.Integral <= integral).OrderByDescending(a => a.Integral).FirstOrDefault();
            if (grade == null)
                return defaultGrade;
            return grade;
        }

        public static MemberGradeInfo GetGradeByMember(long memberId)
        {
            var integral = MemberIntegralApplication.GetMemberIntegral(memberId);
            var grades = GetMemberGrades();
            var info = GetMemberGradeByIntegral(grades, integral.HistoryIntegrals);
            return info;
        }
        /// <summary>
        /// 判断用户等级是否达到限制要求
        /// </summary>
        /// <param name="memberId">用户ID</param>
        /// <param name="limitGradeId">最低限制等级</param>
        /// <returns></returns>
        public static bool IsAllowGrade(long memberId, int limitGradeId)
        {
            if (limitGradeId == 0) return true;
            var grade = GetGradeByMember(memberId);
            var limit = Service.GetMemberGrade(limitGradeId);
            if (grade.Integral >= limit.Integral)
                return true;
            return false;
        }
    }
}
