using System.Net.Http.Json;

namespace FarFiles.Services;

public class FileDataService
{
    //JEEWEE
    //HttpClient httpClient;
    public FileDataService()
    {
        //JEEWEE
        //this.httpClient = new HttpClient();
    }

    List<Model.FileData> filesList = new();
    //JEEWEE
    //public async Task<List<Model.FileData>> GetFilesData(string fullRootPath)
    public List<Model.FileData> GetFilesData(string fullRootPath)
    {
        //JEEWEE
        //if (filesList?.Count > 0)
        //    return filesList;

        //JEEWEE
        //// Online
        //var response = await httpClient.GetAsync("https://www.montemagno.com/monkeys.json");
        //if (response.IsSuccessStatusCode)
        //{
        //    filesList = await response.Content.ReadFromJsonAsync(MonkeyContext.Default.ListMonkey);
        //}




        // Offline
        /*using var stream = await FileSystem.OpenAppPackageFileAsync("monkeydata.json");
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();
        filesList = JsonSerializer.Deserialize(contents, MonkeyContext.Default.ListMonkey);*/

        filesList.Clear();

        try
        {
            filesList.AddRange(Directory.GetFiles(fullRootPath, "*", SearchOption.TopDirectoryOnly)
                .Select(f => new FileData(f)));
        }
        catch { }

        foreach (string fullPathSubDir in Directory.GetDirectories(fullRootPath))
        {
            filesList.Add(new FileData(fullPathSubDir));
            try
            {
                filesList.AddRange(Directory.GetFiles(fullPathSubDir, "*", SearchOption.AllDirectories)
                    .Select(f => new FileData(f)));
            }
            catch { }
        }

        return filesList;
    }
}
