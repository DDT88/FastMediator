using BenchmarkDotNet.Running;
using FastMediator.Benchmarks;

// Esegui i benchmark
var summary = BenchmarkRunner.Run<MediatorBenchmarks>();
Console.WriteLine("Benchmark completato!");

// Esegui i benchmark avanzati
var advancedSummary = BenchmarkRunner.Run<AdvancedMediatorBenchmarks>();
Console.WriteLine("Benchmark avanzati completati!");