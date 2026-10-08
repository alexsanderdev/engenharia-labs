using BenchmarkDotNet.Running;
using F3M06.Dapper.Benchmarks;

// Rode SEMPRE em Release (precisa do Docker; cada processo de benchmark sobe o próprio container):
//   dotnet run -c Release --project labs/F3-M06-dapper/benchmarks/F3M06.Dapper.Benchmarks -- --filter *
// Para reaproveitar um SQL Server que já está no ar, defina a variável de ambiente
// F3M06_BENCH_CONNECTION com a connection string (o banco F3M06Bench é recriado nele).
BenchmarkSwitcher.FromAssembly(typeof(RelatorioEfVsDapper).Assembly).Run(args);
