using Himall.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Models
{
    public class AgreementModel
    {
        public Entities.AgreementInfo.AgreementTypes AgreementType { get; set; }
        public string AgreementTypeText { get { return this.AgreementType.ToDescription(); } }
        public string AgreementContent { get; set; }
    }
}