﻿using DataPetriNetGeneration;
using DataPetriNetIterativeVerificationApplication.Enums;
using DataPetriNetIterativeVerificationApplication.Services;
using DataPetriNetOnSmt;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DataPetriNetIterativeVerificationApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<int, string> paths = new Dictionary<int, string>();
        private ObservableCollection<VerificationOutputWithNumber> verificationResults = new();
        private CancellationTokenSource source;
        private VerificationRunner verificationRunner = new();
        public MainWindow()
        {
            InitializeComponent();
            StopsBtn.IsEnabled = false;
            MaxDtTb.Visibility = Visibility.Hidden;
            PercentLb.Visibility = Visibility.Hidden;
            BoundnessCmb.Visibility = Visibility.Hidden;
            SoundnessCmb.Visibility = Visibility.Hidden;

            PlacesNumberTb.Text = "0.8";
            TransitionsNumberTb.Text = "1";
            ArcsNumberTb.Text = "0.5";
            VariablesNumberTb.Text = "0.5";
            ConditionsNumberTb.Text = "1";
            MaxDtTb.Text = "60";
            QEWithoutTransChb.IsChecked = true;
            BoundnessCmb.SelectedIndex = 0;
            SoundnessCmb.SelectedIndex = 0;
            InitialValueTb.Text = "5";
            IncrementValueTb.Text = "5";
            DpnNumberTb.Text = "3";
            DirectoryTb.Text = AppDomain.CurrentDomain.BaseDirectory + "Output";

            MinPlacesTb.Text = "2";
            MinTransitionsTb.Text = "2";
            MinArcsTb.Text = "0";
            MinVarsTb.Text = "1";
            MinConditionsTb.Text = "0";

            MaxPlacesTb.Text = "150";
            MaxTransitionsTb.Text = "150";
            MaxArcsTb.Text = "50";
            MaxConditionsTb.Text = "150";
            MaxVarsTb.Text = "150";

            VerificationDG.ItemsSource = verificationResults;
            verificationResults.CollectionChanged += listChanged;
        }
        private void listChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            var t = (VerificationOutputWithNumber)args.NewItems[0];
            if (t != null)
            {
                paths.Add(t.Number, System.IO.Path.Combine(DirectoryTb.Text, t.Id));
            }
            VerificationDG.Items.Refresh();
            VerificationDG.Columns[^1].Visibility = Visibility.Collapsed;
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = (DataGridRow)sender;
            var item = (VerificationOutputWithNumber)row.Item;
            //LoadCG(item);
            LoadDpn(item);
        }

        private void LoadCG(VerificationOutputWithNumber item)
        {
            if (item != null)
            {
                using (var fs = new FileStream(paths[item.Number] + ".cgml", FileMode.Open))// + "_" + item.VerificationType
                {
                    var cgmlParser = new CgmlParser();
                    var xDocument = XDocument.Load(fs);

                    var constraintGraphToVisualize = cgmlParser.Deserialize(xDocument);

                    var constraintGraphWindow = new LtsWindow(constraintGraphToVisualize);
                    constraintGraphWindow.Owner = this;
                    constraintGraphWindow.Show();
                }
            }
        }
        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            StopsBtn.IsEnabled = true;
            StartBtn.IsEnabled = false;
            source = new CancellationTokenSource();

            var conditionsInfo = new ConditionsInfo
            {
                Boundedness = BoundednessChb.IsChecked.Value ? BoundednessChb.IsChecked : null,
                Soundness = SoundnessChb.IsChecked.Value ? SoundnessChb.IsChecked : null,
                DeadTransitions = MaxDtChb.IsChecked.Value ? byte.Parse(MaxDtTb.Text) : null,
            };

            if (iterativeGenerationTab.IsSelected)
            {

                var dpnInfo = new DpnToGenerateInfo
                {
                    Places = double.Parse(PlacesNumberTb.Text, CultureInfo.InvariantCulture),
                    Transitions = double.Parse(TransitionsNumberTb.Text, CultureInfo.InvariantCulture),
                    ExtraArcs = double.Parse(ArcsNumberTb.Text, CultureInfo.InvariantCulture),
                    Conditions = double.Parse(ConditionsNumberTb.Text, CultureInfo.InvariantCulture),
                    Variables = double.Parse(VariablesNumberTb.Text, CultureInfo.InvariantCulture)
                };

                var verificationType =
                    (QEWithTransChb.IsChecked.Value ? VerificationTypeEnum.QeWithTransformation : VerificationTypeEnum.None) |
                    (QEWithoutTransChb.IsChecked.Value ? VerificationTypeEnum.QeWithoutTransformation : VerificationTypeEnum.None) |
                    (nSQEWithTransChb.IsChecked.Value ? VerificationTypeEnum.NsqeWithTransformation : VerificationTypeEnum.None) |
                    (nSQEWithoutTransChb.IsChecked.Value ? VerificationTypeEnum.NsqeWithoutTransformation : VerificationTypeEnum.None);


                var iterationsInfo = new IterationsInfo
                {
                    DpnsPerConfiguration = ushort.Parse(DpnNumberTb.Text),
                    IncrementValue = ushort.Parse(IncrementValueTb.Text),
                    InitialN = ushort.Parse(InitialValueTb.Text)
                };
                var verificationInput = new VerificationInputForIterative
                {
                    DpnInfo = dpnInfo,
                    ConditionsInfo = conditionsInfo,
                    VerificationType = verificationType,
                    IterationsInfo = iterationsInfo,
                    OutputDirectory = DirectoryTb.Text,
                };

                var iterativeVerificationTask = verificationRunner.RunIterativeVerificationLoop(
                    verificationInput,
                    verificationResults,
                    source.Token);

                try
                {
                    await iterativeVerificationTask;
                }
                catch
                {

                }
            }
            else
            {
                var verificationInput = new VerificationInputForRandom
                {
                    ConditionsInfo = conditionsInfo,
                    MinPlaces = int.Parse(MinPlacesTb.Text),
                    MaxPlaces = int.Parse(MaxPlacesTb.Text),
                    MinTransitions = int.Parse(MinTransitionsTb.Text),
                    MaxTransitions = int.Parse(MaxTransitionsTb.Text),
                    MinArcs = int.Parse(MinArcsTb.Text),
                    MaxArcs = int.Parse(MaxArcsTb.Text),
                    MinConditions = int.Parse(MinConditionsTb.Text),
                    MaxConditions = int.Parse(MaxConditionsTb.Text),
                    MinVars = int.Parse(MinVarsTb.Text),
                    MaxVars = int.Parse(MaxVarsTb.Text),
                    OutputDirectory = DirectoryTb.Text,
                };

                var randomVerificationTask = verificationRunner.RunRandomVerificationLoop(
                    verificationInput,
                    verificationResults,
                    source.Token);
                try
                {
                    await randomVerificationTask;
                }
                catch
                {

                }
            }

            StopsBtn.IsEnabled = false;
            StartBtn.IsEnabled = true;
        }

        private void MaxDtChb_Checked(object sender, RoutedEventArgs e)
        {
            MaxDtTb.Visibility = Visibility.Visible;
            PercentLb.Visibility = Visibility.Visible;
        }
        private void MaxDtChb_Unchecked(object sender, RoutedEventArgs e)
        {
            MaxDtTb.Visibility = Visibility.Hidden;
            PercentLb.Visibility = Visibility.Hidden;
        }

        private void BoundednessChb_Checked(object sender, RoutedEventArgs e)
        {
            BoundnessCmb.Visibility = Visibility.Visible;
        }

        private void BoundednessChb_Unchecked(object sender, RoutedEventArgs e)
        {
            BoundnessCmb.Visibility = Visibility.Hidden;
        }

        private void SoundnessChb_Checked(object sender, RoutedEventArgs e)
        {
            SoundnessCmb.Visibility = Visibility.Visible;
        }

        private void SoundnessChb_Unchecked(object sender, RoutedEventArgs e)
        {
            SoundnessCmb.Visibility = Visibility.Hidden;
        }

        private void StopsBtn_Click(object sender, RoutedEventArgs e)
        {
            StartBtn.IsEnabled = true;
            StopsBtn.IsEnabled = false;

            source.Cancel();
        }

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //DpnWindow dpnWindow = new DpnWindow(currentNet);
            //dpnWindow.Owner = this;
            //dpnWindow.Show();
        }

        private void PositiveIntegerNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void PositiveRealNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9.]+");
        }

        private void OpenDpn_Click(object sender, RoutedEventArgs e)
        {
            var item = (VerificationOutputWithNumber)VerificationDG.SelectedItem;
            LoadDpn(item);
        }

        private void LoadDpn(VerificationOutputWithNumber item)
        {
            if (item != null)
            {
                using (var fs = new FileStream(paths[item.Number] + ".pnmlx", FileMode.Open))
                {
                    var pnmlParser = new PnmlParser();
                    var xDocument = new XmlDocument();
                    xDocument.Load(fs);

                    var dataPetriNet = pnmlParser.DeserializeDpn(xDocument);

                    DpnWindow dpnWindow = new DpnWindow(dataPetriNet);
                    dpnWindow.Owner = this;
                    dpnWindow.Show();
                }
            }
        }

        private void OpenCg_Click(object sender, RoutedEventArgs e)
        {
            var item = (VerificationOutputWithNumber)VerificationDG.SelectedItem;
            LoadCG(item);
        }
    }
}
