﻿namespace DataPetriNet.Abstractions
{
    public abstract class Node : ILabeledElement
    {
        public string Label { get; set; }
    }
}
