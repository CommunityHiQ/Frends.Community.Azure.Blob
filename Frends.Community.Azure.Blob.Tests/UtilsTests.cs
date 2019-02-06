using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.Community.Azure.Blob.Tests
{
    [TestClass]
    public class UtilsTests
    {
        private string _existingFileName;
        private FileInfo _file;
        private string _testDirectory;
        private string _testPath;

        [TestInitialize]
        public void TestSetup()
        {
            _existingFileName = "existing_file.txt";
            // create test folder 
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _testPath = Path.Combine(_testDirectory, _existingFileName);
            File.WriteAllText(_testPath, "I'm walking here! I'm walking here!");
            _file = new FileInfo(_testPath);

            if (!_file.Exists) throw new Exception("File Be Not Present.");
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

        [TestMethod]
        public void GetStream_ReturnsReadableStream()
        {
            // UploadAsync needs readable stream.
            using (var stream = Utils.GetStream(false, false, Encoding.UTF8, _file))
            {
                Assert.IsTrue(stream.CanRead);
            }

            using (var stream = Utils.GetStream(false, true, Encoding.UTF8, _file))
            {
                Assert.IsTrue(stream.CanRead);
            }

            using (var stream = Utils.GetStream(true, false, Encoding.UTF8, _file))
            {
                Assert.IsTrue(stream.CanRead);
            }

            using (var stream = Utils.GetStream(true, true, Encoding.UTF8, _file))
            {
                Assert.IsTrue(stream.CanRead);
            }

            using (var file = _file.Open(FileMode.Open, FileAccess.Read))
            {
                Assert.IsTrue(file.CanRead); // == streams dispose and file is closed properly.
            }
        }

        [TestMethod]
        public void GetStream_ReturnsFileUncompressed()
        {
            using (var stream = Utils.GetStream(false, true, Encoding.UTF8, _file))
            {
                Assert.AreEqual(
                    stream.Length,
                    Encoding.UTF8.GetBytes(File.ReadAllText(_file.FullName)).Length
                );
            }
        }

        [TestMethod]
        public void GetStream_ReturnsFileCompressed()
        {
            using (var stream = Utils.GetStream(true, false, Encoding.UTF8, _file))
            {
                Assert.AreNotEqual(
                    stream.Length,
                    Encoding.UTF8.GetBytes(File.ReadAllText(_file.FullName)).Length
                );
            }
        }

        [TestMethod]
        public void GetStream_ReturnsCompressedMemoryStream()
        {
            using (var stream = Utils.GetStream(true, false, Encoding.UTF8, _file))
            {
                Assert.IsTrue(
                    stream.Length != Encoding.UTF8.GetBytes(File.ReadAllText(_file.FullName)).Length &&
                    stream is MemoryStream
                );
            }
        }
    }
}