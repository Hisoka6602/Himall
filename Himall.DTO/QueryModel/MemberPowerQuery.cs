﻿using Himall.CommonModel;
using System;
using System.Collections.Generic;

namespace Himall.DTO.QueryModel
{
    /// <summary>
    /// 会员购买力度搜索条件
    /// </summary>
    public class MemberPowerQuery : QueryBase
    {
        /// <summary>
        /// 最近消费时间
        /// </summary>
        public RecentlySpentTime? RecentlySpentTime { get; set; }

        /// <summary>
        /// 自定义消费开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 自定义消费结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 会员购买次数
        /// </summary>
        public Purchases? Purchases { get; set; }

        /// <summary>
        /// 自定义购买次数开始
        /// </summary>
        public int? StartPurchases { get; set; }

        /// <summary>
        /// 自定义购买次数结束
        /// </summary>
        public int? EndPurchases { get; set; }

        /// <summary>
        /// 类别ID
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// 消费金额
        /// </summary>
        public AmountOfConsumption? AmountOfConsumption { get; set; }

        /// <summary>
        /// 消费金额自定义搜索开始
        /// </summary>
        public int? StartAmountOfConsumption { get; set; }

        /// <summary>
        /// 消费金额自定义搜索结束
        /// </summary>
        public int? EndAmountOfConsumption { get; set; }

        /// <summary>
        /// 会员分组ID
        /// </summary>
        public int? LabelId { get; set; }

        /// <summary>
        /// 标签数组
        /// </summary>
        public IEnumerable<long> LabelIds { get; set; }

        /// <summary>
        /// 会员分组搜索
        /// </summary>
        public MemberStatisticsType? MemberStatisticsType { get; set; }
    }
}
