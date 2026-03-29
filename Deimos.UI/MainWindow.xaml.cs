using System.Diagnostics;
using System.Windows.Input; // Mouse button events

using Deimos.UI.ViewModels;


namespace Deimos.UI;    // Defines the namespace this class belongs to

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow    // Connects partial logic from xaml file and inherits Window class
{
    private readonly MainViewModel _viewModel; // View model for bindings

    public MainWindow() // Constructor
    {
        InitializeComponent();  // Builds and connects the XAML UI components to this class
        InitializeWindowChrome();
        _viewModel = new MainViewModel(Player); // Wire VM to playback controls
        DataContext = _viewModel; // Bind UI to view model
        _viewModel.PropertyChanged += ViewModel_OnPropertyChangedForEditWindow;
        InitializeSeekBarLogic();
        InitializeNowPlayingScroll(); // Enable title scrolling
    }

    private void PlayList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Debug.WriteLine("Playlist double-click detected.");
        if (_viewModel.PlaySelectedCommand.CanExecute(null)) _viewModel.PlaySelectedCommand.Execute(null); // Trigger playback
    }
}
