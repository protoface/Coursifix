using System.Diagnostics;
using Courser.Common.Interfaces;
using Courser.Common.Models;
using Humanizer;
using Microsoft.Extensions.Hosting;
using Pillar.Demystifier;
using Spectre.Console;

namespace Courser;

internal class ConsoleWorker(ICalendarService calendarService) : IHostedService
{
    private Task? _runningWorker;
    private CancellationTokenSource? _cts;
    private uint _ticker = 0;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runningWorker = RunnerTask(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task RunnerTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await AnsiConsole.Live(new Panel(new Text("No lecture data")).BorderColor(Spectre.Console.Color.Red))
                .Cropping(VerticalOverflowCropping.Bottom)
                .Overflow(VerticalOverflow.Crop)
                .StartAsync(async ctx =>
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
                    do
                    {
                        var panel = await GetLecturePanel() switch
                        {
                            Panel p => p.DoubleBorder().BorderColor(Spectre.Console.Color.Green),
                            Error e => new Panel(e.Ex is { } ex
                                ? new Rows(new Text(e.Message), ex.GetRenderable())
                                : new Text(e.Message)
                            ).Header("An error occured").BorderColor(Spectre.Console.Color.Red)
                        }; ;
                        ctx.UpdateTarget(panel);
                        _ticker++;
                    } while (await timer.WaitForNextTickAsync(cancellationToken));
                });
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    await File.WriteAllTextAsync($"{DateTime.Now.Ticks}.log", ex.ToStringDemystified(new StyledBuilderOption(false)), default);
            }
            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task<Result<Panel>> GetLecturePanel()
    {
        switch (await calendarService.GetNextEndingLectureAsync())
        {
            case null:
                return new Error("No lecture found");
            case Error e:
                return e;
            case Lecture lecture:
                {
                    var now = DateTimeOffset.UtcNow;
                    var isUpcoming = lecture.Start > now;

                    var timeString = isUpcoming
                    ? _ticker % 2 == 0
                        ? lecture.Start.LocalDateTime.Humanize(dateToCompareAgainst: now.LocalDateTime)
                        : "Start: " + lecture.Start.LocalDateTime.ToString(lecture.Start.Date == now.Date ? "t" : "g")

                    : string.Format("{0} ({1}%)",
                        _ticker % 2 == 0
                            ? $"Ende: {lecture.End.LocalDateTime:t}"
                            : $"noch {(lecture.End.LocalDateTime - now).Humanize()}",
                        Math.Round(100 * (now - lecture.Start) / (lecture.End - lecture.Start), 2));
                    return CreateLecturePanel((isUpcoming ? "Next: " : "") + lecture.Name, lecture.Rooms, timeString);
                }
        }
    }

    private static Panel CreateLecturePanel(string header, string rooms, string time) => new Panel(new Rows(new Text(time), new Text(rooms))).Header(header);

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        if (_runningWorker != null)
        {
            await _runningWorker.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}
