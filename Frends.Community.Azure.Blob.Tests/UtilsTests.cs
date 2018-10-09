using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class UtilsTests
    {
        private string _testDirectory;
        private string _existingFileName;

        [TestInitialize]
        public void TestSetup()
        {
            _existingFileName = "existing_file.txt";
            // create test folder 
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            File.WriteAllText(Path.Combine(_testDirectory, _existingFileName), "I'm walking here! I'm walking here!");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // remove all test files and directory
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        [TestMethod]
        public void GetRenamedFileName_DoesNotRename_If_FileName_Is_Available()
        {
            var fileName = "new_file_name.txt";
            var result = Utils.GetRenamedFileName(fileName, _testDirectory);

            Assert.AreEqual(fileName, result);
        }

        [TestMethod]
        public void GetRenamedFileName_AddsNumberInParenthesis()
        {
            var result = Utils.GetRenamedFileName(_existingFileName, _testDirectory);

            Assert.AreNotEqual(_existingFileName, result);
            Assert.IsTrue(result.Contains("(1)"));
        }

        [TestMethod]
        public void GetRenamedFileName_IncrementsNumberUntillAvailableFileNameIsFound()
        {
            //create files ..(1) to ...(10)
            for (var i = 0; i < 10; i++)
            {
                var fileName = Utils.GetRenamedFileName(_existingFileName, _testDirectory);
                File.WriteAllText(Path.Combine(_testDirectory, fileName), "You can't handle the truth!");
            }

            // we should have 11 files in test directory, last being 'existing_file(10).txt'
            Assert.AreEqual(11, Directory.GetFiles(_testDirectory, "*.txt").Length);
            Assert.IsTrue(File.Exists(Path.Combine(_testDirectory, "existing_file(10).txt")));
        }
    }
}
