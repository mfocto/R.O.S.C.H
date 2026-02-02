// 라우터
const routes = {
    '': renderLogin,
    'login': renderLogin,
    'admin': renderAdmin,
    'user': renderUser
}

let currentUser = null;

function restoreSession() {
    const savedRole = localStorage.getItem('userRole');
    const savedUsername = localStorage.getItem('username');
    const loginTime = localStorage.getItem('loginTime');

    if (savedRole && savedUsername) {
        // USER의 경우 새로고침 시 1시간 체크
        if (savedRole === 'USER' && loginTime) {
            const elapsed = new Date() - new Date(loginTime);
            const oneHour = 60 * 60 * 1000; // 1시간 (밀리초)

            if (elapsed > oneHour) {
                console.log('USER 세션 만료 (1시간 초과)');
                localStorage.clear();
                return false;
            }
        }

        currentUser = {
            role: savedRole,
            username: savedUsername
        };
        return true;
    }
    return false;
}

// 초기화
document.addEventListener('DOMContentLoaded', () => {
    // 세션 복원
    const hasSession = restoreSession();
    
    // 현재 Hash 확인
    const currentHash = window.location.hash.slice(1) || '';
    
    if (hasSession) {
        // 로그인되어 있는데 login 페이지나 빈 페이지에 있으면 올바른 페이지로 리다이렉트
        if (currentHash === '' || currentHash === 'login') {
            const targetPage = currentUser.role === 'ADMIN' ? 'admin' : 'user';
            navigate(targetPage);
            return;
        }
        // 권한 확인
        if (currentHash === 'admin' && currentUser.role !== 'ADMIN') {
            navigate('user');
            return;
        }
    } else {
        // 로그인되어 있지 않은데 로그인이 필요한 페이지에 있으면 로그인으로
        if (currentHash === 'admin' || currentHash === 'user') {
            navigate('login');
            return;
        }
    }
    
    // Hash 라우팅 이벤트 리스너
    window.addEventListener('hashchange', router);
    
    // 초기 라우팅
    router();
});

function router() {
    const hash = window.location.hash.slice(1) || '';
    
    const renderFn = routes[hash] || renderLogin;
    
    try {
        renderFn();
    } catch (error) {
        console.error('Routing error:', error);
        renderLogin();
    }
}

function navigate(path) {
    window.location.hash = path;
}

// ===== 로그인 페이지 =====
function renderLogin() {
    if (currentUser) {
        const targetPage = currentUser.role === 'ADMIN' ? 'admin' : 'user';
        navigate(targetPage);
        return;
    }
    
    const app = document.getElementById('app');
    app.innerHTML = `
        <div style="display:flex; align-items:center; justify-content:center; height:100vh;">
            <div class="glass-panel" style="width:350px; text-align:center; padding:40px;">
                <h2 class="text-green">INDUSTRIAL ACCESS</h2>
                <p style="color:#666; font-size:12px; margin-bottom:30px;">Digital Twin Logistics Control</p>
                
                <input type="text" id="login-id" placeholder="USERNAME" class="input-dark">
                <input type="password" id="login-pw" placeholder="PASSWORD" class="input-dark">
                
                <button id="login-btn" class="btn-control" style="border-color:var(--neon-green); color:var(--neon-green); margin-top:10px;">
                    SECURE LOGIN
                </button>
                
                <p id="login-msg" style="color:var(--danger); font-size:11px; margin-top:15px; min-height:1em;"></p>
            </div>
        </div>
    `;
    
    // 이벤트 리스너 등록
    document.getElementById('login-btn').onclick = handleLogin;
    document.getElementById('login-pw').onkeypress = (e) => {
        if (e.key === 'Enter') handleLogin();
    };
    document.getElementById('login-id').focus();
}

async function handleLogin() {
    const id = document.getElementById('login-id').value;
    const pw = document.getElementById('login-pw').value;
    const msg = document.getElementById('login-msg');
    const btn = document.getElementById('login-btn');

    if (!id || !pw) {
        msg.innerText = "아이디와 비밀번호를 입력해주세요.";
        return;
    }

    btn.innerText = "AUTHENTICATING...";
    btn.disabled = true;

    try {
        // 서버 호출
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                Username: id,
                Password: pw
            })
        });
        const data = await response.json();
        
        // 세션 저장
        currentUser = { role: data.role, username: id };
        localStorage.setItem('userRole', data.role);
        localStorage.setItem('username', id);
        localStorage.setItem('loginTime', new Date().toISOString());
        
        // Hash 라우팅으로 이동
        navigate(data.role === 'ADMIN' ? 'admin' : 'user');

    } catch (error) {
        msg.innerText = error.message;
        btn.disabled = false;
        btn.innerText = "SECURE LOGIN";
    }
}

// ===== Admin 페이지 =====
function renderAdmin() {
    if (!currentUser) {
        navigate('login');
        return;
    }
    
    if (currentUser.role !== 'ADMIN') {
        alert('관리자 권한이 필요합니다.');
        navigate('user');
        return;
    }
    
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="dashboard">
            <header class="glass-panel" style="display:flex; align-items:center;">
                <h2 class="text-green">🏭 LOGISTICS TWIN <small class="text-blue">ADMIN</small></h2>
                <div style="margin-left:auto; display:flex; align-items:center; gap:20px;">
                    <span style="font-size:12px; color:#666;">User: <strong class="text-green">${currentUser.username}</strong></span>
                    <button id="logout-btn" style="background:none; border:none; color:#666; cursor:pointer; font-family:inherit;">LOGOUT</button>
                </div>
            </header>

            <main class="glass-panel neon-border-blue main-viewer" style="padding:0; grid-row: 2 / 4;">
                <div class="overlay-info">
                    MODE: <span class="text-green">AUTO</span><br>
                    PING: <span id="ping" class="text-blue">--</span>
                </div>
                <div class="viewer-content" style="width:100%; height:100%; position:relative; overflow:hidden;">
                    <video id="video" autoplay playsinline muted 
                        style="position:absolute; top:0; left:0; width:100%; height:100%; object-fit:cover; background:black;">
                    </video>
                </div>
            </main>

            <section class="glass-panel control-panel" style="display:grid; grid-template-columns: repeat(4, 1fr); gap:10px;">
                <button class="btn-control" onclick="alert('MOVE TO A')">RUN</button>
                <button class="btn-control" onclick="alert('EMERGENCY_STOP')">STOP</button>
                <button class="btn-control btn-danger" onclick="alert('RESCAN VISION')">EMERGENCY STOP</button>
                <button class="btn-control" onclick="alert('CALL ADMIN')">CALL ADMIN</button>
            </section>

            <aside class="glass-panel sidebar-right" style="display:flex; flex-direction:column; gap:12px; padding:12px;">
    <!-- 카메라 선택 버튼 -->
    <div style="display:flex; flex-direction:column; flex-shrink:0;">
        <h3 style="margin:0 0 8px 0; font-size:13px;">Camera Select</h3>
        <div style="display:grid; grid-template-columns: 1fr 1fr; gap:5px; margin-bottom:8px;">
            <button class="camera-btn active" data-camera="Main" onclick="selectCamera('Main')" 
                    style="padding:10px 8px; font-size:10px; background:#1a1a1a; border:1.5px solid var(--neon-green); color:var(--neon-green); border-radius:6px; cursor:pointer; font-family:var(--font); transition:0.2s; font-weight:bold;">
                MAIN
            </button>
            <button class="camera-btn" data-camera="Camera1" onclick="selectCamera('Camera1')" 
                    style="padding:10px 8px; font-size:10px; background:#1a1a1a; border:1.5px solid #333; color:#888; border-radius:6px; cursor:pointer; font-family:var(--font); transition:0.2s;">
                CAM 1
            </button>
            <button class="camera-btn" data-camera="Camera2" onclick="selectCamera('Camera2')" 
                    style="padding:10px 8px; font-size:10px; background:#1a1a1a; border:1.5px solid #333; color:#888; border-radius:6px; cursor:pointer; font-family:var(--font); transition:0.2s;">
                CAM 2
            </button>
            <button class="camera-btn" data-camera="Camera3" onclick="selectCamera('Camera3')" 
                    style="padding:10px 8px; font-size:10px; background:#1a1a1a; border:1.5px solid #333; color:#888; border-radius:6px; cursor:pointer; font-family:var(--font); transition:0.2s;">
                CAM 3
            </button>
        </div>
        <div style="font-size:10px; color:#666; padding:6px 8px; background:rgba(0,255,0,0.05); border:1px solid rgba(0,255,0,0.2); border-radius:4px; text-align:center;">
            <span id="current-camera" class="text-green" style="font-weight:bold;">Main</span>
        </div>
    </div>

    <hr style="border:0; border-top:1px solid #333; margin:0;">

    <!-- 컨베이어 속도 제어 -->
        <div style="flex:1; display:flex; flex-direction:column; min-height:0;">
            <h3 style="margin:0 0 10px 0; font-size:13px;">Conveyor Speed Control</h3>
            <div style="display:flex; flex-direction:column; gap:10px; overflow-y:auto; flex:1;">
                
                <!-- CV-1 (Load) -->
                <div style="background:rgba(0,0,0,0.3); padding:10px; border-radius:6px; border:1px solid rgba(255,255,255,0.05);">
                    <div style="display:flex; align-items:center; justify-content:space-between; margin-bottom:8px;">
                        <span style="font-size:11px; color:#888; font-weight:bold;">CONVEYOR 1</span>
                    </div>
                    
                    <!-- 현재 속도 표시 영역 (읽기 전용) -->
                    <div style="background:rgba(0,0,0,0.5); padding:12px; border-radius:4px; margin-bottom:8px; text-align:center; border:1px solid rgba(255,255,255,0.1);">
                        <div style="font-size:9px; color:#666; margin-bottom:4px;">Current Speed</div>
                        <div id="conv-1-display" style="font-size:24px; color:var(--neon-green); font-weight:bold; font-family:monospace;">
                            0.0
                        </div>
                        <div style="font-size:9px; color:#666; margin-top:2px;">m/s</div>
                    </div>
                    
                    <!-- 입력 영역 -->
                    <div style="display:flex; align-items:center; gap:6px;">
                        <input type="number" id="conv-1-input" class="input-dark" 
                               style="flex:1; padding:6px 8px; font-size:11px; margin:0; text-align:center; font-weight:bold;" 
                               placeholder="Set speed..." step="0.1" min="0" max="10">
                        <button onclick="applyConveyorSpeed(1)" 
                                style="padding:6px 12px; font-size:9px; background:#1a1a1a; border:1.5px solid var(--neon-blue); color:var(--neon-blue); border-radius:4px; cursor:pointer; font-family:var(--font); white-space:nowrap; font-weight:bold; transition:0.2s;">
                            SET
                        </button>
                    </div>
                </div>
                
                <!-- CV-2 (Main) -->
                <div style="background:rgba(0,0,0,0.3); padding:10px; border-radius:6px; border:1px solid rgba(255,255,255,0.05);">
                    <div style="display:flex; align-items:center; justify-content:space-between; margin-bottom:8px;">
                        <span style="font-size:11px; color:#888; font-weight:bold;">CONVEYOR 2</span>
                    </div>
                    
                    <!-- 현재 속도 표시 영역 (읽기 전용) -->
                    <div style="background:rgba(0,0,0,0.5); padding:12px; border-radius:4px; margin-bottom:8px; text-align:center; border:1px solid rgba(255,255,255,0.1);">
                        <div style="font-size:9px; color:#666; margin-bottom:4px;">Current Speed</div>
                        <div id="conv-2-display" style="font-size:24px; color:var(--neon-green); font-weight:bold; font-family:monospace;">
                            0.0
                        </div>
                        <div style="font-size:9px; color:#666; margin-top:2px;">m/s</div>
                    </div>
                    
                    <!-- 입력 영역 -->
                    <div style="display:flex; align-items:center; gap:6px;">
                        <input type="number" id="conv-2-input" class="input-dark" 
                               style="flex:1; padding:6px 8px; font-size:11px; margin:0; text-align:center; font-weight:bold;" 
                               placeholder="Set speed..." step="0.1" min="0" max="10">
                        <button onclick="applyConveyorSpeed(2)" 
                                style="padding:6px 12px; font-size:9px; background:#1a1a1a; border:1.5px solid var(--neon-blue); color:var(--neon-blue); border-radius:4px; cursor:pointer; font-family:var(--font); white-space:nowrap; font-weight:bold; transition:0.2s;">
                            SET
                        </button>
                    </div>
                </div>
                
                <!-- CV-3 (Sort) -->
                <div style="background:rgba(0,0,0,0.3); padding:10px; border-radius:6px; border:1px solid rgba(255,255,255,0.05);">
                    <div style="display:flex; align-items:center; justify-content:space-between; margin-bottom:8px;">
                        <span style="font-size:11px; color:#888; font-weight:bold;">CONVEYOR 3</span>
                    </div>
                    
                    <!-- 현재 속도 표시 영역 (읽기 전용) -->
                    <div style="background:rgba(0,0,0,0.5); padding:12px; border-radius:4px; margin-bottom:8px; text-align:center; border:1px solid rgba(255,255,255,0.1);">
                        <div style="font-size:9px; color:#666; margin-bottom:4px;">Current Speed</div>
                        <div id="conv-3-display" style="font-size:24px; color:var(--neon-green); font-weight:bold; font-family:monospace;">
                            0.0
                        </div>
                        <div style="font-size:9px; color:#666; margin-top:2px;">m/s</div>
                    </div>
                    
                    <!-- 입력 영역 -->
                    <div style="display:flex; align-items:center; gap:6px;">
                        <input type="number" id="conv-3-input" class="input-dark" 
                               style="flex:1; padding:6px 8px; font-size:11px; margin:0; text-align:center; font-weight:bold;" 
                               placeholder="Set speed..." step="0.1" min="0" max="10">
                        <button onclick="applyConveyorSpeed(3)" 
                                style="padding:6px 12px; font-size:9px; background:#1a1a1a; border:1.5px solid var(--neon-blue); color:var(--neon-blue); border-radius:4px; cursor:pointer; font-family:var(--font); white-space:nowrap; font-weight:bold; transition:0.2s;">
                            SET
                        </button>
                    </div>
                </div>
                
            </div>
        </div>
        
    </div>
</aside>
        </div>
    `;
    
    document.getElementById('logout-btn').onclick = logout;
    
    // WebRTC 초기화 (webrtc.js의 initWebRTC 함수 호출)
    if (typeof initWebRTC === 'function') {
        initWebRTC();
    }
    
    if (typeof initOpcWebSocket === 'function') {
        initOpcWebSocket();
    }
}

// ===== User 페이지 =====
function renderUser() {
    if (!currentUser) {
        navigate('login');
        return;
    }
    
    const app = document.getElementById('app');
    app.innerHTML = `
       <div class="dashboard">
            <header class="glass-panel" style="display:flex; align-items:center;">
                <h2 class="text-green">🏭 LOGISTICS TWIN <small class="text-blue">VIEWER</small></h2>
                <div style="margin-left:auto; display:flex; align-items:center; gap:20px;">
                    <span style="font-size:12px; color:#666;">User: <strong class="text-blue">${currentUser.username}</strong></span>
                    <button id="logout-btn" style="background:none; border:none; color:#666; cursor:pointer; font-family:inherit;">LOGOUT</button>
                </div>
            </header>

            <main class="glass-panel neon-border-blue main-viewer" style="padding:0; grid-row: 2 / 4;">
                <div class="overlay-info">
                    MODE: <span class="text-green">AUTO</span><br>
                    PING: <span id="ping" class="text-blue">--</span>
                </div>
                <div class="viewer-content" style="width:100%; height:100%; position:relative; overflow:hidden;">
                    <video id="video" autoplay playsinline muted 
                        style="position:absolute; top:0; left:0; width:100%; height:100%; object-fit:cover; background:black;">
                    </video>
                </div>
            </main>

            <aside class="glass-panel sidebar-right" style="display:flex; flex-direction:column; gap:15px;">
                <!-- 카메라 선택 -->
                <div style="display:flex; flex-direction:column; flex-shrink:0;">
                    <h3 style="margin:0 0 8px 0; font-size:13px;">Camera Select</h3>
                    <div style="display:grid; grid-template-columns: 1fr 1fr; gap:5px; margin-bottom:8px;">
                        <button class="camera-btn active" data-camera="Main" onclick="selectCamera('Main')" 
                                style="padding:10px 8px; font-size:10px; background:#1a1a1a; border:1.5px solid var(--neon-green); color:var(--neon-green); border-radius:6px; cursor:pointer; font-family:var(--font); transition:0.2s; font-weight:bold;">
                            MAIN
                        </button>
                        <button class="camera-btn" data-camera="Camera1" onclick="selectCamera('Camera1')" 
                                style="padding:10px 8px; font-size:10px; background:#1a1a1a; border:1.5px solid #333; color:#888; border-radius:6px; cursor:pointer; font-family:var(--font); transition:0.2s;">
                            CAM 1
                        </button>
                        <button class="camera-btn" data-camera="Camera2" onclick="selectCamera('Camera2')" 
                                style="padding:10px 8px; font-size:10px; background:#1a1a1a; border:1.5px solid #333; color:#888; border-radius:6px; cursor:pointer; font-family:var(--font); transition:0.2s;">
                            CAM 2
                        </button>
                        <button class="camera-btn" data-camera="Camera3" onclick="selectCamera('Camera3')" 
                                style="padding:10px 8px; font-size:10px; background:#1a1a1a; border:1.5px solid #333; color:#888; border-radius:6px; cursor:pointer; font-family:var(--font); transition:0.2s;">
                            CAM 3
                        </button>
                    </div>
                    <div style="font-size:10px; color:#666; padding:6px 8px; background:rgba(0,255,0,0.05); border:1px solid rgba(0,255,0,0.2); border-radius:4px; text-align:center;">
                        <span id="current-camera" class="text-green" style="font-weight:bold;">Main</span>
                    </div>
                </div>

                <hr style="border:0; border-top:1px solid #333; margin:0;">

                <!-- 컨베이어 속도 상태 (읽기 전용) -->
                <div style="flex:1;">
                    <h3 style="margin:0 0 10px 0;">Conveyor Status</h3>
                    <div style="display:flex; flex-direction:column; gap:8px; font-size:11px;">
                        <div style="display:flex; justify-content:space-between; padding:6px; background:rgba(0,0,0,0.2); border-radius:3px;">
                            <span style="color:#666;">CV-LOAD:</span>
                            <span id="user-conv-1" class="text-green">-- m/s</span>
                        </div>
                        <div style="display:flex; justify-content:space-between; padding:6px; background:rgba(0,0,0,0.2); border-radius:3px;">
                            <span style="color:#666;">CV-MAIN:</span>
                            <span id="user-conv-2" class="text-green">-- m/s</span>
                        </div>
                        <div style="display:flex; justify-content:space-between; padding:6px; background:rgba(0,0,0,0.2); border-radius:3px;">
                            <span style="color:#666;">CV-SORT:</span>
                            <span id="user-conv-3" class="text-green">-- m/s</span>
                        </div>
                    </div>
                </div>

            </aside>
        </div>
    `;
    
    document.getElementById('logout-btn').onclick = logout;
    
    // WebRTC 초기화
    if (typeof initWebRTC === 'function') {
        initWebRTC();
    }

    if (typeof initOpcWebSocket === 'function') {
        initOpcWebSocket();
    }
}

async function logout() {
    try {
        // 백엔드 로그아웃 API 호출 (ADMIN IsActive 해제)
        const username = localStorage.getItem('username');

        if (username) {
            await fetch('/api/auth/logout', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    username: username
                })
            });
        }
    } catch (error) {
        console.error('로그아웃 API 호출 실패:', error);
        // 에러가 발생해도 로컬 로그아웃은 진행
    } finally {
        // 로컬 세션 정리
        currentUser = null;
        localStorage.clear();

        // WebRTC 정리
        if (typeof cleanupWebRTC === 'function') {
            cleanupWebRTC();
        }
        if (typeof cleanupOpcWebSocket === 'function') {
            cleanupOpcWebSocket();
        }

        navigate('login');
    }
}


/**
 * 시간 체크해서 로그인한지 10분 넘었으면 자동 로그아웃 처리
 * */
function checkTimeout () {
    const loginTime = localStorage.getItem('loginTime');
    const userRole = localStorage.getItem('userRole');
    if (loginTime && userRole === 'ADMIN') {
        if ((new Date() - new Date(loginTime)) > (10 * 60 * 1000)) {
            // 밀리초로 timeout 비교
            logout()
        }
    }
}


setInterval(() => {
    checkTimeout();
}, 10000)