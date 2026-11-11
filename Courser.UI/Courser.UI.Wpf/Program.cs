using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Eto.Wpf;
using Eto.Wpf.Forms.Controls;
using Eto.Wpf.Forms.ToolBar;
using Application = Eto.Forms.Application;

namespace Courser.UI.Wpf;

static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Eto.Style.Add<GroupBoxHandler>(null, widget => widget.Control.BorderThickness = new(3));
        Eto.Style.Add<GroupBoxHandler>("glow-running", widget => widget.Control.BorderBrush = SystemColors.HighlightBrush);
        Eto.Style.Add<GroupBoxHandler>("glow-done", widget => widget.Control.BorderBrush = new SolidColorBrush(Colors.LimeGreen));
        Eto.Style.Add<GroupBoxHandler>("glow-upcoming", widget => widget.Control.BorderBrush = new LinearGradientBrush(Eto.Drawing.SystemColors.Highlight.ToWpf(), Eto.Drawing.SystemColors.Control.ToWpf(), new(0, 1), new(1, 0)));
        Eto.Style.Add<ScrollableHandler>("no-horizontal", widget =>
        {
            if (widget.Control.Child is EtoScrollViewer scrollViewer)
            {
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
        });
        Eto.Style.Add<CheckToolItemHandler>(null, test =>
        {
            var style = new Style(typeof(ToggleButton), test.Control.Style);
            var trigger = new Trigger()
            {
                Property = ToggleButton.IsCheckedProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
            style.Triggers.Add(trigger);

            test.Control.Style = style;
        });
        
        // Eto.Style.Add<WebView2Handler>(null, handler =>
        // {
        //     handler.Control.CoreWebView2InitializationCompleted += (_, _) =>
        //     {
        //         handler.Control.CoreWebView2.NewWindowRequested += (s, e) =>
        //         {
        //             Debug.WriteLine($"NewWindowRequested NewWindow={e.WindowFeatures} Uri={e.Uri}");
        //
        //         };
        //     };
        // });

        var platform = Eto.Platform.Get(Eto.Platforms.Wpf);
        platform.Add<UI.ExtendedWebView.IHandler>(() => new ExtendedWebView());
        
        EntryPoint.Enter(new(platform));
    }
}