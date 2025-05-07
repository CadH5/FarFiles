using System.Text.Json.Serialization;

namespace FarFiles.Model;

public class FileOrFolderData
{
    public Exception ExcThrown { get; set; }
    public string Name { get; set; } = "";
    public FileAttributes Attrs { get; set; }
    public DateTime DtCreation { get; set; }
    public DateTime DtLastWrite { get; set; }
    public FileOrFolderData[] Children_NullIfFile { get; set; }
    public bool IsDir { get => null != Children_NullIfFile; }


    /// <summary>
    /// This does not fill the children if it's a folder (a dir)
    /// </summary>
    /// <param name="fullPath">this does not get stored in a member, only Name</param>
    /// <param name="isDir"></param>
    public FileOrFolderData(string fullPath, bool isDir)
    {
        Name = Path.GetFileName(fullPath);
        Children_NullIfFile = isDir ? new FileOrFolderData[0] : null;
        bool exists = isDir ? Directory.Exists(fullPath) : File.Exists(fullPath);
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
            }
        }
    }

    public FileOrFolderData(Exception excThrown)
    {
        ExcThrown = excThrown;
    }
}


//JEEWEE
//[JsonSerializable(typeof(List<FileData>))]
//internal sealed partial class MonkeyContext : JsonSerializerContext{

//}