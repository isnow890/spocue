<p align="center">
  <img src="문서/이미지/아이콘.png" width="116" height="116" alt="SpoCue 앱 아이콘" />
</p>

<h1 align="center">SpoCue</h1>

<p align="center">
  <strong>어느 창에서나 Spotify를 제어하는 전역 단축키, 앨범 커버 색상 기반 곡 알림 토스트, F10–F12 볼륨 키를 하나로 묶은 경량 네이티브 Windows 트레이 앱</strong>
</p>

<p align="center">
  <a href="https://github.com/isnow890/spocue/actions/workflows/build.yml"><img src="https://img.shields.io/github/actions/workflow/status/isnow890/spocue/build.yml?label=build&style=flat-square" alt="CI Build" /></a>
  <a href="https://github.com/isnow890/spocue/releases/latest"><img src="https://img.shields.io/github/v/release/isnow890/spocue?style=flat-square" alt="Latest Release" /></a>
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011%20(x64)-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Windows 10 / 11" />
  <img src="https://img.shields.io/badge/.NET-10%20WPF-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10 WPF" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue?style=flat-square" alt="MIT License" /></a>
</p>

<p align="center">
  <a href="README.md">English</a> · <strong>한국어</strong>
</p>

---

## 소개

**SpoCue**는 과거 많은 사랑을 받았던 **Toastify**의 빠르고 직관적인 워크플로를 최신 Windows 10/11과 현행 Spotify 데스크톱 앱 환경에 맞게 처음부터 재설계한 **외부 의존성 0의 순수 C# / .NET 10 WPF 네이티브 트레이 유틸리티**입니다. 무거운 Electron 런타임이나 백그라운드 서비스, 번거로운 Spotify Web API 개발자 키·OAuth 로그인 설정 없이 즉시 동작합니다.

- **어느 창에서나 동작하는 전역 Spotify 단축키**: 코딩, 문서 작업, 전체 화면 게임 중에도 창을 전환하지 않고 **재생 / 일시정지**, **다음 곡**, **이전 곡**, **현재 곡 토스트 표시**를 바로 실행합니다. Win32 `RegisterHotKey`(`MOD_NOREPEAT`)와 **2단계 원자적(Atomic) 등록 엔진**을 사용하여, 두 기능의 단축키를 서로 맞바꾸거나 타 프로그램과 충돌이 발생해도 기존 단축키가 풀리지 않고 안전하게 유지됩니다.
- **앨범 커버 색상 연동 다이내믹 토스트 알림**:
  - 현재 재생 중인 곡의 앨범 커버에서 대표 색상을 실시간 추출(`ArtworkPalette`)하여 명도 대비가 검증된 배경·테두리·강조색을 생성하며, Windows **고대비(High Contrast)** 모드에서는 시스템 색상으로 자동 전환됩니다.
  - 곡이 바뀔 때 현재 작업 중인 창의 키보드 포커스를 빼앗지 않고(`WS_EX_NOACTIVATE`) 주 모니터 우측 하단에 4초간 깔끔하게 표시됩니다.
  - **원클릭 Spotify 창 호출**: 토스트 알림이나 설정창의 현재 곡 카드를 클릭하면 백그라운드의 Spotify 데스크톱 창을 즉시 앞으로 가져옵니다(`SpotifyWindow`).
- **계정·API 키가 필요 없는 Windows SMTC 직접 연동**: Windows 표준 미디어 세션(`GlobalSystemMediaTransportControlsSessionManager`)을 통해 일반 설치형(`Spotify.exe`) 및 Microsoft Store 버전 Spotify 데스크톱 앱과 직접 통신합니다. 외부 네트워크 호출이 전혀 없습니다.
- **macOS 스타일 `F10`–`F12` 시스템 볼륨 키**: 저수준 키보드 훅(`WH_KEYBOARD_LL`)을 통해 단독 `F10`(음소거), `F11`(볼륨 낮추기), `F12`(볼륨 높이기) 입력 시 시스템 오디오 볼륨을 조절하며, `Ctrl + Shift + F10` 같은 조합 키는 가로채지 않고 정상 통과시킵니다.
- **Windows 시작 시 자동 실행 & 한국어/영어 실시간 전환**: 로그인 시 트레이에서 조용히 시작하는 자동 실행 옵션(`--startup`)과 Windows 표시 언어 자동 감지(한국어 / English 즉시 전환)를 기본 제공합니다.

<p align="center">
  <img src="문서/이미지/토스트-알림.png" width="320" alt="SpoCue 앨범 색상 기반 토스트 알림" />
</p>

<p align="center">
  <img src="문서/이미지/설정-화면.png" width="460" alt="SpoCue 설정 화면" />
</p>

### 한눈에 비교

| 기능 | SpoCue | 기존 Toastify | Windows 기본 미디어 오버레이 |
| :--- | :---: | :---: | :---: |
| 최신 Spotify 데스크톱(일반 설치형 & Store 버전 SMTC) 호환 | ✅ | ⚠️ (최신 버전 호환 중단) | ✅ |
| 미디어 전용 키가 없는 키보드에서 사용자 지정 전역 단축키 | ✅ | ✅ | — |
| 단축키 맞바꾸기 및 충돌 시 원자적 롤백(Atomic Rollback) | ✅ | — | — |
| 포커스를 빼앗지 않는 앨범 아트 색상 연동 토스트 | ✅ | — | — |
| 토스트 클릭 시 Spotify 데스크톱 창 즉시 앞으로 열기 | ✅ | ✅ | — |
| macOS 스타일 `F10` / `F11` / `F12` 시스템 볼륨 키 옵션 | ✅ | — | — |
| Spotify API 키 불필요, 서드파티 NuGet 의존성 0, MIT 오픈소스 | ✅ | ✅ (MIT) | 비공개 소스 |

---

## 설치 방법

### 방법 1: Windows 설치 프로그램 (`Setup.exe` — 권장)

1. [Releases](https://github.com/isnow890/spocue/releases/latest) 페이지에서 `spocue-vX.Y.Z-win-x64-setup.exe`를 다운로드합니다.
2. 설치 프로그램을 실행하면 관리자 권한(UAC) 요청 없이 `%LOCALAPPDATA%\Programs\SpoCue`에 깔끔하게 설치되며, 시작 메뉴(및 선택 시 바탕화면) 바로가기와 Windows *설치된 앱* 제거 항목이 자동으로 등록됩니다.
3. 설치 마지막 단계에서 바로 **SpoCue**를 실행하거나 시작 메뉴에서 실행합니다.

### 방법 2: 무설치 포터블 압축 파일 (`.zip`)

1. [Releases](https://github.com/isnow890/spocue/releases/latest) 페이지에서 `spocue-vX.Y.Z-win-x64.zip`을 다운로드합니다.
2. 원하는 고정 폴더에 압축을 모두 풀고 `SpoCue.exe`를 실행합니다. 설치 프로그램과 포터블 ZIP 모두 .NET 10 런타임이 내장(Self-contained)되어 있어 별도 런타임 설치가 필요 없습니다.

### 방법 3: 소스에서 직접 빌드

Windows 10 버전 2004 이상(빌드 19041) 또는 Windows 11 및 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)가 필요합니다.

```powershell
git clone https://github.com/isnow890/spocue.git
cd spocue

dotnet build TrackRelay.slnx -c Release
dotnet run --project src/TrackRelay/TrackRelay.csproj -- --settings
```

배포용 설치 프로그램(`Setup.exe`), 포터블 `.zip`, 체크섬(`SHA256SUMS.txt`) 일괄 패키징 (Inno Setup 6 필요):

```powershell
powershell -ExecutionPolicy Bypass -File 배포/패키지-빌드.ps1 -Version 0.1.0
```

---

## 첫 실행 및 단축키 안내

처음 실행하면 **SpoCue** 설정 창이 자동으로 열리고 작업 표시줄 트레이 영역(숨겨진 아이콘 `^` 포함)에 파형 아이콘이 등록됩니다. 설정 창을 닫아도 SpoCue는 트레이에서 계속 실행되며, 트레이 아이콘을 더블 클릭하거나 우클릭 메뉴에서 **설정**을 눌러 언제든 다시 열 수 있습니다.

| 동작 | 기본 단축키 | 사용자 지정 범위 |
| :--- | :--- | :--- |
| **재생 / 일시정지** | `Ctrl + Shift + F8` | 조합 키(`Ctrl+Alt`, `Ctrl+Shift`, `Alt+Shift`, `Ctrl+Alt+Shift`) + `F1`~`F12`, `A`~`Z`, `0`~`9`, `Space`, 방향키, `Home`/`End`, `PageUp`/`PageDown` 또는 `끄기` |
| **다음 곡** | `Ctrl + Shift + F9` | 위와 동일한 조합 키 + 일반 키 선택 또는 `끄기` |
| **이전 곡** | `Ctrl + Shift + F7` | 위와 동일한 조합 키 + 일반 키 선택 또는 `끄기` |
| **현재 곡 표시** | `Ctrl + Shift + F10` | 자동 곡 변경 알림을 꺼 두었을 때도 원할 때 즉시 4초간 현재 곡 토스트 표시 |
| **시스템 볼륨 키** | `F10` · `F11` · `F12` | 설정에서 *F10–F12로 시스템 볼륨 조절* 활성화 시 동작 (`F10` 음소거 · `F11` 볼륨 낮추기 · `F12` 볼륨 높이기). `Ctrl + Shift + F10` 등 조합 키는 그대로 정상 동작 |

---

## 실행 인자(CLI) 및 로컬 저장 경로

필요에 따라 명령줄 인자를 지정해 실행할 수 있습니다:

```powershell
# 실행 시 설정 창을 즉시 표시 (이미 실행 중이면 중복 실행 없이 안전하게 종료)
SpoCue.exe --settings

# 설정 창을 띄우지 않고 트레이에서 조용히 시작 ('Windows 시작 시 실행' 레지스트리가 사용하는 인자)
SpoCue.exe --startup
```

- **환경설정 파일**: `%LOCALAPPDATA%\SpoCue\settings.json`
- **자동 시작 레지스트리**: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (`SpoCue`)
- **단일 인스턴스 뮤텍스**: `Local\TrackRelay`

---

## 개인정보, 서명 및 알려진 한계

1. **100% 오프라인 동작 & 텔레메트리 0**: SpoCue에는 외부 네트워크 통신 코드가 전혀 없습니다. 재생 중인 곡 제목·아티스트·144px로 디코딩된 앨범 커버는 메모리에만 유지되며, 디스크에는 사용자가 지정한 단축키와 옵션(`%LOCALAPPDATA%\SpoCue\settings.json`)만 저장됩니다.
2. **코드 서명 안내 (Windows SmartScreen)**: 릴리스 바이너리는 유료 EV 인증서 없이 GitHub Actions(`windows-latest`)에서 투명하게 빌드됩니다. 최초 실행 시 Windows SmartScreen에서 *"Windows의 PC 보호"* 창이 뜨면 **추가 정보 → 실행**을 선택하고, 릴리스 페이지의 `SHA256SUMS.txt`로 해시를 검증할 수 있습니다.
3. **Spotify 데스크톱 앱 전용 (웹 플레이어 미지원)**: 유튜브 등 브라우저 탭이 음악 단축키를 가로채지 않도록 Windows 미디어 세션 중 네이티브 Spotify 데스크톱 앱(`Spotify.exe` 및 Microsoft Store 버전)만 정확히 필터링합니다. 브라우저 기반 Spotify 웹 플레이어는 대상이 아닙니다.
4. **관리자 권한 창 활성 시 볼륨 훅 제한**: SpoCue는 일반 사용자 권한(`PrivilegesRequired=lowest`)으로 동작하므로, 관리자 권한으로 실행된 프로그램이 포커스를 가진 동안에는 Windows UIPI 보안 정책에 의해 `WH_KEYBOARD_LL` 기반 `F10`–`F12` 볼륨 키 훅이 전달되지 않을 수 있습니다.
5. **자동 시작 경로 고정**: *Windows 시작 시 실행*을 켜면 그 시점에 실행 중인 `SpoCue.exe`의 절대 경로가 레지스트리에 등록됩니다. 포터블 ZIP 버전을 다른 폴더로 옮겼다면 설정에서 자동 시작을 한 번 껐다가 다시 켜 주세요 (`Setup.exe` 설치본은 `%LOCALAPPDATA%\Programs\SpoCue` 고정 경로를 사용하므로 신경 쓰지 않아도 됩니다).

<details>
<summary><strong>문제 해결 / FAQ</strong></summary>

- **Spotify가 켜져 있는데도 *"Spotify 대기 중"*으로 표시될 때**: Spotify 데스크톱 앱에서 아무 곡이나 한 번 재생하면 Windows SMTC 미디어 세션이 활성화되며 자동으로 `"Spotify 연결됨"`으로 전환됩니다.
- **단축키 적용 시 *"사용할 수 없습니다"* 오류가 뜰 때**: Windows `RegisterHotKey`는 시스템 전체에서 선점형으로 동작합니다. 다른 백그라운드 앱(GeForce Experience, PowerToys, 화면 캡처 도구 등)이 이미 해당 키 조합을 사용 중이므로 다른 키 조합을 선택하거나 충돌 앱의 단축키를 해제해 주세요.
- **곡이 바뀌어도 토스트가 뜨지 않을 때**: 설정에서 *"곡이 바뀌면 알림 표시"*가 체크되어 있는지 확인하세요. 앱 실행 직후 처음 감지된 곡은 기준점(Baseline)으로만 잡히고 이후 곡이 바뀔 때부터 토스트가 표시됩니다. **알림 미리 보기** 버튼이나 `Ctrl + Shift + F10`으로 즉시 테스트할 수 있습니다.
- **토스트를 클릭해도 Spotify 창이 안 올라올 때**: Spotify가 트레이로 완전히 숨겨져 있거나 종료된 상태라면 Spotify 메인 창을 한 번 열어 두세요. SpoCue는 `Spotify.exe`의 최상위 창 핸들을 찾아 `ShowWindow` 및 `SetForegroundWindow`로 활성화합니다.

</details>

---

## 프로젝트 구조

```text
src/TrackRelay/
├── App.xaml(.cs)             # 단일 인스턴스 뮤텍스, 트레이 NotifyIcon, Fluent 다크 테마 & 고대비 모드 동기화
├── SpotifySession.cs         # Windows SMTC 세션 탐색, 재생 제어 명령 및 144px 앨범 커버 스트림 디코더
├── SpotifyWindow.cs          # Win32 최상위 창 열거 및 Spotify.exe 전면 활성화(Foreground)
├── GlobalHotkeys.cs          # Win32 RegisterHotKey 전역 단축키 관리 및 2단계 원자적 적용·롤백
├── VolumeKeys.cs             # 선택적 WH_KEYBOARD_LL 키보드 훅 & SendInput 기반 F10 / F11 / F12 볼륨 제어
├── ArtworkPalette.cs         # 앨범 커버 픽셀 샘플링 및 명도 대비 기반 배경·강조색 생성 엔진
├── ArtworkBadge.xaml(.cs)    # 앨범 커버 썸네일 / 기본 파형 아이콘 폴백 배지 컨트롤
├── TrackToast.xaml(.cs)      # 포커스를 빼앗지 않는(WS_EX_NOACTIVATE) 4초 앨범 커버 토스트 알림 창
├── SettingsWindow.xaml(.cs)  # 실시간 현재 곡 카드, 단축키 편집 그리드 및 환경설정 UI
├── StartupRegistration.cs    # 현재 사용자 HKCU\...\Run 자동 시작 레지스트리 관리 및 저장 실패 시 롤백
├── Preferences.cs            # 서드파티 의존성 없는 %LOCALAPPDATA%\SpoCue\settings.json 직렬화
├── Strings.cs                # 한국어 / English 내장 리소스 사전 및 실시간 UI 언어 갱신
└── Theme.xaml                # 다크 블루그레이 & 앰버 기본 팔레트 및 카드/버튼 스타일 정의
```

---

## 라이선스

[MIT License](LICENSE)로 배포됩니다. 아이디어는 고전 유틸리티인 [Toastify](https://github.com/nachmore/toastify)에서 영감을 얻었으며, SpoCue는 처음부터 새롭게 작성된 독립 오픈소스 프로젝트입니다.
