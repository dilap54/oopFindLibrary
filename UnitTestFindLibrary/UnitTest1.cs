using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using FindLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestFindLibrary
{
    [TestClass]
    public class UnitTest1
    {
        private TestContext testContextInstance;
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        DirectoryInfo testDir;
        [TestInitialize]
        public void CreatingTmpFiles()
        {
            Directory.CreateDirectory(testContextInstance.TestRunDirectory+"\\tmpFiles");
            testDir = new DirectoryInfo(testContextInstance.TestRunDirectory + "\\tmpFiles");
           
            using (StreamWriter sw = File.CreateText(testDir.FullName + "\\searchString.txt"))
            {
                sw.WriteLine("searchString");
                sw.WriteLine("And");
                sw.WriteLine("Welcome");
                sw.Close();
            }

            using (StreamWriter sw = File.CreateText(testDir.FullName + "\\top3Passwords.csv"))
            {
                sw.WriteLine("123456");
                sw.WriteLine("admin");
                sw.WriteLine("password");
                sw.Close();
            }
        }

        [TestCleanup]
        public void DeletingTmpFiles()
        {
            Directory.Delete(testDir.FullName, true);
        }

        [TestMethod]
        [ExpectedException(typeof(System.IO.DirectoryNotFoundException))]
        public async Task ThrowExceptionIfDirectoryIsNotExist()
        {
            List<FileInfo> fileInfoList = await FileFind.Find(testDir.FullName + "\\nonexistentDir", "*", "searchString");
            //Assert.AreEqual(0, fileInfoList.Count);
        }

        //Возвращает все файлы в папке, если строка содержимого файла
        [TestMethod]
        public async Task ReturnAllFilesIfSearchStringEmpty()
        {
            List<FileInfo> fileInfoList = await FileFind.Find(testDir.FullName, "*", "");
            Assert.AreEqual(2, fileInfoList.Count);
        }

        //Возвращает top3Passwords.csv, если ищется 123456
        [TestMethod]
        public async Task ReturnPasswordsFileIfSearchString123456()
        {
            List<FileInfo> fileInfoList = await FileFind.Find(testDir.FullName, "*", "123456");

            Assert.AreEqual(1, fileInfoList.Count);

            Assert.IsTrue(fileInfoList.Exists((file)=> {
                return file.Name == "top3Passwords.csv";
            }));
        }

        //Возвращает файлы .txt, если маска содержит *.txt
        [TestMethod]
        public async Task ReturnFilesWithTxtExtension()
        {
            List<FileInfo> fileInfoList = await FileFind.Find(testDir.FullName, "*.txt", "");

            Assert.AreEqual(1, fileInfoList.Count);

            Assert.IsTrue(fileInfoList.Exists((file) =>
            {
                return file.Extension == ".txt";
            }));
        }
    }
}
