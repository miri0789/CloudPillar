namespace Shared.Entities.Twin;

public class KnownIdentities
{
    public string Subject { get; set; }
    public string Thumbprint { get; set; }
    public string ValidThru { get; set; }
    public KnownIdentities(string subject, string thumbprint, string validThru)
    {
        Subject = subject;
        Thumbprint = thumbprint;
        ValidThru = validThru;
    }
}