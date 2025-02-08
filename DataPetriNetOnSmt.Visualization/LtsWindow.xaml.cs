﻿using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.Visualization.Extensions;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ToGraphParser;

namespace DataPetriNetOnSmt.Visualization
{
    /// <summary>
    /// Логика взаимодействия для ConstraintGraphWindow.xaml
    /// </summary>
    public partial class LtsWindow : Window
    {
        private readonly LtsToGraphParser constraintGraphToGraphParser;

        public LtsWindow(ConstraintGraphToVisualize constraintGraph)
        {
            InitializeComponent();

            constraintGraphToGraphParser = new LtsToGraphParser();

            graphControl.Graph = constraintGraphToGraphParser.FormGraphBasedOnCG(constraintGraph);
            graphControl.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            logControl.FormSoundnessVerificationLog(constraintGraph);
        }
    }
}
