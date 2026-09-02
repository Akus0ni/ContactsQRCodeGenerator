using System.Windows.Controls;

namespace ContactQR.App.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();

        // Search is focused on arrival: the returning-client reprint spends almost its whole
        // time budget in this field (PRD M3).
        Loaded += (_, _) => SearchBox.Focus();
    }
}
