using System.Diagnostics;   // Debug.WriteLine for debug output
using System.Windows;   // Core WPF types like Window, Application, MessageBox
using System.Windows.Controls;  // WPF controls like MenuItem and ListViewItem
using System.Windows.Input; // Mouse button events

using System.Collections.ObjectModel;

using Deimos.UI.Models;
using Deimos.UI.Services;


namespace Deimos.UI;    // Defines the namespace this class belongs to

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow    // Connects partial logic from xaml file and inherits Window class
{
    public ObservableCollection<MediaFile> PlayList { get; } = []; // Notifies UI when items are added/removed
    private readonly MediaPlayback _mediaPlayback;
    
    public MainWindow() // Constructor
    {
        InitializeComponent();  // Builds and connects the XAML UI components to this class
        InitializeWindowChrome();
        InitializeSeekBarLogic();

        _mediaPlayback = new MediaPlayback(PlayList, Player, ImageViewer, NowPlaying);
        _mediaPlayback.LoadDefaultMediaFiles();
        
        DataContext = this;
    }
    
    private void PlayList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Debug.WriteLine("Playlist double-click detected.");
        _mediaPlayback.PlaySelected(LvPlayList.SelectedItem as MediaFile);
    }
    
    /// <summary>
    /// Event handler for clicking Exit
    /// sender = control that triggered the event, e = event data for this routed event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ExitClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Detected: " + ((MenuItem)sender).Name);    // Writes the clicked MenuItem name to debug output
        Debug.WriteLine("Shutting down application.");   // Writes a debug message before shutting down
        Application.Current.Shutdown(); // Closes the entire application
    }
    
    // private void PlayList_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    // {
    //     Debug.WriteLine("Click detected for: " + e.ChangedButton);  // Writes which mouse button changed
    //     MessageBox.Show("File name: " + ((ListViewItem)sender).Content);    // Shows a message box with the clicked item's content
    // }
}
