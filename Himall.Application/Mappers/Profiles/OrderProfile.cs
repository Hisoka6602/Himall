using AutoMapper;
using Himall.DTO;
using Himall.Entities;
using System.Collections.Generic;

namespace Himall.Application.Mappers.Profiles
{
    public class OrderProfile : Profile
    {
        protected override void Configure()
        {
            base.Configure();

            CreateMap<OrderInfo, DTO.Order>();
            CreateMap<DTO.Order, OrderInfo>();

            CreateMap<OrderInfo, DTO.FullOrder>();
            CreateMap<DTO.FullOrder, OrderInfo>();

            CreateMap<OrderPayInfo, DTO.OrderPay>();
            CreateMap<DTO.OrderPay, OrderPayInfo>();

            CreateMap<OrderRefundInfo, DTO.OrderRefund>();
            CreateMap<DTO.OrderRefund, OrderRefundInfo>();

            CreateMap<OrderItemInfo, DTO.OrderItem>();
            CreateMap<DTO.OrderItem, OrderItemInfo>();

            CreateMap<OrderCommentInfo, DTO.OrderComment>().ReverseMap();
            CreateMap<OrderRefundLogInfo, DTO.OrderRefundlog>().ReverseMap();
            CreateMap<VerificationRecordInfo, DTO.VerificationRecordModel>().ReverseMap();
            CreateMap<OrderVerificationCodeInfo, DTO.OrderVerificationCodeModel>().ReverseMap();
            CreateMap<Invoices, OrderInvoice>();
            CreateMap<OrderInvoice, OrderInvoiceInfo>().ReverseMap();
            CreateMap<OrderCreating.SubOrder, OrderInfo>();
            CreateMap<OrderCreating.ProductItem, OrderItemInfo>()
                .ForMember(p => p.RealTotalPrice, opt => opt.MapFrom(v => v.Amount))
                .ForMember(p => p.ThumbnailsUrl, opt => opt.MapFrom(v => v.Thumbnails))
                .ForMember(p => p.PlatCouponDiscount, opt => opt.MapFrom(v => v.PlatformDiscount));
        }
    }
}
