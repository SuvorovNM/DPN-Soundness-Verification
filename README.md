# DPN Verifier

A toolkit implemented for **data Petri nets** that allows:
- Analyzing DPNs at different granularity levels with different state-space structures.
- Verifying two forms of soundness: classical and relaxed-lazy.
- Repairing unsound models: both with defects on control and data flow levels.
- Generating random DPNs with the predefined parameters.
- Importing/exporting both DPNs and state space structures.

As input, the toolkit takes DPNs with conditions composed of variable-operator-constant/variable-operator-variable atoms.
Allowed variable types: Boolean, Real, Integer. For Real- and Boolean-typed variables, termination of each implemented procedure is guaranteed.

## Model Analysis

The tool allows to construct:
- Abstract Reachability Graph (ARG)
- Abstract Coverability Graph (ACG)
- Abstract Coverability Tree (ACT)

Each of them is a generalization of a classical state-space structure, where each node represents a set of nodes with different variable states but similar markings. 
Sets of variable states are described using formulas.

Before constructing the state space structures, the tool allows to conduct DPN transformations to make the analysis more subtle. Three DPN types can be taken as input:
- **Source DPN**: reveals problems with boundedness, dead transitions, the absence of paths leading to the final state, presence of deadlocks/livelocks at the backbone level, etc.
- **Tau DPN**: additionally reveals problems with deadlocks induced by adding the data perspective to a Petri net.
- **Tau Refined DPN**: additionally reveals problems with livelocks induced by adding the data perspective to a Petri net.

## Soundness Verification and Repair

**Classical soundness** is a behavioral property that ensures model boundedness, the absence of deadlocks, livelocks and dead transitions.

The implemented soundness verification procedure consists of four main steps: 
1. Checking boundedness of a DPN.
2. Constructing Tau Refined DPN: splitting transitions occurring in cycles and adding $\tau$-transitions, whose guards are negations of guards of the existing DPN transitions.
3. Constructing an ARG for the resulting DPN. 
4. Analyzing the ARG for the soundness properties.

Two algorithms for classical soundness verification are implemented. The first one follows the described above schema. 
The second one postpones the DPN refinement and performs the preliminary checks on the ARG of the source DPN and the ARG of the tau DPN.
The latter is quicker if a DPN has errors connected with the control flow structure or the deadlocks induced by adding the data flow.

The soundness repair procedure consists of the following steps:
1. Refining a DPN.
2. Constructing an ACG.
3. Restricting transition constraints to forbid paths leading to 'red' nodes (deadlocks/livelocks/unboundedness).
4. Constructing an ACG. If all nodes are green and previous tree had red nodes, go to step 1. If all nodes are green and previous tree had only green nodes, finish (SUCCESSFUL repair). If all nodes are red, finish (FAILED repair). If some nodes are red, go to step 3.

For resource-oriented models, it is more convenient to use **relaxed-lazy soundness** that ensures the presence of some paths leading to the output place with potentially other tokens in the model, the absence of transitions that are not included in any of these paths, and the absence of markings with more than one token in the output place.

The implemented relaxed-lazy soundness verification procedure consists of two steps:
1. Constructing an ACG.
2. Analyzing the ACG for the relaxed-lazy soundness properties.

## Interoperability

DPN can be imported/exported in the PNMLX format. The XSD schema for this format is located at `DPN.Parsers/XsdSchemas/pnml.xsd`.

Abstract state space structures (ARG/ACG/ACT) can be imported/exported in the ASML format. The XSD schema for this format is located at `DPN.Parsers/XsdSchemas/asml.xsd`.

Sample DPNs and abstract state space structures can be found at `Samples/`.

## Requirements

- Windows 10/11
- .NET SDK 8

## Run as Console App

Download the `DPNVerifier.Console.zip` of the last release version.
The needed executable is `DPNVerifier.Console.exe`.

Running it without parameters will show the information regarding the admissible arguments and provide samples of executions. 
The main arguments are the following:
- `Operation`: type of the operation to be executed (`Verification`/`Repair`).
- `DpnFile`: the location of the DPN file in the PNMLX format.
- `OutputDirectory`: the location to save the repaired DPN or the abstract state space structure.
- `SoundnessType`: type of the verified soundness property (`Classical`/`RelaxedLazy`)
- `Verbose`: put additional information regarding verification/repair in the console log. As an example, verbosity allows to describe all the failure points found in the DPN.

Sample arguments:

  `--Operation Verify --DpnFile model.pnmlx --OutputDirectory ./results --SoundnessType RelaxedLazy --Verbose`

  `--Operation Repair --DpnFile model.pnmlx --OutputDirectory ./results --SoundnessType Classical`



## Run as Desktop App

Download the `DPNVerifier.Desktop.zip` of the last release version.
The needed executable is `DPNVerifier.Desktop.exe`.

### File Tab

Allows to import a DPN in the PNMLX format (`Open DPN...`), export a DPN in the PNMLX format (`Save DPN...`) and import an abstract state space structure for visualization (`Open State Space Structure...`).

### Transition Systems Tab

Allows to construct the abstract state space structures for the currently displayed DPN. The structures include an ARG (`Construct Reachability Graph`), an ACG (`Construct Coverability Graph`), and an ACT (`Construct Coverability Tree`). The structures can be constructed for a source DPN, for a tau DPN, and for a tau-refined DPN. Tau and tau-refined DPN can be constructed using DPN transformations available at the Model tab.

Constructed state space structures are visualized and analyzed for some kind of soundness (ARG for the classical, ACG/ACT for the relaxed-lazy). Sources of unsoundness are highlighted by the node coloring function.
For instance, final nodes are colored in green, nodes with no path to finals are not colored but have a red border, dead nodes are colored in red, nodes with unclean finals are colored in blue.

### Soundness Tab

Allows verification of relaxed lazy soundness (`Verify` $\rightarrow$ `RelaxedLazy Soundness`) and classical soundness (`Verify`$\rightarrow$`Classical Soundness`). 

If a model is classically unsound, it can be automatically repaired (`Repair` $\rightarrow$ `Classical Soundness`).

### Model Tab

Allows to generate a random DPN according to the predefined parameters (`Generate new model...`) and transform a currently displayed DPN to the tau DPN and tau-refined DPN.

### Usage [Repair]

1. Import a DPN choosing File -> Open DPN...
2. Choose Model -> Transform Model -> Transform Model to Repaired.
3. When the repair is done and result is SUCCESS, the resultant DPN is returned. If result is FAILURE, the message box that notifies about the failure appears.

### Sample Applications

A DPN with a livelock that occurs when $t_1$ sets $a \ge 3$ and $t_3$ fires at least once:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/livelockDPN.png?raw=true)</kbd>

Result of its soundness verification:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/livelockDPN-Verification.png?raw=true)</kbd>

Result of its soundness repair:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/livelockDPN-RepairResult.png?raw=true)</kbd>

-----

A DPN with a deadlock if $Register$ fires when $age <= 18$:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/CasinoDPN.png?raw=true)</kbd>

Result of its soundness verification:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/CasinoDPN-Verification.png?raw=true)</kbd>

Result of its soundness repair:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/CasinoDPN-RepairResult.png?raw=true)</kbd>

-----

A sound DPN from an event log:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/SepsisDPN.png?raw=true)</kbd>

Result of its soundness verification:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/SepsisDPN-Verification.png?raw=true)</kbd>

Since the graph is too large for visualization, the tool proposes a user to export it. Information regarding soundness is still presented.

-----

A relaxed-lazy sound DPN representing gambling:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/GamblingDPN.png?raw=true)</kbd>

Result of its relaxed-lazy soundness verification:
<kbd>![alt text](https://github.com/SuvorovNM/DPN-Soundness-Verification/blob/master/img/GamblingDPN-Verification.png?raw=true)</kbd>

The model has a deadlock, but relaxed lazy soundness allows models to have deadlocks. The tool still highlights them, if they are detected, to illustrate potential errors.

