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

        public List<string> ListFilesInUriAndSubpath(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, bool forDirs)
        {
            List<string> fileOrDirNames = new();

            UriAndSubpathCore(androidUri, dirNamesSubPath, forDirs, fileOrDirNames, null);

            return fileOrDirNames;
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
                        FileDataService.DispRelPath(dirNamesSubPath, fileOrFolderName) +
                        ": " + exc.Message);
            }
        }

        protected DocumentFile UriAndSubpathCore(global::Android.Net.Uri androidUri,
                string[] dirNamesSubPath, bool forDirs,
                List<string> fileOrDirNamesOrNull, string fileOrFolderNameOrNull)
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
                        $"ListFilesInUriAndSubpath: error finding sub path '{logErrPath}'");
                fileOrFolders = subDir.ListFiles();
            }

            foreach (DocumentFile f in fileOrFolders)
            {
                if (forDirs && f.IsDirectory ||
                    !forDirs && f.IsFile)
                {
                    if (f.Name == fileOrFolderNameOrNull)
                        return f;
                    if (null != fileOrDirNamesOrNull)
                        fileOrDirNamesOrNull.Add(f.Name);
                }
            }

            return null;
        }
    }
}
