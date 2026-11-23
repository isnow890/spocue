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
        ("ShortcutsHint", "Choose Off to disable an action.", "사용하지 않을 단축키는 ‘끄기’를 선택하세요."),
        ("PreferencesHeading", "Preferences", "환경설정"),
        ("NotifyOnChange", "Show a toast when the track changes", "곡이 바뀌면 알림 표시"),
        ("StartWithWindows", "Start with Windows", "Windows 시작 시 실행"),
        ("VolumeKeysLabel", "Use F10–F12 for system volume", "F10–F12로 시스템 볼륨 조절"),
        ("VolumeKeysHelp", "F10 Mute · F11 Lower · F12 Raise", "F10 음소거 · F11 볼륨 낮추기 · F12 볼륨 높이기"),
        ("LanguageLabel", "Language", "언어"),
        ("LanguageSystem", "System default", "시스템 설정 따르기"),
        ("ApplyChanges", "Apply changes", "변경 사항 적용"),
        ("ChangesApplied", "Changes applied.", "변경 사항을 적용했습니다."),
        ("ActionPlayPause", "Play / pause", "재생 / 일시정지"),
        ("ActionNext", "Next track", "다음 곡"),
        ("ActionPrevious", "Previous track", "이전 곡"),
        ("ActionShowTrack", "Show current track", "현재 곡 표시"),
        ("ShortcutModifiers", "{0} modifiers", "{0} 보조 키"),
        ("ShortcutKey", "{0} key", "{0} 키"),
        ("KeyOff", "Off", "끄기"),
        ("SettingsMenu", "Settings", "설정"),
        ("ExitMenu", "Exit", "종료"),
        ("OpenSpotify", "Open Spotify", "Spotify 열기"),
        ("OpenSpotifyError", "Couldn’t open the Spotify desktop app.", "Spotify 데스크톱 앱을 열 수 없습니다."),
        ("VolumeError", "Windows couldn’t adjust system volume.", "Windows 시스템 볼륨을 조절할 수 없습니다."),
        ("LoadOptionsError", "Windows couldn’t load all startup or volume key settings.", "시작 프로그램 또는 볼륨 키 설정을 불러오지 못했습니다."),
        ("SaveVolumeError", "Windows couldn’t change the volume key setting.", "Windows에서 볼륨 키 설정을 변경하지 못했습니다."),
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
