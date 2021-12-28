﻿// JavaScript source code
$(function () {
    eidtor = UE.getEditor('contentDesc1');
    eidtor.addListener('contentChange', function () {
    });
    //同步内容
    eidtor.addListener("blur", function () {
        eidtor.sync();
        var content = eidtor.getContent();
    });

    $(".tag-choice").click(function () {
        $(".dialog-tag").css("display", "block");
        $(".coverage").show();
        $('input[name=check_Label]').each(function (i, checkbox) {
            $(checkbox).get(0).checked = false;
        });
        $('.tag-area span').each(function (idx, el) {
            $('#check_' + $(el).attr('LabelId')).get(0).checked = true;
        });
    });
    $(".tag-submit").click(function () {
        var labels = [];
        $('input[name=check_Label]').each(function (i, checkbox) {
            if ($(checkbox).get(0).checked) {
                labels.push('<span labelid="' + $(checkbox).attr('datavalue') + '">' + $(checkbox).val() + '</span>');
            }
        });
        $('.tag-area').html(labels.join(''));
        $(".dialog-tag").hide();
        $(".coverage").hide();
    });
    $(".dialog-tag .glyphicon-remove").click(function () {
        $(".dialog-tag").hide();
        $(".coverage").hide();
    });
    $('.tag-back').click(function () {
        $(".dialog-tag").hide();
        $(".coverage").hide();
    });
    $('#SendMsg').click(SendMsg);
});
function SendMsg() {
    var label = '';
    var desc = [];
    if (!$('#allLabel').get(0).checked) {
        $('.tag-area span').each(function (idx, el) {
            label += ',' + $(el).attr('labelid');
            desc.push($(el).text());
        });
        if (label.length == 0) {
            $.dialog.alert('请选择要发送的标签');
            return;
        }
        label = label.substr(1, label.length - 1);
    }
    else {
        desc.push('标签:全部');
    }
    var title = $('#title').val();
    if ($('#title').val() == '') {
        $.dialog.alert('标题不能为空！');
        return;
    }
    var content = eidtor.getContent();
    if (content == '') {
        $.dialog.alert('发送内容不能为空！');
        return;
    }
    loading = showLoading('正在发送');
    $.post('SendEmailMsg', { labelids: label, title: title, content: content, labelinfos: desc.join(' ') }, function (result) {
        loading.close();
        if (result.success) {
            $.dialog.alert('发送成功！');
        }
        else {
            $.dialog.alert(result.msg);
        }
    });
}