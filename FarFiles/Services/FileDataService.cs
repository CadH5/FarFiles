using System.Collections.Generic;
using System.Net.Http.Json;

namespace FarFiles.Services;

public class FileDataService
{
    // JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // This Service doesn't make sense thus far, because all methods can become static

    public FileDataService()
    {
    }

    public FileOrFolderData[] GetFilesAndFoldersData(string fullRootPath,
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
                    dataList.Add(new FileOrFolderData(fullPath, i == 0, true));
                }
            }

            if (searchOption == SearchOption.AllDirectories)
            {
                foreach (FileOrFolderData fileOrFolderData in dataList.Where(d => d.IsDir))
                {
                    fileOrFolderData.Children_NullIfFile = GetFilesAndFoldersData(
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


    /// <summary>
    /// Returns: array errmsgs per file or dir that could not be deleted
    /// </summary>
    /// <param name="fullPathTopDir"></param>
    /// <returns></returns>
    public string[] DeleteDirPlusSubdirsPlusFiles(string fullPathTopDir)
    {
        var retList = new List<string>();

        if (Directory.Exists(fullPathTopDir))
        {
            foreach (string fullPathFile in Directory.GetFiles(fullPathTopDir))
            {
                try
                {
                    File.Delete(fullPathFile);
                    if (File.Exists(fullPathFile))
                        throw new Exception("reason unknown");
                }
                catch (Exception exc)
                {
                    retList.Add($"Could not delete file '{fullPathFile}': {exc.Message}");
                }
            }

            var foldersToDeleteList = Directory.GetDirectories(fullPathTopDir).ToList();
            foldersToDeleteList.Add(fullPathTopDir);
            for (int i = 0; i < foldersToDeleteList.Count; i++)
            {
                string fullPathFolder = foldersToDeleteList[i];
                if (i < foldersToDeleteList.Count - 1)              // not again for fullPathTopDir
                {
                    retList.AddRange(DeleteDirPlusSubdirsPlusFiles(fullPathFolder));    // recursive
                }
                try
                {
                    Directory.Delete(fullPathFolder);
                }
                catch (Exception exc)
                {
                    retList.Add($"Could not delete directory '{fullPathFolder}': {exc.Message}");
                }
            }
        }

        return retList.ToArray();
    }
}
