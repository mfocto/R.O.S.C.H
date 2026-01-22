let roomId = 'UNITY-1'        // 카메라 선택에 따라 바뀔 값
let clientId = ''      // 클라이언트 아이디
let pc = null           // RTC PeerConnection 객체
let camera = "Main"             // video 에 표시할 카메라명

let ping = 0;
let pingStartTime = 0;
let pingInterval = null;

let pingHistory = []
let MAX_HISTORY_SIZE = 10;

const video = document.getElementById('video')

// error log 
window.onerror = (m, s, l, c, e)=> 
    console.error("window.onerror", m, s, l, c, e)

window.onunhandledrejection = (e) => {
    console.error("window.onunhandledrejection", e.reason || e)
}

// WebSocketMessage 
class WebSocketMessage {
    constructor(Type, Payload, SenderId, SenderType, ReceiverId) {
        this.Type = Type;
        this.Payload = Payload;
        this.SenderId = SenderId;
        this.SenderType = SenderType;
        this.ReceiverId = ReceiverId;
        this.Timestamp = new Date().toISOString()
    }
    
    convert (message) {
        const d = JSON.parse(message.data)
        this.Type = d.Type;
        this.Payload = d.Payload;
        this.SenderId = d.SenderId;
        this.SenderType = d.SenderType;
        this.ReceiverId = d.ReceiverId;
        this.Timestamp = d.Timestamp;
    }
    
}

// google STUN 서버
const iceServer = [{urls: ["stun:stun.l.google.com:19302"]}]   


// socket
const socket = new WebSocket(`ws://localhost:5178/ws/rtc?type=Client&roomId=${roomId}`)

// open시 join 요청 전송
socket.onopen = () => {
    const joinMessage = new WebSocketMessage("Join", "", "", "Client", "")
    
    socket.send(JSON.stringify(joinMessage))
    
    pingInterval = setInterval(() => {
        sendPing()
    }, 5000)
}

socket.onclose = () => {
    if (pingInterval) {
        clearInterval(pingInterval)
        pingInterval = null
    }
}

// ping 체크
function sendPing() {
    if (socket.readyState === WebSocket.OPEN) {
        pingStartTime = Date.now()
        
        console.log('ping', pingStartTime)
        
        const pingMessage = new WebSocketMessage("Ping", JSON.stringify(pingStartTime), clientId, "Client", "")
        
        socket.send(JSON.stringify(pingMessage))
    }
}


// 메시지 수신
socket.onmessage = async (message) => {
    // message 를 WebSocketMessage 클래스 형태로 치환
    const data = new WebSocketMessage();
    data.convert(message);
    
    // System 메시지는 콘솔로 출력
    if (data.Type === "System") {
        const received = JSON.parse(data.Payload)
        
        console.log(`\x1b[35m[SystemMessage] ${received.Message}`)
    }
    
    // Pong 수신
    if (data.Type === 'Pong') {
        const pingTime = Date.now() - pingStartTime
        
        pingHistory.push(pingTime)
        
        // 최신 10개의 평균만 계산
        if(pingHistory.length > MAX_HISTORY_SIZE) {
            pingHistory.shift()
        }
        
        ping  = Math.round(pingHistory.reduce((a, b) => a + b, 0) / pingHistory.length)
        
        console.log(ping)
    }
    
    if (data.Type === "Joined") {
        clientId = data.Payload
        await sendMessage("BroadcasterList", JSON.stringify({roomId: roomId}), roomId)
    }
    
    // 화면 표시할 broadcaster 가 있을 경우에만 로직 실행
    if (data.Type === "broadcasterList") {
        let broadcasters = JSON.parse(data.Payload)
        
        if (broadcasters.length > 0) {
            createPeerConnection()
            
            const offer = pc.createOffer()
            await pc.setLocalDescription(offer)

            await sendMessage("Offer", JSON.stringify(offer), "")
        }
    }
    
    if (data.Type === "answer") {
        await pc.setRemoteDescription(JSON.parse(data.Payload))
    }
    
    if (data.Type === 'ice'){
        await pc.addIceCandidate(JSON.parse(data.Payload))
    }
}

// peer connection 생성
async function createPeerConnection(){
    // 기존 pc정리
    try {
        if (pc) {
            pc.ontrack = null;
            pc.onicecandidate= null
            pc.close()
        }
    } catch (error) {
        console.error("[createPeerConnection] peer connection 정리 중 오류")
    }
    
    pc = new RTCPeerConnection({ iceServers: iceServer })
    
    pc.addTransceiver('video', {direction: 'recvonly'})
    
    pc.ontrack = (e) => {
        console.log('ontrack', e)
        
        video.srcObject = new MediaStream([e.track])
        video.muted = true;
        video.autoplay = true;
        video.playsInline = true;
        video.play().catch(console.warn);
    }
    
    pc.onicecandidate = async (e) => {
        if (!e.candidate) return;
        await sendMessage("Ice", JSON.stringify(e.candidate), roomId)
    }
}

async function sendMessage (type, payload, receiverId) {
    const webSocketMessage = new WebSocketMessage(type, payload, clientId, "Client", receiverId)
    
    socket.send(JSON.stringify(webSocketMessage))
}