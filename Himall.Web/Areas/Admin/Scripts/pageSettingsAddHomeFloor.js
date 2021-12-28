// JavaScript source code

$(function () {

    checkEditMode();
    bindSaveBtnClickEvent();

});

function checkEditMode() {
    var id = $('input[name="id"]').val();
    id = parseInt(id);
    if (id) {
        //�༭ģʽ
        $('#mode').html('�༭');
        $('#saveBtn').html('����');
        $('#floor-edit-next').show();
        bindSaveAndNextClickEvent();
    }

}

function bindSaveAndNextClickEvent() {
    var id = $('input[name="id"]').val();
    $('#floor-edit-next').click(function () {
        save(function () {
            location.href = 'AddHomeFloorDetail?id=' + id;
        });
    });
}

function bindSaveBtnClickEvent() {
    var id = $('input[name="id"]').val();
    id = parseInt(id);
    $('#saveBtn').click(function () {
        var func = null;
        if (id) {
            //�༭ģʽ
            func = function () {
                $.dialog.tips('����ɹ���', function () {
                    location.href = 'HomeFloor';
                })
            }
        }
        else {
            func = function (id) { location.href = 'AddHomeFloorDetail?id=' + id };
        }
        save(func);
    });
}


function save(callBack) {
    var floorName = $.trim($('#floorName').val());
    if (!floorName) {
        $.dialog.tips('����д¥������');
        $('#floorName').focus();
        return;
    }

    var categoryIds = [];
    $.each($('input[name="category"]:checked'), function () {
        categoryIds.push($(this).val());
    });
    if (categoryIds.length == 0) {
        $.dialog.tips('������ѡ��һ������');
        return;
    }

    var id = $('input[name="id"]').val();
    id = parseInt(id);
    var loading = showLoading();
    $.post('SaveHomeFloorBasicInfo',
        { id: id, name: floorName, categoryIds: categoryIds.toString() },
        function (result) {
            loading.close();
            if (result.success) {
                callBack && callBack(result.id);
            }
            else {
                $.dialog.errorTips('����ʧ��!' + result.msg);
            }
        });

}
