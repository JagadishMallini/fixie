namespace Fixie.Tests.Runner
{
    using System.Collections.Generic;
    using System.Linq;
    using Fixie.Runner;
    using Fixie.Runner.Contracts;
    using Newtonsoft.Json;
    using Should;

    public class DesignTimeDiscoveryListenerTests : MessagingTests
    {
        public void ShouldReportDiscoveredMethodGroupsToDiscoverySink()
        {
            string assemblyPath = typeof(MessagingTests).Assembly.Location;

            var sink = new StubDesignTimeSink();
            Discover(new DesignTimeDiscoveryListener(sink, assemblyPath));

            sink.LogEntries.ShouldBeEmpty();

            var tests = sink.Messages
                .Select(jsonMessage => Payload<Test>(jsonMessage, "TestDiscovery.TestFound"))
                .OrderBy(x => x.FullyQualifiedName)
                .ToArray();

            foreach (var test in tests)
            {
                test.Id.ShouldEqual(null);
                test.DisplayName.ShouldEqual(test.FullyQualifiedName);
                test.Properties.ShouldBeEmpty();

                test.CodeFilePath.EndsWith("MessagingTests.cs").ShouldBeTrue();
                test.LineNumber.ShouldBeGreaterThan(0);
            }

            tests.Select(x => x.FullyQualifiedName)
                .ShouldEqual(
                    TestClass + ".Fail",
                    TestClass + ".FailByAssertion",
                    TestClass + ".Pass",
                    TestClass + ".SkipWithoutReason",
                    TestClass + ".SkipWithReason");
        }

        public void ShouldDefaultSourceLocationPropertiesWhenSourceInspectionThrows()
        {
            const string invalidAssemblyPath = "assembly.path.dll";

            var sink = new StubDesignTimeSink();
            Discover(new DesignTimeDiscoveryListener(sink, invalidAssemblyPath));

            sink.LogEntries.Count.ShouldEqual(5);

            var tests = sink.Messages
                .Select(jsonMessage => Payload<Test>(jsonMessage, "TestDiscovery.TestFound"))
                .OrderBy(x => x.FullyQualifiedName)
                .ToArray();

            foreach (var test in tests)
            {
                test.Id.ShouldEqual(null);
                test.DisplayName.ShouldEqual(test.FullyQualifiedName);
                test.Properties.ShouldBeEmpty();

                test.CodeFilePath.ShouldBeNull();
                test.LineNumber.ShouldBeNull();
            }

            tests.Select(x => x.FullyQualifiedName)
                .ShouldEqual(
                    TestClass + ".Fail",
                    TestClass + ".FailByAssertion",
                    TestClass + ".Pass",
                    TestClass + ".SkipWithoutReason",
                    TestClass + ".SkipWithReason");
        }

        static TExpectedPayload Payload<TExpectedPayload>(string jsonMessage, string expectedMessageType)
        {
            var message = JsonConvert.DeserializeObject<Message>(jsonMessage);

            message.MessageType.ShouldEqual(expectedMessageType);

            return message.Payload.ToObject<TExpectedPayload>();
        }

        class StubDesignTimeSink : IDesignTimeSink
        {
            public List<string> Messages { get; } = new List<string>();
            public List<string> LogEntries { get; } = new List<string>();

            public void Send(string message) => Messages.Add(message);
            public void Log(string message) => LogEntries.Add(message);
        }
    }
}