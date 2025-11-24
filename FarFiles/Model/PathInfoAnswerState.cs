using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    /// <summary>
    /// Class for Server that contains all file and folder data of current path, to be
    /// returned in parts, first folders, then files.
    /// </summary>
    public class PathInfoAnswerState
    {
        protected string[] _folderNames;
        protected string[] _fileNames;
        protected long[] _fileSizes;
        protected DateTime[] _dtLastWrites;

        protected int _idx0isFolders1isFiles;
        protected int _idxInArray;

        public bool EndReached { get => 1 == _idx0isFolders1isFiles &&
                    _idxInArray >= _fileNames.Length; }
        public PathInfoAnswerState(string[] folderNames, string[] fileNames, long[] fileSizes,
                        DateTime[] dtLastWrites)
        {
            if (fileNames.Length != fileSizes.Length || fileNames.Length != dtLastWrites.Length)
                throw new Exception(
                    $"PROGRAMMERS: PathInfoAnswerState ctor: fileNames.Length={fileNames.Length}" +
                    $", fileSizes.Length={fileSizes.Length}, dtLastWrites.Length={dtLastWrites.Length}, ");

            _folderNames = folderNames;
            _fileNames = fileNames;
            _fileSizes = fileSizes;
            _dtLastWrites = dtLastWrites;
            _idx0isFolders1isFiles = 0;
            _idxInArray = 0;
        }


        /// <summary>
        /// Gets next folder or file data and adjusts internal indexes
        /// </summary>
        /// <param name="isDir"></param>
        /// <param name="name"></param>
        /// <param name="fileSize"></param>
        /// <returns>true if data valid, false if there is no next</returns>
        public bool GetNextFileOrFolder(out bool isDir, out string name, out long fileSize,
                out DateTime dtLastWrite)
        {
            if (0 == _idx0isFolders1isFiles)
            {
                if (_idxInArray < _folderNames.Length)
                {
                    isDir = true;
                    name = _folderNames[_idxInArray++];
                    fileSize = 0;
                    dtLastWrite = FileOrFolderData.MINDT2000;
                    return true;
                }

                _idx0isFolders1isFiles++;
                _idxInArray = 0;
            }

            if (_idxInArray < _fileNames.Length)
            {
                isDir = false;
                name = _fileNames[_idxInArray];
                fileSize = _fileSizes[_idxInArray];
                dtLastWrite = _dtLastWrites[_idxInArray++];
                return true;
            }

            isDir = false;
            name = "";
            fileSize = 0;
            dtLastWrite = FileOrFolderData.MINDT2000;
            return false;
        }
    }
}
