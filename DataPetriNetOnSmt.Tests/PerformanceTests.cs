using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetOnSmt.Tests
{
    [TestClass]
    public class PerformanceTests
    {
        private const string dpnFile = "testModel.pnml";
        private DataPetriNet dpn;

        [TestInitialize]
        public void Initialize()
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(dpnFile);

            var pnmlParser = new PnmlParser();
            dpn = pnmlParser.DeserializeDpn(xDoc);
        }

        [TestMethod]
        public void TestManualConcat()
        {
            var constraintGraph = new ConstraintGraph
                (dpn, new ConstraintExpressionOperationServiceWithManualConcat(dpn.Context));

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            constraintGraph.GenerateGraph();
            stopwatch.Stop();

            var resultTime = stopwatch.Elapsed;

            Assert.AreEqual(216, constraintGraph.ConstraintStates.Count);
            Assert.AreEqual(528, constraintGraph.ConstraintArcs.Count);

            File.AppendAllText("Performance.txt", resultTime.ToString()+"\n");
        }
    }
}
