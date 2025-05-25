using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using FarFiles.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Platforms.Android
{
    public class AndroidFileAccessHelper
    {
        protected DocumentFile? _uriDir = null;

        public List<DocumentFile> ListDocumentFilesInUriAndSubpath(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, bool forDirs)
        {
            List<DocumentFile> fileOrDirDocusOrNull = new();

            UriAndSubpathCore(androidUri, dirNamesSubPath, forDirs, fileOrDirDocusOrNull, null);

            return fileOrDirDocusOrNull;
        }


        public DocumentFile GetDocumentFileFromUriAndSubpath(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, bool forDirs, string fileOrFolderName)
        {
            return UriAndSubpathCore(androidUri, dirNamesSubPath, forDirs,
                        null, fileOrFolderName);
        }

        public BinaryReader GetBinaryReaderFromUriAndSubpath(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, string fileName)
        {
            try
            {
                DocumentFile documentFile = GetDocumentFileFromUriAndSubpath(androidUri,
                        dirNamesSubPath, false, fileName);
                var context = global::Android.App.Application.Context;
                var stream = context.ContentResolver.OpenInputStream(documentFile.Uri);
                if (stream == null)
                    throw new InvalidOperationException("Cannot create reader stream");
                return new BinaryReader(stream);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Exception trying to read " +
                        FileDataService.DispRelPath(dirNamesSubPath, fileName) +
                        ": " + exc.Message);
            }
        }


        /// <summary>
        /// Core for various methods. Returns Android DocumentFile,
        /// or null if searchFileOrFolderNameOrNull is null or not found 
        /// </summary>
        /// <param name="androidUri"></param>
        /// <param name="dirNamesSubPath"></param>
        /// <param name="forDirs"></param>
        /// <param name="fileOrDirDocusOrNull">if non-null, file or folder DoucumentFile's are added</param>
        /// <param name="searchFileOrFolderNameOrNull">null if only fileOrDirNamesOrNull intended </param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected DocumentFile UriAndSubpathCore(global::Android.Net.Uri androidUri,
                string[] dirNamesSubPath, bool forDirs,
                List<DocumentFile> fileOrDirDocusOrNull, string searchFileOrFolderNameOrNull)
        {
            if (null == _uriDir)
            {
                var context = global::Android.App.Application.Context;
                _uriDir = DocumentFile.FromTreeUri(context, androidUri);
            }

            if (null == _uriDir || !_uriDir.IsDirectory)
                throw new Exception($"AndroidUri is invalid or not a directory: {androidUri.ToString()}");

            DocumentFile[] fileOrFolders = _uriDir.ListFiles();
            string logErrPath = "";
            foreach (string dirName in dirNamesSubPath)
            {
                logErrPath += (String.IsNullOrEmpty(logErrPath) ? "" : "/") + dirName;
                DocumentFile subDir = fileOrFolders.Where(f => f.Name == dirName).FirstOrDefault();
                if (subDir == null)
                    throw new Exception(
                        $"ListDocumentFilesInUriAndSubpath: error finding sub path '{logErrPath}'");
                fileOrFolders = subDir.ListFiles();
            }

            foreach (DocumentFile f in fileOrFolders)
            {
                if (forDirs && f.IsDirectory ||
                    !forDirs && f.IsFile)
                {
                    if (f.Name == searchFileOrFolderNameOrNull)
                        return f;
                    if (null != fileOrDirDocusOrNull)
                        fileOrDirDocusOrNull.Add(f);
                }
            }

            return null;
        }
    }
}
