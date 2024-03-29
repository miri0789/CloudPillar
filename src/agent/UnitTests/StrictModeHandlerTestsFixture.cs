using Moq;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers.Interfaces;
using Microsoft.Extensions.FileSystemGlobbing;
using CloudPillar.Agent.Wrappers;

namespace CloudPillar.Agent.Handlers.Tests
{
    [TestFixture]
    public class StrictModeHandlerTests
    {
        private StrictModeSettings mockStrictModeSettingsValue = new StrictModeSettings();
        private Mock<IOptions<StrictModeSettings>> mockStrictModeSettings;
        private Mock<IMatcherWrapper> mockMatchWrapper;
        private Mock<ILoggerHandler> mockLogger;
        private Mock<FileStreamerWrapper> mockFileStreamer;
        private StrictModeHandler _target;
        private const TwinActionType UPLAOD_ACTION = TwinActionType.SingularUpload;
        private const TwinActionType DOWNLOAD_ACTION = TwinActionType.SingularDownload;

        [SetUp]
        public void Setup()
        {
            mockStrictModeSettings = new Mock<IOptions<StrictModeSettings>>();
            mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock();
            mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);

            mockFileStreamer = new Mock<FileStreamerWrapper>();
            mockMatchWrapper = new Mock<IMatcherWrapper>();
            mockLogger = new Mock<ILoggerHandler>();
            SetMatchResult("test.txt", "");
            _target = new StrictModeHandler(mockStrictModeSettings.Object, mockMatchWrapper.Object, mockFileStreamer.Object, mockLogger.Object);

        }
        [Test]
        public void ReplaceRootById_ValidData_ReturnReplacedString()
        {
            var fileName = $"${{{StrictModeMockHelper.UPLOAD_KEY}}}test.txt";
            var replacedFileName = $"{StrictModeMockHelper.ROOT_UPLOAD}test.txt";

            var res = _target.ReplaceRootById(UPLAOD_ACTION, fileName);

            Assert.AreEqual(res, replacedFileName);
        }

        [Test]
        public void ReplaceRootById_NoRootValue_ThrowException()
        {

            var fileName = $"${{{StrictModeMockHelper.UPLOAD_KEY}}}test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.UPLOAD).Root = "";

            Assert.Throws<KeyNotFoundException>(() =>
              {
                  _target.ReplaceRootById(UPLAOD_ACTION, fileName);
              }, ResultCode.StrictModeRootNotFound.ToString());
        }

        [Test]
        public void ReplaceRootById_KeyNotFound_ThrowException()
        {
            var fileName = "${}/test.txt";

            Assert.Throws<ArgumentException>(() =>
              {
                  _target.ReplaceRootById(UPLAOD_ACTION, fileName);
              });
        }


        [Test]
        public void CheckSizeStrictMode_ZeroSize_Return()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_DOWNLOAD}/test.txt";

            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.DOWNLOAD).MaxSize = 0;
            mockMatchWrapper.Verify(x => x.IsMatch(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void CheckSizeStrictMode_NanSize_Return()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_DOWNLOAD}/test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.DOWNLOAD).MaxSize = null;
            mockMatchWrapper.Verify(x => x.IsMatch(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void CheckSizeStrictMode_SizeExceedsLimit_ThrowException()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_DOWNLOAD}/test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.DOWNLOAD).MaxSize = 2;

            Assert.Throws<ArgumentException>(() =>
               {
                   _target.CheckSizeStrictMode(DOWNLOAD_ACTION, 9, fileName);

               }, ResultCode.StrictModeSize.ToString());
        }

        [Test]
        public void CheckFileAccessPermissions_StrictModeFalse_ExitingTheFunction()
        {
            mockStrictModeSettingsValue.StrictMode = false;
            _target.CheckFileAccessPermissions(UPLAOD_ACTION, "");

            mockMatchWrapper.Verify(x => x.IsMatch(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void CheckFileAccessPermissions_NotExistsZoneWithGlobalPatterns_ThrowFormatException()
        {
            SetMatchResult(StrictModeMockHelper.ROOT_GLOBAL, "");

            var fileName = StrictModeMockHelper.ROOT_GLOBAL;

            Assert.Throws<FormatException>(() =>
                          {
                              _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

                          }, ResultCode.StrictModeSize.ToString());

        }
        [Test]
        public void CheckFileAccessPermissions_NotExistsZoneAndNoGlobalPatterns_ThrowFormatException()
        {
            var fileName = "notExistsZone";
            mockStrictModeSettingsValue.GlobalPatterns = new List<string>();

            Assert.Throws<FormatException>(() =>
                          {
                              _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

                          }, ResultCode.StrictModeSize.ToString());
        }

        [Test]
        public void CheckFileAccessPermissions_NotAllowPatterns_ThrowFormatException()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_GLOBAL}/test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.UPLOAD).AllowPatterns = new List<string>();

            Assert.Throws<FormatException>(() =>
                         {
                             _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

                         }, ResultCode.StrictModeSize.ToString());
        }
        [Test]
        public void CheckFileAccessPermissions_NotAllowPatternsAndNoGlobalPattern_ThrowFormatException()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_UPLOAD}/test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.UPLOAD).AllowPatterns = new List<string>();
            mockStrictModeSettingsValue.GlobalPatterns = new List<string>();

            Assert.Throws<FormatException>(() =>
                         {
                             _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

                         }, ResultCode.StrictModeSize.ToString());
        }


        [Test]
        public void CheckFileAccessPermissions_GlobalPatternStartWithDoubleAstreisk_RootPathFromFile()
        {
            SetMatchResult("globalupload/test.txt", "");
            var fileName = $"{StrictModeMockHelper.ROOT_GLOBAL}/test.txt";
            mockStrictModeSettingsValue.GlobalPatterns = new List<string>() { $"{FileConstants.DOUBLE_ASTERISK}/test.txt" };
            mockStrictModeSettingsValue.FilesRestrictions = new List<FileRestrictionDetails>()
            {
                new FileRestrictionDetails()
                {
                    Id = "LogUploadAllow", Type = "Upload", Root = StrictModeMockHelper.ROOT, AllowPatterns = new List<string>() { "**" } }
            };
            _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            mockMatchWrapper.Verify(x => x.IsMatch(It.Is<string[]>(x => x.Contains($"{FileConstants.DOUBLE_ASTERISK}/test.txt")), It.Is<string>(x => x == "c:/"), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void CheckFileAccessPermissions_GlobalPatternContainsDoubleAstreisk_SplitPatternToRootAndPAttern()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_GLOBAL}/test.txt";
            mockStrictModeSettingsValue.GlobalPatterns = new List<string>() { $"{StrictModeMockHelper.ROOT_GLOBAL}/{FileConstants.DOUBLE_ASTERISK}/test.txt" };
            mockStrictModeSettingsValue.FilesRestrictions = new List<FileRestrictionDetails>()
            {
                new FileRestrictionDetails()
                {
                    Id = "LogUploadAllow", Type = "Upload", Root = StrictModeMockHelper.ROOT_GLOBAL, AllowPatterns = new List<string>() { "**" } }
            };

            _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            mockMatchWrapper.Verify(x => x.IsMatch(It.Is<string[]>(x => x.Contains("**/test.txt")), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void CheckFileAccessPermissions_GlobalPatternAbsolutePath_RootFromPattern()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_GLOBAL}/test.txt";
            mockStrictModeSettingsValue.GlobalPatterns = new List<string>() { $"{StrictModeMockHelper.ROOT_GLOBAL}/test.txt" };
            mockStrictModeSettingsValue.FilesRestrictions = new List<FileRestrictionDetails>()
            {
                new FileRestrictionDetails()
                {
                    Id = "LogUploadAllow", Type = "Upload", Root = StrictModeMockHelper.ROOT_GLOBAL, AllowPatterns = new List<string>() { "**" } }
            };

            _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            mockMatchWrapper.Verify(x => x.IsMatch(It.Is<string[]>(x => x.Contains("test.txt")), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void CheckFileAccessPermissions_EmptyPatternString_ThrowException()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_UPLOAD}/test.txt";
            mockStrictModeSettingsValue.GlobalPatterns = new List<string>();

            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.UPLOAD).AllowPatterns = new List<string>() { "" };

            Assert.Throws<FormatException>(() =>
                          {
                              _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

                          }, ResultCode.StrictModeSize.ToString());
        }
        [Test]
        public void CheckFileAccessPermissions_Backslashes_FindRestriction()
        {
            _target.CheckFileAccessPermissions(UPLAOD_ACTION, "c:\\demoUpload\\test.txt");

            mockMatchWrapper.Verify(x => x.IsMatch(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
        [Test]
        public void CheckFileAccessPermissions_MatchPatternToFileName_NoThrowing()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_UPLOAD}/test.txt";

            _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            mockMatchWrapper.Verify(x => x.IsMatch(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
        [Test]
        public void CheckFileAccessPermissions_NoMatchPatternToFileName_ThrowException()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_UPLOAD}/img.png";

            Assert.Throws<FormatException>(() =>
            {
                _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);
            }, ResultCode.StrictModePattern.ToString());
        }

        [Test]
        public void CheckFileAccessPermissions_CamelCase_NoExceptionThrown()
        {
            var upperLetterRoot = StrictModeMockHelper.ROOT_UPLOAD.ToUpper();
            var lowerLetterRoot = StrictModeMockHelper.ROOT_UPLOAD.ToLower();

            var fileName = $"{upperLetterRoot}/test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.UPLOAD).Root = lowerLetterRoot;

            _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            mockMatchWrapper.Verify(x => x.IsMatch(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private void SetMatchResult(string path, string stem = "")
        {
            mockMatchWrapper.Setup(x => x.IsMatch(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>()))
              .Returns(new PatternMatchingResult(new List<FilePatternMatch>() { new FilePatternMatch(path, stem) }, true));
        }
    }
}
