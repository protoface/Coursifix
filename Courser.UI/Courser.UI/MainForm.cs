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
using System.Net.Http;
using System.Reflection;
using System.Linq;
using Courser.UI.Utils;

namespace Courser.UI;

internal sealed class MainForm : Form<MainForm.MainViewModel>
{
    private ICalendarService CalendarService { get; }
    private CourseConfig CourseConfig { get; }
    private Task DataUpdateTask { get; }
    // private MainViewModel Model { get; }

    public MainForm(ICalendarService calendarService, IOptions<CourseConfig> options)
    {
        CalendarService = calendarService;
        CourseConfig = options.Value;

        // DataContext = Model = new();
        Title = "Courser";
        MinimumSize = new Size(250, 150);

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

        var openInCode = new Command { ToolBarText = "Open in Code", Shortcut = Application.Instance.CommonModifier | Keys.O };
        openInCode.Executed += async (_, _) =>
        {
            switch (await calendarService.GetNextEndingLectureAsync())
            {
                case Lecture lecture:
                    {
                        var lectureDirectory = Path.Combine(CourseConfig.NotesRoot, lecture.Name, DateTime.Today.ToString("dd.MM.yy"));
                        if (!Directory.Exists(lectureDirectory)) Directory.CreateDirectory(lectureDirectory);
                        var notesFile = Path.Combine(lectureDirectory, "notes.typ");
                        if (File.Exists(notesFile))
                            File.WriteAllText(notesFile, """
                                                         #import "../../style.typ": style
                                                         #show: style

                                                         """);
                        Process.Start("code.exe", notesFile);
                        break;
                    }
            }
        };

        ToolBar = new()
        {
            Items =
            {
                new CheckToolItem { Text = "On Top", Style = "darker" }.Apply(e => e.CheckedChanged += (s, _) => Topmost = ((CheckToolItem)s)!.Checked),
                new ButtonToolItem { Text = "Cal" }.Apply(e => e.Click += async (_, _) => await Services.GetService<CalendarForm>().ShowModalAsync(this))
            },
        };

        if (CourseConfig is { WlanPassword: not null, WlanUserName: not null, LoginUri: not null })
        {
            ToolBar.Items.Add(new ButtonToolItem { Text = "Login" }.Apply(e => e.Click += async (_, _) => await LoginToNetwork()));
        }

        DataUpdateTask = Task.Run(UpdateTask);
    }



    async Task LoginToNetwork()
    {
        try
        {
            using var client = new HttpClient();
            if (!(await client.GetAsync(CourseConfig.LoginUri)).IsSuccessStatusCode)
            {
                MessageBox.Show("Fetch of login page failed", MessageBoxButtons.OK, MessageBoxType.Error);
                return;
            }
            var response = await client.PostAsync(CourseConfig.LoginUri, new FormUrlEncodedContent([
                new("buttonClicked", "4"),
                new("redirect_url", CourseConfig.LoginRedirectUri.ToString()),
                new("err_flag", "0"),
                new("username", CourseConfig.WlanUserName),
                new("password", CourseConfig.WlanPassword)
            ]));

            if (response.IsSuccessStatusCode)
                MessageBox.Show("Login returned OK", MessageBoxButtons.OK, MessageBoxType.Information);
            else
                MessageBox.Show($"Login failed: {response}", MessageBoxButtons.OK, MessageBoxType.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }

    private async Task UpdateTask()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        int ticker = 0;
        do
        {
            switch (await CalendarService.GetNextEndingLectureAsync())
            {
                case Lecture lecture:

                    var now = DateTimeOffset.UtcNow;

                    string nameString, timeString;
                    Icon icon;
                    if ((now - lecture.Start) / (lecture.End - lecture.Start) is >= 0 and var elapsedLecturePercent)
                    {
                        nameString = lecture.Name;
                        timeString = ParseRunningLecture(lecture, now);
                        icon = lecture.Icon;

                        await Utils.UI.InvokeAsync(() =>
                        {
                            Model.ElapsedPercent = $"{100 * elapsedLecturePercent:F2}%";
                            Taskbar.SetProgress(TaskbarProgressState.Progress, (float)elapsedLecturePercent);
                        });
                    }
                    else
                    {
                        nameString = $"Next: {lecture.Name}";
                        timeString = ParseUpcomingLecture(lecture, now);
                        icon = lecture.Start - now < TimeSpan.FromDays(7)
                            ? lecture.Icon
                            : Icons.DiamondIcon;

                        await Utils.UI.InvokeAsync(() =>
                        {
                            Model.ElapsedPercent = "";
                            Taskbar.SetProgress(TaskbarProgressState.None);
                        });
                    }

                    await Utils.UI.InvokeAsync(() =>
                    {
                        Model.CourseName = nameString;
                        Model.FormattedTime = timeString;
                        Model.Rooms = lecture.Rooms;
                        Icon = icon;
                    });
                    break;
                case Error error:
                    await Utils.UI.InvokeAsync(() =>
                    {
                        Model.CourseName = error.Message;
                        Icon = Icons.ErrorIcon;
                    });
                    break;
                case null:
                    await Utils.UI.InvokeAsync(() =>
                    {
                        Model.CourseName = "No lecture found";
                        Model.FormattedTime = "";
                        Model.ElapsedPercent = "";
                        Model.Rooms = "";
                        Taskbar.SetProgress(TaskbarProgressState.None);
                    });
                    break;
            }
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
        public string CourseName { get; set => SetAndRaise(ref field, value); }
        public string FormattedTime { get; set => SetAndRaise(ref field, value); }
        public string ElapsedPercent { get; set => SetAndRaise(ref field, value); }
        public string Rooms { get; set => SetAndRaise(ref field, value); }
    }
}
