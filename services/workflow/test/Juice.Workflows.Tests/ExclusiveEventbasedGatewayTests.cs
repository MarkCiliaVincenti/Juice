﻿using Newtonsoft.Json;

namespace Juice.Workflows.Tests
{
    public class ExclusiveEventbasedGatewayTests
    {
        private ITestOutputHelper _output;

        public ExclusiveEventbasedGatewayTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /*
         * Should catch event e2 (branch 2)
         * 
         *   ---------------                                    ---------------
( )----0---->|    utask_0   |---1----><~>----2---->(O)--------->|    utask_1   |-------->())
             ---------------           |                        ---------------
                                       |                        ---------------
                                       '-----3---->(O)----4---->|    utask_2   |---5---->())
                                                                ---------------
         * 
         */

        [Fact(DisplayName = "Should select single branch")]

        public async Task Eventbased_gateway_should_select_single_path_Async()
        {
            var resolver = new DependencyResolver
            {
                CurrentDirectory = AppContext.BaseDirectory
            };

            var workflowId = new DefaultStringIdGenerator().GenerateRandomId(6);

            resolver.ConfigureServices(services =>
            {
                var configService = services.BuildServiceProvider().GetRequiredService<IConfigurationService>();
                var configuration = configService.GetConfiguration();

                services.AddLocalization(options => options.ResourcesPath = "Resources");

                services.AddDefaultStringIdGenerator();

                services.AddSingleton(provider => _output);

                services.AddLogging(builder =>
                {
                    builder.ClearProviders()
                    .AddTestOutputLogger()
                    .AddConfiguration(configuration.GetSection("Logging"));
                });

                services.AddMediatR(typeof(StartEvent));

                services.AddWorkflowServices()
                    .AddInMemoryReposistories();
                services.RegisterNodes(typeof(TestCatchEvent));

                services.RegisterWorkflow(workflowId, builder =>
                {
                    builder
                       .Start()
                       .Then<UserTask>("utask_0")
                       .ExclusiveEventbased()
                           .Fork().Wait<TestCatchEvent>("e1").Then<UserTask>("utask_1").End()
                           .Fork().Wait<TestCatchEvent>("e2").Then<UserTask>("utask_2").End()
                       ;

                });
            });

            using var scope = resolver.ServiceProvider.CreateScope();
            var workflow = scope.ServiceProvider.GetRequiredService<IWorkflow>();

            var branch = "e2";

            var executor = new WorkflowTestHelper(_output);

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                try
                {
                    var listeningEvents = workflow.ExecutedContext
                        .BlockingNodes.Where(b =>
                            workflow.ExecutedContext.GetNode(b.Id).Node is IEvent);

                    _output.WriteLine("Waiting for events: " + JsonConvert.SerializeObject(listeningEvents));

                    var @event = listeningEvents.Where(n => n.Name == branch).First();

                    _output.WriteLine("********* Event " + @event.Name + " " + @event.Id);
                    executor.Catched(@event.Id);
                }
                catch (Exception ex)
                {
                    _output.WriteLine("ERROR raise event" + ex.Message);
                }

            });

            var result = await executor.StartAsync(workflow, workflowId, default);

            workflow.ExecutedContext.Should().NotBeNull();

            _output.WriteLine(ContextPrintHelper.Visualize(workflow.ExecutedContext));

            workflow.ExecutedContext.Should().NotBeNull();

            result?.Status.Should().Be(WorkflowStatus.Finished);
            var state = workflow.ExecutedContext?.State;
            state?.Should().NotBeNull();

            var expectedNodes = workflow.ExecutedContext.Nodes.Values
                .Where(n =>
                n.Record.Name.StartsWith(branch == "e1" ? "utask_1" : "utask_2")
                || n.Record.Name == "utask_0"
                || n.Node.GetType().IsAssignableTo(typeof(StartEvent))
                || (n.Node.GetType().IsAssignableTo(typeof(TestCatchEvent))
                    && n.Record.Name == branch
                )
                || n.Node.GetType().IsAssignableTo(typeof(EventBasedGateway)))
                .Select(n => n.Record.Id);

            expectedNodes.Count().Should().BeGreaterThan(4);

            expectedNodes.Should().BeSubsetOf(state?.ExecutedNodes?.Select(n => n.Id));

            var cancelledEvent = branch == "e1" ? "e2" : "e1";
            var @event = workflow.ExecutedContext.NodeSnapshots
                .Where(n => n.Name == cancelledEvent)
                .First();

            @event.Status.Should().Be(WorkflowStatus.Idle);
            @event.Message.Should().Be("Cancelled because other event was cactched");
        }

    }

}