using DataPetriNetIterativeVerificationApplication.Services;
using DPN.Parsers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DPN.Experiments.Common;
using DPN.Soundness;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.Verification;
using Microsoft.Z3;

namespace DataPetriNetIterativeVerificationApplication
{
	public partial class MainWindow : Window
	{
		private readonly Dictionary<int, string> paths = new();
		private readonly ObservableCollection<VerificationOutputWithNumber> verificationResults = new();
		private readonly VerificationRunner verificationRunner;
		private CancellationTokenSource source;

		public MainWindow()
		{
			InitializeComponent();
			StopsBtn.IsEnabled = false;
			MaxDtTb.Visibility = Visibility.Hidden;
			PercentLb.Visibility = Visibility.Hidden;
			BoundnessCmb.Visibility = Visibility.Hidden;
			SoundnessCmb.Visibility = Visibility.Hidden;

			PlacesNumberTb.Text = "0.9";
			TransitionsNumberTb.Text = "1";
			ArcsNumberTb.Text = "0.2";
			ResourcesNumberTb.Text = "0";
			VariablesNumberTb.Text = "0.25";
			ConditionsNumberTb.Text = "0.75";
			MaxDtTb.Text = "25";
			BoundnessCmb.SelectedIndex = 0;
			SoundnessCmb.SelectedIndex = 0;
			SoundnessTypeCmb.SelectedIndex = 0;
			WithRepairCmb.SelectedIndex = 0;
			InitialValueTb.Text = "5";
			IncrementValueTb.Text = "1";
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

			verificationRunner = new();

			VerificationDG.ItemsSource = verificationResults;
			verificationResults.CollectionChanged += ListChanged;
		}

		private void ListChanged(object? sender, NotifyCollectionChangedEventArgs args)
		{
			var t = (VerificationOutputWithNumber?)args.NewItems![0];
			if (t != null)
			{
				paths.Add(t.Number, Path.Combine(DirectoryTb.Text, t.Id));
			}

			VerificationDG.Items.Refresh();
			VerificationDG.Columns[^1].Visibility = Visibility.Collapsed;
		}

		private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			var row = (DataGridRow)sender;
			var item = (VerificationOutputWithNumber)row.Item;
			LoadDpn(item);
		}

		private async void StartBtn_Click(object sender, RoutedEventArgs e)
		{
			StopsBtn.IsEnabled = true;
			StartBtn.IsEnabled = false;
			source = new CancellationTokenSource();

			var conditionsInfo = new ConditionsInfo
			{
				Boundedness = BoundednessChb.IsChecked!.Value
					? bool.Parse(BoundnessCmb.Text)
					: null,
				Soundness = SoundnessChb.IsChecked!.Value
					? bool.Parse(SoundnessCmb.Text)
					: null,
				DeadTransitions = MaxDtChb.IsChecked!.Value ? byte.Parse(MaxDtTb.Text) : null,
			};

			if (iterativeGenerationTab.IsSelected)
			{
				var dpnInfo = new DpnToGenerateInfo
				{
					Places = double.Parse(PlacesNumberTb.Text, CultureInfo.InvariantCulture),
					Transitions = double.Parse(TransitionsNumberTb.Text, CultureInfo.InvariantCulture),
					ResourcePlaces = double.Parse(ResourcesNumberTb.Text, CultureInfo.InvariantCulture),
					ExtraArcs = double.Parse(ArcsNumberTb.Text, CultureInfo.InvariantCulture),
					Conditions = double.Parse(ConditionsNumberTb.Text, CultureInfo.InvariantCulture),
					Variables = double.Parse(VariablesNumberTb.Text, CultureInfo.InvariantCulture)
				};

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
					IterationsInfo = iterationsInfo,
					SoundnessType = Enum.Parse<SoundnessType>(SoundnessTypeCmb.Text),
					WithRepair = bool.Parse(WithRepairCmb.Text),
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
					SoundnessType = Enum.Parse<SoundnessType>(SoundnessTypeCmb.Text),
					WithRepair = bool.Parse(WithRepairCmb.Text),
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
			using var fs = new FileStream(paths[item.Number] + ".pnmlx", FileMode.Open);
			var pnmlParser = new PnmlxParser();
			var dataPetriNet = pnmlParser.Deserialize(fs, new Context());

			var dpnWindow = new DpnWindow(dataPetriNet);
			dpnWindow.Owner = this;
			dpnWindow.Show();
		}
	}
}