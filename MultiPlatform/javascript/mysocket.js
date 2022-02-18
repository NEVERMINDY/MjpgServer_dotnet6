path = "";
var httpGetHead = "GET" + path + "HTTP/1.1\r\n" +
    "Host: 10.132.60.231:2022\r\n" +
    "Connection: keep-alive\r\n" +
    "User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36\r\n" +
    "Accept: */*\r\n" +
    "Referer: http://10.132.60.231:2022/\r\n" +
    "Accept-Encoding: gzip, deflate\r\n" +
    "Accept-Language: zh-CN,zh;q=0.9\r\n";

var httpPostHead = "POST / HTTP/1.1\r\n" +
    "Host: 10.132.60.231:2022\r\n" +
    "Connection: keep-alive\r\n" +
    "User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36\r\n" +
    "Accept: */*\r\n" +
    "Referer: http://10.132.60.231:2022/\r\n" +
    "Accept-Encoding: gzip, deflate\r\n" +
    "Accept-Language: zh-CN,zh;q=0.9\r\n";

//var hostIPEndPoint = "192.168.1.6:2022";
var hostIPEndPoint = "10.132.60.231:2022";
var IfConnect = false;
var postData = "";
var imageIndex = 0;
var imageCount = 0;
var timer = 0.01;
var mySocket;

function streamStart() {
    //function Start
    if (document.getElementById("startBtn").textContent.trim() == "开始接收") {
        timer = 0.01;
        imageIndex = 0;
        document.getElementById("startBtn").textContent = "停止接收";
        if (IfConnect) {
            alert("已经连接到服务器！请勿重新连接！");
            return;
        }
        document.getElementById("pauseBtn").textContent = "暂停接收";
        setInterval("calculateFps()", 1000);
        if ("WebSocket" in window) {
            mySocket = new WebSocket("ws://" + hostIPEndPoint);
            mySocket.onopen = function() {
                //alert("握手成功！");
                IfConnect = true;
                mySocket.send(httpGetHead);
            }
            mySocket.onmessage = function(e) {
                onReceiveImage(e);
            }
            mySocket.onclose = function(e) {
                //连接关闭时，重置图片索引，将IfConnect设为false
                imageIndex = 0;
                IfConnect = false;
                console.log("连接断开:" + e.code + " " + e.reason + " " + e.wasClean);
            }
        } else {
            alert("该浏览器不支持WebSocket!")
        }
    }
    //function Stop
    else {
        timer = 0.01;
        imageIndex = 0;
        document.getElementById("startBtn").textContent = "开始接收"
        IfConnect = false;
        mySocket.close(code = "1000", reason = "shutdown");
    }

};

function pauseButton() {
    if (document.getElementById("pauseBtn").textContent == "暂停接收") {
        streamPause();
        document.getElementById("pauseBtn").textContent = "继续接收";
    } else {
        streamContinue();
        document.getElementById("pauseBtn").textContent = "暂停接收";
    }
};

function streamContinue() {
    timer = 0.01;
    var PauseRequst = new XMLHttpRequest();
    PauseRequst.open("POST", HostIPEndPoint);
    PauseRequst.send("CONTINUE");
};

function streamPause() {
    imageCount = 0;
    timer = 0.01;
    var PauseRequst = new XMLHttpRequest();
    PauseRequst.open("POST", "/");
    PauseRequst.send("PAUSE");
};

function calculateFps() {
    timer++;
    var fps = imageCount / timer;
    fps = fps.toFixed(1);
    document.getElementById("fpsParagraph").innerHTML = "FPS:" + fps.toString();
};

function streamReset() {
    imageIndex = 0;
    timer = 0.01;
    var ResetRequest = new XMLHttpRequest();
    ResetRequest.open("POST", "/");
    ResetRequest.send("RESET");
};

function onReceiveImage(e) {
    imageIndex++;
    imageCount++;
    document.getElementById("indexParagraph").innerHTML = "当前索引：" + imageIndex.toString();
    var reader = new FileReader();
    reader.readAsDataURL(e.data);
    reader.onload = function(e) {
        var imagebox = document.getElementById("imageShowBox");
        var image = new Image();
        image.src = e.target.result;
        imagebox.src = e.target.result;
        image.onload = () => {
            console.log("width:" + image.width + "\t" + "height:" + image.height);
        }
        imagebox.onload = () => {
            console.log("boxwidth:" + imagebox.width + "\t" + "boxheight:" + imagebox.height);
        }
    }
}

function onReceiveMessage(e) {

}