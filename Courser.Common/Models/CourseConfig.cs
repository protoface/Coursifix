namespace Courser.Common.Models;

public class CourseConfig
{
    public required string Course { get; set; }

    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(30);

    public string? NotesRoot { get; set; }

    public string? WlanUserName { get; set; }

    public string? WlanPassword { get; set; }

    public Uri? LoginUri { get; set; }

    public Uri LoginRedirectUri { get; set; } = new("http://www.msftconnecttest.com/redirect");
}
