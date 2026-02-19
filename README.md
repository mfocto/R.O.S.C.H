# R.O.S.C.H

산업 자동화 시스템을 위한 OPC UA 기반 실시간 모니터링 및 제어 플랫폼

## 프로젝트 개요

R.O.S.C.H는 .NET 10.0 기반의 웹 애플리케이션으로, OPC UA 프로토콜을 통해 산업용 디바이스(AGV, Conveyor, Robot, Lift)를 실시간으로 모니터링하고 제어할 수 있는 시스템입니다.

본 프로젝트는 **산업 설비(OPC UA)와 서버 간 실시간 데이터 수집·제어 환경**을
가정하여 설계한 **산업용 서버 소프트웨어**입니다.
핵심 목표는 다음과 같습니다.

- 설비 통신 지연/불안정 상황에서도 안정적으로 상태를 동기화
- 실제 하드웨어가 없는 환경에서도 개발·테스트가 가능한 구조

## 주요 기능

- **OPC UA 통신**: ModbusTCP를 통한 ESP32, STM32 디바이스와의 실시간 데이터 통신
- **WebSocket 기반 실시간 업데이트**: 클라이언트에 실시간 디바이스 상태 전송
- **디바이스 제어**: 웹 인터페이스를 통한 디바이스 원격 제어
- **WebRTC 지원**: 실시간 영상 스트리밍 및 통신
- **데이터 로깅**: PostgreSQL을 활용한 제어 이력 및 오류 로그 관리

## 기술 스택

### Backend
- .NET 10.0
- ASP.NET Core Web API
- OPC Foundation .NET Standard Opc.Ua (v1.5.378.65)
- Dapper (ORM)

### Database
- PostgreSQL
- 시계열 데이터를 위한 파티셔닝 지원

### Frontend
- Vanilla JavaScript
- WebSocket
- WebRTC

### 주요 라이브러리
- `BCrypt.Net-Next`: 비밀번호 해싱
- `Npgsql`: PostgreSQL 연결
- `Newtonsoft.Json`: JSON 직렬화

## 프로젝트 구조

```
R.O.S.C.H/
adapter/                # OPC UA 어댑터
 -Interface/
 -OpcUaAdapter.cs     # 실제 OPC UA 통신
 -MockOpcUaAdapter.cs # 테스트용 Mock 어댑터
API/                    # REST API
 -Endpoints/          # API 엔드포인트
 -Service/            # 비즈니스 로직
 -DTO/                # 데이터 전송 객체
Database/
 -Models/             # 데이터베이스 모델
 -Repository/         # 데이터 접근 계층
WS/                     # WebSocket 관리
 -Opc/                # OPC 데이터 WebSocket
 -RTC/                # WebRTC 시그널링
Worker/                 # 백그라운드 작업
 -StatePollingWorker.cs
wwwroot/                # 정적 웹 리소스
Program.cs              # 애플리케이션 진입점
```

## 시작하기

### 사전 요구사항

- .NET 10.0 SDK
- PostgreSQL 12 이상
- OPC UA 서버 (또는 테스트용 Mock 모드)

### 데이터베이스 설정

1. PostgreSQL 데이터베이스 생성

```sql
CREATE DATABASE rosch_db;
```

2. DDL 스크립트 실행

```bash
psql -U postgres -d rosch_db -f ddl.sql
```

### 설정

`appsettings.Development.json` 파일을 생성하여 연결 문자열을 설정합니다:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=rosch_db;Username=postgres;Password=yourpassword"
  },
  "UseMockData": false,
  "OpcUaServerUrl": "opc.tcp://localhost:4840"
}
```

### 실행

```bash
dotnet restore
dotnet build
dotnet run
```

애플리케이션은 `5178`포트에서 실행됩니다.

### 테스트 모드

OPC UA 서버 없이 테스트하려면 `appsettings.Development.json`에서 `UseMockData`를 `true`로 설정합니다.

## API 엔드포인트

### 인증
- `POST /auth/login` - 사용자 로그인
- `POST /auth/register` - 사용자 등록

### 제어
- `POST /control` - 디바이스 제어 명령 전송
- `GET /control/logs` - 제어 이력 조회

## WebSocket 엔드포인트

### OPC 데이터 스트림
- **URL**: `ws://localhost:5178/ws/opc`
- **설명**: 실시간 OPC UA 디바이스 데이터 수신

### WebRTC 시그널링
- **URL**: `ws://localhost:5178/ws/rtc`
- **설명**: WebRTC 연결을 위한 시그널링 서버

## 데이터베이스 스키마

### 주요 테이블

- `user`: 사용자 정보 및 권한 관리
- `device`: 디바이스 정보 (AGV, Conveyor, Robot, Lift)
- `device_tag`: OPC UA 태그 정보
- `opc_data`: 실시간 OPC 데이터 (월별 파티셔닝)
- `control_log`: 제어 명령 이력
- `error_log`: 오류 로그 (월별 파티셔닝)
- `code`: 공통 코드 (오류코드, 디바이스 타입, 제어 명령)

## 지원 디바이스

### ESP32 (ModbusTCP)
- AGV 위치 추적 (PosX, PosY, PosTheta)
- 상태 모니터링
- 제어 명령 전송

### STM32
- Conveyor (LOAD, MAIN, SORT) 속도 제어
- Robot 작업 상태 모니터링
- Lift 층 이동 및 상태 확인

## 개발

### 의존성 설치

```bash
dotnet restore
```

### 빌드

```bash
dotnet build
```

### 코드 구조

- **Repository 패턴**: 데이터 접근 계층 분리
- **Service 패턴**: 비즈니스 로직 계층
- **Dependency Injection**: 자동 Repository 및 Service 등록
- **WebSocket Manager**: 클라이언트 연결 관리 및 브로드캐스트


