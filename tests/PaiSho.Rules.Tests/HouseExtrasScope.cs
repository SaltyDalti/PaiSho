using System;
using PaiSho.Domain;

namespace PaiSho.Rules.Tests
{
    /// <summary>Temporarily enable/disable house extras for a test, then restore.</summary>
    internal sealed class HouseExtrasScope : IDisposable
    {
        private readonly bool previous;

        public HouseExtrasScope(bool enabled = true)
        {
            previous = MatchRules.HouseExtrasEnabled;
            MatchRules.HouseExtrasEnabled = enabled;
        }

        public void Dispose() => MatchRules.HouseExtrasEnabled = previous;
    }
}
