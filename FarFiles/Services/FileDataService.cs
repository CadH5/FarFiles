﻿#if ANDROID
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

        try
        {
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
        }
        catch (Exception exc)
        {
            return new FileOrFolderData[] { new FileOrFolderData(exc) };
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

        try
        {
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
        }
        catch (Exception exc)
        {
            return new FileOrFolderData[] { new FileOrFolderData(exc) };
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
            object androidUriRoot, string[] svrSubParts, string name,
            bool isDir)
    {
        try
        {
#if ANDROID
            DocumentFile documentFile = _androidFileAccessHelper.GetDocumentFileFromUriAndSubpath(
                        (Android.Net.Uri)androidUriRoot, svrSubParts, isDir, name);
            return NewFileOrFolderDataAndroid(documentFile);
#else
            string fullPath = Path.Combine(PathFromRootAndSubPartsWindows(
                        fullPathTopDir, svrSubParts), name);
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





    public int SetDateTimesGeneric(string fullPathDirOrFile, bool isDir,
                                DateTime dtCreation, DateTime dtLastWrite)
    {
        int numErrs = 0;
        DateTime dtNow = DateTime.Now;

#if ANDROID
//JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
#else

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
#endif

        return numErrs;
    }



    public static string DispRelPath(string[] subParts, string fileOrFolderNameOrNull)
    {
        string retStr = String.Join("/", subParts);
        if (!String.IsNullOrEmpty(fileOrFolderNameOrNull))
            retStr += "/" + fileOrFolderNameOrNull;
        return retStr;
    }

}
