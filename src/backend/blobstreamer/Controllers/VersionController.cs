using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.BlobStreamer.Controllers;

[AllowAnonymous]
[Route("[controller]")]
[ApiController]
public class VersionController : ControllerBase
{
    [HttpGet]
    public ActionResult? GetVersion()
    {
        try
        {
            return Ok(GetStringVersion());
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? GetStringVersion()
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
