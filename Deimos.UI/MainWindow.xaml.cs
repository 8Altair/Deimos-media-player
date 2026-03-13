using System.Text;
using System.Diagnostics;   // Debug.WriteLine for debug output
using System.Windows;   // Core WPF types like Window, Application, MessageBox
using System.Windows.Controls;  // WPF controls like MenuItem and ListViewItem
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input; // Input-related types like MouseButtonEventArgs
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Deimos.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window    // Connects partial logic from xaml file and inherits Window class
{
    private bool _isDraggingSeekBar;
    private const double SeekMinimum = 0;
    private const double SeekMaximum = 100;
    private double _seekValue;
    
    public MainWindow() // Constructor
    {
        InitializeComponent();  // Builds and connects the XAML UI components to this class
        
        UpdateSeekBarVisual();
        
        // Temporary test items
        LvPlayList.Items.Add("Song1.mp3");
        LvPlayList.Items.Add("Song2.mp3");
        LvPlayList.Items.Add("Song3.mp3");
    }
    
    private void ExitClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Detected: " + ((MenuItem)sender).Name);
        Debug.WriteLine("Shutting down application.");
        Application.Current.Shutdown();
    }

    private void PlayList_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        Debug.WriteLine("Click detected for: " + e.ChangedButton);
        MessageBox.Show("File name: " + ((ListViewItem)sender).Content);
    }
}
