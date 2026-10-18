using System;

namespace Courser.Common.Models;

public class Lecture
{
    public required string Name { get; init; }
    public required string Rooms { get; init; }
    public required DateTimeOffset Start { get; set; }
    public required DateTimeOffset End { get; set; }
}
