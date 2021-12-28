var defaultDate = new Date();
defaultDate.setDate(defaultDate.getDate() - 1);

$(function () {
    document.onkeydown = function () {
        if (window.event && window.event.keyCode == 13) {
            window.event.returnValue = false;
        }
    }

    $('#searchButton').click(function (e) {
        GetData();
    });
    GetData(1);
});

function GetData(pageNumber) {
    $(function () {
        var name = $('#txtAnchorName').val();
        $("#list").hiMallDatagrid({
            url: './GetAnchors',
            nowrap: true,
            rownumbers: true,
            NoDataMsg: '没有找到符合条件的数据',
            border: false,
            fit: true,
            fitColumns: true,
            pagination: true,
            idField: "id",
            pageSize: 15,
            pagePosition: 'bottom',
            pageNumber: pageNumber,
            queryParams: { AnchorName: name },
            columns:
                [[
                    {
                        field: "AnchorName", title: "主播名称", align: "center"
                    },
                    {
                        field: "WeChat", title: "微信号", align: "center"
                    },
                    {
                        field: "CellPhone", title: "手机号", align: "center"
                    },
                    {
                        field: "ShowName", title: "关联会员帐号", align: "center"
                    }
                    ,
                    {
                        field: "s", title: "操作", width: 200, align: "center",
                        formatter: function (value, row, index) {
                            var html = "";
                            html += '<a class="good-check" href="AddAnchor?AnchorId=' + row.Id + '">编辑</a>&nbsp;&nbsp;';
                            html += '<a class="good-del"  href="javascript:ToDelete(' + row.Id + ',\'' + row.ShowName + '\')">删除</a></span>';
                            return html;
                        }
                    }
                ]]
        });
    });
}


function ToDelete(anchorId, name) {

    $.dialog.confirm('您确定要删除名称为 ' + (name + ' 的主播') + '吗？', function () {
        var loading = showLoading();
        $.post('DeleteAnchor', { anchorId: anchorId }, function (result) {
            loading.close();
            if (result.success) {
                $.dialog.tips('删除主播成功');
                var pageNo = $("#list").hiMallDatagrid('options').pageNumber;
                GetData(pageNo);
            }
            else
                $.dialog.alert('删除主播失败!' + result.msg);
        });
    });
}




