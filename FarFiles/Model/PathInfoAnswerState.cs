using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarFiles.Model
{
    public class PathInfoAnswerState
    {
        public string[] FolderNames;
        public string[] FileNames;
        public long[] FileSizes;

        public int Idx0isFolders1isFiles;
        public int IdxInArray;
        public int SeqNrAnswer;

        public bool EndReached { get => 1 == Idx0isFolders1isFiles &&
                    IdxInArray >= FileNames.Length; }
        public PathInfoAnswerState(string[] folderNames, string[] fileNames, long[] fileSizes)
        {
            if (fileNames.Length != fileSizes.Length)
                throw new Exception(
                    $"PROGRAMMERS: PathInfoAnswerState ctor: fileNames.Length={fileNames.Length}" +
                    $", fileSizes.Length={fileSizes.Length}");

            FolderNames = folderNames;
            FileNames = fileNames;
            FileSizes = fileSizes;
            Idx0isFolders1isFiles = 0;
            IdxInArray = 0;
            SeqNrAnswer = 0;
        }
    }
}
