﻿using Himall.CommonModel;
using System;
using System.Collections.Generic;

namespace Himall.DTO.QueryModel
{
    public  class MemberQuery : QueryBase
    {
        public MemberQuery()
        {
            RegionIds = new List<int>();
        }
        /// <summary>
        /// 关键字
        /// </summary>
        public string keyWords { set; get; }
        /// <summary>
        /// 会员状态
        /// </summary>
        public bool? Status { set; get; }
        /// <summary>
        /// 供应商名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 微信昵称
        /// </summary>
        public string weChatNick { get; set; }

        /// <summary>
        /// 城市ID
        /// </summary>
        public int CityId { get; set; }

        /// <summary>
        /// 省份ID
        /// </summary>
        public int ProvinceId { get; set; }

        /// <summary>
        /// 区ID
        /// </summary>
        public int CountyId { get; set; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public long UserId { get; set; }

        public List<int> RegionIds { get; set; }
        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime? RegistTimeStart { get; set; }
        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime? RegistTimeEnd { get; set; }
        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime? LoginTimeStart { get; set; }
        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime? LoginTimeEnd { get; set; }
        /// <summary>
        /// 是否入驻商家
        /// </summary>
        public bool? IsSeller { get; set; }
        /// <summary>
        /// 是否关注公众号
        /// </summary>
        public bool? IsFocusWeiXin { get; set; }
        /// <summary>
        /// 标签Id
        /// </summary>
        public long[] Labels { get; set; }
        /// <summary>
        /// 是否有EMAIL
        /// </summary>
        public bool? IsHaveEmail { get; set; }
        public bool? IsHavePhone { get; set; }
        /// <summary>
        /// 手机号码
        /// </summary>
        public string Mobile { set; get; }

        /// <summary>
        /// 等级ID
        /// </summary>
        public long? GradeId { set; get; }

        /// <summary>
        /// 会员积分范围开始
        /// </summary>
        public int? MinIntegral { set; get; }

        /// <summary>
        /// 会员积分范围结束
        /// </summary>
        public int? MaxIntegral { set; get; }

        /// <summary>
        /// 会员分组搜索
        /// </summary>
        public MemberStatisticsType? MemberStatisticsType { get; set; }

        /// <summary>
        /// 终端来源搜索
        /// </summary>
        public int? Platform { get; set; }
    }
}
