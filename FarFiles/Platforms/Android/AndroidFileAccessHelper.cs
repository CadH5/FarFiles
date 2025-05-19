using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;
using AndroidX.DocumentFile.Provider;

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

            if (null == _uriDir)
            {
                var context = global::Android.App.Application.Context;
                _uriDir = DocumentFile.FromTreeUri(context, androidUri);
            }

            if (null == _uriDir || ! _uriDir.IsDirectory)
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
                    ! forDirs && f.IsFile)
                {
                    fileOrDirNames.Add(f.Name);
                }
            }

            return fileOrDirNames;
        }
    }
}
