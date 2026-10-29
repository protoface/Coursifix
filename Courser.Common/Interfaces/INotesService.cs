using System;
using Courser.Common.Models;

namespace Courser.Common.Interfaces;

public interface INotesService
{
    public ILectureNotes GetLectureNotes(Lecture lecture);
    
}
