using System;
using System.Collections.Generic;
using DeDuplicate.Bll;
using DeDuplicate.Bll.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace DeDuplicate.Tests
{
    [TestClass]
    public class DuplicateTests
    {
        private static string _fileName = "test.txt";
        private static string _folder = "Duplicater";

        private static string _filePath01
        {
            get { return Path.Combine(_folderPath01, _fileName); }
        }
        private static string _filePath02
        {
            get { return Path.Combine(_folderPath02, _fileName); }
        }

        private static string _folderPath01
        {
            get { return Path.Combine(Path.GetTempPath(), _folder); }
        }
        private static string _folderPath02
        {
            get { return Path.Combine(Path.GetTempPath(), _folder, "02"); }
        }

        [TestMethod]
        public void CreateTestDataAndRemove()
        {
            DeleteTempFolders();
            _fileName = "file1.txt";
            CreateContent(_filePath01, "This is the data to include - expected deletion");
            _fileName = "file2.txt";
            CreateContent(_filePath01, "This is the data to include - shouldn't be deleted");
            _fileName = "file3.txt";
            CreateContent(_filePath02, "This is the data to include - expected deletion");

            Bll.Duplication duplication = new Duplication(_folderPath01, _folderPath01);

            duplication.GetDuplicateList(null, Done, null);


        }

        private void Done(List<Duplicate> duplicateList)
        {
            if (duplicateList.Count < 1)
                Assert.Fail("Expected only 1 item, there are " + duplicateList.Count);

            DeleteTempFolders();
        }

        private void CreateContent(string folderPath, string content)
        {
            CreateTempFolders(folderPath);
            using (StreamWriter sw = new StreamWriter(folderPath))
            {
                sw.Write(content);
            }
        }

        private void CreateTempFolders(string folderPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(folderPath));
        }

        private void DeleteTempFolders()
        {
            if (Directory.Exists(_folderPath01))
                Directory.Delete(_folderPath01, true);
        }

        [TestMethod]
        public void TestFilesDoNotMatch()
        {
            //    DeleteTempFolders();
            //    CreateContent(_folderPath01, "This is the data to include");
            //    CreateContent(_folderPath02, "This is the DIFFERENT data to include");

            //    Bll.Duplication duplication = new Duplication();
            //    duplication.SourcePath = Path.GetDirectoryName(_folderPath01);
            //    List<Duplicate> duplicateList = duplication.GetDuplicateList();

            //    if (duplicateList.Count > 1)
            //        Assert.Fail("Expected only 0 items, there are " + duplicateList.Count);

            //    DeleteTempFolders();
        }
    }
}
