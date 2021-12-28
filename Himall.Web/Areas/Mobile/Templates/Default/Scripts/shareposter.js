//---------------------下面是关于海报操作开始-----
function Vanmodal() {
    $(".van-modal").addClass("hide");//海报图片隐藏
    $("#posterBox").addClass("hide");//海报图片隐藏
}

$(function () {
    $(".van-modal").on("click", function (e) {
        Vanmodal();
    });

    $('body').on("click", function (e) {
        //除点击海报外隐藏海报图片
        //alert(e.target.id);
        if (e.target.id != 'posterImg') {
            if (!$("#posterBox").hasClass("hide")) {
                Vanmodal();
            }
        }
    })

    if ($("#qrcodeimgBox").length > 0) {
        $("#qrcodeimgBox").html();
        $("#qrcodeimgBox").qrcode({//生成二维码
            render: "canvas",
            text: location.href,
            width: "300",
            height: "300",
            background: "#ffffff",
            foreground: "#000000"
        });
    }
})


function Posterbg() {
    var loading = showLoading();
    $(".poster-box").removeClass("hide");//显示才有宽带

    /////判断海报图不否不同，如不同转base64图片--------////
    var posterimg = $(".poster-box .poster-img-content img");
    if (posterimg.length > 0) {
        var rehref = location.href.replace("https://", "").replace("http://", "");
        rehref = rehref.substring(0, rehref.indexOf('/'));

        var imgsrc = posterimg.attr('src');
        //if (imgsrc != null && imgsrc != "" && imgsrc.indexOf("data:image")==-1 && imgsrc.indexOf(rehref) == -1) {
        //    //说明不是同一个域名图片需转为base64图片
        //    $.ajax({
        //        type: 'GET',
        //        url: '/Common/PublicCommon/GetBase64Pic',
        //        data: { picUrl: imgsrc },
        //        dataType: 'json',
        //        //cache: true,// 开启ajax缓存
        //        async: false,//同步
        //        success: function (result) {
        //            if (result.success) {
        //                posterimg.attr("src", result.msg);
        //            }
        //        }
        //    });
        //}
    }
    ////---------////

    var str = $('.poster-box');
    var w = $(".poster-box").innerWidth();
    var h = $(".poster-box").innerHeight();
    //alert(w + "--" + h);
    //要将 canvas 的宽高设置成容器宽高的 2 倍
    var canvas = document.createElement("canvas");
    canvas.width = w * 2;
    canvas.height = h * 2;
    canvas.style.width = w + "px";
    canvas.style.height = h + "px";
    var context = canvas.getContext("2d");
    context.scale(2, 2);
    html2canvas([str.get(0)], {
        canvas: canvas,
        useCORS: true,
        onrendered: function (canvas) {
            var image = canvas.toDataURL("image/png");
            var strdownname = isWeiXin() ? "长按图片保存海报" : "保存图片";
            //alert(JSON.stringify(image));
            var pHtml = "<div><img class='posterImg' id='posterImg' src=" + image + " /></div>";
            pHtml += "<div style=\"text-align:center; \" id='posterbtn' onclick='Vanmodal()'><a class=\"download\" download=\"海报\" href=\"" + image + "\">" + strdownname + "</a></div>";
            $('#posterBox').html(pHtml);

            $(".poster-box").addClass("hide");//模板样式隐藏
            $(".van-modal").removeClass("hide");//遮罩显示
            $("#posterBox").removeClass("hide");//海报图片显示
        }
    });
    loading.close();
}

function isWeiXin() {
    var ua = window.navigator.userAgent.toLowerCase();    
    if (ua.match(/MicroMessenger/i) == 'micromessenger') {//通过正则表达式匹配ua中是否含有MicroMessenger字符串
        return true;
    } else {
        return false;
    }
}
//----------------------下面是关于海报操作结束----