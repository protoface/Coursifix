using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Courser.Common.Models;
using Eto.Forms;
using Microsoft.Extensions.Options;

namespace Courser.UI;

public class MoodleView : Form
{
    private readonly IOptions<CourseConfig> _userConfig;
    private readonly IOptions<MoodleConfig> _moodleConfig;
    private const string LoginTypeSelectPath = "/simplesaml/module.php/multiauth/discovery";
    private const string LoginPath = "/simplesaml/module.php/core/loginuserpassorg";
    [StringSyntax("javascript")]
    private string LoginScript => $$"""
                                    [
                                        {prop: 'username', value: '{{UserConfig.WlanUserName}}'},
                                        {prop: 'password', value: '{{UserConfig.WlanPassword}}'},
                                        {prop: 'organization', value: '{{MoodleConfig.Organization}}'}
                                    ].map(e => document.getElementById(e.prop).value = e.value);
                                    document.getElementById('submit_button').click();
                                    """;

    private static string DarkCss()
    {
        using var stream = typeof(MoodleView).Assembly.GetManifestResourceStream("Courser.UI.style.css");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    [StringSyntax("javascript")]
    private static string CssInjectScript => field ??= $"""
                                                        const node = document.createElement("style");
                                                        node.innerText = `{DarkCss()}`;
                                                        document.head.appendChild(node);
                                                        """;

    private WebView _webView;

    private CourseConfig UserConfig => _userConfig.Value;
    private MoodleConfig MoodleConfig => _moodleConfig.Value;

    public MoodleView(IOptions<CourseConfig> userConfig, IOptions<MoodleConfig> moodleConfig)
    {
        _userConfig = userConfig;
        _moodleConfig = moodleConfig;

        MinimumSize = new(800, 600);

        InitWebView();
        Content = _webView;
    }

    [MemberNotNull(nameof(_webView))]
    private void InitWebView()
    {
        _webView = new() { Url = new UriBuilder(Uri.UriSchemeHttps, MoodleConfig.MoodleHost) { Path = MoodleConfig.StartPath }.Uri };

        _webView.DocumentLoaded += async (_, e) =>
        {
            if (e.Uri.Host != MoodleConfig.MoodleHost) // Don't do anything if we're not in moodle
                return;

            switch (e.Uri.AbsolutePath)
            {
                case LoginTypeSelectPath:
                    await _webView.ExecuteScriptAsync("document.getElementById('button-Student').click()");
                    break;
                case LoginPath:
                    await _webView.ExecuteScriptAsync(LoginScript);
                    break;
                default:
                    await _webView.ExecuteScriptAsync(CssInjectScript);
                    break;
            }

        };
        _webView.DocumentTitleChanged += (_, e) => { Utils.UI.Invoke(() => Title = e.Title); };
    }
}
