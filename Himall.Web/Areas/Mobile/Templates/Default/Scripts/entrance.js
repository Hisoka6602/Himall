$(function () {
    $(".login div.option").width($(".login").width() - 21);
    var openId = QueryString('openId');
    var serviceProvider = QueryString('serviceProvider');
    var realName = QueryString('realName');
    var nickName = QueryString('nickName');
    var headimgurl = QueryString('headimgurl');
    var appidtype = QueryString('AppidType');
    var unionid = QueryString('unionid');
    var sex = QueryString('sex');
    var city = QueryString('city');
    var province = QueryString('province');
    var country = QueryString('country');
    var returnUrl = QueryString('returnUrl');
    if (returnUrl != null && returnUrl != "") {//判断跳回域名是否有当前域名
        var reUrl = unescape(returnUrl).replace("https://", "").replace("http://", "").split('/')[0];
        var siteUrl = location.href.replace("https://", "").replace("http://", "").split('/')[0];
        if (reUrl != siteUrl) {
            returnUrl = location.href.indexOf("https://") != -1 ? "https://" : "http://" + siteUrl + "/m-wap";
        }
    }
    var method = '/' + areaName + '/Login/BindUser';
    var queryString = '?openId=' + openId + '&serviceProvider=' + serviceProvider + '&AppidType=' + appidtype + '&headimgurl=' + headimgurl + '&unionid=' + unionid + '&returnUrl=' + returnUrl;
    queryString += '&nickName=' + nickName + '&sex=' + sex + '&city=' + city + '&province=' + province + '&country=' + country;

    $('#btregister').attr('href', '/' + areaName + '/Register' + queryString);
    $('.forget-pwd-link').attr('href', '/' + areaName + '/Login/ForgotPassword');

    if (serviceProvider && openId) {
        $('#fastloginbox').show();
    }
    else {
        //判断是否为信任登录
        $('#titleType').html('登录');
        $('#bindBtn').html('登 录');
        method = '/' + areaName + '/Login';
        $('#fastloginbox').hide();
    }


    $('#eyebtn').click(function () {
        if ($(this).hasClass('active')) {
            $(this).removeClass('active');
            document.getElementById("password").setAttribute("type", "password");
        } else {
            $(this).addClass('active');
            document.getElementById("password").setAttribute('type', 'text');
        }
    });

    //账号密码登录
    $('#bindBtn').click(function () {
        var username = $('#username').val();
        var password = $('#password').val();
        var checkCode = $('#checkCodeBox').val();
        if (!username) {
            $.dialog.errorTips('请填写用户名');
            $('#username').focus();
        }
        else if (!password) {
            $.dialog.errorTips('请填写密码');
            $('#password').focus();
        }
        else {
            var loading = showLoading();
            $.post(method,
                {
                    username: username, password: password, serviceProvider: serviceProvider, openId: openId,
                    headimgurl: headimgurl, appidtype: appidtype, unionid: unionid, sex: sex, city: city,
                    province: province, country: country, nickName: nickName, checkCode: checkCode
                },
                function (result) {
                    loading.close();
                    if (result.success) {
                        $.dialog.succeedTips($('#titleType').html() + '成功!', function () {
                            if (!returnUrl) {
                                returnUrl = '/' + areaName;
                            }
                            if (decodeURIComponent(returnUrl).toLocaleLowerCase().indexOf("member/accountsecure") >= 0) {//如果是从该页面回跳则个人中心
                                returnUrl = '/' + areaName + '/member/center';
                            }
                            location.replace(decodeURIComponent(returnUrl));
                        });
                    }
                    else {
                        var isFirstShowCheckcode = false;
                        refreshCheckCode();
                        if (result.errorTimes > result.minTimesWithoutCheckCode) {//需要验证码
                            if ($("#checkCodeArea").is(":hidden")) {
                                isFirstShowCheckcode = true;
                            }
                            $('#checkCodeArea').show();
                        }
                        else {
                            $('#checkCodeArea').hide();
                        }
                        if (!isFirstShowCheckcode) {
                            $('#password').focus();
                        }
                        else
                            $('#checkCodeBox').focus();

                        $.dialog.alert($('#titleType').html() + '失败!' + result.msg);
                    }
                });
        }
    });

    //信任登录
    $('#skip').click(function () {
        var loading = showLoading();
        $.post('../Register/Skip', {
            openId: openId, serviceProvider: serviceProvider, realName: realName, nickName: nickName,
            headimgurl: headimgurl, appidtype: appidtype, unionid: unionid, sex: sex, city: city,
            province: province, country: country
        }, function (result) {
            loading.close();
            if (result.success) {
                var strMessage = "快捷登录成功!";
                if (result.data.num > 0) {
                    FillCoupon(result.data.coupons, returnUrl);
                } else if (getQueryString("type") == "gift") {
                    strMessage = "很抱歉！优惠券已被领完，请期待下次活动！";
                }

                $.dialog.succeedTips(strMessage, function () {
                    location.replace(decodeURIComponent(returnUrl));
                });
            }
            else {
                if (result.code == 0) {
                    //开启强制绑定，且会员不存在，会员暂时不注册，提示绑定已有账号
                    $("#bPhoneNickName").html(decodeURI(nickName + ""));
                    $('#bNickName').html(decodeURI(nickName + ""));
                    $("#bPhoneUserPicture").attr("src", headimgurl);

                    $("#divlogin").hide();
                    $("#divForceBindPhone").show();
                } else if (result.code == 99) {
                    //开启了强制绑定，它会员之前已存在，跳到去绑定手机号
                    location.replace(decodeURIComponent('/' + areaName + '/Member/BindPhone'));
                } else {
                    $.dialog.alert('一键注册失败' + result.msg);
                }
            }
        });
    });

    //没账号去注册
    $("#btnGoPhoneRegister").click(function (e) {
        location.href = $("#btregister").attr("href");//跳到注册页直接去注册
    });

    //已有账号，绑定现有账号
    $("#btnGoBindUser").click(function (e) {
        $("#divForceBindPhone").hide();

        $("#divlogin").show();
        $("#bNickName").show();
        $("#bPhoneTishi").show();
        $('#bindBtn').html('立即绑定');
        $('#fastloginbox').hide();//信任登录去掉
    });
})
function refreshCheckCode() {
    var path = $('#checkCodeImg').attr('src').split('?')[0];
    path += '?time=' + new Date().getTime();
    $('#checkCodeImg').attr('src', path);
    $('#checkCodeBox').val('');
}
//获取URL中值
function getQueryString(name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)", "i");
    var r = window.location.search.substr(1).match(reg);
    if (r != null) return unescape(r[2]); return null;
}