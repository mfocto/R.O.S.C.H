let opcWebSocket = null;
let opcReconnectTimer = null;

function initOpcWebSocket() {
    if (opcWebSocket && opcWebSocket.readyState === WebSocket.OPEN) {
        return;
    }
    
    if (!currentUser) {
        return;
    }
    
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/ws/opc`
    
    opcWebSocket = new WebSocket(wsUrl);
    
    opcWebSocket.onopen = () => {
        if (opcReconnectTimer) {
            clearTimeout(opcReconnectTimer);
            opcReconnectTimer = null;
        }
    }
    
    opcWebSocket.onmessage = (e) => {
        try {
            const data = JSON.parse(e.data);
            
            updateConveyor(data);
        } catch (e) {
            console.error('WebSocket error', e);
        }
    }

    opcWebSocket.onerror = (error) => {
        console.error('[OPC] WebSocket 오류:', error);
    };

    opcWebSocket.onclose = () => {
        opcWebSocket = null;

        // 로그인 상태이면 5초 후 재연결 시도
        if (currentUser) {
            opcReconnectTimer = setTimeout(() => {
                console.log('[OPC] 재연결 시도...');
                initOpcWebSocket();
            }, 5000);
        }
    };
}

function cleanupOpcWebSocket() {
    // 재연결 타이머 클리어
    if (opcReconnectTimer) {
        clearTimeout(opcReconnectTimer);
        opcReconnectTimer = null;
    }

    // WebSocket 연결 종료
    if (opcWebSocket) {
        opcWebSocket.close();
        opcWebSocket = null;
    }
}

// 컨베이어 속도 업데이트 
function updateConveyor(data) {
    // OPC에서 받은 데이터 매핑
    const conveyorData = {
        load: data.conv_load || '0.0',
        main: data.conv_main || '0.0',
        sort: data.conv_sort || '0.0'
    };

    // User 페이지 업데이트
    updateUserConveyorDisplay(conveyorData);

    // Admin 페이지 업데이트
    updateAdminConveyorDisplay(conveyorData);
}

// User 페이지의 컨베이어 상태 업데이트 (ID 기반)
function updateUserConveyorDisplay(conveyorData) {
    const mapping = {
        'user-conv-load': conveyorData.load,
        'user-conv-main': conveyorData.main,
        'user-conv-sort': conveyorData.sort
    };

    for (const [id, value] of Object.entries(mapping)) {
        const element = document.getElementById(id);
        if (element) {
            const speed = parseFloat(value);
            element.textContent = `${speed.toFixed(1)} m/s`;

            // 속도에 따라 색상 변경
            if (speed > 0) {
                element.className = 'text-green';
            } else {
                element.className = 'text-blue';
            }
        }
    }
}

// Admin 페이지의 컨베이어 입력 필드 업데이트
function updateAdminConveyorDisplay(conveyorData) {
    const mapping = {
        'conv-load-display': conveyorData.load,
        'conv-main-display': conveyorData.main,
        'conv-sort-display': conveyorData.sort
        // conv-4는 OPC 데이터가 없으면 제외
    };

    for (const [id, value] of Object.entries(mapping)) {
        const element = document.getElementById(id);
        if (element) {
            const speed = parseFloat(value);
            element.textContent = speed.toFixed(1);

            // 속도에 따라 색상 변경 (선택사항)
            if (speed > 5) {
                element.style.color = 'var(--neon-red)'; // 고속
            } else if (speed > 0) {
                element.style.color = 'var(--neon-green)'; // 정상
            } else {
                element.style.color = '#666'; // 정지
            }
        }
    }
}
// 전역 함수로 노출 (router.js에서 사용)
window.initOpcWebSocket = initOpcWebSocket;
window.cleanupOpcWebSocket = cleanupOpcWebSocket;