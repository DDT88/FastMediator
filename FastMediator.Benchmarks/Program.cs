using System.Reflection;
using BenchmarkDotNet.Running;

// Esegui i benchmark selezionabili da riga di comando.
// Esempi:
//   dotnet run -c Release -- --filter *MediatRComparison*
//   dotnet run -c Release -- --filter *
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
