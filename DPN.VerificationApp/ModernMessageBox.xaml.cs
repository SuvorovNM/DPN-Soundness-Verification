using System.Windows;

namespace DPN.VerificationApp
{
    /// <summary>
    /// Interaction logic for ModernMessageBox.xaml
    /// </summary>
    public partial class ModernMessageBox : Window
    {
        public ModernMessageBox()
        {
            InitializeComponent();
        }

        public static void Show(Window owner, string message, string title = "Message")
        {
            var dlg = new ModernMessageBox();
            dlg.Owner = owner;
            dlg.MessageTextBlock.Text = message;
            dlg.Title = title;
            dlg.ShowDialog();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
