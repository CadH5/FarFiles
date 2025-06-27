using System.Text.Json.Serialization;

namespace FarFiles.Model;

public class FileOrFolderData
{
    public string Name { get; set; } = "";
    public FileAttributes Attrs { get; set; }
    public DateTime DtCreation { get; set; }
    public DateTime DtLastWrite { get; set; }
    public long FileSize { get; set; }
    public bool IsDir { get; set; }
    public string ImageSrc { get => IsDir ?
        (MauiProgram.Info.CpClientToFromMode == CpClientToFromMode.CLIENTFROMSVR ?
            "folder.png" : "folderLocal.png") :
            "file.png"; }

    public FileOrFolderData(string name, bool isDir, long fileSize,
                FileAttributes attrs, DateTime dtCreation, DateTime dtLastWrite)
    {
        Name = name;
        IsDir = isDir;
        FileSize = isDir ? 0 : fileSize;
        Attrs = isDir ? FileAttributes.Directory : attrs;
        DtCreation = dtCreation;
        DtLastWrite = dtLastWrite;
    }

    public FileOrFolderData(string name, bool isDir, long fileSize)
    {
        Name = name;
        IsDir = isDir;
        FileSize = isDir ? 0 : fileSize;
        Attrs = isDir ? FileAttributes.Directory : FileAttributes.None;
        var dt = new DateTime(2000, 1, 1);
        DtCreation = dt;
        DtLastWrite = dt;
    }


    public string FormatFileSize
    {
        get
        {
            if (IsDir)
                return "";

            // thanks, ChatGPT !
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = FileSize;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.#} {sizes[order]}";
        }
    }
}

