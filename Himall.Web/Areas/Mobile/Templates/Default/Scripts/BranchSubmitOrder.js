﻿$(function () {
    InvoiceOperationInit();
});
var paymentShown = false;
var loading;
var orderIds = '';
function integralSubmit(ids) {
    ajaxRequest({
        type: 'POST',
        url: '/' + areaName + '/Order/PayOrderByIntegral',
        param: { orderIds: ids },
        dataType: 'json',
        success: function (data) {
            if (data.success == true) {
                $.dialog.succeedTips('支付成功！', function () {
                    var deliveryType = $(".divider .active").attr("deliverytype");
                    if (deliveryType == "1") {
                        location.href = '/' + areaName + '/Member/Orders';//当是自提订单跳订单列表;
                    } else {
                        location.href = '/' + areaName + '/Order/OrderShare?orderids=' + orderIds;
                    }
                }, 0.5);

            }
        },
        error: function (data) { $.dialog.tips('支付失败,请稍候尝试.', null, 0.5); }
    });
}

$('#submit-order').click(function () {

    var isCashOnDelivery = false;
    if ($("#icod").val() == "True") {
        isCashOnDelivery = $(".way-01 .offline").hasClass("active");
    }

    var integral = 0;
    if (isintegral) {
        integral = $("#useintegral").val();
        integral = isNaN(integral) ? 0 : integral;
    }
    var capital = 0;
    if (iscapital) {
        capital = $("#userCapitals").val();
        capital = isNaN(capital) ? 0 : capital;
    }
    var couponIds = ""; var platcouponId = "";
    $('input[name="couponIds"]').each(function (i, e) {
        var type = $(this).attr("data-type");
        var shopid = $(this).attr("data-shopid");
        couponIds = couponIds + $(e).val() + '_' + type + '_' + shopid + ',';
    })
    if (couponIds != '') {
        couponIds = couponIds.substring(0, couponIds.length - 1);
    }

    $('input[name="platCouponIds"]').each(function (i, e) {
        var type = $(this).attr("data-type");
        var shopid = $(this).attr("data-shopid");
        platcouponId = platcouponId + $(e).val() + '_' + type + '_' + shopid + ',';
    })
    if (platcouponId != '') {
        platcouponId = platcouponId.substring(0, platcouponId.length - 1);
    }
    // alert(couponIds); return;
    var latAndLng = $("#latAndLng").val();
    var recieveAddressId = $('#shippingAddressId').val();
    recieveAddressId = parseInt(recieveAddressId);
    recieveAddressId = isNaN(recieveAddressId) ? null : recieveAddressId;
    var productType = $("#productType").val();
    var deliveryType = $(".divider .active").attr("deliverytype");
    if (!recieveAddressId && productType == 0 && deliveryType != 1)
        $.dialog.alert('请选择或新建收货地址');
    else {
        var model = {};
        model.AddressId = recieveAddressId;
        model.UseIntegral = integral;
        model.UseCapital = capital;
        model.IsCashOnDelivery = isCashOnDelivery;
        //model.invoiceType = invoiceType;
        //model.invoiceTitle = invoiceTitle;
        //model.invoiceCode = invoiceCode;
        //model.invoiceContext = invoiceContext;

        model.GrouponGroupId = $('#groupId').val();
        model.GrouponId = $('#groupActionId').val();
        model.Password = $("#PayCapitalPwd").val();
        model.CollocationId = $("#collocationId").val();
        model.FlashSaleId = $("#flashSaleId").val();
        model.IsVirtual = productType == "1" ? true : false;
        model.CartItems = [];
        if ($("#cartItems").val() != "") {
            var cartItemArr = $("#cartItems").val().split(",");
            for (var i = 0; i < cartItemArr.length; i++) {
                model.CartItems.push(cartItemArr[i]);
            }
        }
        model.Choices = [];
        var choicesCoupons = JSON.parse($("#choiceCoupons").val());
        if (choicesCoupons && choicesCoupons.length > 0) {
            $(choicesCoupons).each(function (index, item) {
                if (item.RecordId > 0) {
                    model.Choices.push(item);
                }
            });
        }
        var isTrue = false;
        var orderShops = [];
        $('.goods-info[shopid]').each(function () {
            var shopId = $(this).attr('shopid');
            var orderShop = {};
            orderShop.ShopId = shopId;
            orderShop.ShopBranchId = $("#shopBranchId").val();
            orderShop.Items = [];
            $('.item[skuid][count]', this).each(function () {
                var productItem = {};
                productItem.RoomId = 0;
                productItem.ProductId = $(this).attr('pid');
                productItem.SkuId = $(this).attr('skuid');
                productItem.Price = $(this).attr('price');
                productItem.Quantity = $(this).attr('count');
                productItem.VirtualContents = [];
                //虚拟商品用户信息项
                vContents = JSON.parse(window.localStorage.getItem("virtualProductItemJson"));
                if (vContents && vContents.length > 0) {
                    $(vContents).each(function (index, item) {
                        var vContentItem = {};
                        vContentItem.Name = item.VirtualProductItemName;
                        vContentItem.Content = item.Content;
                        vContentItem.Type = item.VirtualProductItemType;
                        productItem.VirtualContents.push(vContentItem);
                    });
                }
                orderShop.Items.push(productItem);
            });
            var deliveryType = $(".divider .active").attr("deliverytype");
            orderShop.DeliveryType = deliveryType;
            orderShop.IsCashOnDelivery = 0;
            orderShop.PaymentType = {};
            if (orderShop.IsCashOnDelivery) {
                orderShop.PaymentType.Id = -1;
                orderShop.PaymentType.DisplayName = "货到付款";
            }
            else {
                orderShop.PaymentType.Id = 0;
                orderShop.PaymentType.DisplayName = "在线支付";
            }
            if (orderShop.DeliveryType == "1" && !orderShop.ShopBranchId) {
                $.dialog.tips('到店自提必须选择门店！');
                isTrue = true;
                return false;
            }
            orderShop.Remark = $('.orderRemarks#remark_' + shopId).val();
            if (orderShop.Remark.length > 200) {
                $.dialog.tips('留言信息至多200个汉字！');
                isTrue = true;
                return false;
            }
            //发票
            var para = {};
            var $form = $('div[name="formInvoice' + shopId + '"]');
            var typeid = parseInt($form.find('.dvInvoiceType span.active').attr('data-id'));
            var typename = $('.bill-title' + shopId).html();
            if (typename == "不需要发票")
                typeid = 0;
            if (typeid > 0) {
                var title = $form.find('.dvInvoiceRise span.active').html();
                var code = "", s = "";
                if (title.indexOf("公司") > -1 && typeid != 3 && typeid != 0) {
                    title = $form.find('input[name="invoicename"]').val();
                    if ($.trim(title) == "") {
                        $.dialog.errorTips('发票公司名必填！');
                        isTrue = true;
                        return;
                    }
                    code = $form.find('input[name="invoicecode"]').val();
                    if ($.trim(code) == "") {
                        $.dialog.errorTips('发票税号必填！');
                        isTrue = true;
                        return;
                    }
                }
                if (typeid != 3 && title == "个人") {
                    title = $("#invoicepepole").val();
                    if ($.trim(title) == "") {
                        $.dialog.errorTips('个人发票抬头必填！');
                        isTrue = true;
                        return;
                    }
                }
                para.InvoiceTitle = title;
                para.InvoiceCode = code;
                var context = $form.find('.dvInvoiceContext span.active').html();
                if ($.trim(context) == "") {
                    $.dialog.errorTips('请选择发票类容！');
                    isTrue = true;
                    return;
                }
                if (typeid == 2) {
                    var cellphone = $form.find('input[name="cellphone"]').val();
                    var email = $form.find('input[name="email"]').val();
                    if (!cellphone) {
                        $.dialog.errorTips("请输入收票人手机号");
                        isTrue = true;
                        return;
                    }
                    if (!email) {
                        $.dialog.errorTips("请输入收票人邮箱");
                        isTrue = true;
                        return;
                    }
                    para.CellPhone = cellphone;
                    para.Email = email;
                }

                if (typeid == 3) {
                    $form.find('.vatInvoice input').each(function () {
                        if ($.trim($(this).val()) == "") {
                            $.dialog.errorTips("增值税发票项均为必填");
                            isTrue = true;
                            return;
                        }
                    });
                    para.InvoiceTitle = $form.find('input[name="companyname"]').val();
                    para.InvoiceCode = $form.find('input[name="companyno"]').val();
                    para.RegisterAddress = $form.find('input[name="registeraddress"]').val();
                    para.RegisterPhone = $form.find('input[name="registerphone"]').val();
                    para.BankName = $form.find('input[name="bankname"]').val();
                    para.BankNo = $form.find('input[name="bankno"]').val();
                    para.RealName = $form.find('input[name="vatrealname"]').val();
                    para.CellPhone = $form.find('input[name="vatcellphone"]').val();
                    para.RegionID = regionid || $form.find('input[name="RegionID"]').val();
                    //para.InvoiceContext = $form.find(".dvInvoiceContextVat .invoice-item-selected span").html();
                    para.Address = $form.find('input[name="vataddress"]').val();
                }
            }
            para.InvoiceType = typeid;
            para.InvoiceContext = context;
            orderShop.Invoice = para;

            orderShops.push(orderShop);
            window.localStorage.removeItem("invoiceInfo" + shopId);
        });
        if (isTrue) {
            return false;
        }
        model.Subs = orderShops;

        model.IsVirtual = productType == 1;



        loading = showLoading();
        var producttotal = parseFloat($("#producttotal").val());
        var tax = CalInvoiceRate(parseFloat($("#integralPerMoney").val()));
        var total = parseFloat($("#total").val());
        var integralAmount = 0;
        var enabledIntegral = $('#userIntegralSwitch').is(':checked');
        if (enabledIntegral) {
            integralAmount = parseFloat($("#integralPerMoney").val());
            if (isNaN(integralAmount)) { integralAmount = 0; }
        }
        var producttotal = CaclProductTotal();
        var freight = CaclFreight();
        var platcouponAmount = parseFloat($("#platcoupontotal").val());
        if (isNaN(platcouponAmount)) { platcouponAmount = 0; }
        model.Amount = parseFloat((isNaN(producttotal) ? 0 : producttotal).toAdd(tax).toSub(integralAmount).toSub(capital));
        $.post('/' + areaName + '/Order/IsAllDeductible', { integral: model.UseIntegral, total: total }, function (result) {
            if (result.data) {
                loading.close();
                $.dialog.confirm("您确定用积分抵扣全部金额吗?", function () {
                    submit(model);
                });
            }
            else {
                submit(model, loading);
            }
        });
    }
});


//计算邮费
function CaclProductTotal() {
    var totalprice = parseFloat($("#proAmount").data('nfprice'));
    if (isNaN(totalprice)) { totalprice = 0; }
    var freight = 0;
    if (!$("#divDeliveFee").is(":hidden")) {
        freight = parseFloat($("#divDeliveFee").data("fee"));
        if (isNaN(freight)) { freight = 0; }
    }
    totalprice = totalprice.toAdd(freight);
    return totalprice;
}
function submit(model, loading) {
    if (loading == null)
        loading = showLoading();
    var url = '/' + areaName + '/BranchOrder/NewSubmit';
    //if (isLimitTimeBuy=="True") {
    //    url = '/' + areaName + '/Order/SubmitLimitOrder';
    //}
    $.post(url, model, function (result) {
        if (result.success) {
            if (result.data != undefined && result.data.Orders != null) {
                orderIds = result.data.Orders.join(",");//当前订单号
                //在货到付款，且只有一个店铺时
                if (model.isCashOnDelivery && model.orderShops.length == 1) {
                    loading.close();
                    if (result.data.realTotalIsZero) {
                        integralSubmit(orderIds);
                    }
                    else {
                        $.dialog.succeedTips('提交成功！', function () {
                            location.href = '/' + areaName + '/Member/Orders';
                        }, 0.5);
                    }
                }
                else if (result.data.Amount <= 0) {
                    loading && loading.close();
                    integralSubmit(orderIds);
                }
                else {
                    loading && loading.close();
                    GetPayment(orderIds);
                }
            }
            else if (result.data.Id != null && result.data != undefined) {//限时购
                var requestcount = 0;
                ///检查订单状态并做处理
                function checkOrderState() {
                    $.getJSON('/OrderState/Check', { Id: result.data.Id }, function (r) {
                        if (r.state == "Processed" && r.Total === 0) {
                            loading && loading.close();
                            integralSubmit(r.orderIds[0].toString());
                        }
                        else if (r.state == "Processed") {
                            loading && loading.close();
                            GetPayment(r.orderIds[0].toString());
                        }
                        else if (r.state == "Untreated") {
                            requestcount = requestcount + 1;
                            if (requestcount <= 10)
                                setTimeout(checkOrderState, 0);
                            else {
                                $.dialog.tips("服务器繁忙,请稍后去订单中心查询订单");
                                loading && loading.close();
                            }
                        }
                        else {
                            $.dialog.tips('订单提交失败,错误原因:' + r.message);
                            loading && loading.close();
                        }
                    });
                }
                checkOrderState();
            }
            else {
                loading && loading.close();
                $.dialog.alert(result.msg, function () {
                    window.location.href = window.location.href;
                });
            }
        } else {
            loading && loading.close();
            $.dialog.alert(result.msg, function () {
                window.location.href = window.location.href;
            });
        }
    });
}

$(document).on('click', '#paymentsChooser .close', function () {
    $('.cover,.custom-dialog').hide();
    $('#capitalstepone').remove();
    $('#payCapitalPwd').remove();
    if ($("#userCapitalSwitch").is(':checked')) {
        $('#userCapitalSwitch').click();
    }
    if (paymentShown) {//如果已经显示支付方式，则跳转到订单列表页面
        //location.href = '/' + areaName + '/Member/Orders';
        location.href = '/common/site/pay?area=mobile&platform=' + areaName.replace('m-', '') + '&controller=member&action=orders&neworderids=' + orderIds;
    }
});
function CaclFreight() {
    var freight = 0;
    //门店只支持自提时，没有配送费div，则判断
    if ($("#divDeliveFee").length > 0) {
        var delivefeeOpen = !$("#divDeliveFee").is(":hidden");
        if (delivefeeOpen) {//减配送费
            freight += parseFloat($("#divDeliveFee").attr("data-fee"));
        }
    }
    return freight;
}

//计算总价格
function CalcPrice() {
    var sum = 0;
    $('.cart-total .price').each(function () {
        var pr = $(this).data('price');
        if (pr == undefined || pr == null || pr == "")
            pr = 0;
        sum = parseFloat(sum.toAdd(parseFloat(pr)));
        //sum += parseFloat($(this).data('price'));
    });
    var enabledIntegral = $('#userIntegralSwitch').is(':checked');
    if (enabledIntegral) {
        sum = parseFloat(sum.toSub(parseFloat($("#integralPerMoney").val())));
        $("#integralPrice").html("-￥" + $("#integralPerMoney").val());
    }

    //门店只支持自提时，没有配送费div，则判断
    if ($("#divDeliveFee").length > 0) {
        var delivefeeOpen = $("#divDeliveFee").is(":hidden");
        if (delivefeeOpen) {//减配送费
            sum = parseFloat(sum.toSub(parseFloat($("#divDeliveFee").attr("data-fee"))));
        }
    }
    var invoice = CalInvoiceRate(parseFloat($("#integralPerMoney").val()));

    if ($("#platCouponIds").val() != undefined) {
        //sum = sum - $("#platCouponIds").val();//减去平台优惠券
    }
    sum = parseFloat(sum.toAdd(invoice));
    var enabledCapital = $("#userCapitalSwitch").is(':checked');
    if (enabledCapital) {
        var totalCapital = parseFloat($("#capitalAmount").val());
        var inputcapital = parseFloat($("#capital").val());
        var capital = totalCapital;
        if (sum <= 0) {
            capital = 0;
            if (inputcapital != capital) {
                $("#capital").val(parseFloat(capital).toFixed(2));
            }
            $("#capitalPrice").html("-￥" + parseFloat(capital).toFixed(2));
            $("#userCapitals").val(parseFloat(capital).toFixed(2));
        } else {
            if (!inputcapital || inputcapital < 0) {
                inputcapital = 0;
            }
            if (inputcapital > totalCapital) {
                inputcapital = totalCapital;
            }
            if (sum <= inputcapital) {
                capital = sum;
            } else {
                capital = inputcapital;
            }
            //重新计算余额
            if (isResetUseCapital && totalCapital > 0) {
                if (totalCapital < sum) {
                    capital = totalCapital;
                } else {
                    capital = sum;
                }
                isResetUseCapital = false;
            }
            sum = parseFloat(sum.toSub(capital));
            if (inputcapital != capital) {
                $("#capital").val(parseFloat(capital).toFixed(2));
            }
            $("#capitalPrice").html("-￥" + parseFloat(capital).toFixed(2));
            $("#userCapitals").val(parseFloat(capital).toFixed(2));
        }
    }
    if (sum <= 0) sum = 0;
    $('#allTotal').html('¥' + MoneyRound(sum)).attr('data-alltotal', MoneyRound(sum));
}

function getCount() {
    var result = [];
    $('.goods-info[shopid]').each(function () {
        var shopId = $(this).attr('shopid');
        $('.item[pid][count]', this).each(function () {
            var pid = $(this).attr('pid');
            var count = $(this).attr('count');
            var amount = count * parseFloat($(this).attr('price'));//总金额
            result.push({ shopId: shopId, productId: pid, count: count, amount: amount });
        });
    });

    return result;
}

function freeFreight(shopId) {
    var goodsInfo = $('.goods-info#' + shopId);
    var priceElement = goodsInfo.find('.item .price');
    var oldPrice = parseFloat(priceElement.data('oldprice'));
    var price = parseFloat(oldPrice.toSub(parseFloat(priceElement.data('freight'))));
    priceElement.html('￥' + parseFloat(price).toFixed(2)).data('price', parseFloat(price).toFixed(2));
    goodsInfo.find(".showfreight").html("免运费");
    CalcPrice();
}

//刷新运费
function refreshFreight(regionId) {
    //获取运费
    var data = getCount();
    $.post('/{0}/order/CalcFreight'.format(areaName), { parameters: data, addressId: regionId }, function (result) {
        if (result.success == true) {
            for (var i = 0; i < result.data.length; i++) {
                var item = result.data[i];
                var shopId = item.shopId;
                var freight = item.freight;

                var priceDiv = $('.goods-info#{0} .price'.format(shopId));
                var amount = parseFloat(priceDiv.data('price')) - parseFloat(priceDiv.data('freight'));
                var freeFreightAmount = parseFloat(priceDiv.data('freefreight'));
                if (freeFreightAmount <= 0 || amount < freeFreightAmount) {
                    $('.goods-info#{0} .showfreight'.format(shopId)).html('￥' + parseFloat(freight).toFixed(2));
                    priceDiv.data('price', (parseFloat(amount) + parseFloat(freight)).toFixed(2)).data('freight', freight).html('￥' + (parseFloat(amount) + parseFloat(freight)).toFixed(2));
                }
                if (priceDiv.is('[selftake]'))
                    freeFreight(shopId);
            }
            CalcPrice();
        } else
            $.dialog.errorTips(result.msg);
    });
}


$("#btnAddInvoice").click(function () {
    var _t = $(this);
    _t.hide();

    var html = '<div>';
    html += '<div class="top">';
    html += '<span class="bill-check" aria-hidden="true" onclick="AddSpanClick(this)"></span>';
    html += '<i>公司</i>';
    html += '</div>';
    html += '<div class="rights">';
    html += '<a href="javascript:void(0);" class="update-tit" style="color:#2894FF">保存</a>';
    html += '<a href="javascript:void(0);" class="ml10 del-tit hide" style="color:#2894FF">删除</a>';
    html += '</div>';
    html += '<div style="display:block;">';
    html += '<div><input type="text" autofocus="autofocus" placeholder="公司全称" id="invoicename"  style="opacity:1"/></div>';
    html += '<div><input type="text" placeholder="税号" id="invoicecode"  style="opacity:1"/></div>';
    html += '</div>';
    html += '</div>';

    $("#AllInvoice").prepend(html);
    $("#AllInvoice input").get(0).focus();
    InvoiceOperationInit();
})

function AddSpanClick(obj) {
    $("#AllInvoice .bill-check").removeClass("active");
    $(obj).addClass("active");
}

//加载发票事件
var regionid,
    showCity = document.getElementsByName('showCity'),
    province = {}, cityPicker3, selectCityName = '', selectAreaName = '';
function InvoiceOperationInit() {
    //发票弹框动画
    $('.bill').click(function (e) {
        var shopId = $(this).data('shopid');
        e.stopPropagation();
        $('.cover').show();
        $('div[name="divInvoice' + shopId + '"]').show();
    });
    $('.cover').click(function (e) {
        e.stopPropagation();
        $('.cover').hide();
        $('.bill-Cart').hide();
    });
    $('.dvInvoiceType span').click(function () {
        var shopId = parseInt($(this).attr('data-shopid'));
        var $fromInvoice = $('div[name="formInvoice' + shopId + '"]');
        $fromInvoice.find('.dvInvoiceType span').removeClass("active");
        $(this).addClass("active");
        var type = parseInt($(this).attr('data-id'));
        switch (type) {
            case 0:
                break;
            case 1:
                $fromInvoice.find('.elInvoice').hide();
                $fromInvoice.find('.dvInvoiceTitle').show();
                $fromInvoice.find('.vatInvoice').hide();
                break;
            case 2:
                $fromInvoice.find('.elInvoice').show();
                $fromInvoice.find('.dvInvoiceTitle').show();
                $fromInvoice.find('.vatInvoice').hide();
                break;
            case 3:
                $fromInvoice.find('.elInvoice').hide();
                $fromInvoice.find('.dvInvoiceTitle').hide();
                $fromInvoice.find('.vatInvoice').show();
                break;
        }
    });
    if ($(".dvInvoiceType span.active").length > 0) {
        $(".dvInvoiceType span.active").eq(0).click();//有选择发票，加载初始值选择的发票信息
    }
    $('.dvInvoiceRise span').click(function () {
        $(this).parent().find('span').removeClass("active");
        $(this).addClass("active");
        var type = parseInt($(this).attr('data-id'));
        if (type == 0) {
            $(this).parent().parent().find('.dvInvoincCompany').hide();
            $(this).parent().parent().find('.dvInvoincPepole').show();

        } else {
            $(this).parent().parent().find('.dvInvoincCompany').show();
            $(this).parent().parent().find('.dvInvoincPepole').hide();
        }
    });
    $('.dvInvoiceContext span').click(function () {
        $(this).parent().find('span').removeClass("active");
        $(this).addClass("active");
    });
    $('input[name="invoicename"]').focus(function () {
        $(this).parent().parent().find('.companylist').show();
    });
    $('input[name="invoicename"]').keyup(function () {
        var val = $(this).val();
        if (val == "")
            $(this).parent().parent().find('.companylist').show();
        else
            $(this).parent().parent().find('.companylist').hide();
    });
    $('ul.companylist li a').click(function () {
        //alert($(this).attr('data-id'));
        var parentDiv = $(this).parent().parent().parent().parent();
        parentDiv.find('input[name="invoicename"]').val($(this).text());
        parentDiv.find('input[name="invoicecode"]').val($(this).attr('data-code'));
        $(this).parent().parent().parent().find('.companylist').hide();

    });

    $('.noInvoice').click(function () {
        var shopId = parseInt($(this).attr('data-shopid'));
        var $form = $(this).parent().parent().parent().parent().find('div[name="formInvoice' + shopId + '"]');
        $form.find('.dvInvoiceType span').removeClass('active');
        $form.find('.dvInvoiceType span').eq(0).addClass('active');
        var s = '不需要发票';
        $('.bill-title' + shopId).html(s);
        CalcPrice();
        $('.cover').hide();
        $('.bill-Cart').hide();
    });

    $('.bill-submit').click(function () {
        var shopId = parseInt($(this).attr('data-shopid'));
        var $form = $(this).parent().find('div[name="formInvoice' + shopId + '"]');
        var typename = $form.find('.dvInvoiceType span.active').html();
        var typeid = parseInt($form.find('.dvInvoiceType span.active').attr('data-id'));
        var rate = parseFloat($form.find('.dvInvoiceType span.active').attr('data-rate'));
        var title = $form.find('.dvInvoiceRise span.active').html();
        var code = "", s = "";
        if (title.indexOf("公司") > -1 && typeid != 3 && typeid != 0) {
            title = $form.find('input[name="invoicename"]').val();
            if ($.trim(title) == "") {
                $.dialog.errorTips('公司名必填！');
                return;
            }
            code = $form.find('input[name="invoicecode"]').val();
            if ($.trim(code) == "") {
                $.dialog.errorTips('税号必填！');
                return;
            }
        }
        var context = "";
        if (typeid > 0) {
            context = $form.find('.dvInvoiceContext span.active').html();
            if ($.trim(context) == "") {
                $.dialog.errorTips('请选择发票内容！');
                return;
            }
        }
        s = '(税率<span class="taxprice red" shopid="' + shopId + '">' + rate + '%</span>) ' + typename + '(' + title + ')';
        var para = {};
        para.InvoiceType = typeid;
        var isSubmit = false;
        switch (typeid) {
            case 0:
                $.dialog.errorTips("请选择发票类型");
                return;
                //s = '不需要发票';
                //$('.bill-title' + shopId).html(s);
                //CalcPrice();
                //$('.cover').hide();
                //$('.bill-Cart').hide();
                break;
            case 1:
                para.Name = title;
                para.Code = code;
                para.InvoiceContext = context;
                isSubmit = true;
                break;
            case 2:
                var cellphone = $form.find('input[name="cellphone"]').val();
                var email = $form.find('input[name="email"]').val();
                if (!cellphone) {
                    $.dialog.errorTips("请输入收票人手机号");
                    return;
                }
                if (!email) {
                    $.dialog.errorTips("请输入收票人邮箱");
                    return;
                }
                para.Name = title;
                para.Code = code;
                para.InvoiceContext = context;
                para.CellPhone = cellphone;
                para.Email = email;
                isSubmit = true;
                break;
            case 3:
                para.Name = $form.find('input[name="companyname"]').val();
                para.Code = $form.find('input[name="companyno"]').val();
                para.RegisterAddress = $form.find('input[name="registeraddress"]').val();
                para.RegisterPhone = $form.find('input[name="registerphone"]').val();
                para.BankName = $form.find('input[name="bankname"]').val();
                para.BankNo = $form.find('input[name="bankno"]').val();
                para.RealName = $form.find('input[name="vatrealname"]').val();
                para.CellPhone = $form.find('input[name="vatcellphone"]').val();
                para.RegionID = regionid || $form.find('input[name="RegionID"]').val();
                para.InvoiceContext = $form.find(".dvInvoiceContextVat .invoice-item-selected span").html();
                para.Address = $form.find('input[name="vataddress"]').val();
                isSubmit = true;
                s = '(税率<span class="taxprice red" shopid="' + shopId + '">' + rate + '%</span>) ' + typename + '(' + para.Name + ')';
                break;
        }

        if (isSubmit) {
            var loading = showLoading();
            $.post("/" + areaName + "/order/SaveInvoiceTitleNew", para, function (result) {
                loading.close();
                if (result.success) {
                    $('.bill-title' + shopId).html(s);
                    CalcPrice();
                    $('.cover').hide();
                    $('.bill-Cart').hide();
                    //写入发票信息缓存
                    window.localStorage.setItem("invoiceInfo" + shopId, JSON.stringify(para));
                }
                else {
                    $.dialog.tips(result.msg);
                }
            });
        }
    });

    $(".dvInvoincCompany .del-title").click(function () {
        var self = this;
        var id = $(self).attr("data-id");
        $.dialog.confirm("确定删除该发票抬头吗？", function () {
            var loading = showLoading();
            $.post("/" + areaName + "/BranchOrder/DeleteInvoiceTitle", { id: id }, function (result) {
                loading.close();
                if (result.success == true) {
                    var _p = $(self).parent();
                    _p.remove();
                    $.dialog.tips('删除成功！');
                }
                else {
                    $.dialog.tips('删除失败！');
                }
            });
        });
    });
    // loadRegion();
}

function loadRegion() {

    regionid = Number($("#RegionID").val());//如果是修改收货地址
    var _temp, _proIndex = 0, _cityIndex = 0, _districtIndex = 0, _streetIndex = 0;
    var _proId = 0, _cityId = 0, _districtId = 0, _streetId = 0;
    $.ajax({
        url: '/common/RegionAPI/GetAllRegion',
        type: 'get', //GET
        async: true,    //或false,是否异步
        data: {
        },
        timeout: 10000,    //超时时间
        dataType: 'json',    //返回的数据格式：json/xml/html/script/jsonp/text
        success: function (data, textStatus, jqXHR) {
            cityPicker3 = new mui.PopPicker({
                layer: 4,
                getData: function (parentId) {
                    var ret = [];
                    if (!parentId) return ret;
                    $.ajax({
                        url: '/common/RegionAPI/GetSubRegion',
                        type: 'get', //GET
                        async: false,    //或false,是否异步
                        data: { parent: parentId, bAddAll: true },
                        timeout: 10000,    //超时时间
                        dataType: 'json',    //返回的数据格式：json/xml/html/script/jsonp/text
                        success: function (data, textStatus, jqXHR) {
                            ret = data;
                        }
                    });
                    return ret;
                }
            });
            cityPicker3.setData(data);
            province = data;
            $(showCity).click(function () {
                var focus = document.querySelector(':focus');
                if (focus)
                    focus.blur();
                cityPicker3.show(function (items) {
                    $(showCity).val((items[0].Name || '') + " " + (items[1].Name || '') + " " + (items[2].Name || '') + " " + (items[3].Name || '').replace("其它", ""));
                    if (items[2].Name) {
                        selectCityName = items[2].Name;
                        selectAreaName = items[1].name;
                    }
                    else {
                        selectCityName = items[1].Name;//当用户先选择了地区，则定位搜索范围为用户选择区域。优先区县
                        selectAreaName = selectCityName;
                    }
                    if (!items[1].Id) {
                        regionid = items[0].Id;
                    } else {
                        if (!items[2].Id) {
                            regionid = items[1].Id;
                        } else {
                            regionid = items[2].Id;
                        }
                    }
                    $("#RegionID").val(regionid);
                });
            });
            if (Number(_proId) > 0) {//当修改收货地址的时候才进行
                _temp = province.filter(function (a, index) {
                    if (a.Id == _proId) {
                        _proIndex = index;
                    }
                    return a.Id == _proId;
                    return;
                });
                cityPicker3.pickers[0].setSelectedIndex(_proIndex);
                var ret = [];
                $.ajax({
                    url: '/common/RegionAPI/GetSubRegion',
                    type: 'get', //GET
                    async: false,    //或false,是否异步
                    data: { parent: _temp[0].Id, bAddAll: true },
                    timeout: 10000,    //超时时间
                    dataType: 'json',    //返回的数据格式：json/xml/html/script/jsonp/text
                    success: function (data, textStatus, jqXHR) {
                        ret = data;
                    }
                });
                _temp = ret.filter(function (a, index) {
                    if (a.Id == _cityId) {
                        _cityIndex = index;
                    }
                    return a.Id == _cityId;
                    return;
                });
                cityPicker3.pickers[1].setSelectedIndex(_cityIndex);
                ret = [];
                $.ajax({
                    url: '/common/RegionAPI/GetSubRegion',
                    type: 'get', //GET
                    async: false,    //或false,是否异步
                    data: { parent: _temp[0].Id, bAddAll: true },
                    timeout: 10000,    //超时时间
                    dataType: 'json',    //返回的数据格式：json/xml/html/script/jsonp/text
                    success: function (data, textStatus, jqXHR) {
                        ret = data;
                    }
                });
                _temp = ret.filter(function (a, index) {
                    if (a.Id == _districtId) {
                        _districtIndex = index;
                    }
                    return a.Id == _districtId;
                    return;
                });
                cityPicker3.pickers[2].setSelectedIndex(_districtIndex);
                ret = [];
                $.ajax({
                    url: '/common/RegionAPI/GetSubRegion',
                    type: 'get', //GET
                    async: false,    //或false,是否异步
                    data: { parent: _temp[0].Id, bAddAll: true },
                    timeout: 10000,    //超时时间
                    dataType: 'json',    //返回的数据格式：json/xml/html/script/jsonp/text
                    success: function (data, textStatus, jqXHR) {
                        ret = data;
                    }
                });
                _temp = ret.filter(function (a, index) {
                    if (a.Id == _streetId) {
                        _streetIndex = index;
                    }
                    return a.Id == _streetId;
                    return;
                });
                cityPicker3.pickers[3].setSelectedIndex(_streetIndex)
            }
        }
    });
}

//计算税费
function CalInvoiceRate(orderTotalIntegral) {
    var invoiceRate = 0.00;
    $('.taxprice').each(function () {
        var _rate = parseFloat($(this).text());
        if (_rate > 0) {
            var shopid = $(this).attr('shopid');
            var total = parseFloat($('#total').val());//商家商品价格
            var delivefee = parseFloat($("#divDeliveFee").attr("data-fee"));
            delivefee = isNaN(delivefee) ? 0 : delivefee;//配送费
            total = total.toSub(delivefee);
            var enabledIntegral = $('#userIntegralSwitch').is(':checked');
            if (enabledIntegral) {
                if (orderTotalIntegral > 0) {
                    total = total.toSub(orderTotalIntegral);//商品总价（商品价-积分抵扣金额）
                }
            }
            invoiceRate = parseFloat(parseFloat(invoiceRate).toAdd(Math.floor(total * (_rate / 100) * 100) / 100));

        }
    });

    if (invoiceRate > 0) {
        $('#divInvoiceAmount').show();
        $('#invoiceAmount').html("￥" + invoiceRate).attr('data-amount', invoiceRate);
    } else
        $('#divInvoiceAmount').hide();
    return invoiceRate;
}
