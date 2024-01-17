﻿
namespace Shared.Entities.Twin;

public class TwinReported
{
   public DeviceStateType? DeviceState { get; set; }
   public string AgentPlatform { get; set; }
   public ShellType[] SupportedShells { get; set; }
   public List<TwinReportedChangeSpec>? ChangeSpec { get; set; }
   // public TwinReportedChangeSpec? ChangeSpecDiagnostics { get; set; }
   public string SecretKey { get; set; }
   public List<TwinReportedCustomProp> Custom { get; set; }
   public string ChangeSign { get; set; }
   public string ChangeSpecId { get; set; }
   public CertificateValidity CertificateValidity { get; set; }
   public DeviceStateType? DeviceStateAfterServiceRestart { get; set; }
   public List<KnownIdentities>? KnownIdentities { get; set; }
}
