using Eto.Forms;

namespace Courser.UI;

internal abstract class Form<TModel> : Form where TModel : ViewModel<TModel>, new()
{
    protected TModel Model
    {
        get => (TModel)DataContext;
        private init => DataContext = value;
    }

    protected Form()
    {
        Model = new();
    }
}
