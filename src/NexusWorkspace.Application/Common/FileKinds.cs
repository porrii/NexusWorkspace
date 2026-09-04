namespace NexusWorkspace.Application.Common;

/// <summary>Coarse file family for attachment filtering, icons and thumbnails.</summary>
public enum AttachmentKind
{
    Image = 0,
    Pdf = 1,
    Document = 2,
    Spreadsheet = 3,
    Archive = 4,
    Audio = 5,
    Video = 6,
    Text = 7,
    Other = 8,
}

/// <summary>Extension → MIME type and <see cref="AttachmentKind"/>. Offline, no dependency.</summary>
public static class FileKinds
{
    public static AttachmentKind KindOf(string fileName) => Ext(fileName) switch
    {
        "png" or "jpg" or "jpeg" or "gif" or "bmp" or "webp" or "tif" or "tiff" or "ico" or "svg" => AttachmentKind.Image,
        "pdf" => AttachmentKind.Pdf,
        "doc" or "docx" or "odt" or "rtf" or "pages" => AttachmentKind.Document,
        "xls" or "xlsx" or "ods" or "csv" => AttachmentKind.Spreadsheet,
        "zip" or "rar" or "7z" or "tar" or "gz" or "bz2" => AttachmentKind.Archive,
        "mp3" or "wav" or "ogg" or "flac" or "m4a" or "aac" => AttachmentKind.Audio,
        "mp4" or "mkv" or "mov" or "avi" or "webm" or "wmv" => AttachmentKind.Video,
        "txt" or "md" or "log" or "json" or "xml" or "yml" or "yaml" or "ini" or "sql" => AttachmentKind.Text,
        _ => AttachmentKind.Other,
    };

    public static bool IsImage(string fileName) => KindOf(fileName) == AttachmentKind.Image && Ext(fileName) != "svg";

    public static string? MimeOf(string fileName) => Ext(fileName) switch
    {
        "png" => "image/png",
        "jpg" or "jpeg" => "image/jpeg",
        "gif" => "image/gif",
        "bmp" => "image/bmp",
        "webp" => "image/webp",
        "svg" => "image/svg+xml",
        "tif" or "tiff" => "image/tiff",
        "ico" => "image/x-icon",
        "pdf" => "application/pdf",
        "doc" => "application/msword",
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "xls" => "application/vnd.ms-excel",
        "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "csv" => "text/csv",
        "zip" => "application/zip",
        "7z" => "application/x-7z-compressed",
        "rar" => "application/vnd.rar",
        "txt" or "log" => "text/plain",
        "md" => "text/markdown",
        "json" => "application/json",
        "xml" => "application/xml",
        "mp3" => "audio/mpeg",
        "wav" => "audio/wav",
        "mp4" => "video/mp4",
        "mkv" => "video/x-matroska",
        "mov" => "video/quicktime",
        _ => null,
    };

    public static string HumanSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{value:0.#} {units[unit]}";
    }

    private static string Ext(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        return dot < 0 || dot == fileName.Length - 1
            ? string.Empty
            : fileName[(dot + 1)..].ToLowerInvariant();
    }
}
