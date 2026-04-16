using System.Windows;

namespace DPN.VerificationApp
{
    public partial class DPNVerifierMessageBox : Window
    {
        public DPNVerifierMessageBox()
        {
            InitializeComponent();
        }

        public static void Show(Window owner, string message, string title = "Message")
        {
            var dlg = new DPNVerifierMessageBox();
            dlg.Owner = owner;
            dlg.MessageTextBlock.Text = message;
            dlg.Title = title;
            dlg.ShowDialog();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
