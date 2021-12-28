// JavaScript source code
$(function () {
    $("#Title").focus();

    $("#SelectProduct").click(function () {
        $.productSelector.show(null, function (selectedProducts) {
            console.log(selectedProducts);
            $("#ProductId").val(selectedProducts[0].id);
            $("#ProductName").val(selectedProducts[0].name);
            $("#ProductPrice").val(selectedProducts[0].price);
        }, 'selleradmin', false);
    });
});
    var a = v({
    form: 'form1',
    ajaxSubmit: true,
    beforeSubmit: function () {
        loadingobj = showLoading();
    },
    afterSubmit: function (data) {// ���ύ�ɹ��ص�
        loadingobj.close();
        var d = data;
        if (d.success) {
            $.dialog.succeedTips("����ɹ���", function () {
                window.location.href = "../Management";
            });
        } else {
            $.dialog.errorTips(d.msg, '', 1);
        }
    }
});
a.add(
     {
         target: 'Title',
         empty: true,
         ruleType: 'required',// v.js������֤
         error: '������д�����'
     },
    {
        target: 'ProductName',
        empty: true,
        ruleType: 'required',// v.js������֤
        error: '����ѡ����Ʒ'
    }, {
        target: 'Price',
        empty: true,
        ruleType: 'money',// v.js������֤
        fnRule: function () {
            var a = $('#ProductPrice').val(),
                  b = $('#Price').val();
            try {
                a = parseFloat(a);
            } catch (ex) {
                a = 0;
            }
            try {
                b = parseFloat(b);
            } catch (ex) {
                b = 0;
            }
            if (b >= a || b < 0 || a < 0) {
                return false;
            }
        },
        error: 'ֻ��Ϊ���֣�  �Ҵ���0'
    }, {
        target: 'StartTime',
        ruleType: 'required',// v.js������֤
        error: '��ѡ����ʼʱ��'
    }, {
        target: 'EndTime',
        ruleType: 'required',// v.js������֤
        error: '��ѡ������ʱ��'
    }, {
        target: 'MaxSaleCount',
        empty: true,
        ruleType: 'uint&&(value>0)',// v.js������֤
        error: '�������ֻ��Ϊ���֣�  �Ҵ���0'
    });