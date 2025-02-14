var socket;
var objName = "WebSocketManager";

mergeInto(LibraryManager.library, {
    createWebSocket: function () {
        socket = new WebSocket("wss://app2.davaogames.com");
        socket.onopen = function (e) { UnityInstance.SendMessage(objName, "HandleOnOpenWebSocket"); };
        socket.onerror = function (e) { UnityInstance.SendMessage(objName, "HandleOnErrorWebSocket"); };
        socket.onclose = function (e) { UnityInstance.SendMessage(objName, "HandleOnCloseWebSocket"); };
        socket.onmessage = function (e) { UnityInstance.SendMessage(objName, "HandleOnMessageWebSocket", e.data); };
    },
    closeWebSocket: function () {
        if (socket) socket.close();
    },
    checkWebSocketReady: function () {
        return socket && socket.readyState === WebSocket.OPEN;
    },
    SendData: function (data) {
        socket.send(data);
    },
    GetInputParamsFromTelegramUrl: function () {
        var url = new URL(window.location.href);
        var params = new URLSearchParams(url.search);
        var token = params.get("token");
        var gameName = params.get("game");
        var gameId = 0;
        var walletAddress = params.get("wa");
        switch (gameName) {
            case "fruit": gameId = 9007; break;
            case "inca": gameId = 9008; break;
            case "noel": gameId = 8818; break;
            case "tarzan": gameId = 9950; break;
            case "juicy": gameId = 9900; break;
            case "hilo": gameId = 8010; break;
        }
        // token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3MzAyNTY5MjksInVpZCI6NTMxMX0.AdGBJS3mhnfbCNYTU-8TppsTuq40la9pk6eqMaG_QzQ";
        // gameId = 8818;
        return [token, gameId, walletAddress];
    },
    OpenNewTab: function (url) {
        url = Pointer_stringify(url);
        window.open(url, '_blank');
    },
});