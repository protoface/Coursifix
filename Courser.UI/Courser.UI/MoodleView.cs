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
    private const string LoginStyleScript = """
                                            document.body.style.backgroundColor = "black";
                                            document.body.style.color = "white";
                                            document.getElementById("header").style.background = "black";
                                            document.querySelector("#header #logo img").style.background = "transparent";
                                            document.querySelector(".pure-form select").style.background = "black";
                                            document.querySelector(".pure-form select").style.color = "white";
                                            """;
    [StringSyntax("javascript")]
    private const string LoginSelectScript = $"""
                                              {LoginStyleScript}
                                              document.getElementById('button-Student').click();
                                              """;

    [StringSyntax("javascript")]
    private string LoginAutofillScript => $$"""
                                            {{LoginStyleScript}}
                                            [
                                                {prop: 'username', value: '{{UserConfig.WlanUserName}}'},
                                                {prop: 'password', value: '{{UserConfig.WlanPassword}}'},
                                                {prop: 'organization', value: '{{MoodleConfig.Organization}}'}
                                            ].map(e => document.getElementById(e.prop).value = e.value);
                                            if(document.getElementsByClassName('message-box error').length === 0) {
                                                document.getElementById('submit_button').click();
                                            }
                                            """;

    [StringSyntax("javascript")]
    private const string CssInjectScriptFormat = """
                                                 const node = document.createElement("style");
                                                 node.innerText = `{0}`;
                                                 document.head.appendChild(node);
                                                 """;

    private static string DarkCss()
    {
        using var stream = typeof(MoodleView).Assembly.GetManifestResourceStream("Courser.UI.style.css");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static string StyleInjectScript => field ??= string.Format(CssInjectScriptFormat, DarkCss());


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

            await _webView.ExecuteScriptAsync(e.Uri.AbsolutePath switch
            {
                LoginTypeSelectPath => LoginSelectScript,
                LoginPath => LoginAutofillScript,
                _ => StyleInjectScript
            });
        };
        _webView.DocumentTitleChanged += (_, e) => { Utils.UI.Invoke(() => Title = e.Title); };
    }
}
