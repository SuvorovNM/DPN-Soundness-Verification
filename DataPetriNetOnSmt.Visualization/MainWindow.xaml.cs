﻿using DataPetriNetGeneration;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetOnSmt.Visualization.Services;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using Microsoft.Win32;
using Microsoft.Z3;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using ToGraphParser;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataPetriNet currentDisplayedNet;
        private readonly DPNToGraphParser dpnParser;
        private readonly PnmlParser pnmlParser;
        private readonly SampleDPNProvider dpnProvider;
        //private readonly ITransformation dpnTransformation;
        private Context context;

        public MainWindow()
        {
            InitializeComponent();
            dpnParser = new DPNToGraphParser();
            dpnProvider = new SampleDPNProvider();
            pnmlParser = new PnmlParser();
            //dpnTransformation = new TransformationToAtomicConstraints();
            context = new Context();

            currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private void OpenDpn_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Model files (*.pnmlx) | *.pnmlx"
            };
            if (ofd.ShowDialog() == true)
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(ofd.FileName);

                currentDisplayedNet = pnmlParser.DeserializeDpn(xDoc);

                graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
            }
        }

        private void GenerateModelItem_Click(object sender, RoutedEventArgs e)
        {
            ModelGenerationPropertiesWindow modelGenerationPropertiesWindow = new ModelGenerationPropertiesWindow();
            if (modelGenerationPropertiesWindow.ShowDialog() == true)
            {
                var dpnGenerator = new DPNGenerator(context);
                currentDisplayedNet = dpnGenerator.Generate(
                    modelGenerationPropertiesWindow.PlacesCount,
                    modelGenerationPropertiesWindow.TransitionCount,
                    modelGenerationPropertiesWindow.ExtraArcsCount,
                    modelGenerationPropertiesWindow.VarsCount,
                    modelGenerationPropertiesWindow.ConditionsCount);
                graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
            }
        }

        private void DefaultVOCMenuItem_Click(object sender, RoutedEventArgs e)
        {
            currentDisplayedNet = dpnProvider.GetVOCDataPetriNet();
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }
        private void DefaultVOVMenuItem_Click(object sender, RoutedEventArgs e)
        {
            currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }
        private async void QeTacticSoundnessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await DisplayConstraintGraphBasedOnQeConcat();
        }
        private async void ManualSoundnessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await DisplayConstraintGraphManualConcat();
        }

        private async Task DisplayConstraintGraphBasedOnQeConcat()
        {
            if (currentDisplayedNet != null)
            {
                var constraintGraph = new ConstraintGraph(currentDisplayedNet, 
                    new ConstraintExpressionOperationServiceWithEqTacticConcat(currentDisplayedNet.Context));
                var constraintGraphToVisualize = await CheckSoundness(constraintGraph);
                VisualizeConstraintGraph(constraintGraphToVisualize);
            }
        }

        private void VisualizeConstraintGraph(LtsToVisualize constraintGraphToVisualize)
        {
            var constraintGraphWindow = new ConstraintGraphWindow(constraintGraphToVisualize);
            constraintGraphWindow.Owner = this;
            constraintGraphWindow.Show();
        }

        private async Task DisplayConstraintGraphManualConcat()
        {
            if (currentDisplayedNet != null)
            {
                var constraintGraph = new ConstraintGraph(currentDisplayedNet,
                    new ConstraintExpressionOperationServiceWithManualConcat(currentDisplayedNet.Context));
                var constraintGraphToVisualize = await CheckSoundness(constraintGraph);
                VisualizeConstraintGraph(constraintGraphToVisualize);
            }
        }

        private async Task<LtsToVisualize> CheckSoundness
            (LabeledTransitionSystem lts)
        {
            //var constraintGraph = new ConstraintGraph(currentDisplayedNet, expressionService);
            await Task.Run(() => lts.GenerateGraph(true));
            var soundnessProperties = ConstraintGraphAnalyzer.CheckSoundness(currentDisplayedNet, lts);

            return new LtsToVisualize(lts, soundnessProperties);
        }

        private void TransformModelToAtomicItem_Click(object sender, RoutedEventArgs e)
        {
            var dpnTransformation = new TransformationToAtomicConstraints();
            (currentDisplayedNet,_) = dpnTransformation.Transform(currentDisplayedNet);
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }

        private void TransformModelToRefinedItem_Click(object sender, RoutedEventArgs e)
        {
            var dpnTransformation = new TransformationToRefined();
            (currentDisplayedNet,_) = dpnTransformation.Transform(currentDisplayedNet);
            graphControl.Graph = dpnParser.FormGraphBasedOnDPN(currentDisplayedNet);
        }

        private async void ConstructLtsItem_Click(object sender, RoutedEventArgs e)
        {
            if (currentDisplayedNet != null)
            {
                var lts = new ClassicalLabeledTransitionSystem(currentDisplayedNet, new ConstraintExpressionOperationServiceWithEqTacticConcat(currentDisplayedNet.Context));

                //lts.GenerateGraph();

                //var check = new CyclesFinder().GetCycles(lts);


                var ltsToVisualize = await CheckSoundness(lts);
                VisualizeConstraintGraph(ltsToVisualize);
            }
        }

        private void OpenCG_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "CG files (*.cgml) | *.cgml"
            };
            if (ofd.ShowDialog() == true)
            {
                using (var fs = new FileStream(ofd.FileName, FileMode.Open))
                {
                    var cgmlParser = new CgmlParser();
                    var xDocument = XDocument.Load(fs);

                    var constraintGraphToVisualize = cgmlParser.Deserialize(xDocument);

                    var constraintGraphWindow = new ConstraintGraphWindow(constraintGraphToVisualize);
                    constraintGraphWindow.Owner = this;
                    constraintGraphWindow.Show();
                }
            }
        }
    }
}
