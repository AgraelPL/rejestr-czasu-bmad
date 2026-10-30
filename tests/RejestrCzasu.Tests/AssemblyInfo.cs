// One PostgreSQL container for the whole test assembly; tests reset data, so they run sequentially.
[assembly: AssemblyFixture(typeof(RejestrCzasu.Tests.PostgresFixture))]
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
