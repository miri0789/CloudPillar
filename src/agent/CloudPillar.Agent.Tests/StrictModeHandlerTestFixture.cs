using Moq;
using Shared.Logger;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers.Tests
{
    [TestFixture]
    public class StrictModeHandlerTests
    {
        private AppSettings mockAppSettingsValue = new AppSettings();
        private Mock<IOptions<AppSettings>> mockAppSettings;
        private Mock<ILoggerHandler> mockLogger;
        private StrictModeHandler _target;
        private const TwinActionType UPLAOD_ACTION = TwinActionType.SingularUpload;
        private const TwinActionType DOWNLOAD_ACTION = TwinActionType.SingularDownload;
        private const string DOWNLOAD = "Download";
        private const string UPLOAD = "Upload";
        private const string ROOT_UPLOAD = "c:/demoUpload/";
        private const string ROOT_DOWNLOAD = "c:/demoDownload/";
        private const string KEY = "LogUploadAllow";

        [SetUp]
        public void Setup()
        {
            SetAppSettingsValueMock();

            mockAppSettings = new Mock<IOptions<AppSettings>>();
            mockAppSettings.Setup(x => x.Value).Returns(mockAppSettingsValue);

            mockLogger = new Mock<ILoggerHandler>();

            _target = new StrictModeHandler(mockAppSettings.Object, mockLogger.Object);
        }
        [Test]
        public void ReplaceRootById_ValidData_ReturnReplacedString()
        {
            var fileName = "${" + KEY + "}test.txt";
            var replacedFileName = $"{ROOT_UPLOAD}test.txt";

            var res = _target.ReplaceRootById(UPLAOD_ACTION, fileName);

            Assert.AreEqual(res, replacedFileName);
        }

        [Test]
        public void ReplaceRootById_NoRootValue_ThrowException()
        {

            var fileName = "${" + KEY + "}test.txt";
            mockAppSettingsValue.FilesRestrictions.First(x => x.Type == UPLOAD).Root = "";

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
            var fileName = $"{ROOT_DOWNLOAD}/test.txt";

            mockAppSettingsValue.FilesRestrictions.First(x => x.Type == DOWNLOAD).MaxSize = 0;
            void SendRequest() => _target.CheckSizeStrictMode(DOWNLOAD_ACTION, 9, fileName);

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckSizeStrictMode_NanSize_Return()
        {
            var fileName = $"{ROOT_DOWNLOAD}/test.txt";
            mockAppSettingsValue.FilesRestrictions.First(x => x.Type == DOWNLOAD).MaxSize = null;
            void SendRequest() => _target.CheckSizeStrictMode(DOWNLOAD_ACTION, 9, fileName);

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckSizeStrictMode_SizeExceedsLimit_ThrowException()
        {
            var fileName = $"{ROOT_DOWNLOAD}/test.txt";
            mockAppSettingsValue.FilesRestrictions.First(x => x.Type == DOWNLOAD).MaxSize = 2;

            Assert.Throws<ArgumentException>(() =>
               {
                   _target.CheckSizeStrictMode(DOWNLOAD_ACTION, 9, fileName);

               }, ResultCode.StrictModeSize.ToString());
        }

        [Test]
        public void CheckFileAccessPermissions_StrictModeFalse_ExitingTheFunction()
        {
            mockAppSettingsValue.StrictMode = false;
            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, "");

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckFileAccessPermissions_NotExistsZone_ExitingTheFunction()
        {
            var fileName = "notExistsZone";
            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckFileAccessPermissions_NotAllowPatterns_ExitingTheFunction()
        {
            var fileName = $"{ROOT_UPLOAD}/test.txt";
            mockAppSettingsValue.FilesRestrictions.First(x => x.Type == UPLOAD).AllowPatterns = new List<string>();

            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckFileAccessPermissions_EmptyPatternString_ThrowException()
        {
            var fileName = $"{ROOT_UPLOAD}/test.txt";
            mockAppSettingsValue.FilesRestrictions.First(x => x.Type == UPLOAD).AllowPatterns = new List<string>() { "" };

            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckFileAccessPermissions_MatchPatternToFileName_NoThrowing()
        {
            var fileName = $"{ROOT_UPLOAD}/test.txt";
            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            Assert.DoesNotThrow(SendRequest);
        }
        [Test]
        public void CheckFileAccessPermissions_NoMatchPatternToFileName_ThrowException()
        {
            var fileName = $"{ROOT_UPLOAD}/img.png";

            Assert.Throws<FormatException>(() =>
            {
                _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);
            }, ResultCode.StrictModePattern.ToString());
        }

        private void SetAppSettingsValueMock()
        {
            var uploadRestrictionDetails = new FileRestrictionDetails()
            {
                Id = "LogUploadAllow",
                Type = "Upload",
                Root = ROOT_UPLOAD,
                AllowPatterns = new List<string>
                {
                    "*.txt"
                },
                DenyPatterns = new List<string>() // Add any deny patterns here if needed
            };
            var downloadRestrictionDetails = new FileRestrictionDetails()
            {
                Id = "LogDownloadAllow",
                Type = "Download",
                Root = ROOT_DOWNLOAD,
                MaxSize = 1,
                AllowPatterns = new List<string>
                {
                    "**/*.log",
                    "*/*.png",
                    "*.txt"
                },
                DenyPatterns = new List<string>() // Add any deny patterns here if needed
            };

            mockAppSettingsValue = new AppSettings()
            {
                StrictMode = true,
                FilesRestrictions = new List<FileRestrictionDetails> { uploadRestrictionDetails, downloadRestrictionDetails }
            };

        }
    }
}
