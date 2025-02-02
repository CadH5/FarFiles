using System.Text.Json.Serialization;

namespace FarFiles.Model;

public class FileData
{
    public string Name { get; set; }
    public string FullPath { get; set; }


    public FileData(string fullPath)
    {
        this.FullPath = fullPath;
        this.Name = Path.GetFileName(fullPath);
    }
}


//JEEWEE
//[JsonSerializable(typeof(List<FileData>))]
//internal sealed partial class MonkeyContext : JsonSerializerContext{

//}