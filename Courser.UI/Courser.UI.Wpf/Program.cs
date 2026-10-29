using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Eto.Wpf;
using Eto.Wpf.Forms;
using Eto.Wpf.Forms.Controls;
using Eto.Wpf.Forms.ToolBar;
using Humanizer;


namespace Courser.UI.Wpf
{
    class Program
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
                trigger.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty, Brushes.Black));
                style.Triggers.Add(trigger);

                test.Control.Style = style;
            });

            EntryPoint.Enter(new Eto.Forms.Application(Eto.Platforms.Wpf));
        }
    }
}
