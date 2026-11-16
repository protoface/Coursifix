using System;
using Eto.Forms;
using Eto.Drawing;
using Courser.Common.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using Courser.Common.Models;
using Humanizer;
using Microsoft.Extensions.Options;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using Courser.UI.Utils;

namespace Courser.UI;

internal sealed class MainForm : Form<MainForm.MainViewModel>
{
    private ICalendarService CalendarService { get; }
    private CourseConfig CourseConfig { get; }
    private Task DataUpdateTask { get; }


    public MainForm(ICalendarService calendarService, IOptions<CourseConfig> options)
    {
        CalendarService = calendarService;
        CourseConfig = options.Value;

        Model.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.TaskbarProgressState):
                    if (Model.TaskbarProgressState == 0)
                        Taskbar.SetProgress(TaskbarProgressState.None);
                    else
                        Taskbar.SetProgress(TaskbarProgressState.Progress, Model.TaskbarProgressState / 100f);
                    break;
                case nameof(MainViewModel.WindowIcon):
                    Icon = Model.WindowIcon;
                    break;
            }
        };

        Title = "Courser";
        MinimumSize = new(250, 150);

        DynamicLayout layout = new() { Padding = new(10, 5, 10, 0) };
        layout.Add(null);
        layout.Add(new Label().Bind(this, e => e.TextBinding, m => m.CourseName));
        layout.AddSeparateRow(xscale: true, controls:
            [
                new Label().Bind(this, e => e.TextBinding, m => m.FormattedTime),
                null,
                new Label().Bind(this, e => e.TextBinding, m => m.ElapsedPercent)
            ]
        );
        layout.Add(new Label().Bind(this, e => e.TextBinding, m => m.Rooms));
        layout.Add(null);

        Content = layout;
        Icon = Icons.DiamondIcon;
        
        ToolBar = new()
        {
            Items =
            {
                PinToolItem(),
                CalendarToolItem(),
                MoodleToolItem(),
                NetworkLoginToolItem(),
            }
        };
        
        if (OpenInCodeToolItem(calendarService) is {} openInCode)
            ToolBar.Items.Add(openInCode);

        DataUpdateTask = Task.Run(UpdateTask);
    }

    private Command? OpenInCodeToolItem(ICalendarService calendarService)
    {
        if (CourseConfig.NotesRoot is null)
            return null;

        var openInCode = new Command { ToolBarText = "Open in Code", Shortcut = Application.Instance.CommonModifier | Keys.O };
        openInCode.Executed += async (_, _) =>
        {
            switch (await calendarService.GetNextEndingLectureAsync())
            {
                case Lecture lecture:
                    {
                        var lectureDirectory = Path.Combine(CourseConfig.NotesRoot, lecture.Name, DateTime.Today.ToString("dd.MM.yy", CultureInfo.InvariantCulture));
                        var notesFile = Path.Combine(lectureDirectory, "notes.typ");
                        Process.Start(@"C:\Program Files\Microsoft VS Code\Code.exe", [notesFile]);
                        break;
                    }
            }
        };
        return openInCode;
    }

    private CheckToolItem PinToolItem() => BuildToolItem<CheckToolItem>("Pin", (_, _) => { }, "darker").Apply(e => e.CheckedChanged += (_, _) => Topmost = e.Checked);
    private ButtonToolItem CalendarToolItem() => BuildToolItem<ButtonToolItem>("Cal", (_, _) => Services.Get<CalendarForm>().ShowModalAsync(this));
    private ButtonToolItem MoodleToolItem() => BuildToolItem<ButtonToolItem>("Moodle", (_, _) => Services.Get<MoodleView>().Show());

    private ButtonToolItem? NetworkLoginToolItem() =>
        BuildToolItem<ButtonToolItem>("Login", (_, _) => _ = LoginToNetwork())
            .Apply(e => e.Enabled = CourseConfig is { WlanPassword: not null, WlanUserName: not null, LoginUri: not null });

    private T BuildToolItem<T>(string text, EventHandler<EventArgs> action, string? style = null) where T : ToolItem, new()
    {
        var item = new T
        {
            Text = text,
            Style = style
        };
        item.Click += action;
        return item;
    }

    private async static Task LoginToNetwork()
    {
        switch (await Services.Get<INetworkLoginService>().Login())
        {
            case Ok:
                MessageBox.Show("Login successful", MessageBoxButtons.OK);
                break;
            case Error error:
                MessageBox.Show($"Login failed\n {error.Message}", MessageBoxButtons.OK, MessageBoxType.Error);
                break;
        }
    }

    private async Task UpdateTask()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        int ticker = 0;
        do
        {
            MainViewModel updateModel;
            switch (await CalendarService.GetNextEndingLectureAsync())
            {
                case Lecture lecture:

                    var now = DateTimeOffset.UtcNow;

                    updateModel = (now - lecture.Start) / (lecture.End - lecture.Start) is >= 0 and var elapsedLecturePercent
                        ? new()
                        {
                            CourseName = lecture.Name,
                            FormattedTime = ParseRunningLecture(lecture, now),
                            ElapsedPercent = $"{100 * elapsedLecturePercent:F2}%",
                            TaskbarProgressState = (int)(elapsedLecturePercent * 100),
                            WindowIcon = lecture.Icon
                        }
                        : new()
                        {
                            CourseName = $"Next: {lecture.Name}",
                            FormattedTime = ParseUpcomingLecture(lecture, now),
                            WindowIcon = lecture.Start.DayOfYear != now.DayOfYear
                                ? lecture.Icon
                                : Icons.DiamondIcon
                        };

                    updateModel.Rooms = lecture.Rooms;
                    break;
                case Error error:
                    updateModel = new() { CourseName = error.Message };
                    break;
                case null:
                    updateModel = new() { CourseName = "No lecture found" };
                    break;
            }

            await Utils.UI.InvokeAsync(() => Model.Update(updateModel));
            ticker++;

        } while (await timer.WaitForNextTickAsync());
        return;


        string ParseRunningLecture(Lecture lecture, DateTimeOffset compareDate) => ticker % 4 >= 2
            ? $"Ende: {lecture.End.LocalDateTime:t}"
            : $"noch {(lecture.End.LocalDateTime - compareDate).Humanize(precision: 2, minUnit: TimeUnit.Second)}";

        string ParseUpcomingLecture(Lecture lecture, DateTimeOffset compareDate) => ticker % 4 >= 2
            ? lecture.Start.Humanize(dateToCompareAgainst: compareDate.LocalDateTime)
            : "Start: " + lecture.Start.ToString(lecture.Start.Date == compareDate.Date ? "t" : "g", CultureInfo.CurrentCulture);
    }

    internal class MainViewModel : ViewModel<MainViewModel>
    {
        public string? CourseName { get; set => SetAndRaise(ref field, value); }
        public string? FormattedTime { get; set => SetAndRaise(ref field, value); }
        public string? ElapsedPercent { get; set => SetAndRaise(ref field, value); }
        public string? Rooms { get; set => SetAndRaise(ref field, value); }

        public int TaskbarProgressState { get; set => SetAndRaise(ref field, value); }
        public Icon WindowIcon { get; set => SetAndRaise(ref field, value); } = Icons.ErrorIcon;
    }

}
