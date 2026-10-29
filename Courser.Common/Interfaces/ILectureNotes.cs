using System;

namespace Courser.Common.Interfaces;

public interface ILectureNotes
{
    public string[] Todos {get;}
    public void EnsureCreated();
    public void OpenInEditor();
}
