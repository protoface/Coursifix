using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Courser.Common.Models;
using Courser.UI.Utils;
using Eto.Drawing;
using Eto.Forms;

namespace Courser.UI;

internal static class Extensions
{
    extension<T>(T obj)
    {
        public T Apply(Action<T> action)
        {
            action(obj);
            return obj;
        }
    }

    extension<T>(T widget) where T : BindableWidget
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public T Bind<TBindable, TModel, TValue>(Form<TModel> parent, Func<T, BindableBinding<TBindable, TValue>> bind,
            Expression<Func<TModel, TValue>> expression) where TModel : ViewModel<TModel>, new()
            where TBindable : IBindable
#pragma warning restore IDE0060 // Remove unused parameter
        {
            bind(widget).BindDataContext(Binding.Property(expression));
            return widget;
        }
    }

    extension<T>(EventHandler<T> eventHandler)
    {
        public void Add(Func<Task> taskFunc) => eventHandler += (_, _) => taskFunc();
    }

    extension(Lecture lecture)
    {
        public Icon Icon => Icons.GetLectureIcon(lecture.Name);
    }
}

internal abstract class ViewModel<TModel> : INotifyPropertyChanged where TModel : ViewModel<TModel>, new()
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected bool SetAndRaise<T>(ref T field, T value, [CallerMemberName] string propertyName = "") where T : IEquatable<T>
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        field = value;
        PropertyChanged?.Invoke(this, new(propertyName));

        return true;
    }

    protected void Raise(string propertyName)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}
