using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;
using Android.Webkit;
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
        protected NavigationDirsCachePerPath _navDirsCachePerPath = new();

        public List<DocumentFile> ListDocumentFilesInUriAndSubpath(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, bool forDirs)
        {
            List<DocumentFile> fileOrDirDocusOrNull = new();

            UriAndSubpathCore(androidUri, dirNamesSubPath,
                        forDirs ? CoreMode.FILLLISTDIRS : CoreMode.FILLLISTFILES,
                        fileOrDirDocusOrNull, null,
                        out DocumentFile parentDirDocu, out bool pathCreated);

            return fileOrDirDocusOrNull;
        }


        /// <summary>
        /// It turns out that path in settings in Android may become invalid; this function enforces re-browse
        /// </summary>
        public void SetUriToNullIfInvalid(global::Android.Net.Uri androidUri)
        {
            UriAndSubpathCore(androidUri, new string[0], CoreMode.CHECKURIDIR, null, null,
                    out DocumentFile dummyParentDirDocu, out bool dummyPathCreated);
        }

        public DocumentFile GetDocumentFileFromUriAndSubpath(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, bool forDirs, string fileOrFolderName,
                    out DocumentFile parentDirDocu)
        {
            return UriAndSubpathCore(androidUri, dirNamesSubPath,
                        forDirs ? CoreMode.SEARCHDIR : CoreMode.SEARCHFILE,
                        null, fileOrFolderName,
                        out parentDirDocu, out bool pathCreated);
        }

        public BinaryReader GetBinaryReaderFromUriAndSubpath(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, string fileName)
        {
            return (BinaryReader)GetBinaryReaderOrWriterFromUriAndSubpath(false,
                    androidUri, dirNamesSubPath, fileName,
                    false, out bool fileExistedBeforeDummy);
        }


        /// <summary>
        /// Create path including sub paths, if not already existent
        /// </summary>
        /// <param name="androidUri"></param>
        /// <param name="dirNamesSubPathInclCreating"></param>
        /// <returns>true if created, false if already existent</returns>
        public bool CreatePathIfNonExistent(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPathInclCreating)
        {
            UriAndSubpathCore(androidUri, dirNamesSubPathInclCreating,
                        CoreMode.CREATEPATH, null, "",
                        out DocumentFile parentDirDocu, out bool pathCreated);
            return pathCreated;
        }


        /// <summary>
        /// Returns a BinaryWriter or null if skipWriteFileIfExisting is true and file already existed
        /// </summary>
        /// <param name="androidUri"></param>
        /// <param name="dirNamesSubPath"></param>
        /// <param name="fileName"></param>
        /// <param name="skipWriteFileIfExisting"></param>
        /// <param name="fileExistedBefore"></param>
        /// <returns></returns>
        public BinaryWriter GetBinaryWriterFromUriAndSubpath(global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, string fileName,
                    bool skipWriteFileIfExisting, out bool fileExistedBefore)
        {
            return (BinaryWriter)GetBinaryReaderOrWriterFromUriAndSubpath(true,
                    androidUri, dirNamesSubPath, fileName,
                    skipWriteFileIfExisting, out fileExistedBefore);
        }

        public object GetBinaryReaderOrWriterFromUriAndSubpath(bool writer,
                    global::Android.Net.Uri androidUri,
                    string[] dirNamesSubPath, string fileName,
                    bool writeSkipFileIfExisting, out bool writeFileExistedBefore)
        {
            try
            {
                DocumentFile documentFile = GetDocumentFileFromUriAndSubpath(androidUri,
                        dirNamesSubPath, false, fileName, out DocumentFile parentDirDocu);
                writeFileExistedBefore = null != documentFile;
                var context = global::Android.App.Application.Context;
                Stream? stream = null;
                if (writer)
                {
                    if (writeFileExistedBefore)
                    {
                        if (writeSkipFileIfExisting)
                            return null;
                        documentFile.Delete();
                    }
                    string mimeType = GetMimeTypeFromExtension(fileName);
                    documentFile = parentDirDocu.CreateFile(mimeType, fileName);
                    stream = context.ContentResolver.OpenOutputStream(documentFile.Uri);
                }
                else
                {
                    if (null != documentFile)
                        stream = context.ContentResolver.OpenInputStream(
                                        documentFile.Uri);
                }

                if (stream == null)
                {
                    string descr = writer ? "writer" : "reader";
                    throw new InvalidOperationException(
                        $"Cannot create {descr} stream");
                }
                return writer ? new BinaryWriter(stream) : new BinaryReader(stream);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Exception trying to read " +
                        FileDataService.DispRelPath(dirNamesSubPath, fileName) +
                        ": " + exc.Message);
            }
        }


        protected enum CoreMode
        {
            SEARCHDIR,
            SEARCHFILE,
            FILLLISTDIRS,
            FILLLISTFILES,
            CREATEPATH,
            CHECKURIDIR,
        }


        /// <summary>
        /// Core for various methods. Returns Android DocumentFile if
        /// mode is searchmode, else returns null
        /// </summary>
        /// <param name="androidUri"></param>
        /// <param name="dirNamesSubPath"></param>
        /// <param name="mode"></param>
        /// <param name="fileOrDirDocusOrNull">if non-null, file or folder DoucumentFile's are added</param>
        /// <param name="searchFileOrFolderNameOrNull">null if only fileOrDirNamesOrNull intended </param>
        /// <param name="parentDirDocu">if dirNamesSubPath Length 0, results _uriDir</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected DocumentFile UriAndSubpathCore(global::Android.Net.Uri androidUri,
                string[] dirNamesSubPath, CoreMode mode,
                List<DocumentFile> fileOrDirDocusOrNull, string searchFileOrFolderNameOrNull,
                out DocumentFile parentDirDocu, out bool pathCreated)
        {
            if (null == _uriDir)
            {
                var context = global::Android.App.Application.Context;
                _uriDir = DocumentFile.FromTreeUri(context, androidUri);
            }

            pathCreated = false;
            string joinedPath = "";
            string parentJoinedPath = "";

            if (null == _uriDir || !_uriDir.IsDirectory)
            {
                if (mode == CoreMode.CHECKURIDIR)
                {
                    _uriDir = null;
                    parentDirDocu = null;
                    MauiProgram.Settings.AndroidUriRoot = null;
                    //JEEWEE!!!!!!!!!!!!!!!!!!!!!!!!!! MAAR DAT $%^HELPT NIET! LABEL BLIJFT PATH!
                    return null;
                }

                throw new Exception($"AndroidUri is invalid or not a directory: {androidUri.ToString()}");
            }

            parentDirDocu = _uriDir;

            if (mode == CoreMode.CHECKURIDIR)
                return null;

            bool forDirs = mode == CoreMode.SEARCHDIR ||
                mode == CoreMode.FILLLISTDIRS ||
                mode == CoreMode.CREATEPATH;

            DocumentFile[] docusDirs = _navDirsCachePerPath.GetDocusDirsUpdCache(
                        joinedPath, _uriDir);
            for (int i = 0; i < dirNamesSubPath.Length; i++)
            {
                string dirName = dirNamesSubPath[i];
                joinedPath += (String.IsNullOrEmpty(joinedPath) ? "" : "/") + dirName;
                DocumentFile subDirDocu = docusDirs.Where(d => d.Name == dirName).FirstOrDefault();
                if (subDirDocu == null)
                {
                    if (mode == CoreMode.CREATEPATH)
                    {
                        subDirDocu = parentDirDocu.CreateDirectory(dirName);
                        if (null == subDirDocu)
                            throw new Exception($"Android: could not create path '{joinedPath}'");

                        pathCreated = true;
                        docusDirs = CopyMgr.AddOneToArray(docusDirs, subDirDocu);
                        _navDirsCachePerPath.SetDocusDirs(parentJoinedPath, docusDirs);
                    }
                    else
                    {
                        throw new Exception(
                            $"UriAndSubpathCore: error finding sub path '{joinedPath}'");
                    }
                }
                parentDirDocu = subDirDocu;
                parentJoinedPath = joinedPath;
                if (i < dirNamesSubPath.Length - 1)     // GetDocusDirsUpdCache is expensive
                    docusDirs = _navDirsCachePerPath.GetDocusDirsUpdCache(joinedPath, subDirDocu);
            }

            if (mode == CoreMode.CREATEPATH)
                return null;

            DocumentFile[] fileOrFolders = parentDirDocu.ListFiles();
            if (mode == CoreMode.SEARCHDIR || mode == CoreMode.SEARCHFILE)
            {
                return fileOrFolders.FirstOrDefault(f =>
                    f.Name == searchFileOrFolderNameOrNull &&
                    f.IsDirectory == forDirs &&
                    f.IsFile != forDirs);   // also f.IsVirtual exists
            }

            // CoreMode.FILLLISTDIRS || CoreMode.FILLLISTFILES
            fileOrDirDocusOrNull.AddRange(fileOrFolders.Where(f =>
                f.IsDirectory == forDirs &&
                f.IsFile != forDirs));   // also f.IsVirtual exists

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
        protected class NavigationDirsCachePerPath
        {
            protected Dictionary<string, DocumentFile[]> _docusDirsPerPath = new();

            /// <summary>
            /// Get DocumentFile[] of the dirs, from cache or dictionairy
            /// May take time if there are many DocumentFile's, because ListFiles() then takes much time
            /// </summary>
            /// <param name="joinedPath"></param>
            /// <param name="documentFileParentDir"></param>

            public DocumentFile[] GetDocusDirsUpdCache(string joinedPath,
                            DocumentFile documentFileParentDir)
            {
                if (_docusDirsPerPath.TryGetValue(joinedPath, out DocumentFile[] docusDirs))
                    return docusDirs;

                docusDirs = documentFileParentDir.ListFiles().Where(d => d.IsDirectory).ToArray();
                _docusDirsPerPath.Add(joinedPath, docusDirs);
                return docusDirs;
            }


            /// <summary>
            /// Sets 'docusDir' as value for key 'joinedPath', or add the pair
            /// </summary>
            /// <param name="joinedPath"></param>
            /// <param name="docusDir"></param>
            public void SetDocusDirs(string joinedPath, DocumentFile[] docusDir)
            {
                if (_docusDirsPerPath.ContainsKey(joinedPath))
                    _docusDirsPerPath[joinedPath] = docusDir;
                else
                    _docusDirsPerPath.Add(joinedPath, docusDir);
            }
        }


        protected string GetMimeTypeFromExtension(string fileName)
        {
            // slightly changed ChatGPT code
            string extension = MimeTypeMap.GetFileExtensionFromUrl(fileName)?.ToLower();
            if (!string.IsNullOrEmpty(extension))
            {
                string mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(
                                extension);
                if (!string.IsNullOrEmpty(mimeType))
                    return mimeType;
            }

            return "application/octet-stream"; // good default fallback, says ChatGPT
        }
    }
}
