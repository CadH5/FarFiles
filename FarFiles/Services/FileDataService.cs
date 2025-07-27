#if ANDROID
using AndroidX.DocumentFile.Provider;
#endif
using Microsoft.Maui.Controls.PlatformConfiguration;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net.Http.Json;

namespace FarFiles.Services;

public class FileDataService
{

#if ANDROID
    protected FarFiles.Platforms.Android.AndroidFileAccessHelper _androidFileAccessHelper = new();
#endif

    public FileDataService()
    {
#if ANDROID
        _androidFileAccessHelper.SetUriToNullIfInvalid(MauiProgram.Settings.AndroidUriRoot);
#endif
    }


#if ANDROID

    /// <summary>
    /// if Exception is thrown: returns array of one member with non-null ExcThrown 
    /// </summary>
    /// <param name="androidUriRoot"></param>
    /// <param name="dirNamesSubPath"></param>
    /// <returns></returns>
    public FileOrFolderData[] GetFilesAndFoldersDataAndroid(Android.Net.Uri androidUriRoot,
            string[] dirNamesSubPath)
    {
        List<Model.FileOrFolderData> dataList = new();

        DocumentFile[] docusSubdirs = _androidFileAccessHelper.ListDocumentFilesInUriAndSubpath(
                    androidUriRoot, dirNamesSubPath, true).ToArray();
        DocumentFile[] docusFiles = _androidFileAccessHelper.ListDocumentFilesInUriAndSubpath(
                    androidUriRoot, dirNamesSubPath, false).ToArray();

        for (int i = 0; i < 2; i++)
        {
            DocumentFile[] docus = (i == 0 ? docusSubdirs : docusFiles);
            foreach (DocumentFile docu in docus)
            {
                dataList.Add(NewFileOrFolderDataAndroid(docu));
            }
        }

        return dataList.ToArray();
    }

#else

    /// <summary>
    /// if Exception is thrown: returns array of one member with non-null ExcThrown 
    /// </summary>
    public FileOrFolderData[] GetFilesAndFoldersDataWindows(string fullRootPathDir)
    {
        List<Model.FileOrFolderData> dataList = new();

        string[] fullPathSubdirs = Directory.GetDirectories(fullRootPathDir);
        string[] fullPathFiles = Directory.GetFiles(fullRootPathDir, "*", SearchOption.TopDirectoryOnly);
        string[] dummySubPathParts = new string[0];

        for (int i = 0; i < 2; i++)
        {
            string[] fullPaths = (i == 0 ? fullPathSubdirs : fullPathFiles);
            foreach (string fullPath in fullPaths)
            {
                dataList.Add(NewFileOrFolderDataGeneric(Path.GetDirectoryName(fullPath),
                        null, dummySubPathParts, Path.GetFileName(fullPath),
                        i == 0));
            }
        }

        return dataList.ToArray();
    }
#endif

    public FileOrFolderData[] GetFilesAndFoldersDataGeneric(string fullRootPathDir,
            object androidUriRoot, string[] dirNamesSubPath)
    {
#if ANDROID
        return GetFilesAndFoldersDataAndroid((Android.Net.Uri)androidUriRoot,
                dirNamesSubPath);
#else
        string fullPathDir = PathFromRootAndSubPartsWindows(fullRootPathDir,
                    dirNamesSubPath);
        return GetFilesAndFoldersDataWindows(fullPathDir);
#endif
    }




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
        return _androidFileAccessHelper.ListDocumentFilesInUriAndSubpath(
                    (Android.Net.Uri)androidUriRoot, svrSubParts, true)
                        .Select(d => d.Name)
                        .ToArray();
#else
        return Directory.GetDirectories(PathFromRootAndSubPartsWindows(fullPathTopDir, svrSubParts))
                        .Select(d => Path.GetFileName(d))
                        .ToArray();
#endif
    }

    public string[] GetDirFileNamesGeneric(string fullPathTopDir, object androidUriRoot,
                string[] svrSubParts)
    {
#if ANDROID
        return _androidFileAccessHelper.ListDocumentFilesInUriAndSubpath(
                    (Android.Net.Uri)androidUriRoot, svrSubParts, false)
                        .Select(d => d.Name)
                        .ToArray();
#else
        return Directory.GetFiles(PathFromRootAndSubPartsWindows(
                        fullPathTopDir, svrSubParts),
                "*", SearchOption.TopDirectoryOnly)
                .Select(f => Path.GetFileName(f)).ToArray();
#endif
    }



    public BinaryReader OpenBinaryReaderGeneric(
            string fullPathTopDir, object androidUriRoot,
            string[] subParts, string fileName,
            out FileOrFolderData fData)
    {

        fData = NewFileOrFolderDataGeneric(fullPathTopDir, androidUriRoot, subParts,
                    fileName, false);

#if ANDROID
        return _androidFileAccessHelper.GetBinaryReaderFromUriAndSubpath(
                (Android.Net.Uri)androidUriRoot, subParts, fileName);
#else
        string fullPathFile = Path.Combine(PathFromRootAndSubPartsWindows(
                        fullPathTopDir, subParts), fileName);
        return new BinaryReader(File.Open(fullPathFile, FileMode.Open, FileAccess.Read));
#endif

    }



    /// <summary>
    /// Returns a BinaryWriter or null if skipWriteFileIfExisting is true and file already existed
    /// </summary>
    /// <param name="fullPathTopDir"></param>
    /// <param name="androidUriRoot"></param>
    /// <param name="subParts"></param>
    /// <param name="fileName"></param>
    /// <param name="skipFileIfExisting"></param>
    /// <param name="fileExistedBefore"></param>
    /// <returns></returns>
    public BinaryWriter OpenBinaryWriterGeneric(
            string fullPathTopDir, object androidUriRoot,
            string[] subParts, string fileName,
            bool skipFileIfExisting,
            out bool fileExistedBefore)
    {

#if ANDROID
        return _androidFileAccessHelper.GetBinaryWriterFromUriAndSubpath(
                (Android.Net.Uri)androidUriRoot, subParts, fileName,
                skipFileIfExisting, out fileExistedBefore);
#else
        string fullPathFile = Path.Combine(PathFromRootAndSubPartsWindows(
                        fullPathTopDir, subParts), fileName);
        fileExistedBefore = File.Exists(fullPathFile);
        if (fileExistedBefore && skipFileIfExisting)
            return null;
        return new BinaryWriter(File.Open(fullPathFile, FileMode.Create));
#endif

    }



    public void DeleteFileGeneric(string fullPathTopDir, object androidUriRoot,
            string[] subParts, string fileName)
    {

#if ANDROID
        DocumentFile docu = _androidFileAccessHelper.GetDocumentFileFromUriAndSubpath(
                    (Android.Net.Uri)androidUriRoot, subParts, false,
                    fileName, out DocumentFile parentDirDocu);
        if (docu != null)
            docu.Delete();
#else
        string fullPathFile = Path.Combine(PathFromRootAndSubPartsWindows(
                        fullPathTopDir, subParts), fileName);
        File.Delete(fullPathFile);
#endif

    }





#if ANDROID
#else
    public static string PathFromRootAndSubPartsWindows(string fullPathRoot, string[] subParts)
    {
        string path = fullPathRoot;
        foreach (string subPathPart in subParts)
            path = Path.Combine(path, subPathPart);
        return path;
    }
#endif


#if ANDROID
    public FileOrFolderData NewFileOrFolderDataAndroid(DocumentFile documentFile)
    {
        long androidLastModifiedMillis = documentFile.LastModified();
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(
                        androidLastModifiedMillis);
        DateTime dt = dateTimeOffset.LocalDateTime;
        return new FileOrFolderData(documentFile.Name, documentFile.IsDirectory,
                documentFile.Length(), FileAttributes.None, dt, dt);
    }
#endif

    public FileOrFolderData NewFileOrFolderDataGeneric(string fullPathTopDir,
            object androidUriRoot, string[] subParts, string name,
            bool isDir)
    {
#if ANDROID
        DocumentFile documentFile = _androidFileAccessHelper.GetDocumentFileFromUriAndSubpath(
                    (Android.Net.Uri)androidUriRoot, subParts, isDir, name,
                    out DocumentFile parentDirDocu);
        return NewFileOrFolderDataAndroid(documentFile);
#else
        string fullPath = Path.Combine(PathFromRootAndSubPartsWindows(
                    fullPathTopDir, subParts), name);
        return new FileOrFolderData(name, isDir,
                    isDir ? 0 : new FileInfo(fullPath).Length,
                    File.GetAttributes(fullPath),
                    File.GetCreationTime(fullPath), File.GetLastWriteTime(fullPath));
#endif
    }




    public int CalcTotalNumFilesOfFolderWithSubfolders(string fullPathTopDir,
            object androidUriRoot, string[] svrSubParts, string folderName)
    {
        int totalNumFiles = 0;
        string[] svrSubPartsInclFolder = CopyMgr.AddOneToArray(
                    svrSubParts, folderName);
        FileOrFolderData[] data = GetFilesAndFoldersDataGeneric(
                    fullPathTopDir, androidUriRoot, svrSubPartsInclFolder);
        totalNumFiles += data.Count(f => !f.IsDir);
        foreach (FileOrFolderData fdata in data.Where(f => f.IsDir))
        {
            totalNumFiles += CalcTotalNumFilesOfFolderWithSubfolders(fullPathTopDir,
                    androidUriRoot, svrSubPartsInclFolder, fdata.Name);
        }

        return totalNumFiles;
    }



    /// <summary>
    /// Creates directories if non existent
    /// </summary>
    /// <param name="fullPathRoot"></param>
    /// <param name="subParts"></param>
    /// <param name="dtCreationWin">ignored on Android or if dir existent</param>
    /// <param name="dtLastWriteWin">ignored on Android or if dir existent</param>
    /// <returns>true if created, false if already existed</returns>
    public bool CreatePathGeneric(string fullPathRoot, object androidUriRoot,
                    string[] subParts,
                    DateTime dtCreationWin, DateTime dtLastWriteWin,
                    out int dtProblems)
    {
        dtProblems = 0;

#if ANDROID
        return _androidFileAccessHelper.CreatePathIfNonExistent(
                (Android.Net.Uri)androidUriRoot, subParts);
#else
        string path = FileDataService.PathFromRootAndSubPartsWindows(
                fullPathRoot, subParts);
        if (Directory.Exists(path))
            return false;

        Directory.CreateDirectory(path);
        dtProblems = SetDateTimesWindows(path, true, dtCreationWin, dtLastWriteWin);
        return true;
#endif
    }





#if ANDROID
    //not possible on Android
#else

    public int SetDateTimesWindows(string fullPathDirOrFile, bool isDir,
                                DateTime dtCreation, DateTime dtLastWrite)
    {
        int numErrs = 0;
        DateTime dtNow = DateTime.Now;

        if (isDir)
        {
            try
            {
                Directory.SetCreationTime(fullPathDirOrFile, dtCreation);
            }
            catch
            {
                numErrs++;
                Directory.SetCreationTime(fullPathDirOrFile, dtNow);
            }
            try
            {
                Directory.SetLastWriteTime(fullPathDirOrFile, dtCreation);
            }
            catch
            {
                numErrs++;
                Directory.SetLastWriteTime(fullPathDirOrFile, dtNow);
            }
        }
        else
        {
            try
            {
                File.SetCreationTime(fullPathDirOrFile, dtCreation);
            }
            catch
            {
                numErrs++;
                File.SetCreationTime(fullPathDirOrFile, dtNow);
            }
            try
            {
                File.SetLastWriteTime(fullPathDirOrFile, dtCreation);
            }
            catch
            {
                numErrs++;
                File.SetLastWriteTime(fullPathDirOrFile, dtNow);
            }
        }

        return numErrs;
    }
#endif



    public static string DispRelPath(string[] subParts, string fileOrFolderNameOrNull)
    {
        string retStr = String.Join("/", subParts);
        if (!String.IsNullOrEmpty(fileOrFolderNameOrNull))
            retStr += "/" + fileOrFolderNameOrNull;
        return retStr;
    }

}
