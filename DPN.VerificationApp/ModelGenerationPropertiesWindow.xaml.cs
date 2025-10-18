using System;
using System.Windows;

namespace DPN.VerificationApp
{
    /// <summary>
    /// Логика взаимодействия для ModelGenerationPropertiesWindow.xaml
    /// </summary>
    public partial class ModelGenerationPropertiesWindow : Window
    {
        private static string transitionsCount = "10";
        private static string placesCount = "5";
        private static string extraArcsCount = "1";
        private static string varsCount = "1";
        private static string conditionsCount = "10";

        public int TransitionCount { get; private set; }
        public int PlacesCount { get; private set; }
        public int ExtraArcsCount { get; private set; }
        public int VarsCount { get; private set; }
        public int ConditionsCount { get; private set; }
        public ModelGenerationPropertiesWindow()
        {
            InitializeComponent();
            FillTextboxesWithDefaults();
        }

        private void FillTextboxesWithDefaults()
        {
            tbTransitionsCount.Text = transitionsCount;
            tbPlacesCount.Text = placesCount;
            tbExtraArcsCount.Text = extraArcsCount;
            tbVarsCount.Text = varsCount;
            tbConditionsCount.Text = conditionsCount;
        }

        private void UpdateDefaultsWithTextboxes()
        {
            transitionsCount = tbTransitionsCount.Text;
            placesCount = tbPlacesCount.Text;
            extraArcsCount = tbExtraArcsCount.Text;
            varsCount = tbVarsCount.Text;
            conditionsCount = tbConditionsCount.Text;
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
	        this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Int32.TryParse(tbTransitionsCount.Text, out var transitionCount) 
                && Int32.TryParse(tbPlacesCount.Text, out var placesCount)
                && Int32.TryParse(tbExtraArcsCount.Text, out var extraArcsCount)
                && Int32.TryParse(tbVarsCount.Text, out var varsCount)
                && Int32.TryParse(tbConditionsCount.Text, out var conditionsCount))
            {
                TransitionCount = transitionCount;
                PlacesCount = placesCount;
                ExtraArcsCount = extraArcsCount;
                VarsCount = varsCount;
                ConditionsCount = conditionsCount;

                UpdateDefaultsWithTextboxes();

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Input is not correct");
            }
        }
    }
}
