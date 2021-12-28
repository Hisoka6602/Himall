﻿var isShowComment = false;
//商品评价
function GetProductComment() {
    if (!isShowComment) {
        var idList = [],// id存放列表
            uuid = 1,// 用来统计请求次数
            pid = $('#gid').val(),// 商品id
            getData = function (pageno, commenttype) {
                $.ajax({
                    type: 'get',
                    url: '/Product/GetCommentByProduct?pId=' + pid + '&pageNo=' + pageno + '&pageSize=' + 10 + '&commentType=' + commenttype,
                    dataType: 'html',
                    cache: true,// 开启ajax缓存
                    success: function (data) {
                        if (data) {
                            var data = (new Function('return ' + data))();
                            render(data, commenttype);
                        }
                    },
                    error: function (e) {
                        //
                    }
                });
            },
            template = [
                '<div id="comment-0" class="mc">',
                '<div class="item clearfix">',
                '<div class="user">',
                '<div class="u-icon">',
                '<a target="_blank" title="查看TA的全部评价">',
                '<div class="u-name">',
                '<span class="u-level">',
                '<span class="u-address">',
                '<div class="i-item">',
                '<div class="o-topic">',
                '<span class="date-comment">',
                '<div class="comment-content clearfix">'
            ],// 模板数据
            render = function (data, index) {
                var dom = $('#comments-list'),
                    str = '',
                    i,
                    e,
                    con = data.comments;
                for (i = 0; e = con[i++];) {
                    str += template[1]
                            + template[2]
                        + template[3]
                        + '<img src="' + ((e.Picture == null || e.Picture == "") ? "/Areas/Web/images/avatar.png" : e.Picture) + '">'
                                    + e.UserName
                                    + '</div></div>'
                                + template[8]
                                	+'<span class="star sa' + e.ReviewMark + '"></span>'
                                    + template[11]
                        + '<dl class="clearfix"><dt class="reviewdate">发表于' + template[10] + e.ReviewDate.substr(0, 10) + '</span></dt><dd>' + e.ReviewContent + '<div class="after-service-img clearfix">';

                    for (var j = 0; j < e.Images.length; j++) {
                        str += '<img height="40" src="' + e.Images[j].CommentImage + '"/><span class="glyphicon glyphicon-zoom-in"></span>'
                    }
                    str += '</div>';
                    str += '<div class="preview-img"><img src="" /></div>'
                    if (e.ReplyContent != "" && e.ReplyContent != "暂无回复" && e.ReplyContent != null) {
                        str += '<div class="shop-reply">商家回复：' + e.ReplyContent + '</div>';//e.ReplyDate
                    }

                    str += '</dd><div class="comment-sku">';
                    if (e.Color != '')
                        str += '<p>' + e.ColorAlias + '：' + e.Color + '</p>';
                    if (e.Size != '')
                        str += '<p>' + e.SizeAlias + '：' + e.Size + '</p>';
                    if (e.Version != '')
                        str += '<p>' + e.VersionAlias + '：' + e.Version + '</p>';
                    str += '</div></dl>';

                    if (e.AppendDate != null && e.AppendDate != "") {
                        str += '<dl class="clearfix"><dt>收货' + GetDateDiff(e.FinishDate, e.AppendDate) + '后追加评价：</dt><dd>' + e.AppendContent + '<div class="after-service-img clearfix">';
                        for (var k = 0; k < e.AppendImages.length; k++) {
                            str += '<img  height="40" src="' + e.AppendImages[k].CommentImage + '"/>'
                        }

                        str += '</div>';
                        str += '<div class="preview-img"><img src="" /></div>'
                        if (e.ReplyDate != null && e.ReplyAppendContent != null && e.ReplyAppendContent != "" && e.ReplyAppendContent != "暂无回复") {
                            str += '<div class="shop-reply">商家回复：' + e.ReplyAppendContent + '</div>';//e.ReplyDate
                        }

                        str += '</dd></dl>';
                    }

                    str += '</div></div></div>';
                }

                var commentsItem = $('#comment-' + index, dom);
                if (commentsItem.length > 0) {
                    var pagin = $('.pagin', commentsItem);
                    commentsItem.children(':not(.pagin)').remove();
                    pagin.before(str);
                }
                else {
                    var pagin = commonJS.generatePaging(1, data.totalPage, function (pi) {
                        getData(pi, index);
                    });
                    commentsItem = $('<div id="comment-{0}" class="mc">{1}</div>'.format(index, str));
                    commentsItem.append(pagin.div);
                    dom.append(commentsItem);
                }

                // 只会执行一次 生成评论条数
                if (uuid++ === 1) {
                    var total = data.goodComment + data.badComment + data.comment,
                        g = data.goodComment,
                        c = data.comment,
                        b = data.badComment,
                        hasAppend = data.hasAppend,
                        hasImages = data.hasImages,
                        e = total == 0 ? 100 : Math.round((g / total).toFixed(2) * 100),// 取整 误差为1
                        f = total == 0 ? (e == 100 ? 0 : 100) : Math.round((c / total).toFixed(2) * 100),
                        j = (100 - e - f) < 0 ? 0 : Math.round(100 - e - f),//防止少于0
                        arr = [total, g, c, b, hasImages, hasAppend],
                        arr1 = [e, f, j];
                    $('#id_comment_btn').find('li').each(function (i, e) {
                        $(e).find('a').append('<em>(' + arr[i] + ')</em>');
                    });
                    $('#i-comment strong').empty().prepend(e + '%');
                    $('#praiseRate').empty().prepend(e + '%');
                    $('#i-comment .percent').find('span').each(function (i, e) {
                        $(e).html('(' + arr1[i] + '%)');
                    });
                    $('#i-comment .percent').find('div').each(function (i, e) {
                        $(e).css({ width: arr1[i] + 'px' });
                    });
                }
            };

        getData(1, 0);//第一次数据渲染 参数：1:第1页 0:所有评论

        $('#id_comment_btn').find('li').each(function (i, e) {
            $(e).bind('click', function () {
                var dom = $(this);
                dom.siblings('.active').removeClass('active');
                dom.addClass('active');
                if ($('#comment-' + i).length > 0) {
                    $('#comments-list .mc').each(function () {
                        $(this).hide();
                    });
                    $('#comment-' + i).show();
                } else {
                    $('#comments-list .mc').each(function () {
                        $(this).hide();
                    });
                    getData(1, i);// 按需载入评论数据 数据只生成一次 i代表类型
                }
                return false;// 阻止冒泡
            });
        });
        isShowComment = true
    }
}

function GetDateDiff(startDate, endDate) {
    var daysRound = GetDateDiffResult(startDate, endDate, "day");
    var hoursRound = GetDateDiffResult(startDate, endDate, "hour");
    var minutesRound = GetDateDiffResult(startDate, endDate, "minute");

    if (hoursRound > 24) {
        return parseInt(daysRound) + "天";
    } else if (minutesRound > 60) {
        return parseInt(hoursRound) + "小时";
    } else {
        if (parseInt(minutesRound) <= 0) {
            return "1分钟";
        }
        return parseInt(minutesRound) + "分钟";
    }
}


function GetDateDiffResult(startTime, endTime, diffType) {
    startTime = startTime.replace(/\-/g, "/");
    endTime = endTime.replace(/\-/g, "/");
    diffType = diffType.toLowerCase();
    var sTime = new Date(startTime); //开始时间
    var eTime = new Date(endTime); //结束时间
    var timeType = 1;
    switch (diffType) {
        case "second":
            timeType = 1000;
            break;
        case "minute":
            timeType = 1000 * 60;
            break;
        case "hour":
            timeType = 1000 * 3600;
            break;
        case "day":
            timeType = 1000 * 3600 * 24;
            break;
        default:
            break;
    }
    return parseInt((eTime.getTime() - sTime.getTime()) / parseInt(timeType));
}