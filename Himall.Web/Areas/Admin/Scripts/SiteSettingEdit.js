﻿// JavaScript source code
var editor;

$(function () {
    $("#SiteName").focus();
    editor = UE.getEditor('PageFoot');
});
$(function () {
    $('#Save').click(function () {
        var loading = showLoading();
        $.post('./Edit', $('form').serialize(), function (result) {
            loading.close();
            if (result.success) {
                $.dialog.tips('保存成功');
            }
            else
                $.dialog.errorTips('保存失败！' + result.msg);
        });
    });
    $('#btnClearData').click(function () {
        $.dialog.confirm('执行此操作将会一键清除演示店铺，演示店铺下的商品、门店，以及官方自营店铺下的演示商品；操作执行成功后，此按钮将不再显示，确认要执行吗？', function () {
            var loading = showLoading();
            $.post('/Installer/ClearDemoData', null, function (result) {
                loading.close();
                if (result.success)
                {
                    $.dialog.tips(result.msg);
                }
                else {
                    $.dialog.tips(result.msg);
                }
            });
            
        });
    });
    function ShowRegEmailBox() {
        var regtype = $("input[name='RegisterType']:checked").val();
        if (regtype == 1) {
            $("#regemailbox").hide();
        } else {
            $("#regemailbox").show();
        }
    }

    $(function () {

        $("input[name='RegisterType']").click(function () {
            ShowRegEmailBox();
        });
        ShowRegEmailBox();

        $('#Logo').himallUpload({
            title: '网站Logo： <b>*</b>',
            imageDescript: '最佳尺寸：200*60  显示在商城头部、会员登录处等位置',
            displayImgSrc: $("#Logo1").val(),
            imgFieldName: "Logo",
            dataWidth: 8

        });

        $('#wxlogobox').himallUpload({
            title: '微信Logo： <b>*</b>',
            imageDescript: '最佳尺寸：100*100的图片,微信卡券使用',
            displayImgSrc: $("#WXLogo1").val(),
            imgFieldName: "WXLogo",
            dataWidth: 8
        });

        $('#MemberLogo').himallUpload({
            title: '卖家中心Logo： <b>*</b>',
            imageDescript: '最佳尺寸：180*40  显示在卖家中心导航处',
            displayImgSrc: $("#MemberLogo1").val(),
            imgFieldName: "MemberLogo",
            dataWidth: 8
        });

        $('#QRCode').himallUpload({
            title: '微信二维码： ',
            imageDescript: '最佳尺寸：90*90  显示在商城底部',
            displayImgSrc: $("#QRCode1").val(),
            imgFieldName: "QRCode",
            dataWidth: 8
        });
        $('#PCLoginPic').himallUpload({
            title: 'PC登录区域： ',
            imageDescript: '最佳尺寸：460*400  显示在PC登录页左侧',
            displayImgSrc: $("#PCLoginPic1").val(),
            imgFieldName: "PCLoginPic",
            dataWidth: 8
        });
        $('#PCBottomPic').himallUpload({
            title: '底部服务图片： ',
            imageDescript: '最佳尺寸：1190*100  显示在PC端底部',
            displayImgSrc: $("#PCBottomPic1").val(),
            imgFieldName: "PCBottomPic",
            dataWidth: 8
        });
    })
    $("#btnFile").bind("change", function () {
        if ($("#btnFile").val() != '') {
            var dom_btnFile = $('#btnFile');

            //准备表当
            var myform = document.createElement("form");
            myform.action = "./UploadApkFile";
            myform.method = "post";
            myform.enctype = "multipart/form-data";
            myform.style.display = "none";
            //将表单加当document上，
            document.body.appendChild(myform);  //重点
            var form = $(myform);

            var fu = dom_btnFile.clone(true).val(""); //先备份自身,用于提交成功后，再次附加到span中。
            var fu1 = dom_btnFile.appendTo(form); //然后将自身加到form中。此时form中已经有了file元素。

            var loading = showLoading("app上传中..");
            //开始模拟提交表当。
            form.ajaxSubmit({
                success: function (data) {
                    loading.close();
                    if (data == "NoFile" || data == "Error" || data == "上传的文件格式不正确") {
                        $.dialog.errorTips(data);
                    }
                    else {
                        //文件上传成功，返回图片的路径。将路经赋给隐藏域
                        $('#AndriodDownLoad').val(data);
                        $.dialog.tips('app上传成功');
                    }
                    //$(".divFile").append(fu1);
                    $(fu1).insertAfter($(".divFile"));
                    form.remove();
                }, error: function (er) {
                    loading.close();
                    $.dialog.errorTips(JSON.stringify);
                }
            });

        }
        else {
            $('#inputFile').val('请选择文件');
        }
    });

    $("#btnFileShop").bind("change", function () {
        if ($("#btnFileShop").val() != '') {
            var dom_btnFile = $('#btnFileShop');
            
            var myform = document.createElement("form");
            myform.action = "./UploadApkFile";
            myform.method = "post";
            myform.enctype = "multipart/form-data";
            myform.style.display = "none";
            //将表单加当document上，
            document.body.appendChild(myform);  //重点
            var form = $(myform);

            var fu = dom_btnFile.clone(true).val(""); //先备份自身,用于提交成功后，再次附加到span中。
            var fu1 = dom_btnFile.appendTo(form); //然后将自身加到form中。此时form中已经有了file元素。

            var loading = showLoading("商家app上传中..");
            //开始模拟提交表当。
            form.ajaxSubmit({
                success: function (data) {
                    loading.close();
                    if (data == "NoFile" || data == "Error" || data == "上传的文件格式不正确") {
                        $.dialog.errorTips(data);
                    }
                    else {
                        //文件上传成功，返回图片的路径。将路经赋给隐藏域
                        $('#ShopAndriodDownLoad').val(data);
                        $.dialog.tips('app上传成功');
                    }
                    //$(".divFile").append(fu1);
                    $(fu1).insertAfter($(".divFileShop"));
                    form.remove();
                }, error: function (er) {
                    loading.close();
                    $.dialog.errorTips(JSON.stringify);
                }
            });

        }
        else {
            $('#inputFileShop').val('请选择文件');
        }
    });
})
function openBrowse(str) {
    var ie = navigator.appName == "Microsoft Internet Explorer" ? true : false;
    if (str == "shop") {
        if (ie) {
            document.getElementById("btnFileShop").click();
        } else {
            var a = document.createEvent("MouseEvents");//FF的处理 
            a.initEvent("click", true, true);
            document.getElementById("btnFileShop").dispatchEvent(a);
        }
    } else {
        if (ie) {
            document.getElementById("btnFile").click();
        } else {
            var a = document.createEvent("MouseEvents");//FF的处理 
            a.initEvent("click", true, true);
            document.getElementById("btnFile").dispatchEvent(a);
        }
    }
}