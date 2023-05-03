# DPN Soundness Verification App

The toolkit allowing to verify data-aware soundness of DPNs with composite input/output conditions.
The soundness verification procedure consists of five main steps: 
1. Checking boundedness of a DPN, 
2. Splitting DPN transitions occurring in cycles, 
3. Adding $\tau$-transitions whose guards are negations of guards of the existing DPN transitions, 
4. Constructing a Labeled Transition System (LTS) for the resultant DPN, 
5. Analyzing the LTS for data-aware soundness properties.
Two algorithms for soundness verification are implemented. The direct algorithm follows the described above schema. The improved algorithm postpones the DPN refinement and performs the preliminary checks on the LTS of the source DPN and the LTS of the tau-DPN.

Conditions may be either atomic or composite. Each atomic condition is a condition of variable-operator-constant and variable-operator-variable forms.

Model is imported in the extended PNML format, input examples are presented in DataPetriNetOnSmt.Visualization\Samples_For_Import.
For the input model, a transition system highlighting sources of unsoundness is constructed.

## Requirements

- Windows 10/11
- .NET SDK 6.0.407

## Run

To run the app, download SoundnessVerifier.zip of the last release, extract the files to an arbitrary directory and run 'DataPetriNetOnSmt.Visualization.exe'.

## Usage

1. Import a DPN choosing File -> Open DPN...
2. Choose Model -> Check Soundness. Here two algorithms for soundness verification are proposed: direct and improved. The direct approach reveals all the sources of unsoundness and highlights them on the LTS but may take more time than the improved approach. The improved approach checks whether or not the DPN is sound and present the least detailed LTS that can justify it, which generally allows to verify soundness quicker although not all the sources of unsoundness may be highlighted on the LTS. Select the approach which better corresponds to your task.
3. When the verification is done, the corresponding LTS is constructed. Green nodes represent final states. Red nodes represent deadlocks. White nodes with red border represent states from which no final state is reachable. Blue nodes represent states with markings strictly greater than the final marking.
