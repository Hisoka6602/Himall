﻿// JavaScript source code
$(function () {
    $("#reNewTypeB").hide();
    $("#lblRenewTimeTip").hide();

    $('input:radio[value=1]').attr("checked", true);

    //选择按钮事件
    $('input:radio[name="reNewType"]').bind("click", function () {
        var selecttype = $('input:radio[name="reNewType"]:checked').val();
        if (selecttype == 1) {
            $("#reNewTypeA").show();
            $("#lblRenewTimeTip").show();
            $("#reNewTypeB").hide();
            GetInfoAfterTimeSelect();
        }
        else {
            $("#reNewTypeA").hide();
            $("#lblRenewTimeTip").hide();
            $("#reNewTypeB").show();
            GetInfoAfterGradeSelect();
        }
        InitCheckGrade();//判断是否已升级到最高级
    });

    //升级套餐选择事件
    $("#gradeSelect").change(function () {
        GetInfoAfterGradeSelect();
    });

    //时长选择事件
    $("#timeSelect").change(function () {
        GetInfoAfterTimeSelect();
    });

    //支付事件
    $("#RenewPay").bind("click", RenewPay);

    GetInfoAfterTimeSelect();
    InitCheckGrade();//判断是否已升级到最高级
});

//是否可以升级
function InitCheckGrade() {
    var hidgradecount = $("#hidgradecount").val();
    if (hidgradecount > 1)
        return;//说明可以升级套餐，则不需运行下面代码

    var selecttype = $('input:radio[name="reNewType"]:checked').val();
    //选择的是升级套餐
    if (selecttype == 2) {
        $("#lblGradeTip").html("已是最高级套餐");
        $("#lblGradeTip").addClass("red2");
        $("#RenewPay").attr("disabled", "true");
    } else {
        $("#lblGradeTip").html("");
        $("#lblGradeTip").removeClass("red2");
        $("#RenewPay").removeAttr("disabled");//
    }
}

function RenewPay() {
    var flag = $("#RenewPay").attr("data-flag");
    if (flag == "false") {
        $.dialog.errorTips("请选择升级套餐");
        return false;
    }
    else {
        if ($("#lblAmount").html() == "") {
            $.dialog.tips("应缴金额错误");
            return false;
        }
        if (parseFloat($("#lblAmount").html()) && parseFloat($("#lblAmount").html()) < 0) {
            $.dialog.errorTips("应缴金额必须为正数");
            return false;
        }

        var selecttype = $('input:radio[name="reNewType"]:checked').val();
        var value = 0;
        if (selecttype == "1") {
            value = parseInt($("#timeSelect option:selected").val());
        }
        else {
            value = parseInt($("#gradeSelect option:selected").val());
        }
        //如果支付金额为0，则直接跳过支付接口，保存
        if (parseFloat($("#lblAmount").html()) == 0) {
            $.post('UpRenew', { GradeId: value, Year: value, ShopId: $("#ShopId").val(), Amount: 0, Type: parseInt(selecttype) }, function (result) {
                if (result.success) {
                    $.dialog.tips('操作成功!', function () {
                        location.href = location.href;
                    });
                }
                else
                    $.dialog.alert('操作失败!' + result.msg);
            });
        } else {
            var loading = showLoading();
            $.post("PaymentList", { balance: parseFloat($("#lblAmount").html()), type: selecttype, value: value }, function (result) {
                loading.close()
                var html = '';
                $.each(result, function (index, item) {
                    html += '<li style="margin-bottom:20px;margin-left:10px;">\
                        <label>\
                            <input style="position:relative;top:10px;" type="radio" class="jdradio" value="' + item.RequestUrl + '" name="requestUrl" id="' + item.Id + '" urlType="' + parseInt(item.UrlType) + '" />\
                            <img width="180" height="46" style="margin-left:15px;" width="165" height="48" alt="微信PC" src="' + item.Logo + '" />\
                        </label>\
                    </li>'
                });
                html += '<li style="margin-bottom:20px;margin-left:10px;">\
                        <label style="line-height:38px;font-size: 15px;">\
                            <input style="position:relative;top:10px;" type="radio" class="jdradio" value="0" name="requestUrl" id="" urlType="99" />\
                            <span class="ml15">帐号余额 </span> <span class="red">(￥' + balance + ')</span>\
                        </label>\
                    </li>';
                $("#payMent").find("ul").html('');
                $("#payMent").find("ul").append(html);

                $.dialog({
                    title: '店铺续费',
                    lock: true,
                    width: 300,
                    id: 'goodCheck',
                    content: $("#payMent")[0],
                    padding: '20px',
                    button: [
                        {
                            name: '支付',
                            callback: function () {
                                if ($('input[name="requestUrl"]:checked').length != 0) {                                    
                                    if ($('input[name="requestUrl"]:checked').attr('urlType') == "1") {
                                        PayMessage();
                                        var url = $('input[name="requestUrl"]:checked').val();
                                        var toUrl = '/Pay/QRPay/?id=Himall.Plugin.Payment.WeiXinPay_Native&url=' + url;
                                        window.open(toUrl);
                                    }
                                    else if ($('input[name="requestUrl"]:checked').attr('urlType') == "2") {
                                        PayMessage();
                                        var url = $('input[name="requestUrl"]:checked').val();
                                        console.log(url);
                                        BuildPostForm('pay_form', url, '_blank').submit();
                                    }
                                    else if ($('input[name="requestUrl"]:checked').attr('urlType') == "99") {
                                        AccountBalancePay(parseFloat($("#lblAmount").html()), selecttype, value);
                                    }
                                    else {
                                        PayMessage();
                                        window.open($('input[name="requestUrl"]:checked').val());
                                    }
                                }
                                else
                                    $.dialog.tips("请选择支付方式");
                            },
                            focus: true
                        }]
                });
            });

            $('input[name="requestUrl"]').change(function () {
                var url = $(this).val();
                if ($(this).attr('urlType') == "1")
                    url = '/pay/QRPay?url=' + url + '&id=' + $(this).attr('id') + '&orderIds=' + orderIds;
            });
        }
    }
}

function AccountBalancePay(balance, selecttype, value) {
    $.post('AccountBalancePay', { balance: balance, type: selecttype, value: value }, function (result) {
        if (result.success) {
            $.dialog.tips('操作成功!', function () {
                window.location.reload(true);
            });
        }
        else
            $.dialog.alert('操作失败!' + result.msg);
    });
}

function PayMessage() {
    $.dialog({
        title: '登录平台支付',
        lock: true,
        content: '<p>请您在新打开的支付平台页面进行支付，支付完成前请不要关闭该窗口</p>',
        padding: '30px 20px',
        button: [
            {
                name: '已完成支付',
                callback: function () {
                    location.href = location.href;
                }
            },
            {
                name: '支付遇到问题',
                callback: function () {

                }
            }]
    });
}

function BuildPostForm(fm, url, target) {
    var e = null, el = [];
    if (!fm || !url)
        return e;
    target = target || '_blank';
    e = document.getElementById(fm);
    if (!e) {
        e = document.createElement('Form');
        e.Id = fm;
        document.body.appendChild(e);
    }

    e.method = 'post';
    e.target = target;
    e.style.display = 'none';
    e.enctype = 'application/x-www-form-urlencoded';

    var idx = url.indexOf('?');
    var para = [], op = [];
    if (idx > 0) {
        para = url.substring(idx + 1, url.length).split('&');
        url = url.substr(0, idx);//截取URL
        var keypair = [];
        for (var p = 0; p < para.length; p++) {
            idx = para[p].indexOf('=');
            if (idx > 0) {
                el.push('<input type="hidden" name="' + para[p].substr(0, idx) + '" id="frm' + para[p].substr(0, idx) + '" value="' + para[p].substring(idx + 1, para[p].length) + '" />');
            }
        }
    }

    e.innerHTML = el.join('');
    e.action = url;
    return e;
}

function GetInfoAfterGradeSelect() {
    var loading = showLoading();
    var selectGrade = parseInt($("#gradeSelect option:selected").val());
    if (selectGrade == 0) {
        $("#RenewPay").attr("data-flag", "false");
        $("#lblAmount").html("");
        $("#lblGradeTip").html("");
        loading.close();
    }
    else {
        $.ajax({
            type: "post",
            url: "../Shop/GetInfoAfterGradeSelect",
            data: { grade: selectGrade },
            dataType: "json",
            success: function (data) {
                if (data.success) {
                    $("#lblAmount").html(data.amount);
                    $("#lblGradeTip").html(data.gradeTip);
                    $("#lblRenewTimeTip").hide();
                    $("#RenewPay").attr("data-flag", "true");
                } else {
                    $("#lblAmount").html("");
                    $("#lblGradeTip").html("");
                    $("#lblRenewTimeTip").hide();
                    $("#RenewPay").attr("data-flag", "false");
                }
                loading.close();
            },
            error: function (data) {
                loading.close();
                $("#lblAmount").html("");
                $("#lblGradeTip").html("");
                $("#lblRenewTimeTip").hide();
                $("#RenewPay").attr("data-flag", "false");
                $.dialog.errorTips("网络错误");
            }
        });
    }
}

function GetInfoAfterTimeSelect() {
    var loading = showLoading();
    var year = parseInt($("#timeSelect option:selected").val());
    $.ajax({
        type: "post",
        url: "../Shop/GetInfoAfterTimeSelect",
        data: { year: year },
        dataType: "json",
        success: function (data) {
            if (data.success) {
                $("#lblAmount").html(data.amount);
                $("#lblRenewTime").html(data.endtime);
                $("#lblRenewTimeTip").show();
                $("#RenewPay").attr("data-flag", "true");
            } else {
                $("#lblAmount").html("");
                $("#lblRenewTime").html("");
                $("#lblRenewTimeTip").hide();
                $("#RenewPay").attr("data-flag", "false");
            }
            loading.close();
        },
        error: function (data) {
            loading.close();
            $("#lblAmount").html("");
            $("#lblRenewTime").html("");
            $("#lblRenewTimeTip").hide();
            $("#RenewPay").attr("data-flag", "false");
            $.dialog.errorTips("网络错误");
        }
    });
}