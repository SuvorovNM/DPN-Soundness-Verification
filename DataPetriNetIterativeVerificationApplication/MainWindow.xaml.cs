using DataPetriNetGeneration;
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
        private ObservableCollection<VerificationOutputWithNumber> verificationResults = new ObservableCollection<VerificationOutputWithNumber>();
        //private DataPetriNet currentNet;
        private CancellationTokenSource source;
        private IterativeVerificationRunner verificationRunner = new IterativeVerificationRunner();
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
            MaxDtTb.Text = "50";
            QEWithoutTransChb.IsChecked = true;
            BoundnessCmb.SelectedIndex = 0;
            SoundnessCmb.SelectedIndex = 0;
            InitialValueTb.Text = "5";
            IncrementValueTb.Text = "5";
            DpnNumberTb.Text = "3";
            DirectoryTb.Text = AppDomain.CurrentDomain.BaseDirectory + "Output";

            VerificationDG.ItemsSource = verificationResults;
            verificationResults.CollectionChanged += listChanged;
        }
        private void listChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            var t = (VerificationOutputWithNumber)args.NewItems[0];
            if (t != null)
            {
                paths.Add(t.Number, System.IO.Path.Combine(DirectoryTb.Text, t.Identifier));
            }
            VerificationDG.Items.Refresh();
            VerificationDG.Columns[^1].Visibility = Visibility.Collapsed;
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = (DataGridRow)sender;
            var outputRow = (VerificationOutputWithNumber)row.Item;
            if (row != null)
            {
                using (var fs = new FileStream(paths[outputRow.Number]+"_"+outputRow.VerificationType+".cgml", FileMode.Open))
                {
                    var cgmlParser = new CgmlParser();
                    var xDocument = XDocument.Load(fs);

                    var constraintGraphToVisualize = cgmlParser.Deserialize(xDocument);

                    var constraintGraphWindow = new ConstraintGraphWindow(constraintGraphToVisualize);
                    constraintGraphWindow.Owner = this;
                    constraintGraphWindow.Show();
                }
            }
            // execute some code
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            StopsBtn.IsEnabled = true;
            StartBtn.IsEnabled = false;
            source = new CancellationTokenSource();

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

            var conditionsInfo = new ConditionsInfo
            {
                Boundedness = BoundednessChb.IsChecked.Value ? BoundednessChb.IsChecked : null,
                Soundness = SoundnessChb.IsChecked.Value ? SoundnessChb.IsChecked : null,
                DeadTransitions = MaxDtChb.IsChecked.Value ? byte.Parse(MaxDtTb.Text) : null,
            };

            var iterationsInfo = new IterationsInfo
            {
                DpnsPerConfiguration = ushort.Parse(DpnNumberTb.Text),
                IncrementValue = ushort.Parse(IncrementValueTb.Text),
                InitialN = ushort.Parse(InitialValueTb.Text)
            };

            AnonymousPipeServerStream pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

            /*currentNet = await Task.Run(()=> dpnGenerator.Generate(
                dpnInfo.Places * iterationsInfo.InitialN,
                dpnInfo.Transitions * iterationsInfo.InitialN,
                dpnInfo.ExtraArcs * iterationsInfo.InitialN,
                dpnInfo.Variables * iterationsInfo.InitialN,
                dpnInfo.Conditions * iterationsInfo.InitialN));

            DpnInfoLb.Content = new TextBlock()
            {
                Text = $"Places: {currentNet.Places.Count}, Transitions: {currentNet.Transitions.Count}, Arcs: {currentNet.Arcs.Count}," +
                $" Variables: {dpnInfo.Variables}, Conditions: {dpnInfo.Conditions}",
                TextDecorations = TextDecorations.Underline
            };*/

            var verificationInput = new VerificationInput
            {
                DpnInfo = dpnInfo,
                ConditionsInfo = conditionsInfo,
                VerificationType = verificationType,
                IterationsInfo = iterationsInfo,
                OutputDirectory = DirectoryTb.Text,
            };

            var iterativeVerificationTask = verificationRunner.RunVerificationLoop(verificationInput, verificationResults, source.Token);
            //var listeningTask = ListenToPipe(pipeServer, source.Token);

            //await listeningTask;
            try
            {
                await iterativeVerificationTask;
            }
            catch (OperationCanceledException ex)
            {

            }

            StopsBtn.IsEnabled = false;
            StartBtn.IsEnabled = true;
        }

        private async Task ListenToPipe(AnonymousPipeServerStream pipeStream, CancellationToken token)
        {
            //Thread.Sleep(1000);
            //pipeStream.DisposeLocalCopyOfClientHandle();

            using (StreamReader sr = new StreamReader(pipeStream))
            {
                do
                {
                    var lastString = await sr.ReadToEndAsync();
                    if (lastString.Length > 0)
                    {
                        VerificationOutput? verificationOutput = null;

                        XmlSerializer serializer = new XmlSerializer(typeof(VerificationOutput));
                        using (TextReader reader = new StringReader(lastString))
                        {
                            verificationOutput = (VerificationOutput?)serializer.Deserialize(reader);
                        }
                        if (verificationOutput != null)
                        {
                            verificationResults.Add(new VerificationOutputWithNumber(verificationOutput, verificationResults.Count));
                            //VerificationDG.ItemsSource = verificationResults;
                            //VerificationDG.UpdateLayout();
                            VerificationDG.Items.Refresh();
                        }
                    }
                    await Task.Run(() => Thread.Sleep(1000));
                } while (!token.IsCancellationRequested);
            }
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
            var textBox = sender as TextBox;
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void PositiveRealNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = Regex.IsMatch(e.Text, "[^0-9.]+");
        }
    }
}
