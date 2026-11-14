using System;

namespace ProceduralGen.Wfc.Core
{
    // For future implementations
    public interface IWfcConstraint
    {
        bool Apply(WfcSolver solver, Random rng);
    }
}
