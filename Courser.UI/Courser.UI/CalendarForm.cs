using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Courser.Common.Interfaces;
using Courser.Common.Models;
using Eto.Drawing;
using Eto.Forms;

namespace Courser.UI;

internal sealed class CalendarForm : Dialog
{
    private static readonly Font HeaderFont = Fonts.Monospace(10);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private ICalendarService CalendarService { get; }

    private readonly StackLayout _stackLayout;

    public CalendarForm(ICalendarService calendarService)
    {
        CalendarService = calendarService;

        Resizable = true;
        ShowInTaskbar = true;

        Size = new(250, 400);
        Content = new Scrollable
        {
            Style = "no-horizontal",
            Content = _stackLayout = new()
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new(0, 5, 0, 0)
            }
        };

        _ = Task.Run(UpdateFunc);
    }

    protected override void OnOwnerChanged(EventArgs e)
    {
        var bounds = new Rectangle(Owner.Location, Size);
        var displayBounds = Screen.DisplayBounds;
        if (!displayBounds.Contains(bounds))
        {
            if (bounds.Bottom > displayBounds.Bottom)
                bounds.Offset(0, -(int)(bounds.Bottom - displayBounds.Bottom));
            else if (bounds.Top < displayBounds.Top)
                bounds.Offset(0, (int)(displayBounds.Top - bounds.Top));
            if (bounds.Right > displayBounds.Right)
                bounds.Offset(-(int)(bounds.Right - displayBounds.Right), 0);
            else if (bounds.Left < displayBounds.Left)
                bounds.Offset((int)(displayBounds.Left - bounds.Left), 0);
        }
        Bounds = bounds;
        base.OnOwnerChanged(e);
    }

    private async Task UpdateFunc()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            var now = DateTimeOffset.Now;
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            switch (await CalendarService.GetLecturesInAsync(today, today + TimeSpan.FromDays(7)))
            {
                case Lecture[] lectures:
                    var lectureDays = lectures.GroupBy(e => e.Start.LocalDateTime.Date);
                    await Utils.UI.InvokeAsync(() =>
                    {
                        _stackLayout.SuspendLayout();
                        _stackLayout.Items.Clear();
                        foreach (var item in LectureDaysToStackLayoutItems(lectureDays, now, tomorrow))
                            _stackLayout.Items.Add(item);
                        _stackLayout.ResumeLayout();
                    });
                    break;
                case Error error:
                    MessageBox.Show(error.Message);
                    break;
            }
        } while (await timer.WaitForNextTickAsync(_cancellationTokenSource.Token));
    }


    private static Control LectureToContent(Lecture lecture)
    {
        Label nameLabel = new() { Text = lecture.Name },
            timeLabel = new() { Text = $"{lecture.Start:t} - {lecture.End:t}" },
            roomLabel = new() { Text = lecture.Rooms, Trimming = TextTrimming.CharacterEllipsis, };
        var textStack = new StackLayout(nameLabel, timeLabel, roomLabel) { Padding = new(10, 5) };

        return new TableLayout(new TableRow(new TableCell(textStack, true), new Panel { Content = lecture.Icon.WithSize(32, 32), Padding = new(0, 10, 10, 10) }));
    }

    private static IEnumerable<StackLayoutItem> LectureDaysToStackLayoutItems(IEnumerable<IGrouping<DateTime, Lecture>> lectureDays, DateTimeOffset now, DateTime tomorrow)
    {
        foreach (var lectureDay in lectureDays)
        {
            yield return new Panel
            {
                Content = new Label
                {
                    Font = HeaderFont,
                    TextAlignment = TextAlignment.Center,
                    Text = lectureDay.Key.ToString("dddd, d/MM/yy", CultureInfo.CurrentUICulture),
                },
                Padding = new(0, 5, 0, 0)
            };

            foreach (var lecture in lectureDay)
            {
                yield return new Panel
                {
                    Content = new GroupBox
                    {
                        Style = (lecture.Start < now && lecture.End < now)
                            ? "glow-done"
                            : (lecture.Start < now)
                                ? "glow-running"
                                : (lecture.Start < tomorrow)
                                    ? "glow-upcoming"
                                    : "",
                        Content = LectureToContent(lecture)
                    },
                    Padding = new(0, 0, 0, 10)
                };
            }
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.IsKeyUp(Keys.Escape))
        {
            Close();
            e.Handled = true;
        }
        base.OnKeyUp(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _cancellationTokenSource.Cancel();

        base.OnClosed(e);
    }
}
