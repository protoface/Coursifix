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
using System.Net.Http;
using System.Collections.Generic;

namespace Courser.UI;

public sealed partial class MainForm : Form
{
    private ICalendarService CalendarService { get; }
    private CourseConfig CourseConfig { get; }
    private Task DataUpdateTask { get; }
    private MainViewModel Model { get; }


    public MainForm(ICalendarService calendarService, IOptions<CourseConfig> options)
    {
        CalendarService = calendarService;
        CourseConfig = options.Value;

        DataContext = Model = new();
        Title = "Courser";
        MinimumSize = new Size(250, 150);

        DynamicLayout layout = new() { Padding = new(10, 5, 10, 0) };
        layout.Add(null);
        layout.Add(new Label().Bind(e => e.TextBinding, (MainViewModel m) => m.CourseName));
        layout.AddSeparateRow(xscale: true, controls:
            [
                new Label().Bind(e => e.TextBinding, (MainViewModel m) => m.FormattedTime),
                null,
                new Label().Bind(e => e.TextBinding, (MainViewModel m) => m.ElapsedPercent)
            ]
        );
        layout.Add(new Label().Bind(e => e.TextBinding, (MainViewModel m) => m.Rooms));
        layout.Add(null);

        Content = layout;
        Icon = Icon.FromResource("Courser.UI.icon.ico");

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
                new CheckToolItem { Text = "On Top", Style ="darker" }.Apply(e => e.CheckedChanged += (s, _) => Topmost = (s as CheckToolItem).Checked),
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
        using var client = new HttpClient();
        if (!(await client.GetAsync(CourseConfig.LoginUri)).IsSuccessStatusCode)
        {
            MessageBox.Show("Fetch of login page failed", MessageBoxButtons.OK, MessageBoxType.Error);
            return;
        }
        var response = await client.PostAsync(CourseConfig.LoginUri, new FormUrlEncodedContent([
            new("buttonClicked", "4" ),
            new("redirect_url", CourseConfig.LoginRedirectUri ),
            new("err_flag", "0" ),
            new("username", CourseConfig.WlanUserName ),
            new("password", CourseConfig.WlanPassword)]));

        if (response.IsSuccessStatusCode)
            MessageBox.Show("Login returned OK", MessageBoxButtons.OK, MessageBoxType.Information);
        else
            MessageBox.Show($"Login failed: {response}", MessageBoxButtons.OK, MessageBoxType.Error);
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
                    double elapsedLecturePercent = Math.Max((now - lecture.Start) / (lecture.End - lecture.Start), 0);

                    var (timeString, nameString) = elapsedLecturePercent > 0
                        ? (ParseRunningLecture(lecture, now), lecture.Name)
                        : (ParseUpcomingLecture(lecture, now), $"Next: {lecture.Name}");

                    Application.Instance.AsyncInvoke(() =>
                    {
                        Model.CourseName = nameString;
                        Model.FormattedTime = timeString;
                        Model.ElapsedPercent = elapsedLecturePercent > 0 ? $"{100 * elapsedLecturePercent:F2}%" : "";
                        Model.Rooms = lecture.Rooms;
                        Taskbar.SetProgress(elapsedLecturePercent > 0 ? TaskbarProgressState.Progress : TaskbarProgressState.None, (float)elapsedLecturePercent);
                    });
                    break;
                case Error error:
                    Application.Instance.AsyncInvoke(() => Model.CourseName = error.Message);
                    break;
                case null:
                    Application.Instance.AsyncInvoke(() =>
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


        string ParseRunningLecture(Lecture lecture, DateTimeOffset compareDate)
        {
            if (ticker % 4 >= 2)
            {
                return $"Ende: {lecture.End.LocalDateTime:t}";
            }
            else
            {
                return $"noch {(lecture.End.LocalDateTime - compareDate).Humanize(precision: 2, minUnit: TimeUnit.Second)}";
            }
        }

        string ParseUpcomingLecture(Lecture lecture, DateTimeOffset compareDate)
        {
            if (ticker % 4 >= 2)
            {
                return lecture.Start.Humanize(dateToCompareAgainst: compareDate.LocalDateTime);
            }
            else
            {
                return "Start: " + lecture.Start.ToString(lecture.Start.Date == compareDate.Date ? "t" : "g");
            }
        }

    }

    private class MainViewModel : ViewModel<MainViewModel>
    {
        public string CourseName { get; set => SetAndRaise(ref field, value); }
        public string FormattedTime { get; set => SetAndRaise(ref field, value); }
        public string ElapsedPercent { get; set => SetAndRaise(ref field, value); }
        public string Rooms { get; set => SetAndRaise(ref field, value); }
    }
}
