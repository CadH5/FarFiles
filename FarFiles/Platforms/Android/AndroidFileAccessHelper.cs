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
        protected NavigationCachePerPath _navCachePerPath = new();

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

            //JEEWEE
            //int count = GetNumDocumentFiles(_uriDir);

            string joinedPath = "";
            DocumentFile[] fileOrFolders = _navCachePerPath.GetDocusUpdCache(
                        joinedPath, _uriDir);
            foreach (string dirName in dirNamesSubPath)
            {
                joinedPath += (String.IsNullOrEmpty(joinedPath) ? "" : "/") + dirName;
                DocumentFile subDir = fileOrFolders.Where(f => f.Name == dirName).FirstOrDefault();
                if (subDir == null)
                    throw new Exception(
                        $"ListDocumentFilesInUriAndSubpath: error finding sub path '{joinedPath}'");
                //JEEWEE
                //count = GetNumDocumentFiles(subDir);
                fileOrFolders = _navCachePerPath.GetDocusUpdCache(joinedPath, subDir);
            }

            //JEEWEE IS THIS FASTER?
            if (null != searchFileOrFolderNameOrNull)
                return fileOrFolders.FirstOrDefault(f =>
                    f.Name == searchFileOrFolderNameOrNull &&
                    f.IsDirectory == forDirs &&
                    f.IsFile != forDirs);   // also f.IsVirtual exists

            if (null != fileOrDirDocusOrNull)
                fileOrDirDocusOrNull.AddRange(fileOrFolders.Where(f =>
                    f.IsDirectory == forDirs &&
                    f.IsFile != forDirs));   // also f.IsVirtual exists

            //JEEWEE
            //foreach (DocumentFile f in fileOrFolders)
            //{
            //    if (forDirs && f.IsDirectory ||
            //        !forDirs && f.IsFile)
            //    {
            //        if (f.Name == searchFileOrFolderNameOrNull)
            //            return f;
            //        if (null != fileOrDirDocusOrNull)
            //            fileOrDirDocusOrNull.Add(f);
            //    }
            //}

            return null;
        }



        protected int GetNumDocumentFiles(DocumentFile documentFile)
        {
            // Thanks to ChatGPT
            var uri = DocumentsContract.BuildChildDocumentsUriUsingTree(_uriDir.Uri,
                DocumentsContract.GetDocumentId(_uriDir.Uri));

            using (var cursor = global::Android.App.Application.Context.ContentResolver.Query(uri, null, null, null, null))
            {
                if (cursor != null)
                {
                    return cursor.Count;
                }
            }

            return 0;
        }



        /// <summary>
        /// This class exists because DocumentFile.ListFiles() may take tremendous
        /// time, for example half a minute for 350 files in "Camera". But I use it
        /// only for navigation purpose, not for the DocumentFiles in the end folder,
        /// because things may have changed meanwhile (which theoretically also goes
        /// for navigation purpose, but that is VERY theoretically; it's only for
        /// example if "Camera" would have a subdir, and Client wants to goto there, to
        /// not have to get that complete list of DocumentFiles only to find that subdir)
        /// </summary>
        protected class NavigationCachePerPath
        {
            protected Dictionary<string, DocumentFile[]> _docusPerPath = new();

            /// <summary>
            /// It must already be sure this key is not yet in the dictionairy
            /// </summary>
            /// <param name="subPartsPath"></param>
            /// <param name="docus"></param>

            public DocumentFile[] GetDocusUpdCache(string joinedPath,
                            DocumentFile documentFileDir)
            {
                if (_docusPerPath.TryGetValue(joinedPath, out DocumentFile[] docus))
                    return docus;

                docus = documentFileDir.ListFiles();
                _docusPerPath.Add(joinedPath, docus);
                return docus;
            }
        }
    }
}
