using AutoMapper;
using Himall.CommonModel;
using Himall.Entities;

namespace Himall.Application.Mappers.Profiles
{
    public class MemberProfile:Profile
	{
		protected override void Configure()
		{
			base.Configure();

            //CreateMap<MemberGrade, MemberGrade>();
            //CreateMap<MemberGrade, Himall.Model.MemberGrade>();

            CreateMap<Entities.MemberInfo, DTO.Members>();
			CreateMap<DTO.Members, Entities.MemberInfo>();
            CreateMap<QueryPageModel<Himall.Entities.MemberInfo>, QueryPageModel<Himall.DTO.Members>>();


            //CreateMap<Himall.Model.LabelInfo, Himall.DTO.Labels>();
            //CreateMap<Himall.DTO.Labels, Himall.Model.LabelInfo>();


            //  CreateMap<Model.MemberConsumeStatisticsInfo, DTO.MemberConsumeStatistics>();
            //  CreateMap<DTO.MemberConsumeStatistics, Model.MemberConsumeStatisticsInfo>();

            CreateMap<Himall.Entities.MemberInfo, Himall.DTO.MemberPurchasingPower>();
            CreateMap<Entities.MemberOpenIdInfo, DTO.MemberOpenId>();
			CreateMap<DTO.MemberOpenId, Entities.MemberOpenIdInfo>();

			CreateMap<Entities.ChargeDetailInfo, DTO.ChargeDetail>();
			CreateMap<DTO.ChargeDetail, Entities.ChargeDetailInfo>();

            CreateMap<SendMessageRecordInfo, DTO.SendMessageRecord>();
            CreateMap<DTO.SendMessageRecord, SendMessageRecordInfo>();
		}
	}
}
