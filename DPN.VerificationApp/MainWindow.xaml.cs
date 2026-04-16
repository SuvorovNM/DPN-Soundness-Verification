using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataPetriNetGeneration;
using DPN.Models;
using DPN.Parsers;
using DPN.Soundness;
using DPN.Soundness.Repair;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.StateSpace;
using DPN.Soundness.Verification;
using DPN.Visualization.Converters;
using Microsoft.Win32;
using Microsoft.Z3;

namespace DPN.VerificationApp
{
	public partial class MainWindow : Window
	{
		private DataPetriNet currentDisplayedNet;
		private readonly IDpnToGraphConverter dpnConverter;
		private readonly PnmlxParser pnmlxParser;
		private readonly SampleDPNProvider dpnProvider;
		private readonly TransformerToRefined transformerToRefined;
		private readonly TransformerToTau transformerToTau;
		private readonly RelaxedLazySoundnessVerifier relaxedLazySoundnessVerifier;
		private readonly ClassicalSoundnessVerifier classicalSoundnessVerifier;
		private readonly ClassicalSoundnessRepairer classicalSoundnessRepairer;

		private readonly Context context;

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
			pnmlxParser = new PnmlxParser();
			context = new Context();
			Global.SetParameter("parallel.enable", "true");

			currentDisplayedNet = dpnProvider.GetVOVDataPetriNet();
			graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
			graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		}

		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void MaximizeButton_Click(object sender, RoutedEventArgs e)
		{
			if (WindowState == WindowState.Maximized)
			{
				WindowState = WindowState.Normal;
			}
			else
			{
				WindowState = WindowState.Maximized;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
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
					DragMove();
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
				using var fs = new FileStream(ofd.FileName, FileMode.Open);

				try
				{
					currentDisplayedNet = pnmlxParser.Deserialize(fs, new Context());
				}
				catch (SerializationException exception)
				{
					MessageBox.Show(exception.Message);
					return;
				}

				graphControl.Graph = dpnConverter.ConvertToDpn(currentDisplayedNet);
			}
		}

		private void GenerateModelItem_Click(object sender, RoutedEventArgs e)
		{
			var modelGenerationPropertiesWindow = new ModelGenerationPropertiesWindow();
			if (modelGenerationPropertiesWindow.ShowDialog() == true)
			{
				var dpnGenerator = new DPNGenerator(context);
				currentDisplayedNet = dpnGenerator.Generate(
					modelGenerationPropertiesWindow.PlacesCount,
					modelGenerationPropertiesWindow.TransitionCount,
					modelGenerationPropertiesWindow.ResourcePlacesCount,
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
			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties, stateSpace.Arcs.Length));
		}

		private async void ConstructCoverabilityGraph_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Constructing Coverability Graph");
			var stateSpace = await Task.Run(() => StateSpaceConstructor.ConstructCoverabilityGraph(currentDisplayedNet, true, false));
			HideLoader();

			var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);
			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties, stateSpace.Arcs.Length));
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
			var verificationResult = await Task.Run(() => classicalSoundnessVerifier.Verify(
				currentDisplayedNet,
				new Dictionary<string, string> { { ClassicalVerificationSettingsConstants.ConstructFullGraph, "True" } }));
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
					{ ClassicalVerificationSettingsConstants.AlgorithmVersion, ClassicalVerificationSettingsConstants.DeferringRefinementVersion },
					{ ClassicalVerificationSettingsConstants.ConstructFullGraph, "True" }
				}));
			HideLoader();

			VisualizeVerificationResult(verificationResult);
		}

		private async void ConstructConstraintGraphMenuItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Constructing Constraint Graph");
			var stateSpace = await Task.Run(() => StateSpaceConstructor.ConstructConstraintGraph((DataPetriNet)currentDisplayedNet.Clone()));
			HideLoader();

			VisualizeVerificationResult(new VerificationResult(stateSpace, ClassicalSoundnessAnalyzer.CheckSoundness(stateSpace), stateSpace.Arcs.Length));
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

			VisualizeVerificationResult(new VerificationResult(stateSpace, soundnessProperties, stateSpace.Arcs.Length));
		}

		private void OpenStateSpace_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				Filter = "State space files (*.graphml) | *.graphml"
			};
			if (ofd.ShowDialog() == true)
			{
				using var fs = new FileStream(ofd.FileName, FileMode.Open);
				var graphmlParser = new GraphmlParser();

				StateSpaceGraph stateSpace;
				try
				{
					stateSpace = graphmlParser.Deserialize(fs, context);
				}
				catch (SerializationException exception)
				{
					MessageBox.Show(exception.Message);
					return;
				}

				var soundnessProperties = stateSpace.StateSpaceType == TransitionSystemType.AbstractReachabilityGraph
					? ClassicalSoundnessAnalyzer.CheckSoundness(stateSpace)
					: RelaxedLazySoundnessAnalyzer.CheckSoundness(stateSpace);

				var constraintGraphWindow = new StateSpace(new VerificationResult(stateSpace, soundnessProperties, stateSpace.Arcs.Length), isOpenedFromFile: true)
				{
					Owner = this
				};
				constraintGraphWindow.Show();
			}
		}

		private async void TransformModelToRepairedItem_Click(object sender, RoutedEventArgs e)
		{
			ShowLoader("Repairing DPN");
			var repairResult = await Task.Run(() => classicalSoundnessRepairer.Repair(currentDisplayedNet, new Dictionary<string, string>()));
			HideLoader();
			var message = repairResult.IsSuccess
				? $"Success! Time spent: {(long)repairResult.RepairTime.TotalMilliseconds} ms. \n" +
				  $"Repair steps: {repairResult.RepairSteps}. \n" +
				  $"States constructed: {repairResult.TotalStatesConsidered}. \n" +
				  $"Transition refinements: {repairResult.TotalRefinementsDone}. \n" +
				  $"Modified transitions: {string.Join(',', repairResult.RepairModifications.EnhancedTransitions.ToArray())}"
				: $"Failed to repair the model. Try using different repair algorithm. Time spent: {(long)repairResult.RepairTime.TotalMilliseconds} ms.";
			DPNVerifierMessageBox.Show(this, message, "Repair result");
			graphControl.Graph = dpnConverter.ConvertToDpn(repairResult.Dpn);
			currentDisplayedNet = repairResult.Dpn;
		}

		private async void SaveDpn_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new SaveFileDialog()
			{
				Filter = "Model files (*.pnmlx) | *.pnmlx",
			};
			if (ofd.ShowDialog() == true)
			{
				await using var fs = new FileStream(ofd.FileName, FileMode.Create);
				await pnmlxParser.Serialize(currentDisplayedNet, fs);
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
			Dispatcher.Invoke(() =>
			{
				LoaderText.Text = message;
				LoaderOverlay.Visibility = Visibility.Visible;
				SetMenuEnabledState(false);
			});
		}

		private void HideLoader()
		{
			Dispatcher.Invoke(() =>
			{
				LoaderOverlay.Visibility = Visibility.Collapsed;
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