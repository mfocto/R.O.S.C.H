let roomId = ''        // 카메라 선택에 따라 바뀔 값
let clientId = ''      // 클라이언트 아이디
const video = document.getElementById('video')

// error log 
window.onerror = (m, s, l, c, e)=> 
    console.error("window.onerror", m, s, l, c, e)

window.onunhandledrejection = (e) => {
    console.error("window.onunhandledrejection", e.reason || e)
}

// WebSocketMessage 
class WebSocketMessage {
    constructor(Type, Payload, SenderId) {
        this.Type = Type;
        this.Payload = Payload;
        this.SenderId = SenderId;
        this.Timestamp = new Date().toISOString()
    }
    
    convert (message) {
        const d = JSON.parse(message.data)
        this.Type = d.Type
        this.Payload = d.Payload
        this.SenderId = d.SenderId
        this.Timestamp = d.Timestamp
    }
    
}

// google STUN 서버
const iceServer = [{urls: ["stun:stun.l.google.com:19302"]}]   


// socket
const socket = new WebSocket(`ws://localhost:5178/rtc?type=Client&roomId=${roomId}`)

// open시 join 요청 전송
socket.onopen = () => {
    const joinMessage = new WebSocketMessage("Join", "", "")
    
    socket.send(JSON.stringify(joinMessage))
}

socket.onmessage = (message) => {
    // message 를 WebSocketMessage 클래스 형태로 치환
    const data = new WebSocketMessage();
    data.convert(message);
    
    // System 메시지는 콘솔로 출력
    if (data.Type === "System") {
        console.log(`\x1b[35m[SystemMessage] ${data.Payload}`)
    }
    
    if (data.Type === "Joined") {
        clientId = data.Payload
        console.log(clientId);
        // broadcaster 목록 요청
        const broadcastListOffer = new WebSocketMessage("BroadcasterList", JSON.stringify({roomId: roomId}), clientId)
        socket.send(JSON.stringify(broadcastListOffer))
    }

    if (data.Type === "broadcasterList") {
        console.log(data.Payload)
    }
    
    if (data.Type === "answer") {
        
    }
    
    
}
