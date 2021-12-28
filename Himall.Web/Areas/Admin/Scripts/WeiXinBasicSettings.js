// JavaScript source code
$(function () {
    var window;
    $('button').click(function () {
        var loading = showLoading();
        var appId = $('input[name="WeixinAppId"]').val();
        var appSecret = $('input[name="WeixinAppSecret"]').val();
        $.post('./SaveWeiXinSettings', { weixinAppId: appId, WeixinAppSecret: appSecret }, function (result) {
            loading.close();
            if (result.success) {
                $.dialog.tips('保存成功');
                window.location.reload();
            }
            else
                $.dialog.alert('保存失败！' + result.msg);
        });
    });
    $(".j_viewQrcode").click(function () {
        window = $.dialog({
            title: '公众号二维码',
            id: 'j_Qrcode',
            width: '300px',
            padding: '0 40px',
            content: document.getElementById("j_Qrcode"),
            lock: true,
            init: function () {
                var loading = showLoading();
                $.post('/admin/weixin/GetQrcode', {}, function (result) {
                    loading.close();
                    if (result.success) {
                        $(".j_qrcodeImg").attr("src", result.data);
                    }
                    else
                        $.dialog.errorTips(result.msg);
                });
            }
        });
    });
    $(document).mouseup(function (e) {
        var pop = $('#j_Qrcode');
        if (!pop.is(e.target) && pop.has(e.target).length === 0) {
            if (window != null) {
                window.close();
            }
        }
    });
})