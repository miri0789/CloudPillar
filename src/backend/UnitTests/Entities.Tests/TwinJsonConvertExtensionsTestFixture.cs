
using Shared.Entities.Twin;
using Shared.Entities.Utilities;

namespace Backend.Entities.Tests;


public class TwinJsonConvertExtensionsTestFixture
{

    TwinReported twinReported;
    TwinDesired twinDesired;
    TwinReportedChangeSpec twinReportedChangeSpec;
    string changeSpecServerIdentityKey = "ChangeSpecServerIdentity";
    string changeSignKey = "changeSpecSign";

    [SetUp]
    public void Setup()
    {
        SetDesiredAndReported();
    }

    [Test]
    public void GetReportedChangeSpecByKey_ExistsKey_ReturnCorrectValue()
    {
        var changeSpecKey = TwinConstants.CHANGE_SPEC_DIAGNOSTICS_NAME;

        var expectedResult = twinReported.ChangeSpec[changeSpecKey];

        var result = twinReported.GetReportedChangeSpecByKey(changeSpecKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetReportedChangeSpecByKey_NonExistsKey_ReturnNull()
    {

        var result = twinReported.GetReportedChangeSpecByKey(changeSpecServerIdentityKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(result, null), true);
    }

    [Test]
    public void GetReportedChangeSpecByKey_UpperCaseLettersKey_ReturCorrectValue()
    {

        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;
        twinReported.ChangeSpec.Where(x => x.Key == changeSpecKey).FirstOrDefault().Key.ToLower();

        var expectedResult = twinReported.ChangeSpec[changeSpecKey];
        var result = twinReported.GetReportedChangeSpecByKey(changeSpecKey.ToUpper());

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetDesiredChangeSpecByKey__ExistsKey_ReturnCorrectValue()
    {
        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;

        var expectedResult = twinDesired.ChangeSpec[changeSpecKey];

        var result = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetDesiredChangeSpecByKey_NonExistsKey_ReturnNull()
    {

        var result = twinDesired.GetDesiredChangeSpecByKey("ChangeSpecServerIdentity");

        Assert.AreEqual(MockHelperEntities.EqualObjects(result, null), true);
    }

    [Test]
    public void GetDesiredChangeSpecByKey_UpperCaseLettersKey_ReturCorrectValue()
    {

        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;
        twinReported.ChangeSpec.Where(x => x.Key == changeSpecKey).FirstOrDefault().Key.ToLower();

        var expectedResult = twinDesired.ChangeSpec[changeSpecKey];
        var result = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey.ToUpper());

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void SetReportedChangeSpecByKey_NewKey_AddNewChangeSpec()
    {
        var changeSpecKey = changeSpecServerIdentityKey;

        twinReported.SetReportedChangeSpecByKey(twinReportedChangeSpec, changeSpecKey);

        Assert.AreEqual(twinReported.ChangeSpec.Count, 3);
    }

    [Test]
    public void SetReportedChangeSpecByKey_ExistsKey_UpdateChangeSpec()
    {
        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;

        twinReported.SetReportedChangeSpecByKey(twinReportedChangeSpec, changeSpecKey);

        var expectedResult = twinReported.ChangeSpec[changeSpecKey];

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, twinReportedChangeSpec), true);
    }

    [Test]
    public void SetReportedChangeSpecByKey_UpperCasesKey_UpdateChangeSpec()
    {
        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;

        twinReported.SetReportedChangeSpecByKey(twinReportedChangeSpec, changeSpecKey.ToUpper());

        var expectedResult = twinReported.ChangeSpec[changeSpecKey];

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, twinReportedChangeSpec), true);
    }

    [Test]
    public void SetReportedChangeSpecByKey_TwinReportedIsNull_TwinReportedDoesNotChange()
    {
        twinReported = null;

        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;

        twinReported.SetReportedChangeSpecByKey(twinReportedChangeSpec, changeSpecKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(twinReported, null), true);
    }

    [Test]
    public void SetReportedChangeSpecByKey_ChangeSpecIsNull_ChangeSpecDoesNotChange()
    {
        twinReported = new TwinReported()
        {
            ChangeSpec = null
        };

        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;

        twinReported.SetReportedChangeSpecByKey(twinReportedChangeSpec, changeSpecKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(twinReported.ChangeSpec, null), true);
    }

    [Test]
    public void GetReportedChangeSignByKey_ExistsKey_ReturnCorrectValue()
    {

        var expectedResult = twinReported.ChangeSign[changeSignKey];

        var result = twinReported.GetReportedChangeSignByKey(changeSignKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetReportedChangeSignByKey_NonExistsKey_ReturnNull()
    {
        var result = twinReported.GetReportedChangeSignByKey(changeSpecServerIdentityKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(result, null), true);
    }

    [Test]
    public void GetReportedChangeSignByKey_UpperCaseLettersKey_ReturCorrectValue()
    {

        twinReported.ChangeSign.Where(x => x.Key == changeSignKey).FirstOrDefault().Key.ToLower();

        var expectedResult = twinReported.ChangeSign[changeSignKey];
        var result = twinReported.GetReportedChangeSignByKey(changeSignKey.ToUpper());

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetDesiredChangeSignByKey_ExistsKey_ReturnCorrectValue()
    {

        var expectedResult = twinDesired.ChangeSign[changeSignKey];

        var result = twinDesired.GetDesiredChangeSignByKey(changeSignKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetDesiredChangeSignByKey_NonExistsKey_ReturnNull()
    {
        var result = twinDesired.GetDesiredChangeSignByKey(changeSpecServerIdentityKey);

        Assert.AreEqual(MockHelperEntities.EqualObjects(result, null), true);
    }

    [Test]
    public void GetDesiredChangeSignByKey_UpperCaseLettersKey_ReturCorrectValue()
    {

        twinReported.ChangeSign.Where(x => x.Key == changeSignKey).FirstOrDefault().Key.ToLower();

        var expectedResult = twinDesired.ChangeSign[changeSignKey];
        var result = twinDesired.GetDesiredChangeSignByKey(changeSignKey.ToUpper());

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetSignKeyByChangeSpec_GetChangeSpecKey_ReturnChangeSignKey()
    {
        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;

        var expectedResult = $"{changeSpecKey}{DeviceConstants.SIGN_KEY}";

        var result = changeSpecKey.GetSignKeyByChangeSpec();

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetSpecKeyBySignKey_GetChangeSignKey_ReturnChangeSpecKey()
    {
        var changeSignKey = $"{TwinConstants.CHANGE_SPEC_NAME}{DeviceConstants.SIGN_KEY}";

        var expectedResult = TwinConstants.CHANGE_SPEC_NAME;

        var result = changeSignKey.GetSpecKeyBySignKey();

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void GetSignKeyByChangeSpec_GetChangeSpecKey_ReturnChangeIdKey()
    {
        var changeSpecKey = TwinConstants.CHANGE_SPEC_NAME;

        var expectedResult = $"{changeSpecKey}{DeviceConstants.ID_KEY}";

        var result = changeSpecKey.GetChangeSpecIdKeyByChangeSpecKey();

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult, result), true);
    }

    [Test]
    public void SetDesiredChangeSignByKey_NewKey_AddNewChangeSign()
    {
        var changeSignKey = changeSpecServerIdentityKey;

        twinDesired.SetDesiredChangeSignByKey(changeSignKey, "789789");

        Assert.AreEqual(twinDesired.ChangeSign.Count, 3);
    }

    [Test]
    public void SetDesiredChangeSignByKey_ExistsKey_UpdateChangeSpec()
    {

        twinDesired.SetDesiredChangeSignByKey(changeSignKey, "789789");

        Assert.AreEqual(MockHelperEntities.EqualObjects(twinDesired.ChangeSign[changeSignKey], "789789"), true);
    }

    [Test]
    public void SetDesiredChangeSignByKey_UpperCasesKey_UpdateChangeSpec()
    {
        twinDesired.SetDesiredChangeSignByKey(changeSignKey.ToUpper(), "789789");

        Assert.AreEqual(MockHelperEntities.EqualObjects(twinDesired.ChangeSign[changeSignKey], "789789"), true);
    }

    [Test]
    public void SetDesiredChangeSignByKey_TwinReportedIsNull_TwinReportedDoesNotChange()
    {
        twinDesired = null;

        twinDesired.SetDesiredChangeSignByKey(changeSignKey, "789789");

        Assert.AreEqual(MockHelperEntities.EqualObjects(twinDesired, null), true);
    }

    [Test]
    public void SetDesiredChangeSignByKey_ChangeSpecIsNull_ChangeSpecDoesNotChange()
    {
        twinDesired = new TwinDesired()
        {
            ChangeSpec = null
        };

        twinDesired.SetDesiredChangeSignByKey(changeSignKey, "789789");

        Assert.AreEqual(MockHelperEntities.EqualObjects(twinDesired.ChangeSpec, null), true);
    }


    private void SetDesiredAndReported()
    {
        twinReported = new TwinReported()
        {
            ChangeSpec = new Dictionary<string, TwinReportedChangeSpec>
                {
                    {
                        TwinConstants.CHANGE_SPEC_NAME,
                        new TwinReportedChangeSpec
                        {
                            Id = "123",
                            Patch = new Dictionary<string, TwinActionReported[]>
                            {
                                { "transitPackage", new TwinActionReported[] { new TwinActionReported {  Status = StatusType.Success } } }
                            }
                        }
                    },
                    {
                        TwinConstants.CHANGE_SPEC_DIAGNOSTICS_NAME,
                        new TwinReportedChangeSpec
                        {
                            Id = "456",
                            Patch = new Dictionary<string, TwinActionReported[]>
                            {
                                { "transitPackage", new TwinActionReported[] {} }
                            }
                        }
                    }
                },
            ChangeSign = new Dictionary<string, string>
                {
                    { TwinConstants.CHANGE_SPEC_NAME.GetSignKeyByChangeSpec(), "123" },
                    { TwinConstants.CHANGE_SPEC_DIAGNOSTICS_NAME.GetSignKeyByChangeSpec(), "456" }
                }
        };

        twinDesired = new TwinDesired()
        {
            ChangeSpec = new Dictionary<string, TwinChangeSpec>
                {
                    {
                        TwinConstants.CHANGE_SPEC_NAME,
                        new TwinChangeSpec
                        {
                            Id = "123",
                            Patch = new Dictionary<string, TwinAction[]>
                            {
                                { "transitPackage", new TwinAction[] { new TwinAction {Action = TwinActionType.SingularUpload } } }
                            }
                        }
                    },
                    {
                        TwinConstants.CHANGE_SPEC_DIAGNOSTICS_NAME,
                        new TwinChangeSpec
                        {
                            Id = "456",
                            Patch = new Dictionary<string, TwinAction[]>
                            {
                                { "transitPackage", new TwinAction[] {} }
                            }
                        }
                    }
                },
            ChangeSign = new Dictionary<string, string>
                {
                    { TwinConstants.CHANGE_SPEC_NAME.GetSignKeyByChangeSpec(), "123" },
                    { TwinConstants.CHANGE_SPEC_DIAGNOSTICS_NAME.GetSignKeyByChangeSpec(), "456" }
                }
        };

        twinReportedChangeSpec = new TwinReportedChangeSpec
        {
            Id = "999",
            Patch = new Dictionary<string, TwinActionReported[]>
            {
                { "transitPackage", new TwinActionReported[] { new TwinActionReported {  Status = StatusType.Pending } } }
            }
        };
    }

}

