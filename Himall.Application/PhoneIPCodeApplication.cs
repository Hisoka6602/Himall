using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using Himall.CommonModel;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;

namespace Himall.Application
{
   public class PhoneIPCodeApplication: BaseApplicaion<PhoneIPCodeService>
    {

        /// <summary>
        /// 记录一条发送短信通知的手机或Ip地址
        /// </summary>
        /// <param name="phoneipcode"></param>
        public static bool AddPhoneIPCode(string sendname,SMSSourceType smsmtype)
        {
            PhoneIPCodeInfo phoneenty = new PhoneIPCodeInfo()
            {
                SendCount = 1,
                SendName = sendname,
                SendTime = DateTime.Now,
                SendType = smsmtype
            };
            bool tag = false;
            var currententy = Service.GetPhoneIPCodeInfo(sendname, smsmtype);
            if (currententy!=null)
            {
                currententy.SendCount = currententy.SendCount + 1;
                Service.UpdatePhoneIPCode(currententy);
                tag = true;
            }
            else
            {
                Service.AddPhoneIPCode(phoneenty);
                tag = true;
            }
            return tag;
        }

        /// <summary>
        /// 验证指定ip或手机号当天的发送次数
        /// </summary>
        /// <param name="sendname"></param>
        /// <param name="smstype"></param>
        /// <returns></returns>
        public static bool ValidSendCount(string sendname,SMSSourceType smstype)
        {
            bool tag = false;
            var ipcount = SiteSettingApplication.SiteSettings.IpSmsCount;//获取同一天IP设置条数
            var phonecount = SiteSettingApplication.SiteSettings.PhoneSmsCount;//获取同一天手机设置的发送次数
            var currententy =Service.GetPhoneIPCodeInfo(sendname,smstype);
            if (currententy!=null)
            {
                if ((smstype == SMSSourceType.Ip && currententy.SendCount < ipcount) ||
                    (smstype == SMSSourceType.Phone && currententy.SendCount < phonecount))
                {
                    tag = true;
                }
            }
            else {
                tag = true;
            }
            return tag;
        }

        public static void Clear() {
            var expire = DateTime.Now.Date.AddDays(-10);
            Service.Clear(expire);
        }
    }
}
