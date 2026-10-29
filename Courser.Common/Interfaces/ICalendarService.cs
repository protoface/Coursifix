using System;
using Courser.Common.Models;

namespace Courser.Common.Interfaces;

public interface ICalendarService
{
    public Task<Maybe<Lecture>> GetNextEndingLectureAsync();
    public Task<Maybe<Lecture[]>> GetLecturesInAsync(DateTimeOffset start, DateTimeOffset end, int maxLectures = 10, bool hasToIncludeLectureEnd = false);
}
