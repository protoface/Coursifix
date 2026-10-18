using System.Diagnostics;
using Courser.Common.Interfaces;
using Courser.Common.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Options;

namespace Courser.Common.Services;

internal sealed class CalCalendarService(IOptions<CourseConfig> courseOptions) : ICalendarService
{
    private CourseConfig CourseConfig => courseOptions.Value;
    private Calendar? _calendarData;
    private long _lastCalendarUpdate;

    public async Task<Result<Lecture?>> GetNextEndingLectureAsync()
    {
        var now = DateTime.UtcNow;
        switch (await GetCalendarEvent(e => e.End?.AsUtc > now))
        {
            case null:
                return Result<Lecture?>.Empty;
            case Error e:
                return e;
            case CalendarEvent entry:
                return ToLecture(entry);
        }
    }

    private static Lecture ToLecture(CalendarEvent entry) => new Lecture
    {
        Name = entry.Summary?.Trim() ?? "unnamed lecture",
        Start = entry.Start!.Value,
        End = entry.End!.Value,
        Rooms = entry.Location ?? "No room assigned"
    };

    async Task<Result<CalendarEvent?>> GetCalendarEvent(Func<CalendarEvent, bool> match)
    {
        if (await UpdateCalendarDataAsync() is { } updateError) return updateError;
        return _calendarData?.Events.FirstOrDefault(match);
    }

    async Task<Error?> UpdateCalendarDataAsync()
    {
        if (Stopwatch.GetElapsedTime(_lastCalendarUpdate) < CourseConfig.UpdateInterval) return null;
        using var client = new HttpClient();
        
        Stream? response;
        try
        {

            response = await client.GetStreamAsync("https://dhbw.app/ical/" + CourseConfig.Course);
            if (response is null) return new("failed to get calendar data");
        }
        catch (Exception ex)
        {
            return new("failed to get calendar data", ex);
        }

        _calendarData = Calendar.Load(response);
        if (_calendarData is null) return new("Parsing data failed");

        _lastCalendarUpdate = Stopwatch.GetTimestamp();
        return null;
    }

}
