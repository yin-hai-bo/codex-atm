using System.Globalization;

namespace CodexAtm.App;

public static class AppText
{
    public static string ProductName => IsChinese ? "Codex 归档线程管理" : "Codex Archive Thread Manager";

    public static string MainWindowDescription => IsChinese
        ? "查看、预览并删除本机 Codex App 的归档线程"
        : "Browse, preview, and delete archived threads from the local Codex App.";

    public static string AboutWindowTitle => IsChinese ? "关于" : "About";

    public static string AboutWindowDescription => IsChinese
        ? "查看、预览并管理本机 Codex App 的归档线程"
        : "Browse, preview, and manage archived threads from the local Codex App.";

    public static string VersionLabel => IsChinese ? "版本号" : "Version";

    public static string RepositoryLabel => IsChinese ? "仓库地址" : "Repository";

    public static string CopyOnClickToolTip => IsChinese ? "单击复制" : "Click to copy";

    public static string CloseButton => IsChinese ? "关闭" : "Close";

    public static string CopiedToClipboard => IsChinese ? "已复制到剪贴板" : "Copied to clipboard";

    public static string CommitCopiedToClipboard => IsChinese ? "Commit ID 已复制到剪贴板" : "Commit ID copied to clipboard";

    public static string CopyFailed(string message)
    {
        return IsChinese ? $"复制失败：{message}" : $"Copy failed: {message}";
    }

    public static string CopyFailedTitle => IsChinese ? "复制失败" : "Copy Failed";

    public static string CloseApplicationToolTip => IsChinese ? "关闭本程序" : "Close this application";

    public static string SessionListTitle => IsChinese ? "归档列表" : "Archived Threads";

    public static string RefreshButton => IsChinese ? "刷新" : "Refresh";

    public static string NoSessionSelected => IsChinese
        ? "选择左侧归档线程后，这里会显示元信息与最近几条消息。"
        : "Select an archived thread on the left to view its metadata and recent messages here.";

    public static string DetailPreviewTitle => IsChinese ? "详情预览" : "Detail Preview";

    public static string SessionIdLabel => IsChinese ? "线程 ID" : "Thread ID";

    public static string FilePathLabel => IsChinese ? "文件路径" : "File Path";

    public static string WorkingDirectoryLabel => IsChinese ? "工作目录" : "Working Directory";

    public static string LastModifiedLabel => IsChinese ? "修改时间" : "Last Modified";

    public static string FileSizeLabel => IsChinese ? "文件大小" : "File Size";

    public static string SourceLabel => IsChinese ? "来源" : "Source";

    public static string RecentMessagesTitle => IsChinese ? "最近几条消息" : "Recent Messages";

    public static string MoveToRecycleBinButton => IsChinese ? "移到回收站" : "Move to Recycle Bin";

    public static string DeletePermanentlyButton => IsChinese ? "永久删除" : "Delete Permanently";

    public static string FilterHint => IsChinese ? "（输入文字进行过滤）" : "(Type to filter)";

    public static string ConfirmRecycleMessage => IsChinese
        ? "确定要将所选归档线程移到回收站吗？"
        : "Move the selected archived thread to the Recycle Bin?";

    public static string ConfirmPermanentDeleteMessage => IsChinese
        ? "确定要永久删除所选归档线程吗？此操作不可恢复。"
        : "Permanently delete the selected archived thread? This action cannot be undone.";

    public static string ConfirmDeleteTitle => IsChinese ? "确认删除" : "Confirm Delete";

    public static string DeleteFailedTitle => IsChinese ? "删除失败" : "Delete Failed";

    public static string AboutVersionLabel => IsChinese ? "版本号" : "Version";

    public static string GitCommitIdLabel => "Git Commit ID";

    private static bool IsChinese =>
        string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase);
}
