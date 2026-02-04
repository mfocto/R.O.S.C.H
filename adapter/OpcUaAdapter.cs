using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using R.O.S.C.H.adapter.Interface;

namespace R.O.S.C.H.adapter;

public class OpcUaAdapter : IOpcUaAdapter
{
    private ILogger<OpcUaAdapter> _logger;
    private readonly IConfiguration _config;
    
    //연결용 변수
    private readonly string _endPointURL;
    private readonly string _sessionName;
    private readonly int _reconnectInterval;
    private readonly int _connectionTimeout;
    private readonly int _sessionTimeout;

    private Session? _session;  // opc ua session
    private readonly ApplicationConfiguration _appConfig;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // 동시성 제어(Thread-safe)
    private CancellationTokenSource _cts;

    private string _channel;
    private JObject? deviceJson;

    public OpcUaAdapter(ILogger<OpcUaAdapter> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        
        _endPointURL = config.GetValue<string>("OpcUa:EndpointUrl", "opc.tcp://192.168.0.19:49320");
        _sessionName = config.GetValue<string>("OpcUa:SessionName", "ROS_SERVER");
        _reconnectInterval = config.GetValue<int>("OpcUa:ReconnectInterval");
        _connectionTimeout = config.GetValue<int>("OpcUa:ConnectionTimeoutMS");
        _sessionTimeout = config.GetValue<int>("OpcUa:SessionTimeoutMS");
        _appConfig = CreateOpcUaConfiguration();
        
        // 파일 읽어서 device 목록 세팅
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DeviceList.json");

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            
            deviceJson = JsonConvert.DeserializeObject<JObject>(jsonString);
        }
        else
        {
            _logger.LogError("[OpcUaAdapter] 디바이스 설정 정보를 읽을 수 없습니다.");
        }
    }

    /// <summary>
    /// 연결 설정
    /// </summary>
    private ApplicationConfiguration CreateOpcUaConfiguration()
    {
        var autoAcceptCert = _config.GetValue("OpcUa:AutoAcceptUntrustedCertificates", true);
        var sessionTimeout = _config.GetValue<int>("OpcUa:SessionTimeoutMS");
        
        // 인증서 경로
        var certOwnPath = _config.GetValue("OpcUa:CertificatePaths:Own", "c:/auth/pki/own");
        var certTrustedPath = _config.GetValue("OpcUa:CertificatePaths:Trusted", "c:/auth/pki/trusted");
        var certRejectedPath = _config.GetValue("OpcUa:CertificatePaths:Rejected", "c:/auth/pki/rejected");
        var certIssuersPath = _config.GetValue("OpcUa:CertificatePaths:Issuers", "c:/auth/pki/issuers");

        var config = new ApplicationConfiguration
        {
            ApplicationName = "R.O.S.C.H",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                // 인증서 경로
                ApplicationCertificate = new CertificateIdentifier
                {
                  StoreType  = "Directory",
                  StorePath = certOwnPath,
                  SubjectName = "R.O.S.C.H"
                },
                // 신뢰할 수 있는 서버 인증서 
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType  = "Directory",
                    StorePath = certTrustedPath,
                },
                
                // 발행자 인증서 저장 위치
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType  = "Directory",
                    StorePath = certIssuersPath,
                },
                // 거부된 인증서 저장 위치
                RejectedCertificateStore = new CertificateTrustList
                {
                  StoreType  = "Directory",
                  StorePath = certRejectedPath,
                },
                
                // 신뢰되지 않은 인증서 자동 수락
                AutoAcceptUntrustedCertificates =  true,
                
                RejectSHA1SignedCertificates = false,
                RejectUnknownRevocationStatus = false,
                // 애플리케이션 인증서를 신뢰 목록에 자동 추가
                AddAppCertToTrustedStore = true,
            },
            TransportConfigurations = new(),
            
            // 전송 할당량 설정
            TransportQuotas = new TransportQuotas 
            {
                // OPC UA IO 최대 대기 시간
              OperationTimeout = _connectionTimeout  
            },
            
            // 클라이언트 설정 - 세션 타임아웃
            ClientConfiguration = new ClientConfiguration
            {
                DefaultSessionTimeout = _sessionTimeout
            }
        };
        
        // 설정 검증
        config.ValidateAsync(ApplicationType.Client).GetAwaiter().GetResult();
        
        return config;
    }
    
    /// <summary>
    /// 연결처리
    /// </summary>
    private async Task ConnectAsync(CancellationToken ct)
    {
        // double Check Locking
        if (_session != null && _session.Connected) return;
        
        await _semaphore.WaitAsync(ct);

        try
        {
            if (_session != null && _session.Connected) return;

            if (_session != null)
            {
                try
                {
                    // 방어용 코드 - 실시간 처리 중 상태 변경에 대비
                    if (_session.Connected)
                    {
                        _session.CloseAsync().GetAwaiter().GetResult();
                    }

                    _session.Dispose();
                }
                catch
                {
                    /*ignore*/
                }

                _session = null;
            }

            _logger.LogInformation("Connecting to Kepware OPC UA Server at {Url}...", _endPointURL);

            var securityMode = _config.GetValue("OpcUa:SecurityMode", "None");
            var securityPolicy = _config.GetValue("OpcUa:SecurityPolicy", "None");

            // 엔드포인트 설정
            var endpoint = new EndpointDescription
            {
                EndpointUrl = _endPointURL,
                SecurityMode = securityMode.Equals("None")
                    ? MessageSecurityMode.None
                    : MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = securityPolicy.Equals("None")
                    ? SecurityPolicies.None
                    : SecurityPolicies.Basic256Sha256,
                UserIdentityTokens = new UserTokenPolicyCollection
                {
                    new UserTokenPolicy { TokenType = UserTokenType.Anonymous }
                },
                TransportProfileUri = Profiles.UaTcpTransport
            };


            var endpointConfig = EndpointConfiguration.Create(_appConfig);
            var selectedEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfig);

            var sessionTimeout = _config.GetValue("OpcUa:SessionTimeoutMs", 60000);
            
            _session = await Session.Create(
                _appConfig,
                selectedEndpoint,
                false,
                _sessionName,
                (uint)sessionTimeout,
                new UserIdentity(new AnonymousIdentityToken()),
                null);

            _logger.LogInformation("Successfully connected to Kepware OPC UA Server at {Url}", _endPointURL);

            _cts?.Cancel();
            _cts = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Kepware OPC UA Server at {Url}", _endPointURL);

            StartReconnectTask(ct);

            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void StartReconnectTask(CancellationToken ct)
    {
        if (_cts != null && !_cts.Token.IsCancellationRequested) return;

        _cts = new CancellationTokenSource();
        
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        
        _ = Task.Run(async () =>
        {
            while (!linkedCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_reconnectInterval, linkedCts.Token);
                    
                    _logger.LogInformation("Attempting to reconnect to Kepware OPC UA Server...");
                    
                    // 재연결 시도
                    await ConnectAsync(linkedCts.Token);
                    
                    // 연결 성공 시 루프 종료
                    break;
                }
                catch (OperationCanceledException e)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Reconnection attempt failed. Retrying in {Interval}ms...", _reconnectInterval);
                }
            }
        }, linkedCts.Token);
    }

    private NodeId BuildKepwareNodeId(string channelName, string deviceName, string tagName)
    {
        var nodeIdString = $"ns=2;s={channelName}.{deviceName}.{tagName}";
        return new NodeId(nodeIdString);
    }

    public async Task<IDictionary<string, object>> ReadStateAsync(CancellationToken ct)
    {
        await ConnectAsync(ct);
        ConcurrentDictionary<string, object> resultDict = new();
        
        JObject readDeviceJson = deviceJson["read"].ToObject<JObject>();
        
        var espJson = readDeviceJson["esp"].ToObject<JArray>();
        var stmJson = readDeviceJson["stm"].ToObject<JArray>();
        
        ReadValueIdCollection readValueIdCollection = new();
        List<string> readValue = new List<string>();
        
        
        // esp 세팅 
        // TODO : esp 연결 되면 확인할 것
        foreach (var r in espJson)
        {
            var channel = r["channel"].ToString();
            var device =  r["device"].ToString();
            var tag = r["tag"].ToString();
            
            readValue.Add($"{channel.ToLower()}_{device.ToLower()}_{tag.ToLower()}");
            readValueIdCollection.Add(new ReadValueId
            {
                NodeId = BuildKepwareNodeId(channel.Trim(),device.Trim(),tag.Trim()),
                AttributeId = Attributes.Value
            });
        }
        
        // stm 세팅
        foreach (var r in stmJson)
        {
            var channel = r["channel"].ToString();
            var device =  r["device"].ToString();
            var tag = r["tag"].ToString();
            
            readValue.Add($"{channel.ToLower()}_{device.ToLower()}_{tag.ToLower()}");
            readValueIdCollection.Add(new ReadValueId
            {
                
                NodeId = BuildKepwareNodeId(channel.Trim(),device.Trim(),tag.Trim()),
                AttributeId = Attributes.Value
            });
        }
        // 값 읽기
        try
        {
            var readResponse = await _session!.ReadAsync(
                null,
                0,
                TimestampsToReturn.Both,
                readValueIdCollection,
                ct);

            var results = readResponse.Results;
            var result = new Dictionary<string, object>();

            
            for (int i = 0 ; i < results.Count; i++)
            {
                
                if (StatusCode.IsGood(results[i].StatusCode))
                {
                    resultDict.TryAdd(readValue[i],  results[i].Value);
                    _logger.LogInformation(readValue[i] + " : " +  results[i].Value);
                }
                else
                {
                    resultDict.TryAdd(readValue[i],  0);
                    _logger.LogWarning(readValue[i] + " : " +  results[i].StatusCode);
                }
            }

            return resultDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return new  Dictionary<string, object>();
    }

    public async Task WriteStateAsync(CancellationToken ct, string channel, string device, string tag, object value)
    {
        await ConnectAsync(ct);
        
        _logger.LogDebug("[OpcUaClientAdapter] OPC-UA에 데이터 WRITE\n" +
                               $"Channel : {channel}\n"+
                               $"Device : {device}\n"+
                               $"Tag : {tag}\n"+
                               $"Value : {value}\n");

        try
        {
            var nodeId = BuildKepwareNodeId(channel, device, tag);

            var writeValue = new WriteValue
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };

            WriteValueCollection values = new WriteValueCollection { writeValue };

            WriteResponse response = await _session!.WriteAsync(null, values, ct);
            var res = response.Results;

            if (res != null && res.Count > 0)
            {
                if (StatusCode.IsBad(res[0]))
                {
                    // 쓰기 실패
                    throw new InvalidOperationException($"Writefailed :  {res[0]}");
                }

                _logger.LogDebug("[OpcUaClientAdapter] OPC-UA에 데이터 WRITE 성공!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[OpcUaAdapter] OPC-UA에 데이터 전송 실패\n{ex.Message}");
            
            // 연결 끊어진 경우 재연결 시도
            if (_session == null || !_session.Connected)
            {
                _session = null;
                StartReconnectTask(CancellationToken.None);
            }

            //throw;
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        try
        {
            if (_session != null && _session.Connected)
            {
                _session.CloseAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex) {/**/}
        
        _session?.Dispose();
        _semaphore?.Dispose();
        
        _logger.LogInformation("OpcUaClientAdapter disposed. Disconnected from Kepware OPC UA Server.");
    }
}