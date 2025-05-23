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
    //JEEWEE
    //public FileOrFolderData[] Children_NullIfFile { get; set; }
    //public bool IsDir { get => null != Children_NullIfFile; }
    public bool IsDir { get; set; }
    public string ImageSrc { get => IsDir ? "folder.png" : "file.png"; }

    //JEEWEE
    ///// <summary>
    ///// This does not fill the children if it's a folder (a dir)
    ///// </summary>
    ///// <param name="fullPath">this does not get stored in a member, only Name</param>
    ///// <param name="isDir"></param>
    ///// <param name="shouldExist">pass true if file was found on this side's file system</param>
    //public FileOrFolderData(string fullPath, bool isDir, bool shouldExist,
    //            long fileSize = 0)
    //{
    //    Name = Path.GetFileName(fullPath);
    //    //JEEWEE
    //    //Children_NullIfFile = isDir ? new FileOrFolderData[0] : null;
    //    IsDir = isDir;
    //    FileSize = isDir ? 0 : fileSize;
    //    bool exists = (!shouldExist) ? false :
    //            (isDir ? Directory.Exists(fullPath) : File.Exists(fullPath));
    //    if (exists)
    //    {
    //        if (isDir)
    //        {
    //            Attrs = FileAttributes.Directory;
    //            DtCreation = Directory.GetCreationTime(fullPath);
    //            DtLastWrite = Directory.GetLastWriteTime(fullPath);
    //        }
    //        else
    //        {
    //            Attrs = File.GetAttributes(fullPath);
    //            DtCreation = File.GetCreationTime(fullPath);
    //            DtLastWrite = File.GetLastWriteTime(fullPath);
    //            FileSize = new FileInfo(fullPath).Length;
    //        }
    //    }
    //}

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