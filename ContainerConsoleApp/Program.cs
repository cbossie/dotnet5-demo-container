using Amazon.S3;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Amazon.Extensions;
using ContainerConsoleApp.Code;
using Microsoft.Extensions.Logging;

namespace ContainerConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //IConfiguration configuration = CreateConfiguration(args);
            using IHost host = CreateHostBuilder(args/*, configuration*/).Build(); ;


            Console.WriteLine("Starting!");

            await host.RunAsync();

            Console.WriteLine("Done");

        }


        static IHostBuilder CreateHostBuilder(string[] args/*, IConfiguration configuration*/)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();

                })
                .ConfigureServices((context, services) =>
                {
                    var config = context.Configuration;
                    var options = config.GetAWSOptions();
                    services.AddAWSService<IAmazonS3>(options);


                    var bucket = config["BUCKET"];
                    int repetitions = int.Parse(config["REPETITIONS"]);
                    SimulationConfig simConf = new SimulationConfig(bucket, repetitions);
                    services.AddSingleton(simConf);

                    services.AddLogging(a => a.AddConsole());

                    services.AddHostedService<SimulationWorker>();


                });
            return host;
        }


    }
}
