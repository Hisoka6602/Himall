﻿
namespace Himall.DTO
{
    /// <summary>
    /// 信任登录用户信息
    /// </summary>
    public class OAuthUserModel
    {
        public long UserId { get; set; }
        /// <summary>
        /// 公众号类型
        /// </summary>
        public Himall.Entities.MemberOpenIdInfo.AppIdTypeEnum AppIdType { get; set; }
        /// <summary>
        /// OpenId
        /// </summary>
        public string OpenId { get; set; }

        /// <summary>
        /// 多个应用共享ID
        /// </summary>
        public string UnionId { get; set; }
        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 邮箱地址
        /// </summary>
        public string Email { set; get; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        public long? introducer { get; set; }
        /// <summary>
        /// 真实姓名
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string Headimgurl { get; set; }

        /// <summary>
        /// 登录授权平台
        /// </summary>
        public string LoginProvider { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public string Sex { get; set; }
        /// <summary>
        /// 省份
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        /// 城市
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// 国家
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// 终端来源
        /// </summary>
        public int Platform { get; set; }
        /// <summary>
        /// 分销销售员编号
        /// </summary>
        public long? SpreadId { get; set; }
    }
}
