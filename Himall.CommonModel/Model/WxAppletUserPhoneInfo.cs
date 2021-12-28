using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Himall.CommonModel
{
    /// <summary>
    /// 微信小程序获取手机号
    /// </summary>
    public class WxAppletUserPhoneInfo
    {
        public string phoneNumber
        {
            get;set;
        }
        public string purePhoneNumber
        {
            get; set;
        }
        public string countryCode
        {
            get; set;
        }
        public WaterMark watermark
        {
            get; set;
        }        
    }
    //public class WaterMark
    //{
    //    public string appid { get; set; }
    //    public string timestamp { get; set; }
    //}
}
