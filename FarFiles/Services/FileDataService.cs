using AndroidX.DocumentFile.Provider;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace FarFiles.Services;

public class FileDataService
{

#if ANDROID
    protected FarFiles.Platforms.Android.AndroidFileAccessHelper _androidFileAccessHelper = new();
#endif

    public FileDataService()
    {
    }


//JEEWEE
//#if ANDROID

//    /// <summary>
//    /// if Exception is thrown: returns array of one member with non-null ExcThrown 
//    /// </summary>
//    /// <param name="androidUriRoot"></param>
//    /// <param name="dirNamesSubPath"></param>
//    /// <returns></returns>
//    public FileOrFolderData[] GetFilesAndFoldersDataAndroid(Android.Net.Uri androidUriRoot,
//            string[] dirNamesSubPath)
//    {
//        List<Model.FileOrFolderData> dataList = new();

//        try
//        {
//            string[] namesSubdirs = _androidFileAccessHelper.ListFilesInUriAndSubpath(
//                        androidUriRoot, dirNamesSubPath, true).ToArray();
//            string[] namesFiles = _androidFileAccessHelper.ListFilesInUriAndSubpath(
//                        androidUriRoot, dirNamesSubPath, false).ToArray();

//            for (int i = 0; i < 2; i++)
//            {
//                string[] names = (i == 0 ? namesSubdirs : namesFiles);
//                foreach (string name in names)
//                {
//                    dataList.Add(new FileOrFolderData(name, i == 0, true));
//                }
//            }
//        }
//        catch (Exception exc)
//        {
//            return new FileOrFolderData[] { new FileOrFolderData(exc) };
//        }

//        return dataList.ToArray();
//    }

//#else

//    /// <summary>
//    /// if Exception is thrown: returns array of one member with non-null ExcThrown 
//    /// </summary>
//    public FileOrFolderData[] GetFilesAndFoldersDataWindows(string fullRootPath,
//            SearchOption searchOption)
//    {
//        List<Model.FileOrFolderData> dataList = new();

//        try
//        {
//            string[] fullPathSubdirs = Directory.GetDirectories(fullRootPath);
//            string[] fullPathFiles = Directory.GetFiles(fullRootPath, "*", SearchOption.TopDirectoryOnly);


//            for (int i = 0; i < 2; i++)
//            {
//                string[] fullPaths = (i == 0 ? fullPathSubdirs : fullPathFiles);
//                foreach (string fullPath in fullPaths)
//                {
//                    dataList.Add(new FileOrFolderData(fullPath, i == 0, true));
//                }
//            }

//            //JEEWEE
//            ////JEEWEE NOT SUPPORTED YET ON ANDROID,
//            ////how to get an uri for a subdir (but actually for this app we dont need AllDirectories)
//            //if (searchOption == SearchOption.AllDirectories)
//            //{
//            //    foreach (FileOrFolderData fileOrFolderData in dataList.Where(d => d.IsDir))
//            //    {
//            //        fileOrFolderData.Children_NullIfFile = GetFilesAndFoldersDataWindows(
//            //                Path.Combine(fullRootPath, fileOrFolderData.Name),
//            //                searchOption);
//            //    }
//            //}
//        }
//        catch (Exception exc)
//        {
//            return new FileOrFolderData[] { new FileOrFolderData(exc) };
//        }

//        return dataList.ToArray();
//    }
//#endif







    /// <summary>
    /// Returns: array errmsgs per file or dir that could not be deleted 
    /// WINDOWS !
    /// </summary>
    /// <param name="fullPathTopDir"></param>
    /// <returns></returns>
    public string[] DeleteDirPlusSubdirsPlusFilesWindows(string fullPathTopDir)
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
            for (int i = 0; i < foldersToDeleteList.Count; i++)
            {
                string fullPathFolder = foldersToDeleteList[i];
                retList.AddRange(DeleteDirPlusSubdirsPlusFilesWindows(fullPathFolder));    // recursive
            }

            try
            {
                Directory.Delete(fullPathTopDir);
            }
            catch (Exception exc)
            {
                retList.Add($"Could not delete directory '{fullPathTopDir}': {exc.Message}");
            }
        }

        return retList.ToArray();
    }



    public string[] GetDirFolderNamesGeneric(string fullPathTopDir, object androidUriRoot,
                string[] svrSubParts)
    {
#if ANDROID
        return _androidFileAccessHelper.ListFilesInUriAndSubpath(
                    (Android.Net.Uri)androidUriRoot, svrSubParts, true).ToArray();
#else
        return Directory.GetDirectories(PathFromRootAndSubPartsWindows(fullPathTopDir, svrSubParts))
                .Select(d => Path.GetFileName(d)).ToArray();
#endif
    }

    public string[] GetDirFileNamesGeneric(string fullPathTopDir, object androidUriRoot,
                string[] svrSubParts)
    {
#if ANDROID
        return _androidFileAccessHelper.ListFilesInUriAndSubpath(
                    (Android.Net.Uri)androidUriRoot, svrSubParts, false).ToArray();
#else
        return Directory.GetFiles(PathFromRootAndSubPartsWindows(
                        fullPathTopDir, svrSubParts),
                "*", SearchOption.TopDirectoryOnly)
                .Select(f => Path.GetFileName(f)).ToArray();
#endif
    }


//JEEWEE
//    public FileOrFolderData[] GetFilesAndFoldersDataGeneric(string fullPathTopDir, object androidUriRoot,
//                        string[] svrSubParts)
//    {
//#if ANDROID
//        return GetFilesAndFoldersDataAndroid((Android.Net.Uri)androidUriRoot, svrSubParts);
//#else
//        return GetFilesAndFoldersDataWindows(PathFromRootAndSubPartsWindows(
//                        fullPathTopDir, svrSubParts),
//                SearchOption.TopDirectoryOnly);
//#endif
//    }



    public BinaryReader OpenBinaryReaderGeneric(
            string fullPathTopDir, object androidUriRoot,
            string[] svrSubParts, string fileName,
            out FileOrFolderData fData)
    {

        fData = NewFileOrFolderDataGeneric(fullPathTopDir, androidUriRoot, svrSubParts,
                    fileName, false);

#if ANDROID
        return _androidFileAccessHelper.GetBinaryReaderFromUriAndSubpath(
                (Android.Net.Uri)androidUriRoot, svrSubParts, false, fileName);
#else
        string fullPathFile = Path.Combine(PathFromRootAndSubPartsWindows(
                        fullPathTopDir, svrSubParts), fileName);
        return new BinaryReader(File.Open(fullPathFile, FileMode.Open, FileAccess.Read));
#endif

    }




    public static string PathFromRootAndSubPartsWindows(string fullPathRoot, string[] subParts)
    {
#if ANDROID
        //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        return "JEEWEE";
#else
        string path = fullPathRoot;
        foreach (string subPathPart in subParts)
            path = Path.Combine(path, subPathPart);
        return path;
#endif
    }


    public FileOrFolderData NewFileOrFolderDataGeneric(string fullPathTopDir,
            object androidUriRoot, string[] svrSubParts, string name,
            bool isDir)
    {
        try
        {
#if ANDROID
            DocumentFile documentFile = _androidFileAccessHelper.GetDocumentFileFromUriAndSubpath(
                        (Android.Net.Uri)androidUriRoot, svrSubParts, isDir, name);
            DateTime dt = new DateTime(documentFile.LastModified());
            return new FileOrFolderData(name, isDir, documentFile.Length(),
                        FileAttributes.None, dt, dt);
#else
            string fullPath = PathFromRootAndSubPartsWindows(fullPathTopDir, svrSubParts);
            return new FileOrFolderData(name, isDir,
                        isDir ? 0 : new FileInfo(fullPath).Length,
                        File.GetAttributes(fullPath),
                        File.GetCreationTime(fullPath), File.GetLastWriteTime(fullPath));
#endif
        }
        catch (Exception exc)
        {
            return new FileOrFolderData(exc);
        }
    }



    public static string DispRelPath(string[] subParts, string fileOrFolderNameOrNull)
    {
        string retStr = String.Join("/", subParts);
        if (!String.IsNullOrEmpty(fileOrFolderNameOrNull))
            retStr += "/" + fileOrFolderNameOrNull;
        return retStr;
    }

}
