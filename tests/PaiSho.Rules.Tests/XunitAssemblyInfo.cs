using Xunit;

// MatchRules.HouseExtrasEnabled is process-static; scopes must not overlap.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
