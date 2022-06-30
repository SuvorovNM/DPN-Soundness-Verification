using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataPetriNetGeneration.Tests
{
    [TestClass]
    public class BackboneTests
    {
        [TestMethod]
        public void GenerateBackboneWithCorrectNumberOfPlacesAndTransitions()
        {
            var placesCount = 5;
            var transitionsCount = 10;

            var dpnBackboneGenerator = new DPNBackboneGenerator();
            var dpn = dpnBackboneGenerator.Generate(placesCount, transitionsCount);

            Assert.AreEqual(placesCount, dpn.Places.Count);
            Assert.AreEqual(transitionsCount, dpn.Transitions.Count);
        }
    }
}