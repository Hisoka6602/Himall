$(function () {
    $('#btn').click(function () {
        var items = $('input[formItem]');
        var data = [];
        $.each(items, function (i, item) {
            data.push({ key: $(item).attr('name'), value: $(item).val() });
        });
        var dataString = JSON.stringify(data);
        var loading = showLoading();
        var appId, appSecret;
        
            appId = $('input[name="WeixinAppletId"]').val();
            appSecret = $('input[name="WeixinAppletSecret"]').val();
        
         
        $.post('save', { values: dataString, weixinAppletId: appId, WeixinAppletSecret: appSecret }, function (result) {
            if (result.success)
                $.dialog.succeedTips('保存成功！', function () { });
            else
                $.dialog.errorTips('保存失败！' + result.msg);
            loading.close();
        });
    });

    $(".j_viewQrcode").click(function () {
        window = $.dialog({
            title: '小程序二维码',
            id: 'j_Qrcode',
            width: '300px',
            padding: '0 40px',
            content: document.getElementById("j_Qrcode"),
            lock: true,
            init: function () {
                var loading = showLoading();
                $.post('/admin/WXSmallProgram/GetWXSmallCode', {}, function (result) {
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
    

    $(".btnTemplateId").click(function () {
        var loading = showLoading();
        $.post('GetAppletSubscribeTmplate', function (result) {
            loading.close();
            if (result.success) {
                $.dialog.succeedTips('获取微信订阅消息模板ID成功！', function () { window.location.reload(); });
            } else {
                $.dialog.errorTips('获取微信订阅消息模板ID失败！' + result.msg);
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
});