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
        public int TransitionCount { get; private set; }
        public int PlacesCount { get; private set; }
        public ModelGenerationPropertiesWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Int32.TryParse(tbTransitionsCount.Text, out var transitionCount) && Int32.TryParse(tbPlacesCount.Text, out var placesCount))
            {
                TransitionCount = transitionCount;
                PlacesCount = placesCount;
                Close();
            }
            else
            {
                MessageBox.Show("Input is not correct");
            }
        }
    }
}
