using System.Globalization;
using System.Windows;

namespace TrackRelay;

internal enum AppLanguage
{
    System,
    Korean,
    English
}

internal static class Strings
{
    private static readonly bool SystemUsesKorean = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ko";
    private static ResourceDictionary? _current;
    private static readonly (string Key, string English, string Korean)[] Messages =
    [
        ("SettingsTitle", "SpoCue Settings", "SpoCue 설정"),
        ("NotificationTitle", "SpoCue notification", "SpoCue 알림"),
        ("PreviewToast", "Preview toast", "알림 미리 보기"),
        ("ShortcutsHeading", "Global shortcuts", "전역 단축키"),
        ("ShortcutsHint", "Uncheck any shortcut you don’t want to use.", "사용하지 않을 단축키는 체크를 해제하세요."),
        ("PreferencesHeading", "Preferences", "환경설정"),
        ("NotifyOnChange", "Show a toast when the track changes", "곡이 바뀌면 알림 표시"),
        ("StartWithWindows", "Start with Windows", "Windows 시작 시 실행"),
        ("StartupSettings", "Open Windows startup settings", "Windows 시작 앱 설정 열기"),
        ("CheckUpdatesLabel", "Check for updates automatically", "자동으로 업데이트 확인"),
        ("ToastPositionLabel", "Toast position", "알림 표시 위치"),
        ("CornerBottomRight", "Bottom right", "우측 하단"),
        ("CornerTopRight", "Top right", "우측 상단"),
        ("CornerBottomLeft", "Bottom left", "좌측 하단"),
        ("CornerTopLeft", "Top left", "좌측 상단"),
        ("LanguageLabel", "Language", "언어"),
        ("LanguageSystem", "System default", "시스템 설정 따르기"),
        ("AboutLine", "SpoCue v{0} · Independent project, not affiliated with or endorsed by Spotify AB.",
            "SpoCue v{0} · Spotify AB와 제휴하거나 보증받지 않은 독립 프로젝트입니다."),
        ("ApplyChanges", "Apply changes", "변경 사항 적용"),
        ("ChangesApplied", "Changes applied.", "변경 사항을 적용했습니다."),
        ("ActionPlayPause", "Play / pause", "재생 / 일시정지"),
        ("ActionNext", "Next track", "다음 곡"),
        ("ActionPrevious", "Previous track", "이전 곡"),
        ("ActionShowTrack", "Show current track", "현재 곡 표시"),
        ("ActionSpotifyVolumeUp", "Spotify volume up", "Spotify 볼륨 높이기"),
        ("ActionSpotifyVolumeDown", "Spotify volume down", "Spotify 볼륨 낮추기"),
        ("ActionCopyTrack", "Copy current track", "현재 곡 정보 복사"),
        ("ToastTooltip", "Left-click: Open Spotify · Right-click: Copy track info", "좌클릭: Spotify 열기 · 우클릭: 곡 정보 복사"),
        ("SpotifyVolumeToast", "Spotify volume: {0}%", "Spotify 볼륨: {0}%"),
        ("SpotifyVolumeUnavailable", "Play a track in Spotify on this PC to adjust its volume.", "이 PC의 Spotify에서 곡을 재생해야 볼륨을 조절할 수 있습니다."),
        ("StartupTitle", "SpoCue is running in the tray", "SpoCue가 트레이에서 실행 중입니다"),
        ("StartupNoticeTitle", "SpoCue started, but a setting needs attention", "SpoCue가 실행됐지만 확인할 설정이 있습니다"),
        ("StartupSubtitle", "Click to open settings.", "클릭하면 설정이 열립니다."),
        ("SpotifyVolumeElsewhereTitle", "Playing on another device", "다른 기기에서 재생 중입니다"),
        ("SpotifyVolumeElsewhereHint", "Change Spotify's volume on that device.", "Spotify 볼륨은 그 기기에서 조절하세요."),
        ("TrackCopiedToast", "Copied to clipboard", "클립보드에 복사됨"),
        ("CopyTrackError", "Windows couldn’t copy the track info to the clipboard.", "곡 정보를 클립보드에 복사하지 못했습니다."),
        ("CheckUpdatesMenu", "Check for updates…", "업데이트 확인…"),
        ("UpdateAvailableMenu", "Download update (v{0})…", "새 업데이트 다운로드 (v{0})…"),
        ("UpdateAvailableTitle", "SpoCue v{0} is available", "SpoCue v{0} 업데이트 가능"),
        ("UpdateAvailableSubtitle", "Click to open the GitHub Release download page.", "클릭하면 GitHub 릴리스 다운로드 페이지가 열립니다."),
        ("UpToDateTitle", "SpoCue is up to date (v{0})", "최신 버전을 사용 중입니다 (v{0})"),
        ("UpToDateSubtitle", "You are running the latest release.", "현재 설치된 버전이 가장 최신 릴리스입니다."),
        ("UpdateCheckError", "Couldn’t check GitHub Releases right now.", "지금은 GitHub 릴리스 정보를 확인할 수 없습니다."),
        ("ShortcutModifiers", "{0} modifiers", "{0} 보조 키"),
        ("ShortcutKey", "{0} key", "{0} 키"),
        ("SettingsMenu", "Settings", "설정"),
        ("ExitMenu", "Exit", "종료"),
        ("OpenSpotify", "Open Spotify", "Spotify 열기"),
        ("OpenSpotifyError", "Couldn’t open the Spotify desktop app.", "Spotify 데스크톱 앱을 열 수 없습니다."),
        ("LoadOptionsError", "Windows couldn’t load the startup setting.", "시작 프로그램 설정을 불러오지 못했습니다."),
        ("SaveSettingsError", "Check access to Windows startup and the SpoCue settings folder.", "시작 프로그램과 SpoCue 설정 폴더에 접근할 수 있는지 확인하세요."),
        ("ReadSettingsError", "Settings couldn’t be read. Apply changes to save a new copy.", "설정을 읽지 못했습니다. 변경 사항을 적용하면 설정을 다시 저장합니다."),
        ("Connecting", "Connecting to Windows media controls…", "Windows 미디어 제어에 연결하는 중…"),
        ("WaitingSpotify", "Waiting for Spotify", "Spotify 연결 대기 중"),
        ("SpotifyConnected", "Spotify connected", "Spotify 연결됨"),
        ("Reconnecting", "Reconnecting to Spotify…", "Spotify에 다시 연결하는 중…"),
        ("NothingPlaying", "Nothing playing", "재생 중인 곡 없음"),
        ("PlayFirst", "Open Spotify and play a track.", "Spotify를 열고 곡을 재생하세요."),
        ("PreviewTitle", "Track toast preview", "곡 알림 미리 보기"),
        ("PreviewSubtitle", "Artwork and artist appear here.", "여기에 앨범 커버와 아티스트가 표시됩니다."),
        ("MediaCommandError", "Spotify couldn’t perform this action: {0}.", "Spotify에서 ‘{0}’ 동작을 실행하지 못했습니다."),
        ("MediaUnavailable", "Spotify media controls are temporarily unavailable.", "Spotify 재생 제어를 잠시 사용할 수 없습니다."),
        ("DuplicateShortcut", "{0} is already assigned. Choose a different shortcut for {1}.", "{0}은 이미 사용 중입니다. ‘{1}’에 다른 단축키를 지정하세요."),
        ("UnavailableShortcut", "Windows couldn’t register {0} for {1}. Try another combination.", "‘{1}’에 {0}을 등록할 수 없습니다. 다른 조합을 선택하세요.")
    ];

    public static void Apply(AppLanguage language)
    {
        var korean = language == AppLanguage.Korean || (language == AppLanguage.System && SystemUsesKorean);
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var next = new ResourceDictionary();
        foreach (var (key, english, koreanText) in Messages)
        {
            next.Add(key, korean ? koreanText : english);
        }

        dictionaries.Add(next);
        if (_current is not null)
        {
            dictionaries.Remove(_current);
        }
        _current = next;
    }

    public static string Get(string key) => (string)Application.Current.FindResource(key);

    public static string Format(string key, params object[] values) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), values);
}
