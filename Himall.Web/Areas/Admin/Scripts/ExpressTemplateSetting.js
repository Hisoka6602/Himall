// JavaScript source code
$('#Save').click(function () {
    var loading = showLoading();
    $.post('./SaveExpressSetting', { KuaidiApp_key: $("#KuaidiApp_key").val(), KuaidiAppSecret: $("#KuaidiAppSecret").val() }, function (result) {
        loading.close();
        if (result.success) {
            $.dialog.tips('保存成功');
        }
        else
            $.dialog.errorTips('保存失败！' + result.msg);
    });
});