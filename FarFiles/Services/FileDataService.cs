using System.Net.Http.Json;

namespace FarFiles.Services;

public class FileDataService
{
    public FileDataService()
    {
    }

    //JEEWEE
    //public async Task<List<Model.FileData>> GetFilesData(string fullRootPath)
    public FileOrFolderData[] GetFilesData(string fullRootPath,
            SearchOption searchOption)
    {
        List<Model.FileOrFolderData> dataList = new();

        try
        {
            string[] fullPathSubdirs = Directory.GetDirectories(fullRootPath);
            string[] fullPathFiles = Directory.GetFiles(fullRootPath, "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < 2; i++)
            {
                string[] fullPaths = (i == 0 ? fullPathSubdirs : fullPathFiles);
                foreach (string fullPath in fullPaths)
                {
                    dataList.Add(new FileOrFolderData(fullPath, i == 0));
                }
            }

            if (searchOption == SearchOption.AllDirectories)
            {
                foreach (FileOrFolderData fileOrFolderData in dataList.Where(d => d.IsDir))
                {
                    fileOrFolderData.Children_NullIfFile = GetFilesData(
                            Path.Combine(fullRootPath, fileOrFolderData.Name),
                            searchOption);
                }
            }
        }
        catch (Exception exc)
        {
            return new FileOrFolderData[] { new FileOrFolderData(exc) };
        }

        return dataList.ToArray();
    }
}
