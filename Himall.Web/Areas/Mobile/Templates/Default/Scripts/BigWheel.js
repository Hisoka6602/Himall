
var indexs = 0;
var bounsPrice = 0;
// JavaScript source code
$(document).mousedown(function(e){
  if (e.target.id === 'wheel-submit') {
        $(".wheel-cover").hide();
        $(".wheel-alert").hide();
        $(".alert-case1").hide();
        $(".alert-case2").hide();
        $(".alert-case3").hide();
        $(".alert-case4").hide();
        $(".alert-case5").hide();
        $(".alert-case6").hide();
        $(".alert-case7").hide();
  }
})
$(document).ready(function () {
    var Swidth = $(window).width();
    //var Sheight = $(".container").height();
    //$(".big-wheel").height(Sheight);
    $(".big-wheel .wheel-top").height(Swidth * 0.5625);
    $(".big-wheel .wheel-mid").height(Swidth * 0.775);
    $(".big-wheel .wheel-bottom").height(Swidth * 0.922);
    $(".big-wheel .wheel-mid .wheel-wrap").height(Swidth * 0.775 * 0.88);
    $(".big-wheel .wheel-alert").height(Swidth * 1.145);
    //$(".big-wheel .activity-rules").height(Swidth * 0.83);

    // $("#wheel-submit").click(function () {
    //     $(".wheel-cover").hide();
    //     $(".wheel-alert").hide();
    //     $(".alert-case1").hide();
    //     $(".alert-case2").hide();
    //     $(".alert-case3").hide();
    //     $(".alert-case4").hide();
    //     $(".alert-case5").hide();
    //     $(".alert-case6").hide();
    //     $(".alert-case7").hide();
    // });

    var agg = $("table td .start-btn p");
    if (agg.length > 0) {
        $(".wheel-wrap .start-btn").removeClass("tb-cell")
        $(".wheel-wrap .start-btn span").addClass("agg")
        //alert(1)
        
    } else {
        $(".wheel-wrap .start-btn").addClass("tb-cell")
        $(".wheel-wrap .start-btn span").removeClass("agg")
        //alert(2)
        }
    
    $(".container").removeClass('hide');
});

var lottery = {
    index: -1,	//��ǰת�����ĸ�λ�ã����λ��
    count: 0,	//�ܹ��ж��ٸ�λ��
    timer: 0,	//setTimeout��ID����clearTimeout���
    speed: 20,	//��ʼת���ٶ�
    times: 0,	//ת������
    cycle: 50,	//ת�������������Ҫת�����ٴ��ٽ���齱����
    prize: -1,	//�н�λ��
    init: function (id) {
        if ($("#" + id).find(".lottery-unit").length > 0) {
            $lottery = $("#" + id);
            $units = $lottery.find(".lottery-unit");
            this.obj = $lottery;
            this.count = $units.length;
            $lottery.find(".lottery-unit-" + this.index).addClass("active");
        };
    },
    roll: function () {
        var index = this.index;
        var count = this.count;
        var lottery = this.obj;
        $(lottery).find(".lottery-unit-" + index).removeClass("active");
        index += 1;
        if (index > count - 1) {
            index = 0;
        };
        $(lottery).find(".lottery-unit-" + index).addClass("active");
        this.index = index;
        return false;
    },
    stop: function (index) {
        this.prize = index;
    }
};

function GetIndex(idt)
{
   
     var arr = new Array();
     arr[0] = $("#1").val(),
     arr[1] = $("#2").val(),
     arr[2] = $("#3").val(),
     arr[3] = $("#6").val();
     arr[4] = $("#9").val(),
     arr[5] = $("#8").val(),
     arr[6] = $("#7").val(),
     arr[7] = $("#4").val()
     for (var i = 0; i < arr.length; i++) {
        if (arr[i] == idt)
        {
            return i;
        }
    }
}

function roll() {
    lottery.times += 1;
    lottery.roll();
    if (lottery.times > lottery.cycle + 10 && lottery.prize == lottery.index) {
        clearTimeout(lottery.timer);
        //����
        $(".wheel-cover").show();
        $(".wheel-alert").show();
        var obj = $lottery.find(".lottery-unit-" + lottery.index).attr("name");
        if (obj == "integral") {
            
            $("#txtIntegral").text($lottery.find(".lottery-unit-" + lottery.index).find("p").text());
            $(".alert-case4").show();
        }
        else if (obj == "bonus") {
            if (bounsPrice>0) {
                $("#txtBouns").text(bounsPrice);
            }
            
            $(".alert-case5").show();
        } else if (obj == "coupon") {
            $("#txtCoupon").text($lottery.find("#couponName_" + lottery.index).val());
            $(".alert-case6").show();
        }
        else {
            $(".alert-case1").show();
        }
        lottery.prize = -1;
        lottery.times = 0;
        click = false;
    } else {
        if (lottery.times < lottery.cycle) {
            lottery.speed -= 10;
        } else if (lottery.times == lottery.cycle) {
            lottery.prize = indexs;
        } else {
            if (lottery.times > lottery.cycle + 10 && ((lottery.prize == 0 && lottery.index == 7) || lottery.prize == lottery.index + 1)) {
                lottery.speed += 110;
            } else {
                lottery.speed += 20;
            }
        }
        if (lottery.speed < 40) {
            lottery.speed = 40;
        };
        lottery.timer = setTimeout(roll, lottery.speed);

    }
    return false;

}

var click = false;
//��ɵ�ַ  
function addurl() {
    return "/" + areaName + "/BigWheel/Index/" + $("#activityid").val();
}


window.onload = function () {
 
    lottery.init('lottery');
    $("#lottery .start-btn").click(function () {
        var chanceCount =parseInt( $("#chanceCount").text());
        var chanceIntergras = parseInt($("#Integrals").val());
        var consumePoint = parseInt($("#consumePoint").val());
      
       

            var id = $("#activityid").val();
            var userId = $("#userId").val();
            var returnurl = addurl();
            //У���¼
            checkLogin(returnurl, function () {
                //У����
                if (chanceIntergras < consumePoint) {
                    $(".wheel-cover").show();
                    $(".wheel-alert").show();
                    $(".alert-case3").show();
                    return;
                }
                else if (chanceCount == 0) {
                    $(".wheel-cover").show();
                    $(".wheel-alert").show();
                    $(".alert-case2").show();
                    return;
                }
                else if (chanceCount < -1) {
                    $(".wheel-cover").show();
                    $(".wheel-alert").show();
                    $(".alert-case2").show();
                    return;
                }
              
                    $.ajax({
                        type: 'post',
                        url: "/" + areaName + "/BigWheel/Add",
                        data: { id: id, userId: userId },
                        dataType: "json",
                        success: function (data) {
                            if (data.success) {
                                indexs = GetIndex(data.data.split(',')[0]);
                                bounsPrice = data.data.split(',')[1];
                              
                                lottery.speed = 80;
                                roll();
                                click = true;
                                if (chanceCount > 0) {
                                    $("#chanceCount").text(chanceCount - 1);
                                }
                                return false;
                            }
                            else {
                                //$.dialog.errorTips(data.data.split(',')[0]);
                                $.dialog.errorTips(data.msg);
                            }
                        }
                    });
                
            });
    });
};