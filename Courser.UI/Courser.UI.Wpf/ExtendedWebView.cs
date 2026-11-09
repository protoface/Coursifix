using System.Diagnostics;
using System.Reflection;
using Eto.Wpf.Forms.Controls;

namespace Courser.UI.Wpf;

public class ExtendedWebView : WebView2Handler, Courser.UI.ExtendedWebView.IHandler
{
    private static readonly string CourserVersion = "Courser/" + (typeof(EntryPoint).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.0.1");

    public ExtendedWebView()
    {
        Control.CoreWebView2InitializationCompleted += (_, _) =>
        {
            Control.CoreWebView2.Settings.UserAgent = CourserVersion + " " + Control.CoreWebView2.Settings.UserAgent;
        };
    }

    public void OpenDownloadDialog() => CoreWebView2.OpenDefaultDownloadDialog();

    public void CloseDownloadDialog() => CoreWebView2.CloseDefaultDownloadDialog();

    public bool IsDownloadsDialogOpen
    {
        get => Control.CoreWebView2.IsDefaultDownloadDialogOpen;
        set
        {
            if (value)
                OpenDownloadDialog();
            else
                CloseDownloadDialog();
        }
    }

}
