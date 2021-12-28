using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.WeiXin
{
    public class SingleMiniProgramButton : Senparc.Weixin.MP.Entities.Menu.SingleButton
    {
        /// <summary>
        /// 类型为miniprogram时必须
        /// 小程序Url，用户点击按钮可打开小程序，不超过1024字节（不支持小程序的老版本客户端将打开本url）
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// 小程序的appid
        /// </summary>
        public string appid { get; set; }

        /// <summary>
        /// 小程序的页面路径
        /// </summary>
        public string pagepath { get; set; }

        public SingleMiniProgramButton()
            : base("click")
        {
        }
    }
}
