using Moq;
using Shared.Logger;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers.Tests
{
    [TestFixture]
    public class StrictModeHandlerTests
    {
        private StrictModeSettings mockStrictModeSettingsValue = new StrictModeSettings();
        private Mock<IOptions<StrictModeSettings>> mockStrictModeSettings;
        private Mock<ILoggerHandler> mockLogger;
        private StrictModeHandler _target;
        private const TwinActionType UPLAOD_ACTION = TwinActionType.SingularUpload;
        private const TwinActionType DOWNLOAD_ACTION = TwinActionType.SingularDownload;

        [SetUp]
        public void Setup()
        {
            mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock();

            mockStrictModeSettings = new Mock<IOptions<StrictModeSettings>>();
            mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);

            mockLogger = new Mock<ILoggerHandler>();

            _target = new StrictModeHandler(mockStrictModeSettings.Object, mockLogger.Object);
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
            void SendRequest() => _target.CheckSizeStrictMode(DOWNLOAD_ACTION, 9, fileName);

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckSizeStrictMode_NanSize_Return()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_DOWNLOAD}/test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.DOWNLOAD).MaxSize = null;
            void SendRequest() => _target.CheckSizeStrictMode(DOWNLOAD_ACTION, 9, fileName);

            Assert.DoesNotThrow(SendRequest);
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
            var fileName = $"{StrictModeMockHelper.ROOT_UPLOAD}/test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.UPLOAD).AllowPatterns = new List<string>();

            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckFileAccessPermissions_EmptyPatternString_ThrowException()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_UPLOAD}/test.txt";
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.UPLOAD).AllowPatterns = new List<string>() { "" };

            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            Assert.DoesNotThrow(SendRequest);
        }

        [Test]
        public void CheckFileAccessPermissions_MatchPatternToFileName_NoThrowing()
        {
            var fileName = $"{StrictModeMockHelper.ROOT_UPLOAD}/test.txt";

            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            Assert.DoesNotThrow(SendRequest);
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

            void SendRequest() => _target.CheckFileAccessPermissions(UPLAOD_ACTION, fileName);

            Assert.DoesNotThrow(SendRequest);
        }
    }
}
