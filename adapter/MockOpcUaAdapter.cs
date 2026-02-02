using R.O.S.C.H.adapter.Interface;

namespace R.O.S.C.H.adapter;

public class MockOpcUaAdapter: IOpcUaAdapter
{
    private readonly ILogger<MockOpcUaAdapter> _logger;
    private readonly Random _random = new Random();
    
    // AGV 위치 시뮬레이션
    private float _posX = 0.0f;
    private float _posY = 0.0f;
    private float _posTheta = 0.0f;
    
    // 컨베이어 속도 시뮬레이션
    private long _speedLoad = 15;  // 1.5 m/s -> 15 (10배 스케일)
    private long _speedMain = 20;  // 2.0 m/s -> 20
    private long _speedSort = 18;  // 1.8 m/s -> 18
    
    // 시스템 상태
    private string _currentState = "1"; // 0: 부팅, 1: 대기, 2: running, 3: stop, 4: emergency robot, 5: emergency stop
    private long _currentFloor = 1;
    private bool _isLiftMoving = false;
    private bool _isRobotWorking = false;
    private bool _isRobotDone = false;

    public MockOpcUaAdapter(ILogger<MockOpcUaAdapter> logger)
    {
        _logger = logger;
        _logger.LogInformation("🧪 MockOpcUaAdapter 초기화 - 가짜 데이터 모드");
    }

    public Task<IDictionary<string, object>> ReadStateAsync(CancellationToken ct)
    {
        // AGV 위치 시뮬레이션 (원형 경로를 따라 이동)
        _posX += (float)(_random.NextDouble() - 0.5) * 0.5f;
        _posY += (float)(_random.NextDouble() - 0.5) * 0.5f;
        _posTheta += (float)(_random.NextDouble() - 0.5) * 10.0f;
        
        // 범위 제한
        _posX = Math.Clamp(_posX, -10.0f, 10.0f);
        _posY = Math.Clamp(_posY, -10.0f, 10.0f);
        _posTheta = (_posTheta + 360.0f) % 360.0f;

        // 컨베이어 속도 랜덤 변동 (±10%)
        _speedLoad += _random.Next(-2, 3);
        _speedMain += _random.Next(-2, 3);
        _speedSort += _random.Next(-2, 3);
        
        // 속도 범위 제한 (0 ~ 50, 즉 0.0 ~ 5.0 m/s)
        _speedLoad = Math.Clamp(_speedLoad, 0, 50);
        _speedMain = Math.Clamp(_speedMain, 0, 50);
        _speedSort = Math.Clamp(_speedSort, 0, 50);

        // 시스템 상태 시뮬레이션 (가끔 상태 변경)
        if (_random.Next(0, 100) < 5) // 5% 확률로 상태 변경
        {
            int stateNum = _random.Next(1, 3); // 주로 1(대기) 또는 2(running) 상태
            _currentState = stateNum.ToString();
        }

        // 리프트 이동 시뮬레이션
        if (_random.Next(0, 100) < 10) // 10% 확률로 리프트 이동 시작/중지
        {
            _isLiftMoving = !_isLiftMoving;
            if (_isLiftMoving)
            {
                _currentFloor = _random.Next(1, 4); // 1~3층
            }
        }

        // 로봇 작업 시뮬레이션
        if (_random.Next(0, 100) < 15) // 15% 확률로 로봇 상태 변경
        {
            if (!_isRobotWorking && !_isRobotDone)
            {
                _isRobotWorking = true;
                _isRobotDone = false;
            }
            else if (_isRobotWorking)
            {
                _isRobotWorking = false;
                _isRobotDone = true;
            }
            else if (_isRobotDone)
            {
                _isRobotDone = false;
            }
        }

        // DeviceList.json의 키 네이밍 규칙에 맞춰 소문자로 생성
        var mockData = new Dictionary<string, object>
        {
            // ESP32 ModbusTCP 데이터
            { "modbustcp_esp32_01_posx", _posX },
            { "modbustcp_esp32_01_posy", _posY },
            { "modbustcp_esp32_01_postheta", _posTheta },
            { "modbustcp_esp32_01_targeta", _random.Next(0, 2) == 1 },
            { "modbustcp_esp32_01_state", _random.Next(0, 5).ToString() },
            
            // STM Yolo 데이터
            { "stm_stm_yolo_agvloadarrived", _random.Next(0, 2) == 1 },
            { "stm_stm_yolo_agvloaddeparted", _random.Next(0, 2) == 1 },
            { "stm_stm_yolo_agvsortarrived", _random.Next(0, 2) == 1 },
            { "stm_stm_yolo_agvsortdeparted", _random.Next(0, 2) == 1 },
            { "stm_stm_yolo_currentfloor", _currentFloor },
            { "stm_stm_yolo_currentspeedload", _speedLoad },
            { "stm_stm_yolo_currentspeedmain", _speedMain },
            { "stm_stm_yolo_currentspeedsort", _speedSort },
            { "stm_stm_yolo_currentstate", _currentState },
            { "stm_stm_yolo_isliftmoving", _isLiftMoving },
            { "stm_stm_yolo_isrobotdone", _isRobotDone },
            { "stm_stm_yolo_isrobotworking", _isRobotWorking }
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("[Mock] 생성된 데이터:");
            _logger.LogDebug($"  AGV 위치: X={_posX:F2}, Y={_posY:F2}, θ={_posTheta:F2}°");
            _logger.LogDebug($"  컨베이어 속도: Load={_speedLoad}, Main={_speedMain}, Sort={_speedSort}");
            _logger.LogDebug($"  시스템 상태: {_currentState}, 층: {_currentFloor}");
            _logger.LogDebug($"  리프트: {_isLiftMoving}, 로봇 작업: {_isRobotWorking}, 완료: {_isRobotDone}");
        }

        return Task.FromResult<IDictionary<string, object>>(mockData);
    }

    public Task WriteStateAsync(CancellationToken ct, string channel, string device, string tag, object value)
    {
        _logger.LogInformation($"[Mock] Write 시뮬레이션: {channel}.{device}.{tag} = {value} ({value?.GetType().Name})");
        
        // Write 값에 따라 내부 상태 업데이트
        string key = $"{channel}_{device}_{tag}".ToLower();
        
        switch (key)
        {
            case "stm_stm_yolo_targetspeedload":
                if (value is long || value is int)
                {
                    _speedLoad = Convert.ToInt64(value);
                    _logger.LogInformation($"[Mock] Load 컨베이어 속도 변경: {_speedLoad}");
                }
                break;
                
            case "stm_stm_yolo_targetspeedmain":
                if (value is long || value is int)
                {
                    _speedMain = Convert.ToInt64(value);
                    _logger.LogInformation($"[Mock] Main 컨베이어 속도 변경: {_speedMain}");
                }
                break;
                
            case "stm_stm_yolo_targetspeedsort":
                if (value is long || value is int)
                {
                    _speedSort = Convert.ToInt64(value);
                    _logger.LogInformation($"[Mock] Sort 컨베이어 속도 변경: {_speedSort}");
                }
                break;
                
            case "stm_stm_yolo_targetstate":
                if (value != null)
                {
                    _currentState = value.ToString();
                    _logger.LogInformation($"[Mock] 시스템 상태 변경: {_currentState}");
                }
                break;
                
            case "modbustcp_esp32_01_control":
                _logger.LogInformation($"[Mock] ESP32 제어 명령: {value}");
                break;
                
            default:
                _logger.LogWarning($"[Mock] 알 수 없는 Write 요청: {key}");
                break;
        }
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _logger.LogInformation("MockOpcUaAdapter disposed");
    }
}