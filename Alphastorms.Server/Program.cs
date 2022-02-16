using Alphastorms.Server;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {        
        services.AddHostedService<BasicGameServer>();
    })
    .Build();

await host.RunAsync();
