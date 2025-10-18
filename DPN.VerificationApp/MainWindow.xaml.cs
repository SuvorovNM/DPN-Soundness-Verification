using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using DataPetriNetGeneration;
using DPN.Models;
using DPN.Parsers;
using DPN.Soundness;
using DPN.Soundness.Repair;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.Verification;
using DPN.VerificationApp.Services;
using DPN.Visualization.Converters;
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
		private readonly TransformerToRefined transformerToRefined;
		private readonly TransformerToTau transformerToTau;
		private readonly RelaxedLazySoundnessVerifier relaxedLazySoundnessVerifier;
		private readonly ClassicalSoundnessVerifier classicalSoundnessVerifier;
		private readonly ClassicalSoundnessRepairer classicalSoundnessRepairer;

		private Context context;

		public MainWindow()
		{
			InitializeComponent();
			dpnConverter = new DpnToGraphConverter();
			dpnProvider = new SampleDPNProvider();
			transformerToRefined = new TransformerToRefined();
			transformerToTau = new TransformerToTau();
			relaxedLazySoundnessVerifier = new RelaxedLazySoundnessVerifier();
			classicalSoundnessVerifier = new ClassicalSoundnessVerifier();
			classicalSoundnessRepairer = new ClassicalSoundnessRepairer();
			pnmlParser = new PnmlParser();
			context = new Context();
			Global.SetParameter("parallel.enable", "true");

			currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
			graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		}
		
		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
		}

		private void MaximizeButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.WindowState == WindowState.Maximized)
			{
				this.WindowState = WindowState.Normal;
			}
			else
			{
				this.WindowState = WindowState.Maximized;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				if (e.ClickCount == 2)
				{
					MaximizeButton_Click(sender, e);
				}
				else
				{
					this.DragMove();
				}
			}
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
			ShowLoader("Verifying Relaxed Lazy Soundness");
			var verificationResult = await Task.Run(() => relaxedLazySoundnessVerifier.Verify(currentDisplayedNet, new Dictionary<string, string>()));
			HideLoader();

			VisualizeVerificationResult(verificationResult);
		}

		private async void CheckSoundnessDirectItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Verifying Soundness");
			var verificationResult = await Task.Run(() => classicalSoundnessVerifier.Verify(currentDisplayedNet, new Dictionary<string, string>()));
			HideLoader();

			VisualizeVerificationResult(verificationResult);
		}

		private async void CheckSoundnessImprovedItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Verifying Soundness");
			var verificationResult = await Task.Run(() => classicalSoundnessVerifier.Verify(
				currentDisplayedNet,
				verificationSettings: new Dictionary<string, string>
				{
					{ ClassicalVerificationSettingsConstants.AlgorithmVersion, ClassicalVerificationSettingsConstants.ImprovedVersion }
				}));
			HideLoader();

			MessageBox.Show($"Time spent: {(int)verificationResult.VerificationTime!.Value.TotalMilliseconds}ms");

			VisualizeVerificationResult(verificationResult);
		}

		private async void ConstructConstraintGraphMenuItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Constructing Constraint Graph");
			var stateSpace = await Task.Run(() => StateSpaceConstructor.ConstructConstraintGraph(currentDisplayedNet));
			HideLoader();

			VisualizeVerificationResult(new VerificationResult(stateSpace, ClassicalSoundnessAnalyzer.CheckSoundness(stateSpace)));
		}

		private void VisualizeVerificationResult(VerificationResult verificationResult)
		{
			var ltsWindow = new StateSpace(verificationResult, isOpenedFromFile: false)
			{
				Owner = this
			};
			ltsWindow.Show();
		}

		private async void TransformModelToRefinedItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Refining DPN");
			var refinementResult = await Task.Run(() => transformerToRefined.Transform(
				currentDisplayedNet,
				new Dictionary<string, string>
				{
					{ RefinementSettingsConstants.BaseStructure, RefinementSettingsConstants.CoverabilityGraph }
				}));
			HideLoader();

			currentDisplayedNet = refinementResult.RefinedDpn;
			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private void TransformModelToTauItem_Click(object sender, RoutedEventArgs e)
		{
			currentDisplayedNet = transformerToTau.Transform(currentDisplayedNet);
			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
		}

		private async void ConstructReachabilityGraphItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Constructing Reachability Graph");
			var stateSpace = await Task.Run(() => StateSpaceConstructor.ConstructReachabilityGraph(currentDisplayedNet));
			var soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(stateSpace);
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
					? ClassicalSoundnessAnalyzer.CheckSoundness(stateSpace)
					: RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);

				var constraintGraphWindow = new StateSpace(new VerificationResult(stateSpace, soundnessProperties), isOpenedFromFile: true);
				constraintGraphWindow.Owner = this;
				constraintGraphWindow.Show();
			}
		}

		private async void TransformModelToRepairedItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Repairing DPN");
			var repairResult = await Task.Run(() => classicalSoundnessRepairer.Repair(currentDisplayedNet, new Dictionary<string, string>()));
			HideLoader();

			MessageBox.Show(repairResult.IsSuccess
				? $"Success! Time spent: {(long)repairResult.RepairTime.TotalMilliseconds} ms. Repair steps: {repairResult.RepairSteps}."
				: "Failed to repair the model. Try using different repair algorithm.");
			graphControl.Graph = dpnConverter.ConvertToDpn(repairResult.Dpn);
			currentDisplayedNet = repairResult.Dpn;
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