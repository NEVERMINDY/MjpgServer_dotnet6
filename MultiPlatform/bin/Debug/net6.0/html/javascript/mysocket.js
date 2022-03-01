var httpGetHead = "GET /path HTTP/1.1\r\n" + "Upgrade: websocket\r\n";
var httpPostHead = "POST / HTTP/1.1";
var postData = "";
var imageIndex = 0;

openConnection = function openConnection() {
    if ("WebSocket" in window) {
        var mySocket = new WebSocket("mySocket://localhost:2022");
        mySocket.onopen = function() {
            mySocket.send("Hello Server!");
        }
    } else {
        alert("该浏览器不支持WebSocket!")
    }
}