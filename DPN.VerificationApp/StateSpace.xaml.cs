using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using DPN.Parsers;
using DPN.Soundness;
using DPN.VerificationApp.Extensions;
using DPN.Visualization.Converters;
using DPN.Visualization.Models;
using Microsoft.Win32;

namespace DPN.VerificationApp
{
	public partial class StateSpace : Window
	{
		private const int maxNodesToVisualize = 2000;
		private const int maxArcsToVisualize = 5000;

		private readonly VerificationResult verificationResult;

		public StateSpace(VerificationResult verificationResult, bool isOpenedFromFile)
		{
			InitializeComponent();

			this.verificationResult = verificationResult;

			CheckGraphSizeAndSetVisibility(
				verificationResult.StateSpaceGraph.Nodes.Length,
				verificationResult.StateSpaceGraph.Arcs.Length);

			if (FindName("SaveMenu") is Menu menu && (isOpenedFromFile || IsOverlayVisible))
				menu.Visibility = Visibility.Collapsed;

			ShowGraph(showOnlyLog: IsOverlayVisible);
		}

		private void SaveStateSpace_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new SaveFileDialog()
			{
				Filter = "State space files (*.asml) | *.asml"
			};
			if (ofd.ShowDialog() == true)
			{
				using (var fs = new FileStream(ofd.FileName, FileMode.OpenOrCreate))
				{
					var asmlParser = new AsmlParser();
					var xDocument = asmlParser.Serialize(verificationResult.StateSpaceGraph);

					xDocument.Save(fs, SaveOptions.None);
				}
			}
		}

		private void CheckGraphSizeAndSetVisibility(int nodeCount, int edgeCount)
		{
			if (nodeCount <= maxNodesToVisualize && edgeCount <= maxArcsToVisualize)
			{
				GraphTooLargeOverlay.Visibility = Visibility.Collapsed;
				graphControl.Visibility = Visibility.Visible;
			}
			else
			{
				GraphTooLargeOverlay.Visibility = Visibility.Visible;
				graphControl.Visibility = Visibility.Collapsed;

				GraphSizeText.Text = $"The graph is too large for visualization.\n" +
				                     $"Nodes: {nodeCount}, Edges: {edgeCount}\n";
			}
		}

		public void ShowGraph(bool showOnlyLog)
		{
			var graphToVisualize = ToGraphToVisualizeConverter.Convert(verificationResult);
			logControl.FormOutput(
				graphToVisualize, 
				verificationResult.StateSpaceGraph.DpnTransitions, 
				verificationResult.StateSpaceGraph.TypedVariables, 
				verificationResult.VerificationTime);
			if (showOnlyLog)
			{
				return;
			}

			GraphTooLargeOverlay.Visibility = Visibility.Collapsed;
			graphControl.Visibility = Visibility.Visible;

			IToGraphConverter constraintGraphToGraphParser = graphToVisualize.GraphType == GraphType.Lts
				? new LtsToGraphConverter()
				: new CoverabilityGraphToGraphConverter();

			graphControl.Graph = constraintGraphToGraphParser.Convert(graphToVisualize);
			graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

			graphControl.InvalidateMeasure();
			graphControl.InvalidateArrange();
			graphControl.InvalidateVisual();
		}

		private void ShowGraphAnywayButton_Click(object sender, RoutedEventArgs e)
		{
			ShowGraph(false);
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

		private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
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

		public bool IsOverlayVisible => GraphTooLargeOverlay.Visibility == Visibility.Visible;
	}
}