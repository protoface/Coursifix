using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
                                              if(window.courserSelected === true) return;
                                              {LoginStyleScript}
                                              document.getElementById('button-Student').click();
                                              window.courserSelected = true;
                                              """;

    [StringSyntax("javascript")]
    private string LoginAutofillScript => $$"""
                                            if(window.courserLogin === true) return;
                                            {{LoginStyleScript}}
                                            [
                                                {prop: 'username', value: '{{UserConfig.WlanUserName}}'},
                                                {prop: 'password', value: '{{UserConfig.WlanPassword}}'},
                                                {prop: 'organization', value: '{{MoodleConfig.Organization}}'}
                                            ].map(e => document.getElementById(e.prop).value = e.value);
                                            if(document.getElementsByClassName('message-box error').length === 0) {
                                                document.getElementById('submit_button').click();
                                            }
                                            window.courserLogin = true;
                                            """;

    [StringSyntax("javascript")]
    private static readonly string CssInjectScript = $"""
                                                      if(window.styleInjected === true) return;
                                                      const node = document.createElement("style");
                                                      node.innerText = `{DarkCss()}`;
                                                      document.head.appendChild(node);
                                                      window.styleInjected = true;
                                                      """;

    private static string DarkCss()
    {
        using var stream = typeof(MoodleView).Assembly.GetManifestResourceStream("Courser.UI.style.css");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private CourseConfig UserConfig => _userConfig.Value;
    private MoodleConfig MoodleConfig => _moodleConfig.Value;
    private Uri MoodleStartUri => new UriBuilder(Uri.UriSchemeHttps, MoodleConfig.MoodleHost) { Path = MoodleConfig.StartPath }.Uri;

    private TabControl _tabControl;

    public MoodleView(IOptions<CourseConfig> userConfig, IOptions<MoodleConfig> moodleConfig)
    {
        _userConfig = userConfig;
        _moodleConfig = moodleConfig;

        MinimumSize = new(800, 600);

        Content = InitTabControl();

        AddPage(MoodleStartUri);
    }

    private MoodleView(MoodleView parent, WebViewTab page)
    {
        _userConfig = parent._userConfig;
        _moodleConfig = parent._moodleConfig;

        MinimumSize = new(800, 600);

        Content = InitTabControl();
        _tabControl.Pages.Add(page);
    }

    [MemberNotNull(nameof(_tabControl))]
    private TabControl InitTabControl()
    {
        _tabControl = new()
        {
            ContextMenu = new()
            {
                Items =
                {
                    new CheckMenuItem((s, _) => Topmost = ((CheckMenuItem)s!).Checked) { Text = "Pin" },
                    new ButtonMenuItem((_, _) => _tabControl!.SelectedPage = AddPage(MoodleStartUri)) { Text = "New tab" }
                }
            }
        };
        _tabControl.SelectedIndexChanged += (_, _) => Title = _tabControl.SelectedPage.Text;
        return _tabControl;
    }

    private WebViewTab AddPage(Uri uri)
    {
        var webViewTab = new WebViewTab(this, uri);
        _tabControl.Pages.Add(webViewTab);
        return webViewTab;
    }



    private class WebViewTab : TabPage
    {
        private MoodleView Host { get; }
        private readonly ExtendedWebView _webView;

        public WebViewTab(MoodleView host, Uri uri)
        {
            Host = host;
            _webView = host.InitWebView(uri);
            _webView.DocumentTitleChanged += (_, e) =>
            {
                Text = e.Title;
                if (((TabControl)Parent).SelectedPage == this)
                {
                    Host.Title = e.Title;
                }
            };
            Content = _webView;
            ContextMenu = new()
            {
                Items =
                {
                    DetachContextItem(),
                    CloseContextItem(),
                    CloseOthersContextItem(),
                    DownloadsContextItem()
                }
            };
        }

        private ButtonMenuItem DetachContextItem()
        {
            var item = new ButtonMenuItem()
            {
                Text = "Detach"
            };
            item.Click += (_, _) => new MoodleView(Host, this).Show();
            return item;
        }

        private ButtonMenuItem CloseContextItem()
        {
            var item = new ButtonMenuItem()
            {
                Text = "Close"
            };
            item.Click += (_, _) => Detach();
            return item;
        }

        private ButtonMenuItem CloseOthersContextItem()
        {
            var item = new ButtonMenuItem()
            {
                Text = "Close others"
            };
            item.Click += (_, _) =>
            {
                var pages = ((TabControl)Parent).Pages.ToArray();
                foreach (var page in pages)
                {
                    if (page != this) page.Detach();
                }

            };
            return item;
        }

        private ButtonMenuItem DownloadsContextItem()
        {
            var item = new ButtonMenuItem()
            {
                Text = "Downloads"
            };
            item.Click += (_, _) => _webView.IsDownloadsDialogOpen ^= true;
            return item;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Buttons is MouseButtons.Middle)
            {
                Detach();
                e.Handled = true;
            }
            base.OnMouseUp(e);
        }
    }


    private ExtendedWebView InitWebView(Uri uri)
    {
        ExtendedWebView webView = new() { Url = uri, TabStop = false };

        webView.DocumentLoading += async (_, e) =>
        {
            if (e.Uri.Host != MoodleConfig.MoodleHost) // Don't do anything if we're not in moodle
                return;

            await webView.ExecuteScriptAsync(e.Uri.AbsolutePath switch
            {
                LoginTypeSelectPath => LoginSelectScript,
                LoginPath => LoginAutofillScript,
                _ => CssInjectScript
            });
        };

        webView.DocumentLoaded += async (_, e) =>
        {
            if (e.Uri.Host != MoodleConfig.MoodleHost) // Don't do anything if we're not in moodle
                return;

            await webView.ExecuteScriptAsync(e.Uri.AbsolutePath switch
            {
                LoginTypeSelectPath => LoginSelectScript,
                LoginPath => LoginAutofillScript,
                _ => CssInjectScript
            });
        };

        webView.OpenNewWindow += (_, e) =>
        {
            if (!e.Uri.IsFile && Path.GetExtension(e.Uri.Segments.Last()) is not ({ Length: 0 } or ".php" or ".html"))
                webView.Url = e.Uri;
            else
                _tabControl.SelectedPage = AddPage(e.Uri);

            e.Cancel = true;
        };

        return webView;
    }
}
