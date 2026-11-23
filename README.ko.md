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

**SpoCue**는 과거 많은 사랑을 받았던 **Toastify**의 빠르고 직관적인 사용성을 최신 Windows 10/11과 현행 Spotify 데스크톱 앱 환경에 맞게 재설계한 **순수 C# / WPF 네이티브 트레이 유틸리티**입니다. 무거운 Electron 런타임이나 별도의 Spotify 개발자 API 키·로그인 설정 없이 즉시 동작합니다.

- **어느 화면에서나 동작하는 Spotify 전역 단축키**: 코딩, 문서 작업, 전체 화면 게임 중에도 창을 전환하지 않고 **재생 / 일시정지**, **다음 곡**, **이전 곡**, **현재 곡 토스트 표시**를 바로 실행할 수 있습니다. 각 단축키는 자유롭게 변경하거나 개별적으로 끌 수 있습니다.
- **앨범 커버 색상 기반 다이내믹 토스트 알림**:
  - 재생 중인 곡의 앨범 커버에서 대표 색상을 실시간으로 추출(`ArtworkPalette`)하여 토스트 배경과 설정창 미리보기 카드의 색조를 자연스럽게 맞춥니다.
  - 곡이 바뀔 때 현재 작업 중인 창의 포커스를 빼앗지 않고 주 모니터 우측 하단에 4초간 깔끔하게 표시됩니다.
  - **클릭하여 Spotify 바로 열기**: 토스트 알림이나 설정창의 현재 곡 카드를 클릭하면 백그라운드의 Spotify 데스크톱 창을 즉시 앞으로 가져옵니다.
- **로그인·API 키가 필요 없는 네이티브 연동**: Windows 표준 미디어 세션(`GlobalSystemMediaTransportControlsSessionManager`)을 통해 Spotify 데스크톱 앱과 직접 통신합니다. 외부 네트워크 호출이나 계정 연동이 전혀 없습니다.
- **macOS 스타일 `F10`–`F12` 시스템 볼륨 조절**: 미디어 키가 없는 키보드에서도 `F10`(음소거), `F11`(볼륨 낮추기), `F12`(볼륨 높이기)로 Windows 볼륨을 바로 조절할 수 있으며, `Ctrl + Shift + F10` 같은 조합 키는 그대로 정상 동작합니다.
- **Windows 시작 시 자동 실행 & 한국어/영어 지원**: 로그인 시 트레이에서 조용히 시작하는 자동 실행 옵션과 시스템 언어 자동 감지(한국어 / English 수동 전환 포함)를 기본 제공합니다.

<p align="center">
  <img src="문서/이미지/토스트-알림.png" width="320" alt="SpoCue 앨범 색상 기반 토스트 알림" />
</p>

<p align="center">
  <img src="문서/이미지/설정-화면.png" width="460" alt="SpoCue 설정 화면" />
</p>

### 한눈에 비교

| 기능 | SpoCue | 기존 Toastify | Windows 기본 미디어 오버레이 |
| :--- | :---: | :---: | :---: |
| 최신 Spotify 데스크톱(Windows 10/11 SMTC) 완벽 호환 | ✅ | ⚠️ (최신 버전 호환 중단) | ✅ |
| 미디어 전용 키가 없는 키보드에서 사용자 지정 전역 단축키 | ✅ | ✅ | — |
| 포커스를 빼앗지 않는 앨범 아트 색상 연동 토스트 | ✅ | — | — |
| 토스트 클릭 시 Spotify 데스크톱 창 즉시 앞으로 열기 | ✅ | ✅ | — |
| macOS 스타일 `F10` / `F11` / `F12` 시스템 볼륨 키 옵션 | ✅ | — | — |
| Spotify API 키 불필요, 외부 통신 0, MIT 오픈소스 | ✅ | ✅ | 비공개 소스 |

---

## 설치 방법

### 방법 1: Windows 설치 프로그램 (`Setup.exe` — 권장)

1. [GitHub Releases](https://github.com/isnow890/spocue/releases/latest)에서 `spocue-vX.Y.Z-win-x64-setup.exe`를 다운로드합니다.
2. 설치 프로그램을 실행하면 관리자 권한 요청 없이 `%LOCALAPPDATA%\Programs\SpoCue`에 깔끔하게 설치되며, 시작 메뉴(및 선택 시 바탕화면) 바로가기와 Windows '설치된 앱' 제거 항목이 자동으로 등록됩니다.
3. 설치 완료 화면에서 바로 **SpoCue**를 실행하거나 시작 메뉴에서 실행합니다.

### 방법 2: 무설치 포터블 ZIP (`win-x64.zip`)

1. [GitHub Releases](https://github.com/isnow890/spocue/releases/latest)에서 `spocue-vX.Y.Z-win-x64.zip`을 다운로드합니다.
2. 원하는 폴더에 압축을 모두 풀고 `SpoCue.exe`를 실행합니다. 설치 프로그램과 포터블 ZIP 모두 .NET 런타임이 내장(Self-contained)되어 있어 별도의 런타임 설치 없이 바로 실행됩니다.

### 방법 3: 소스에서 직접 빌드

Windows 10 버전 2004 이상(또는 Windows 11)과 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)가 필요합니다.

```powershell
git clone https://github.com/isnow890/spocue.git
cd spocue

dotnet build TrackRelay.slnx -c Release
dotnet run --project src/TrackRelay/TrackRelay.csproj -- --settings
```

배포용 설치 프로그램(`Setup.exe`), 포터블 `.zip`, 체크섬(`SHA256SUMS.txt`) 일괄 생성:

```powershell
powershell -ExecutionPolicy Bypass -File 배포/패키지-빌드.ps1 -Version 0.1.0
```

---

## 기본 단축키 및 환경설정

| 동작 | 기본 단축키 | 설명 |
| :--- | :--- | :--- |
| **재생 / 일시정지** | `Ctrl + Shift + F8` | 조합 키(`Ctrl+Alt`, `Ctrl+Shift`, `Alt+Shift`, `Ctrl+Alt+Shift`) + 일반 키 변경 또는 `끄기` 가능 |
| **다음 곡** | `Ctrl + Shift + F9` | Spotify 데스크톱에서 다음 트랙으로 이동 |
| **이전 곡** | `Ctrl + Shift + F7` | Spotify 데스크톱에서 이전 트랙으로 이동 |
| **현재 곡 표시** | `Ctrl + Shift + F10` | 원할 때 언제든 현재 재생 중인 곡 토스트를 4초간 표시 |
| **시스템 볼륨 조절 (선택)** | `F10` / `F11` / `F12` | 설정에서 활성화 시 `F10` 음소거 · `F11` 볼륨 낮추기 · `F12` 볼륨 높이기 |

- **Windows 시작 시 실행**: 켜 두면 Windows 로그인 시 트레이 상태에서 조용히 자동 시작(`--startup`)됩니다.
- **언어 선택**: 기본값은 `시스템 설정 따르기`(한국어 Windows에서는 한국어, 그 외에는 English)이며, 언제든 **한국어** 또는 **English**로 직접 고정할 수 있습니다.
- **로컬 설정 저장**: 모든 설정은 `%LOCALAPPDATA%\SpoCue\settings.json`에만 저장되며 외부 서버로 어떠한 데이터도 전송하지 않습니다.

---

## 프로젝트 구조

```text
src/TrackRelay/
├── App.xaml(.cs)             # 단일 인스턴스 트레이 진입점, 테마 및 고대비 모드 동기화
├── SpotifySession.cs         # Windows SMTC 미디어 세션 감지, 재생 제어 및 앨범 커버 로드
├── SpotifyWindow.cs          # Spotify.exe 창 핸들 탐색 및 전면 활성화(Foreground)
├── GlobalHotkeys.cs          # Win32 RegisterHotKey 전역 단축키 등록 및 충돌 시 원자적 롤백
├── VolumeKeys.cs             # 선택적 WH_KEYBOARD_LL 저수준 훅 기반 F10 / F11 / F12 볼륨 제어
├── ArtworkPalette.cs         # 앨범 커버 색상 추출 및 명도 대비 기반 배경·강조색 생성
├── TrackToast.xaml(.cs)      # 포커스를 빼앗지 않는 4초 앨범 커버 토스트 알림 창
├── SettingsWindow.xaml(.cs)  # 실시간 현재 곡 카드, 단축키 편집 및 환경설정 UI
├── StartupRegistration.cs    # 현재 사용자 Windows 시작프로그램(HKCU Run) 레지스트리 관리
├── Preferences.cs            # 로컬 JSON 설정 저장 및 로드 (%LOCALAPPDATA%\SpoCue\settings.json)
└── Strings.cs                # 한국어 / English 다국어 리소스 사전
```

---

## 라이선스

[MIT License](LICENSE)로 배포됩니다. 아이디어는 고전 유틸리티인 [Toastify](https://github.com/nachmore/toastify)에서 영감을 얻었으며, SpoCue는 처음부터 새롭게 작성된 독립 오픈소스 프로젝트입니다.
