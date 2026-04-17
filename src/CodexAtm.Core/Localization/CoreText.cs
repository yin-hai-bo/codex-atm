using System.Globalization;

namespace CodexAtm.Core.Localization;

public static class CoreText
{
    public static string UngroupedLabel => IsChinese ? "未分组" : "Ungrouped";

    public static string ThemeModeSystem => IsChinese ? "跟随系统" : "System";

    public static string ThemeModeLight => IsChinese ? "浅色" : "Light";

    public static string ThemeModeDark => IsChinese ? "深色" : "Dark";

    public static string LanguageModeSystem => IsChinese ? "跟随系统" : "System";

    public static string LanguageModeSimplifiedChinese => IsChinese ? "简体中文" : "Simplified Chinese";

    public static string LanguageModeEnglish => IsChinese ? "英文" : "English";

    public static string ArchivedSessionCount(int count)
    {
        return IsChinese ? $"已归档线程数：{count}" : $"Archived threads: {count}";
    }

    public static string FilteredSessionCount(int count)
    {
        return IsChinese ? $"符合过滤条件的线程数：{count}" : $"Filtered threads: {count}";
    }

    public static string LoadingArchivedSessions => IsChinese ? "正在加载归档线程…" : "Loading archived threads...";

    public static string ArchivedFileNotFound => IsChinese ? "归档文件不存在。" : "Archived file does not exist.";

    public static string FileNotFound => IsChinese ? "文件不存在。" : "File does not exist.";

    public static string FilePathCannotBeEmpty => IsChinese ? "文件路径不能为空。" : "File path cannot be empty.";

    public static string OnlyArchivedSessionsAllowed => IsChinese
        ? "只允许操作 archived_sessions 目录下的文件。"
        : "Only files under the archived_sessions directory can be operated on.";

    public static string OnlyJsonlAllowed => IsChinese
        ? "只允许操作 .jsonl 归档文件。"
        : "Only .jsonl archived files can be operated on.";

    public static string ArchiveDirectoryCannotBeEmpty => IsChinese ? "归档目录不能为空。" : "Archive directory cannot be empty.";

    public static string FileCannotBeParsed => IsChinese ? "文件内容无法解析。" : "The file content cannot be parsed.";

    public static string FileEmptyOrMissingData => IsChinese
        ? "文件为空或缺少可识别数据。"
        : "The file is empty or missing recognizable data.";

    public static string PartialLineParseFailed => IsChinese ? "部分行解析失败。" : "Some lines could not be parsed.";

    private static bool IsChinese =>
        string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase);
}
