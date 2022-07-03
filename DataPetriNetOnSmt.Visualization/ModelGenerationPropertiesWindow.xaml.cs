using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataPetriNetOnSmt.Visualization
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
            transitionsCount = tbConditionsCount.Text;
            placesCount = tbPlacesCount.Text;
            extraArcsCount = tbExtraArcsCount.Text;
            varsCount = tbVarsCount.Text;
            conditionsCount = tbConditionsCount.Text;
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
