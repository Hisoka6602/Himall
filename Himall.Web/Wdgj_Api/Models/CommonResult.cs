using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Wdgj_Api.Models
{
    public class CommonResult
    {
        public string code { get; set; }
        public string msg { get; set; }
        public string subcode { get; set; }
        public string submessage { get; set; }
    }
}