// JavaScript source code
$(function () {
    $("#Name").focus();
    $("#upload-img").himallUpload({
        displayImgSrc: $("#Micon").val()
    });

    if ($("#Mero").val() != "") {
        $.dialog.errorTips($("#Mero").val());
    }
    $('#ckbIsSupportVirtualProduct').change(function () {
        var supportVirtualProduct = this.checked == true;
        $("#SupportVirtualProduct").val(supportVirtualProduct);
    });
});