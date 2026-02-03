-- 개발용 DDL 문
    
-- ============================================
-- 1. 사용자 관리
-- ============================================
DROP TABLE IF EXISTS "user" cascade ;
CREATE TABLE "user" (
                        user_id SERIAL PRIMARY KEY,
                        username VARCHAR(50) UNIQUE NOT NULL,
                        password VARCHAR(255) NOT NULL,
                        role VARCHAR(20) NOT NULL CHECK (role IN ('ADMIN', 'USER')),
                        is_active BOOLEAN DEFAULT true,
                        created_at TIMESTAMPTZ DEFAULT NOW(),
                        updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_user_username ON "user"(username);
CREATE INDEX idx_user_role ON "user"(role);

COMMENT ON TABLE "user" IS '사용자 정보';
COMMENT ON COLUMN "user".role IS 'ADMIN: 관리자, USER: 일반 사용자';

-- ============================================
-- 2. 디바이스 정보
-- ============================================
DROP TABLE IF EXISTS "device" cascade ;
CREATE TABLE device (
                        device_id SERIAL PRIMARY KEY,
                        device_code VARCHAR(50) NOT NULL,  -- 'ESP32_01', 'STM32'
                        device_type VARCHAR(50),                  -- 'AGV', 'CONVEYOR', 'ROBOT', 'LIFT'
                        device_alias VARCHAR(50) UNIQUE NOT NULL ,       -- device 구분하기 위한 이름
                        created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_device_code ON device(device_code);

COMMENT ON TABLE device IS '디바이스 정보';

INSERT INTO device (device_code, device_type, device_alias) values ('ESP32_01', 'AGV', 'AGV');
INSERT INTO device (device_code, device_type, device_alias) values ('STM32', 'CONVEYOR', 'LOAD');
INSERT INTO device (device_code, device_type, device_alias) values ('STM32', 'CONVEYOR', 'MAIN');
INSERT INTO device (device_code, device_type, device_alias) values ('STM32', 'CONVEYOR', 'SORT');
INSERT INTO device (device_code, device_type, device_alias) values ('STM32', 'ROBOT', 'ROBOT');
INSERT INTO device (device_code, device_type, device_alias) values ('STM32', 'LIFT', 'LIFT');

-- ============================================
-- 2. 태그 정보
-- ============================================
DROP TABLE IF EXISTS "device_tag" cascade ;
CREATE TABLE device_tag (
                            tag_id SERIAL PRIMARY KEY,
                            device_id INT REFERENCES device(device_id),
                            channel VARCHAR(50) NOT NULL,
                            device_name VARCHAR(50) NOT NULL,
                            tag VARCHAR(50) NOT NULL,
                            access_type VARCHAR(50) NOT NULL CHECK (access_type IN ('Read', 'Write', 'ReadWrite')),
                            data_type VARCHAR(20),         -- 'Int32', 'Float', 'Boolean', 'String' 등
                            description TEXT,                           -- 태그 설명
                            is_active BOOLEAN DEFAULT true,             -- 사용여부
                            created_at TIMESTAMPTZ DEFAULT NOW(),
    
    CONSTRAINT uq_channel_device_tag UNIQUE (channel, device_name, tag)
);

CREATE INDEX idx_device_tag_channel ON device_tag(channel);
CREATE INDEX idx_device_tag_channel_name ON device_tag(channel, device_name);
CREATE INDEX idx_device_tag_active ON device_tag(channel, device_name, is_active);
CREATE INDEX idx_device_tag_type ON device_tag(access_type);

COMMENT ON TABLE device_tag IS 'OPC-UA태그';
-- ============================================
-- 4. 공통코드 (오류코드 + 운영코드)
-- ============================================
DROP TABLE IF EXISTS "code" cascade ;
CREATE TABLE code (
                      code_id SERIAL PRIMARY KEY,
                      type VARCHAR(50) NOT NULL,  -- 'ERROR', 'DEVICE_TYPE', 'CONTROL_CMD'
                      code VARCHAR(50) NOT NULL UNIQUE,
                      code_name VARCHAR(100) NOT NULL,
                      description TEXT,
                      is_active BOOLEAN DEFAULT true, -- 사용여부
                      created_at TIMESTAMPTZ DEFAULT NOW(),
                      updated_at TIMESTAMPTZ,
                      UNIQUE(type, code)
);

CREATE INDEX idx_code_group ON code(type, is_active);

COMMENT ON TABLE code IS '공통코드 (오류코드, 디바이스타입, 제어명령 등)';

-- 공통코드 기본 데이터 기본 세팅
INSERT INTO code (type, code, code_name, description) VALUES
-- 오류코드
('ERROR', 'E001', '통신 오류', 'OPC UA 통신 오류'),
('ERROR', 'E002', '센서 오류', '센서 값 읽기 실패'),
('ERROR', 'E003', '제어 오류', '제어 명령 실패'),
('ERROR', 'E004', '시스템 오류', '시스템 내부 오류'),
-- 디바이스 타입
('DEVICE_TYPE', 'D001', 'AGV', '무인운반차'),
('DEVICE_TYPE', 'D002', 'Conveyor', '컨베이어'),
('DEVICE_TYPE', 'D003', 'Robot', '로봇'),
('DEVICE_TYPE', 'D004', 'Lift', '리프트'),
-- 제어 명령 타입
('CONTROL_CMD', 'C001', '속도 제어', '컨베이어 속도 변경'),
('CONTROL_CMD', 'C002', '상태 제어', '디바이스 상태 변경'),

-- 제어 상세 명령(C002)의 상세명령
('CONTROL_DTL', 'CD001', '동작', '동작'),
('CONTROL_DTL', 'CD002', '정지', '정지'),
('CONTROL_DTL', 'CD003', '긴급 정지', '긴급 정지');

-- ============================================
-- 5. OPC 실시간 데이터 (시계열 데이터)
-- ============================================
DROP TABLE IF EXISTS "opc_data" cascade ;
CREATE TABLE opc_data (
                          data_id BIGSERIAL ,
                          device_id INT REFERENCES device(device_id),
                          tag_name VARCHAR(100) NOT NULL,
                          tag_value TEXT NOT NULL,
                          data_type VARCHAR(20),  -- 'INT', 'FLOAT', 'BOOL', 'STRING'
                          source_time TIMESTAMPTZ,
                          created_at TIMESTAMPTZ DEFAULT NOW(),
                          PRIMARY KEY (data_id, created_at)
) PARTITION BY RANGE (created_at);

CREATE INDEX idx_opc_data_device ON opc_data(device_id, created_at DESC);
CREATE INDEX idx_opc_data_tag ON opc_data(tag_name, created_at DESC);
CREATE INDEX idx_opc_data_time ON opc_data(created_at DESC);

COMMENT ON TABLE opc_data IS 'OPC 실시간 데이터 (시계열, 파티셔닝)';
COMMENT ON COLUMN opc_data.source_time IS '실제 시간(DB처리에 시간이 걸리는 경우 대비)';

-- 파티션 생성 (월별)
CREATE TABLE opc_data_2026_01 PARTITION OF opc_data
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE opc_data_2026_02 PARTITION OF opc_data
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');


-- ============================================
-- 6. 제어 명령 이력
-- ============================================
DROP TABLE IF EXISTS "control_log" cascade ;
CREATE TABLE control_log (
                             log_id BIGSERIAL,
                             user_id INT REFERENCES "user"(user_id),
                             device_id INT REFERENCES device(device_id),
                             control_type VARCHAR(50) NOT NULL,  -- code.group_code='CONTROL_CMD' 참조
                             tag_name VARCHAR(100) NOT NULL,
                             old_value TEXT,
                             new_value TEXT NOT NULL,
                             created_at TIMESTAMPTZ DEFAULT NOW(),
                             PRIMARY KEY (log_id, created_at)
);

CREATE INDEX idx_control_user ON control_log(user_id, created_at DESC);
CREATE INDEX idx_control_device ON control_log(device_id, created_at DESC);
CREATE INDEX idx_control_time ON control_log(created_at DESC);

COMMENT ON TABLE control_log IS '제어 명령 이력';

-- ============================================
-- 7. 통합 오류 이력
-- ============================================
DROP TABLE IF EXISTS "error_log" cascade ;
CREATE TABLE error_log (
                           error_id BIGSERIAL,
                           error_code VARCHAR(50),
                           error_source VARCHAR(100),
                           error_msg TEXT NOT NULL,
                           stack_trace TEXT,
                           user_id INT REFERENCES "user"(user_id),
                           device_id INT REFERENCES device(device_id),
                           created_at TIMESTAMPTZ DEFAULT NOW(),
                           PRIMARY KEY (error_id, created_at)
) PARTITION BY RANGE (created_at);

CREATE INDEX idx_error_source ON error_log(error_source, created_at DESC);
CREATE INDEX idx_error_device ON error_log(device_id, created_at DESC);
CREATE INDEX idx_error_code ON error_log(error_code, created_at DESC);

COMMENT ON TABLE error_log IS '통합 오류 이력';
COMMENT ON COLUMN error_log.error_code IS '오류 코드 (code.group_code)';
COMMENT ON COLUMN error_log.error_source IS '오류 발생 위치';

-- 파티션 생성 (월별)
CREATE TABLE error_log_2026_01 PARTITION OF error_log
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE error_log_2026_02 PARTITION OF error_log
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');


