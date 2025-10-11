using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using DataPetriNetGeneration;
using DPN.Models;
using DPN.Parsers;
using DPN.SoundnessVerification;
using DPN.SoundnessVerification.Services;
using DPN.SoundnessVerification.TransitionSystems;
using DPN.VerificationApp.Services;
using DPN.Visualization.Converters;
using DPN.Visualization.Models;
using Microsoft.Win32;
using Microsoft.Z3;

namespace DPN.VerificationApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private DataPetriNet currentDisplayedNet;
		private readonly IDpnToGraphConverter dpnConverter;
		private readonly PnmlParser pnmlParser;
		private readonly SampleDPNProvider dpnProvider;
		private Context context;

		public MainWindow()
		{
			InitializeComponent();
			dpnConverter = new DpnToGraphConverter();
			dpnProvider = new SampleDPNProvider();
			pnmlParser = new PnmlParser();
			context = new Context();
			Global.SetParameter("parallel.enable", "true");
			Global.SetParameter("threads", "4");
			Global.SetParameter("arith.propagation_mode", "2");

			currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
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

				graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
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
				graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
			}
		}

		private void ConstructCoverabilityTree_Click(object sender, RoutedEventArgs e)
		{
			var stateSpace = StateSpaceConstructor.ConstructCoverabilityTree(currentDisplayedNet);
			var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);
			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties));
		}

		private void ConstructCoverabilityGraph_Click(object sender, RoutedEventArgs e)
		{
			// TODO: добавить возможность выбора - строить весь или нет?
			var stateSpace = StateSpaceConstructor.ConstructCoverabilityGraph(currentDisplayedNet, false);
			var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);
			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties));
		}

		private void CheckLazySoundnessDirectItem_Click(object sender, RoutedEventArgs e)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var soundnessVerifier = new RelaxedLazySoundnessVerifier();
			var verificationResult = soundnessVerifier.Verify(currentDisplayedNet, new Dictionary<string, string>());

			stopwatch.Stop();
			MessageBox.Show($"Time spent: {stopwatch.ElapsedMilliseconds}ms");

			VisualizeVerificationResult(verificationResult);
		}

		private void CheckSoundnessDirectItem_Click(object sender, RoutedEventArgs e)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var soundnessVerifier = new ClassicalSoundnessVerifier();
			var verificationResult = soundnessVerifier.Verify(currentDisplayedNet, new Dictionary<string, string>());
			stopwatch.Stop();
			MessageBox.Show($"Time spent: {stopwatch.ElapsedMilliseconds}ms");
			
			VisualizeVerificationResult(verificationResult);
		}

		private void CheckSoundnessImprovedItem_Click(object sender, RoutedEventArgs e)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var soundnessVerifier = new ClassicalSoundnessVerifier();
			var verificationResult = soundnessVerifier.Verify(
				currentDisplayedNet,
				verificationSettings: new Dictionary<string, string>
				{
					{ ClassicalSoundnessVerifier.VerificationSettingsConstants.AlgorithmVersion, ClassicalSoundnessVerifier.VerificationSettingsConstants.ImprovedVersion }
				});
			
			stopwatch.Stop();
			MessageBox.Show($"Time spent: {stopwatch.ElapsedMilliseconds}ms");
			
			VisualizeVerificationResult(verificationResult);
		}

		private void DefaultVOCMenuItem_Click(object sender, RoutedEventArgs e)
		{
			currentDisplayedNet = dpnProvider.GetVOCDataPetriNet();
			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private void DefaultVOVMenuItem_Click(object sender, RoutedEventArgs e)
		{
			currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private void QeTacticSoundnessMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var stateSpace = StateSpaceConstructor.ConstructConstraintGraph(currentDisplayedNet);
			VisualizeVerificationResult(new VerificationResult(stateSpace, SoundnessAnalyzer.CheckSoundness(stateSpace)));
		}

		private void VisualizeVerificationResult(VerificationResult verificationResult)
		{
			var ltsWindow = new LtsWindow(verificationResult, isOpenedFromFile: false)
			{
				Owner = this
			};
			ltsWindow.Show();
		}

		private void TransformModelToRefinedItem_Click(object sender, RoutedEventArgs e)
		{
			var dpnTransformation = new TransformerToRefined();

			(currentDisplayedNet, _) = dpnTransformation.TransformUsingCg(currentDisplayedNet);

			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private void TransformModelToTauItem_Click(object sender, RoutedEventArgs e)
		{
			var dpnTransformation = new TransformerToTau();

			currentDisplayedNet = dpnTransformation.Transform(currentDisplayedNet);

			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private void ConstructLtsItem_Click(object sender, RoutedEventArgs e)
		{
			var stateSpace = StateSpaceConstructor.ConstructLabeledTransitionSystem(currentDisplayedNet);
			var soundnessProperties = SoundnessAnalyzer.CheckSoundness(stateSpace);
			
			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties));
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

					var stateSpace = cgmlParser.Deserialize(xDocument);
					SoundnessProperties soundnessProperties;
					soundnessProperties = stateSpace.StateSpaceType == TransitionSystemType.AbstractReachabilityGraph 
						? SoundnessAnalyzer.CheckSoundness(stateSpace) 
						: RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);

					var constraintGraphWindow = new LtsWindow(new VerificationResult(stateSpace, soundnessProperties), isOpenedFromFile: true);
					constraintGraphWindow.Owner = this;
					constraintGraphWindow.Show();
				}
			}
		}

		private void TransformModelToRepairedItem_Click(object sender, RoutedEventArgs e)
		{
			var dpnRepairment = new Repairment();

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			(currentDisplayedNet, var repairSteps, var result) = dpnRepairment.RepairDpn(currentDisplayedNet);

			stopwatch.Stop();

			MessageBox.Show(result ? $"Success! Time spent: {stopwatch.ElapsedMilliseconds} ms. Repair steps: {repairSteps}." : "Failure!");
			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private void SaveDpn_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new SaveFileDialog()
			{
				Filter = "Model files (*.pnmlx) | *.pnmlx",
			};
			if (ofd.ShowDialog() == true)
			{
				var xDocument = pnmlParser.SerializeDpn(currentDisplayedNet);
				xDocument.Save(ofd.FileName);
			}
		}
	}
}