using Himall.CommonModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Himall.Service
{
   public class PhoneIPCodeService:ServiceBase
    {
        public void AddPhoneIPCode(PhoneIPCodeInfo phonecodeip) {
            DbFactory.Default.InTransaction(()=> {
                DbFactory.Default.Add(phonecodeip);
            });
        }

        public void UpdatePhoneIPCode(PhoneIPCodeInfo phonecodeip)
        {
            DbFactory.Default.InTransaction(() => {
                DbFactory.Default.Update(phonecodeip);
            });
        }

        /// <summary>
        /// 查询是否存在同一Ip或手机号的信息记录
        /// </summary>
        /// <param name="sendname"></param>
        /// <param name="smstype"></param>
        /// <returns></returns>
        public PhoneIPCodeInfo GetPhoneIPCodeInfo(string sendname, SMSSourceType smstype)
        {
            var phoneipcode = DbFactory.Default.Get<PhoneIPCodeInfo>().Where(enty=>enty.SendName==sendname&&enty.SendType==smstype&&enty.SendTime.Date==DateTime.Now.Date).FirstOrDefault();
            return phoneipcode;
        }

        public void Clear(DateTime expire)=>
            DbFactory.Default.Del<PhoneIPCodeInfo>(p => p.SendTime < expire);
    }
}
