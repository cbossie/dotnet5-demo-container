using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContainerConsoleApp.Code
{
    public class SimulationWorker : IHostedService
    {
        IAmazonS3 S3Cli { get; init; }
        SimulationConfig SimConfig { get; init; }
        IHostApplicationLifetime Lifetime { get; init; }
        Random Rnd = new Random();
        ILogger Logger { get; init; }

        public SimulationWorker(IAmazonS3 s3Cli, SimulationConfig simConfig, IHostApplicationLifetime lifetime, ILogger<SimulationWorker> logger)
        {
            S3Cli = s3Cli;
            SimConfig = simConfig;
            Lifetime = lifetime;
            Logger = logger;
        }

        private void Complete()
        {
            Lifetime.StopApplication();
        }

        private (int, TimeSpan) RunSimulation(int repetitions)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            int tries = 0;
            List<int> uniqueValues = new();
            bool found = default;
            int numresults = 0;
            

            while (numresults < repetitions)
            {
                found = false;
                tries++;
                int nextResult = Rnd.Next(repetitions);

                foreach(var n in uniqueValues)
                {
                    if(nextResult == n)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    uniqueValues.Add(nextResult);
                    numresults++;
                    Logger.LogInformation($"Adding unique value {nextResult}");
                }
                else
                {
                    Logger.LogInformation($"Value {nextResult} is already in the list.");
                }

                Logger.LogInformation($"Try # {tries}. Array contains {uniqueValues.Count} / {repetitions} values");

                if (tries == int.MaxValue)
                {
                    Logger.LogInformation($"Could not find {repetitions} unique values before int32 overflowed");
                    break;
                }

            }
            watch.Stop();
    
            return (tries, watch.Elapsed);
        }

        private static string UniqueKey => DateTime.Now.ToString("s");

        private async Task WriteStringToBucket(string message)
        {
            try
            {
                await S3Cli.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = SimConfig.Bucket,
                    ContentBody = message,
                    Key = UniqueKey


                }).ConfigureAwait(false);

            }
            catch(InvalidOperationException ex)
            {
                Logger.LogError(ex, ex.Message);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var (tries, span) = RunSimulation(SimConfig.Repetitions);
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Generated {SimConfig.Repetitions} unique values in {tries} tries");
                sb.AppendLine($"It took {span.TotalMinutes} minutes");
                await WriteStringToBucket(sb.ToString());
                Logger.LogInformation(sb.ToString());
                Complete();

            }
            finally
            {
                Complete();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Completed");
        }
    }
}
