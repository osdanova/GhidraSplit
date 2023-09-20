using System.Windows;

namespace GhidraSplit
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        string filepath = null;

        private void Drop_File(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                filepath = files[0];
                FileProcessor.processFile(filepath);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(filepath != null)
                FileProcessor.processFile(filepath);
        }
    }
}
