using Eto;
using Eto.Forms;

namespace Courser.UI;

[Handler(typeof(ExtendedWebView.IHandler))]
public class ExtendedWebView : WebView
{
    public new ExtendedWebView.IHandler Handler => (IHandler)base.Handler;

    public void OpenDownloadDialog() => Handler.OpenDownloadDialog();
    public void CloseDownloadDialog() => Handler.CloseDownloadDialog();
    public bool IsDownloadsDialogOpen { get => Handler.IsDownloadsDialogOpen; set => Handler.IsDownloadsDialogOpen = value; }


    public new interface IHandler : WebView.IHandler
    {
        public void OpenDownloadDialog();
        public void CloseDownloadDialog();

        public bool IsDownloadsDialogOpen { get; set; }

    }
}
