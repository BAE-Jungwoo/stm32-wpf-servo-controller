# STM32F429 × WPF UART 서보모터 컨트롤러

Windows WPF 애플리케이션에서 UART 명령을 전송해 STM32F429I-DISCO 보드에 연결된 SG90 서보모터를 제어하는 프로젝트입니다. PC에서 모터의 동작 여부와 속도 단계를 선택하면 STM32 펌웨어가 50Hz PWM을 생성해 서보모터를 0°와 180° 사이에서 왕복시키고, 적용된 상태를 다시 앱으로 전달합니다.


## 주요 기능

- COM 포트 이름을 직접 입력해 STM32 보드와 연결 및 연결 해제
- 서보모터 왕복 운동 시작·정지
- `1~10`단계 속도 조절과 현재 단계의 실시간 표시
- 1바이트 명령과 응답으로 구성된 단순한 UART 프로토콜
- WPF MVVM 구조와 비동기 명령을 이용한 UI·통신 로직 분리
- STM32 UART 수신 인터럽트를 이용한 즉각적인 명령 처리
- TIM4 채널 3의 50Hz PWM을 이용한 `0~180°` 위치 제어
- 실제 하드웨어 없이 앱 로직을 시험할 수 있는 가상 모터 드라이버 포함

## 시스템 동작

```text
WPF UI
  └─ MainViewModel
      └─ UartMotorDriver
          └─ COM Port / USART1
              └─ STM32F429 펌웨어
                  └─ TIM4_CH3 PWM
                      └─ SG90 서보모터
```

앱은 버튼을 누를 때마다 1바이트 명령을 보냅니다. STM32는 UART 수신 인터럽트에서 명령을 처리하고 현재 속도 단계를 1바이트로 응답합니다. 앱은 응답을 `MotorState`로 변환해 화면의 슬라이더와 숫자에 표시합니다.

## 하드웨어 구성

| 구분 | 핀 또는 설정 | 용도 |
| --- | --- | --- |
| MCU | STM32F429ZITx | Cortex-M4F 기반 제어기 |
| UART TX | PA9 / USART1_TX | PC로 상태 응답 전송 |
| UART RX | PA10 / USART1_RX | PC 명령 수신 |
| PWM 출력 | PB8 / TIM4_CH3 | SG90 제어 신호 |
| 상태 LED | PG13 / LD3 | UART 명령 수신 시 토글 |
| UART 설정 | 115200 baud, 8-N-1 | 흐름 제어 없음 |
| PWM 설정 | 50Hz, 20ms 주기 | 1MHz 타이머 카운터 사용 |
| 펄스 폭 | 500~2500µs | 0~180° 선형 변환 |

### SG90 연결

| SG90 선 | 연결 |
| --- | --- |
| Signal | `PB8 (TIM4_CH3)` |
| VCC | 안정적인 외부 5V 전원 권장 |
| GND | 외부 전원과 STM32 보드의 GND를 공통 연결 |

서보모터는 순간적으로 비교적 큰 전류를 사용할 수 있으므로 보드의 GPIO나 3.3V 핀에서 직접 전원을 공급하지 않는 것을 권장합니다.

## 앱 사용법

| UI 요소 | 동작 |
| --- | --- |
| COM 포트 입력 | 연결할 포트 지정(기본값 `COM1`) |
| Connect | 지정한 포트를 열고 제어 버튼 활성화 |
| Disconnect | 시리얼 포트를 닫고 연결 해제 |
| Activate | 현재 속도 단계로 왕복 운동 시작 |
| Deactivate | 현재 위치에서 모터 운동 정지 |
| Accelerate | 속도를 최대 10단계까지 한 단계 증가 |
| Decelerate | 속도를 최소 1단계까지 한 단계 감소 |
| Speed 슬라이더 | STM32가 응답한 현재 상태 표시 전용 |

펌웨어의 초기 각도는 90°이고 기본 속도는 5단계입니다. 모터는 한 번에 2°씩 이동하며 0° 또는 180°에 도달하면 방향을 바꿉니다.

## 속도 단계

속도 단계가 높아질수록 2° 이동 사이의 대기 시간이 짧아집니다.

| 단계 | 이동 간격 | 단계 | 이동 간격 |
| ---: | ---: | ---: | ---: |
| 1 | 100ms | 6 | 19ms |
| 2 | 70ms | 7 | 14ms |
| 3 | 50ms | 8 | 10ms |
| 4 | 36ms | 9 | 7ms |
| 5 | 26ms | 10 | 5ms |

## UART 프로토콜

각 요청과 응답은 모두 단일 바이트입니다. 앱은 명령 전송 후 최대 1초 동안 응답을 기다립니다.

| 명령 | 값 | STM32 동작 | 응답 |
| --- | ---: | --- | ---: |
| Activate | `0x01` | 왕복 운동 시작 | 현재 속도 단계 |
| Deactivate | `0x02` | 모터 운동 정지 | `0` |
| Accelerate | `0x03` | 속도 한 단계 증가 | 변경된 속도 단계 |
| Decelerate | `0x04` | 속도 한 단계 감소 | 변경된 속도 단계 |

정의되지 않은 명령은 무시하며 별도의 응답을 보내지 않습니다.

## 소프트웨어 구조

### WPF 애플리케이션

- `MainWindow.xaml`: 다크 테마 제어 화면과 데이터 바인딩을 정의합니다.
- `MainViewModel`: 연결 상태, 현재 모터 상태, UI 명령을 관리합니다.
- `UartMotorDriver`: 명령 전송과 응답 수신을 비동기로 감싸고 응답을 `MotorState`로 변환합니다.
- `Uart`: `System.IO.Ports.SerialPort`를 이용해 115200 8-N-1 통신을 수행합니다.
- `IMotorDriver`: 실제 UART와 가상 드라이버가 공유하는 제어 인터페이스입니다.
- `FakeMotorDriver`: 하드웨어 없이 지연과 상태 응답을 흉내 내는 테스트용 구현입니다.

### STM32 펌웨어

- 시작 시 TIM4 PWM을 활성화하고 서보모터를 90°로 설정합니다.
- USART1은 1바이트 인터럽트 수신을 사용하며 콜백이 끝날 때 다음 수신을 다시 활성화합니다.
- 메인 루프는 모터가 활성화된 동안 현재 각도를 2°씩 갱신합니다.
- 각도는 `500 + angle × 2000 / 180` 계산식으로 PWM 펄스 폭으로 변환됩니다.
- 모터가 정지한 동안에는 50ms 대기해 불필요한 CPU 사용을 줄입니다.

## 개발 환경

- Windows 10/11
- .NET 10 / WPF
- CommunityToolkit.Mvvm 8.4.2
- System.IO.Ports 10.0.10
- STM32CubeIDE
- STM32Cube FW_F4 V1.27.1
- STM32 HAL Driver

## 빌드 및 실행

### 1. STM32 펌웨어

1. STM32CubeIDE에서 `WPF_Project` 디렉터리를 기존 프로젝트로 가져옵니다.
2. SG90 신호선과 외부 전원을 위의 하드웨어 구성에 맞게 연결합니다.
3. 프로젝트를 빌드하고 STM32F429I-DISCO 보드에 다운로드합니다.
4. 장치 관리자에서 ST-LINK Virtual COM Port의 포트 번호를 확인합니다.

### 2. WPF 애플리케이션

저장소 루트에서 다음 명령을 실행합니다.

```powershell
cd MotorController\MotorController
dotnet restore
dotnet run
```

앱이 실행되면 확인한 COM 포트(예: `COM3`)를 입력하고 **Connect**를 누릅니다. 연결이 완료된 뒤 Activate, Deactivate, Accelerate, Decelerate 버튼으로 모터를 제어할 수 있습니다.

## 프로젝트 구조

```text
MotorController/
├── MotorController.slnx        WPF 솔루션
└── MotorController/            MVVM 앱, UART 드라이버, XAML UI

WPF_Project/
├── Core/                       STM32 초기화 및 서보 제어 코드
├── Drivers/                    CMSIS와 STM32F4 HAL 드라이버
├── WPF_Project.ioc             STM32CubeMX 설정
├── sg90_datasheet.pdf          SG90 데이터시트
└── IMG_3052.jpg                하드웨어 구성 사진
```

STM32 HAL과 CMSIS에는 각 디렉터리에 포함된 원 라이선스가 적용됩니다.
