﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 门店
    /// </summary>
    public class ShopBranch
    {
        /// <summary>
        /// 所属商家店铺名称
        /// </summary>
        public string ShopName { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public string ShopBranchInTagNames { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public string ShopBranchTagId { get; set; }
        /// <summary>
        /// ID
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 所属商家店铺ID
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 门店名称
        /// </summary>
        [Required(ErrorMessage = "请填写门店名称")]
        public string ShopBranchName { get; set; }
        /// <summary>
        /// 门店所在地址ID
        /// </summary>
        [Required(ErrorMessage = "门店所在地址")]
        public int AddressId { get; set; }
        /// <summary>
        /// 门店所在详细地址
        /// </summary>
        [Required(ErrorMessage = "请填写门店所在详细地址")]
        public string AddressDetail { get; set; }
        /// <summary>
        /// 门店地址中文
        /// </summary>
        public string AddressFullName { get; set; }
        /// <summary>
        /// 联系人
        /// </summary>
        [Required(ErrorMessage = "请填写联系人")]
        public string ContactUser { get; set; }
        /// <summary>
        /// 联系电话
        /// </summary>
        [Required(ErrorMessage = "请填写联系电话")]
        public string ContactPhone { get; set; }
        /// <summary>
        /// 门店状态
        /// </summary>
        public Himall.CommonModel.ShopBranchStatus Status { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public System.DateTime CreateDate { get; set; }
        /// <summary>
        /// 管理员帐号
        /// </summary>
        [Required(ErrorMessage = "请填写管理员帐号")]
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "请填写管理员密码")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "密码最小长度为6位")]
        public string PasswordOne { get; set; }

        /// <summary>
        /// 重复密码
        /// </summary>
        [Required(ErrorMessage = "请填写管理员密码")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "密码最小长度为6位")]
        public string PasswordTwo { get; set; }
        /// <summary>
        /// 地址ID PATH
        /// </summary>
        public string RegionIdPath { get; set; }

        /// <summary>
        /// 门店服务半径/配送半径
        /// </summary>
        [Required(ErrorMessage = "门店配送半径")]
        public int ServeRadius { get; set; }

        /// <summary>
        /// 门店经度
        /// </summary>
        [Required(ErrorMessage = "门店经度")]
        public decimal Longitude { get; set; }

        /// <summary>
        /// 门店维度
        /// </summary>
        [Required(ErrorMessage = "门店维度")]
        public decimal Latitude { get; set; }

        /// <summary>
        /// 门店Banner
        /// </summary>
        [Required(ErrorMessage = "门店Banner")]
        public string ShopImages { get; set; }
        /// <summary>
        /// 经纬度距离
        /// </summary>
        [Required(ErrorMessage = "经纬度距离")]
        public double Distance { get; set; }
        /// <summary>
        /// 经纬度距离
        /// </summary>
        [Required(ErrorMessage = "距离单位")]
        public string DistanceUnit { get; set; }
        /// <summary>
        /// 是否有商品且有库存
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// 门店地址全路径
        /// </summary>
        public string AddressPath { get; set; }

        /// <summary>
        /// 是否门店配送
        /// </summary>
        public bool IsStoreDelive { get; set; }
        /// <summary>
        /// 是否自提
        /// </summary>
        public bool IsAboveSelf { get; set; }
        /// <summary>
        /// 配送费
        /// </summary>
        public int DeliveFee { get; set; }
        /// <summary>
        /// 包邮金额
        /// </summary>
        public int FreeMailFee { get; set; }
        /// <summary>
        /// 是否包邮
        /// </summary>
        public bool IsFreeMail { get; set; }
        /// <summary>
        /// 起送费
        /// </summary>
        public int DeliveTotalFee { get; set; }
        /// <summary>
        /// 营业开始时间
        /// </summary>
        public TimeSpan StoreOpenStartTime { get; set; }
        /// <summary>
        /// 营业结束时间
        /// </summary>
        public TimeSpan StoreOpenEndTime { get; set; }

        /// <summary>
        /// 推荐排序
        /// </summary>
        public long RecommendSequence { get; set; }
        /// <summary>
        /// 是否推荐门店
        /// </summary>
        public bool IsRecommend { get; set; }

        /// <summary>
        /// 综合评分
        /// </summary>
        public decimal CommentScore { get; set; }
        public string DaDaShopId { get; set; }
        public bool IsAutoPrint { get; set; }
        public int PrintCount { get; set; }
        public bool EnableSellerManager { get; set; }

        /// <summary>
        /// 是否自行上架商品
        /// </summary>
        public bool IsShelvesProduct { get; set; }
    }
}
