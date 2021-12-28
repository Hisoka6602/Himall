﻿

$(function () {
    if (isWeiXin() && $("#hasdistributionshare").length == 0) {
        $.ajax({
            type: "post",
            url: '/' + areaName + '/Home/GetShare/',
            data: { url: location.href },
            async: false,
            success: function (result) {
                var weiXinShareTitle = 'Himall商城';
                var data = result.data;
                if (data.shareTitle && data.shareTitle != '') {
                    weiXinShareTitle = data.shareTitle;
                }
                if (data.shareIcon && data.shareIcon != '') {
                    data.shareIcon = data.shareIcon + "?r=" + (new Date().getMilliseconds());
                }
                var weiXinShareDesc = weiXinShareTitle + ',精选好货,集您所需';
                var winxinShareArgs = {
                    share: {
                        title: weiXinShareTitle,
                        desc: weiXinShareDesc,
                        link: location.href,
                        imgUrl: data.shareIcon
                    }
                };
                if (winxinShareArgs) {
                    winxinShareArgs = $.extend({
                        appId: data.appId,
                        timestamp: data.timestamp,
                        noncestr: data.nonceStr,
                        signature: data.signature,
                        success: null,
                        error: null,
                        share: {
                            title: document.title,
                            desc: null,
                            link: location.href,
                            imgUrl: null,
                            success: null,
                            cancel: null,
                            fail: null,
                            complete: null,
                            trigger: null
                        }
                    }, winxinShareArgs);

                    if (winxinShareArgs.share.imgUrl == null || winxinShareArgs.share.imgUrl == '')
                        winxinShareArgs.share.imgUrl = location.origin + '/Areas/Mobile/Templates/Default/Images/default.png';
                    //初始化微信分享
                    initWinxin(winxinShareArgs);
                }
            }
        });
    }

    function isWeiXin() {
        var ua = window.navigator.userAgent.toLowerCase();
        if (ua.match(/MicroMessenger/i) == 'micromessenger') {
            return true;
        } else {
            return false;
        }
    }
});