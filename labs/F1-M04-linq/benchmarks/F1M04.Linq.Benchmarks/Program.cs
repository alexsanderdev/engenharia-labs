using BenchmarkDotNet.Running;
using F1M04.Linq.Benchmarks;

// Rode SEMPRE em Release:
//   dotnet run -c Release --project labs/F1-M04-linq/benchmarks/F1M04.Linq.Benchmarks
// Os benchmarks usam só os modelos do lab (não chamam os TODOs), então rodam mesmo antes de você resolver o lab.
BenchmarkSwitcher.FromAssembly(typeof(LinqVsLoop).Assembly).Run(args);
