using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
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
