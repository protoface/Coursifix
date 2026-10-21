using System;
using Microsoft.Extensions.Configuration;

namespace Courser.Common.Models;

public class CourseConfig
{
    // [ConfigurationKeyName("course")]
    public required string Course { get; set; }

    // [ConfigurationKeyName("updateInterval")]
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(30);

    // [ConfigurationKeyName("rootNotesDirectory")]
    public string NotesRoot { get; set; } = @"C:\Users\herdef\OneDrive - Medi GmbH & Co. KG\Dokumente\notes";

    // [ConfigurationKeyName("wlanUsername")]
    public string? WlanUserName { get; set; }

    // [ConfigurationKeyName("wlanPassword")]
    public string? WlanPassword { get; set; }

    // [ConfigurationKeyName("loginUri")]
    public string? LoginUri { get; set; }

    // [ConfigurationKeyName(nameof(LoginRedirectUri))]
    public string LoginRedirectUri { get; set; } = "http://www.msftconnecttest.com/redirect";
}
