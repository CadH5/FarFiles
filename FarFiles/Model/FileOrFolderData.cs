using System.Text.Json.Serialization;

namespace FarFiles.Model;

public class FileOrFolderData
{
    public Exception ExcThrown { get; set; }
    public string Name { get; set; } = "";
    public FileAttributes Attrs { get; set; }
    public DateTime DtCreation { get; set; }
    public DateTime DtLastWrite { get; set; }
    public long FileSize { get; set; }
    public FileOrFolderData[] Children_NullIfFile { get; set; }
    public bool IsDir { get => null != Children_NullIfFile; }
    public string ImageSrc { get => IsDir ? "folder.png" : "file.png"; }

    /// <summary>
    /// This does not fill the children if it's a folder (a dir)
    /// </summary>
    /// <param name="fullPath">this does not get stored in a member, only Name</param>
    /// <param name="isDir"></param>
    /// <param name="shouldExist">pass true if file was found on this side's file system</param>
    public FileOrFolderData(string fullPath, bool isDir, bool shouldExist,
                long fileSize = 0)
    {
        Name = Path.GetFileName(fullPath);
        Children_NullIfFile = isDir ? new FileOrFolderData[0] : null;
        FileSize = isDir ? 0 : fileSize;
        bool exists = (!shouldExist) ? false :
                (isDir ? Directory.Exists(fullPath) : File.Exists(fullPath));
        if (exists)
        {
            if (isDir)
            {
                Attrs = FileAttributes.Directory;
                DtCreation = Directory.GetCreationTime(fullPath);
                DtLastWrite = Directory.GetLastWriteTime(fullPath);
            }
            else
            {
                Attrs = File.GetAttributes(fullPath);
                DtCreation = File.GetCreationTime(fullPath);
                DtLastWrite = File.GetLastWriteTime(fullPath);
                FileSize = new FileInfo(fullPath).Length;
            }
        }
    }

    public FileOrFolderData(Exception excThrown)
    {
        ExcThrown = excThrown;
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


//JEEWEE
//[JsonSerializable(typeof(List<FileData>))]
//internal sealed partial class MonkeyContext : JsonSerializerContext{

//}