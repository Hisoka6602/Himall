$(function () {
    query();
    $("#searchBtn").click(function () { query(); });
    AutoComplete();
})

function query() {
    $("#list").hiMallDatagrid({
        url: './list',
        nowrap: false,
        rownumbers: true,
        NoDataMsg: '没有找到任何的会员积分信息',
        border: false,
        fit: true,
        fitColumns: true,
        pagination: true,
        idField: "memberId",
        pageSize: 10,
        pageNumber: 1,
        queryParams: { userName: $("#autoTextBox").val(), startDate: $("#inputStartDate").val(), endDate: $("#inputEndDate").val() },
        toolbar: /*"#goods-datagrid-toolbar",*/'',
        operationButtons: "#batchOperate",
        columns:
            [[
            { checkbox: true, width: 39 },
            { field: "memberId", hidden: true },
            { field: "userName", title: '会员名' },
            { field: "availableIntegrals", title: '可用积分', sort: true },
            { field: "memberGrade", title: '会员等级' },
            { field: "historyIntegrals", title: '历史积分', sort: true },
            {
                field: "createDate", title: '会员注册时间', sort: true,
                formatter: function (value, row, index) {
                    return value.substring(0,10);
                }
            },
            { field: "operation", operation: true, title: "操作",
                formatter: function (value, row, index) {
                    return "<span class=\"btn-a\"><a href='./Detail/" + row.memberId + "'>查看</a></span>";
                }
            }
        ]]
    });
}

function AutoComplete() {
    //autocomplete
    $('#autoTextBox').autocomplete({
        source: function (query, process) {
            var matchCount = this.options.items;//返回结果集最大数量
            $.post("./getMembers", { "keyWords": $('#autoTextBox').val() }, function (respData) {
                return process(respData);
            });
        },
        formatItem: function (item) {
            return item.value;
        },
        setValue: function (item) {
            return { 'data-value': item.value, 'real-value': item.key };
        }
    });
}


function batchSetMembersScore() {
    var selecteds = $("#list").hiMallDatagrid('getSelections');
    var ids = [];
    $.each(selecteds, function () {
        ids.push(this.memberId);
    });
    if (ids.length == 0) {
        $.dialog.tips('请选择会员！');
        return;
    }
    $('input[name=check_Label]').each(function (i, checkbox) {
        $(checkbox).get(0).checked = false;
    });

    $.dialog({
        title: '批量修改积分',
        lock: true,
        id: 'SetLabel',
        width: '630px',
        height:'200px',
        content: document.getElementById("divSetScore"),
        padding: '0 40px',
        okVal: '确定',
        ok: function () {
            var integral = $("#Integral").val();
            if (isNaN(integral)) {
                $.dialog.tips('请输入正确的积分格式！');
                return false;
            }
            if (parseInt(integral) <= 0 || parseInt(integral) > 10000 || parseInt(integral)%1!=0) {
                $.dialog.tips('积分数为正整数且小于一万！');
                return false;
            }
            var remark = $("#Remark").val();
            var oper = $("#Operation").val();
            var loading = showLoading();
            $.post('SetMembersScore', { Operation: oper,userid: ids.join(','), Integral: integral, reMark: remark }, function (result) {
                if (result.success) {
                    query();
                    $.dialog.tips('设置成功！');
                } else {
                    $.dialog.tips(result.msg);
                }
                loading.close();
            });
        }
    });
}