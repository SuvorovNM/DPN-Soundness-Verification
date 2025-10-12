using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

		private async void ConstructCoverabilityTree_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Constructing Coverability Tree");
			var stateSpace = await Task.Run(() => StateSpaceConstructor.ConstructCoverabilityTree(currentDisplayedNet, false));
			HideLoader();
			
			var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);
			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties));
		}

		private async void ConstructCoverabilityGraph_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Constructing Coverability Graph");
			var stateSpace = await Task.Run(() => StateSpaceConstructor.ConstructCoverabilityGraph(currentDisplayedNet, false));
			HideLoader();
			
			var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);
			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties));
		}

		private async void CheckLazySoundnessDirectItem_Click(object sender, RoutedEventArgs e)
		{
			var soundnessVerifier = new RelaxedLazySoundnessVerifier();
			
			ShowLoader("Verifying Relaxed Lazy Soundness");
			var verificationResult = await Task.Run(() => soundnessVerifier.Verify(currentDisplayedNet, new Dictionary<string, string>()));
			HideLoader();

			VisualizeVerificationResult(verificationResult);
		}

		private async void CheckSoundnessDirectItem_Click(object sender, RoutedEventArgs e)
		{
			var soundnessVerifier = new ClassicalSoundnessVerifier();
			
			ShowLoader("Verifying Soundness");
			var verificationResult = await Task.Run(() => soundnessVerifier.Verify(currentDisplayedNet, new Dictionary<string, string>()));
			HideLoader();

			VisualizeVerificationResult(verificationResult);
		}

		private async void CheckSoundnessImprovedItem_Click(object sender, RoutedEventArgs e)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var soundnessVerifier = new ClassicalSoundnessVerifier();
			
			ShowLoader("Verifying Soundness");
			var verificationResult = await Task.Run(()=>soundnessVerifier.Verify(
				currentDisplayedNet,
				verificationSettings: new Dictionary<string, string>
				{
					{ ClassicalSoundnessVerifier.VerificationSettingsConstants.AlgorithmVersion, ClassicalSoundnessVerifier.VerificationSettingsConstants.ImprovedVersion }
				}));
			HideLoader();

			stopwatch.Stop();
			MessageBox.Show($"Time spent: {stopwatch.ElapsedMilliseconds}ms");

			VisualizeVerificationResult(verificationResult);
		}

		private async void ConstructConstraintGraphMenuItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Constructing Constraint Graph");
			var stateSpace = await Task.Run(()=> StateSpaceConstructor.ConstructConstraintGraph(currentDisplayedNet));
			HideLoader();
			
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

		private async void TransformModelToRefinedItem_Click(object sender, RoutedEventArgs e)
		{
			var dpnTransformation = new TransformerToRefined();

			ShowLoader("Refining DPN");
			(currentDisplayedNet, _) = await Task.Run(() => dpnTransformation.TransformUsingCg(currentDisplayedNet));
			HideLoader();

			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private void TransformModelToTauItem_Click(object sender, RoutedEventArgs e)
		{
			var dpnTransformation = new TransformerToTau();

			currentDisplayedNet = dpnTransformation.Transform(currentDisplayedNet);

			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private async void ConstructReachabilityGraphItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Constructing Reachability Graph");
			var stateSpace = await Task.Run(() => StateSpaceConstructor.ConstructReachabilityGraph(currentDisplayedNet));
			var soundnessProperties = SoundnessAnalyzer.CheckSoundness(stateSpace);
			HideLoader();

			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties));
		}

		private void OpenStateSpace_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Filter = "State space files (*.asml) | *.asml"
			};
			if (ofd.ShowDialog() == true)
			{
				using var fs = new FileStream(ofd.FileName, FileMode.Open);
				var asmlParser = new AsmlParser();
				var xDocument = XDocument.Load(fs);

				var stateSpace = asmlParser.Deserialize(xDocument);
				var soundnessProperties = stateSpace.StateSpaceType == TransitionSystemType.AbstractReachabilityGraph
					? SoundnessAnalyzer.CheckSoundness(stateSpace)
					: RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);

				var constraintGraphWindow = new LtsWindow(new VerificationResult(stateSpace, soundnessProperties), isOpenedFromFile: true);
				constraintGraphWindow.Owner = this;
				constraintGraphWindow.Show();
			}
		}

		private async void TransformModelToRepairedItem_Click(object sender, RoutedEventArgs e)
		{
			var dpnRepairment = new Repairment();

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			ShowLoader("Repairing DPN");
			(currentDisplayedNet, var repairSteps, var result) = await Task.Run(() => dpnRepairment.RepairDpn(currentDisplayedNet));
			HideLoader();

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
		
		private void ShowLoader(string message = "Processing...")
		{
			// Ensure we're on the UI thread
			Dispatcher.Invoke(() =>
			{
				LoaderText.Text = message;
				LoaderOverlay.Visibility = Visibility.Visible;
        
				// Disable menu items while loading
				SetMenuEnabledState(false);
			});
		}

		private void HideLoader()
		{
			// Ensure we're on the UI thread
			Dispatcher.Invoke(() =>
			{
				LoaderOverlay.Visibility = Visibility.Collapsed;
        
				// Re-enable menu items
				SetMenuEnabledState(true);
			});
		}
		
		private void SetMenuEnabledState(bool enabled)
		{
			OpenDpnItem.IsEnabled = enabled;
			OpenCgItem.IsEnabled = enabled;
			SaveDpnItem.IsEnabled = enabled;
			ConstructLtsItem.IsEnabled = enabled;
			ConstructCtItem.IsEnabled = enabled;
			ConstructCgItem.IsEnabled = enabled;
			CheckSoundnessMenuItem.IsEnabled = enabled;
			CheckSoundnessDirectItem.IsEnabled = enabled;
			CheckSoundnessImprovedItem.IsEnabled = enabled;
			CheckSoundnessLazyItem.IsEnabled = enabled;
			TransformModelToRepairedItem.IsEnabled = enabled;
			DefaultVOCMenuItem.IsEnabled = enabled;
			DefaultVOVMenuItem.IsEnabled = enabled;
			GenerateModelItem.IsEnabled = enabled;
			TransformModelToRefinedItem.IsEnabled = enabled;
			TransformModelToTauItem.IsEnabled = enabled;
		}
	}
}