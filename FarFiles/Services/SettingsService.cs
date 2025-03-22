//JEEWEE: MAY GO
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.IO;
//using System.Text.Json;

//namespace FarFiles.Services
//{
//    public class SettingsService
//    {
//        // Note from Avalonia where I copied this from:
//        // This is a hard coded path to the file. It may not be available on every platform. In your real world App you may
//        // want to make this configurable
//        private static string _jsonFileFullPath =
//            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
//            "FarFiles", "Settings.txt");

//        /// <summary>
//        /// Stores the given items into a file on disc
//        /// </summary>
//        /// <param name="settingsToSave">The settings to save</param>
//        public void SaveToFile(Settings settingsToSave)
//        {
//            // Ensure all directories exists
//            Directory.CreateDirectory(Path.GetDirectoryName(_jsonFileFullPath)!);

//            // We use a FileStream to write all items to disc
//            using (var fs = File.Create(_jsonFileFullPath))
//            {
//                JsonSerializer.Serialize(fs, settingsToSave);
//            }
//        }


//        /// <summary>
//        /// Loads the file from disc and returns the items stored inside
//        /// </summary>
//        /// <returns>An IEnumerable of items loaded or null in case the file was not found</returns>
//        public Settings LoadFromFile()
//        {
//            try
//            {
//                // We try to read the saved file and return the ToDoItemsList if successful
//                using (var fs = File.OpenRead(_jsonFileFullPath))
//                {
//                    return JsonSerializer.Deserialize<Settings>(fs);
//                }
//            }
//            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
//            {
//                // In case the file was not found, we simply return null
//                return null;
//            }
//        }
//    }
//}
