var reg = /^(0?(13|15|18|14|17|19|16)[0-9]{9})$/;var emailReg = /^([a-zA-Z0-9._-])+@([a-zA-Z0-9_-])+(.[a-zA-Z0-9_-])+/;

//更换手机
$("#phoneCh").on("click", function () {
    $(this).parent().hide();
    $("#getPhoneCode").show();
    $("#MemberPhone").removeAttr("readonly");
})

//更换邮箱
$("#emailCh").on("click", function () {
    $(this).parent().hide();
    $("#getEmailCode").show();
    $("#MemberEmail").removeAttr("readonly");
})

//获取手机验证码
$("#getPhoneCode").on("click", function () {
    sendPhoneCode();
})

$("#getPhoneCodeT").on("click", function () {
    sendPhoneCode();
})

function sendPhoneCode() {
    var Phone = $("#MemberPhone").val();
    if (Phone == "") {
        $.dialog.errorTips('请输入手机号码');
        return false;
    }
    if (!reg.test($("#MemberPhone").val())) {
        $.dialog.errorTips('请输入正确的手机号码');
        $("#MemberPhone").focus();
        return false;
    }
    if ($("#getPhoneCodeT").attr("disabled") == "disabled") {
        console.log(1);
        return false;
    }

    if ($("#istheftbrush").val() == "1") {
        $.post('CheckMemmberContact', { pluginId: "Himall.Plugin.Message.SMS", destination: $("#MemberPhone").val() }, function (result) {
            if (result.success) {
                SlidingShow();//滑动验证显示
            }
            else {
                $.dialog.errorTips(result.msg);
            }
        });
    } else {
        PostSendCode();
    }
}
function PostSendCode() {
    $.post('SendCode', { pluginId: "Himall.Plugin.Message.SMS", destination: $("#MemberPhone").val() }, function (result) {
        if (result.success) {
            var count = 120;
            $("#pcv").show();
            si = setInterval(function () { count--; countDown1(count, "getPhoneCodeT"); }, 1000);
        }
        else {
            $.dialog.errorTips('发送验证码失败：' + result.msg);
        }
    });
}

function countDown1(ss, dv) {
    if (ss > 0) {
        $("#" + dv).val("重新获取（" + ss + "s）");
        $("#" + dv).attr("disabled", "disabled");
    } else {
        $("#" + dv).val("获取验证码");
        $("#" + dv).removeAttr("disabled");
        clearInterval(si);
    }
}

//获取邮箱验证码
$("#getEmailCode").on("click", function () {
    sendEmailCode();
})
$("#getEmailCodeT").on("click", function () {
    sendEmailCode();
})

function sendEmailCode() {
    var Email = $("#MemberEmail").val();
    if (Email == "") {
        $.dialog.errorTips('请输入邮箱');
        return false;
    }
    if (!emailReg.test($("#MemberEmail").val())) {
        $.dialog.errorTips('请输入正确的邮箱');
        $("#MemberEmail").focus();
        return false;
    }
    if ($("#getEmailCodeT").attr("disabled") == "disabled") {
        console.log(1);
        return false;
    }
    $.post('SendCode', { pluginId: "Himall.Plugin.Message.Email", destination: Email }, function (result) {
        if (result.success) {
            var count = 120;
            $("#ecv").show();
            sie = setInterval(function () { count--; countDown1Emali(count, "getEmailCodeT"); }, 1000);
        }
        else {
            $.dialog.errorTips('发送验证码失败：' + result.msg);
        }
    });
}

function countDown1Emali(ss, dv) {
    if (ss > 0) {
        $("#" + dv).val("重新获取（" + ss + "s）");
        $("#" + dv).attr("disabled", "disabled");
    } else {
        $("#" + dv).val("获取验证码");
        $("#" + dv).removeAttr("disabled");
        clearInterval(sie);
    }
}

//----防盗刷验证
var dialog;
function SlidingShow() {
    //if ($("#getPhoneCodeT").attr("disabled")) {
    //    return;
    //}
    var strcontent = $("#myTab_Content1").html();
    dialog = $.dialog({
        title: '获取验证码',
        lock: true,
        id: 'goodCheck',
        width: '466px',
        content: strcontent,
        padding: '40px 40px',
        //okVal: '确定',
        //ok: function () {
        //}
    });
    hdShow();
}
//加载防盗刷验证
function hdShow() {
    var hidSlideValidateAppKey = $("#hidSlideValidateAppKey").val();
    var nc_token = [hidSlideValidateAppKey, (new Date()).getTime(), Math.random()].join(':');
    var NC_Opt =
        {
            renderTo: "#captcha",
            appkey: hidSlideValidateAppKey,
            scene: "nc_message",
            token: nc_token,
            customWidth: 187,
            trans: { "key1": "code0" },
            elementID: ["usernameID"],
            is_Opt: 0,
            language: "cn",
            isEnabled: true,
            timeout: 3000,
            times: 5,
            apimap: {
            },
            callback: function (data) {
                jQuery.ajax({
                    type: "post",
                    dataType: "json",
                    data: { sessionId: data.csessionid, sig: data.sig, scene: NC_Opt.scene, token: nc_token, appkey: NC_Opt.appkey, issendcode: true, contact: $("#MemberPhone").val(), checkcontacttype: 1 },
                    url: "/WebCommon/AuthenticateSig",
                    success: function (data) {
                        if (data.success) {
                            var count = 120;
                            $("#pcv").show();
                            si = setInterval(function () { count--; countDown1(count, "getPhoneCodeT"); }, 1000);
                            $.dialog.tips("已向‘" + $("#MemberPhone").val() + "'发送验证短信！");
                            dialog.close();//关闭当前弹窗
                        } else {
                            $("#getPhoneCodeT").removeAttr("disabled");
                            $.dialog.errorTips(data.msg);
                        }
                    },
                    error: function () {
                        $.dialog.errorTips("网络连接超时，请您稍后重试");
                    }
                });
            }
        }
    var nc = new noCaptcha(NC_Opt)

    nc.upLang('cn', {
        _startTEXT: "向右滑动验证",
        _yesTEXT: "验证通过",
        _error300: "哎呀，出错了，点击<a href=\"javascript:__nc.reset()\">刷新</a>再来一次",
        _errorNetwork: "网络不给力，请<a href=\"javascript:__nc.reset()\">点击刷新</a>",
    })
}