﻿namespace Fixie.Tests.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Fixie.Execution;
    using Fixie.Internal;
    using Should;

    public class LifecycleMessageTests : MessagingTests
    {
        public void ShouldDescribeCaseCompletion()
        {
            var listener = new StubCaseCompletedListener();

            using (new RedirectedConsole())
                Run(listener);

            var assembly = typeof(LifecycleMessageTests).Assembly;

            var assemblyStarted = listener.AssemblyStarts.Single();
            assemblyStarted.Name.ShouldEqual("Fixie.Tests");
            assemblyStarted.Location.ShouldEqual(assembly.Location);

            listener.Cases.Count.ShouldEqual(5);

            var skipWithReason = (CaseSkipped)listener.Cases[0];
            var skipWithoutReason = (CaseSkipped)listener.Cases[1];
            var fail = (CaseFailed)listener.Cases[2];
            var failByAssertion = (CaseFailed)listener.Cases[3];
            var pass = listener.Cases[4];

            pass.Name.ShouldEqual(TestClass + ".Pass");
            pass.MethodGroup.FullName.ShouldEqual(TestClass + ".Pass");
            pass.Output.Lines().ShouldEqual("Console.Out: Pass", "Console.Error: Pass");
            pass.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            pass.Status.ShouldEqual(CaseStatus.Passed);

            fail.Name.ShouldEqual(TestClass + ".Fail");
            fail.MethodGroup.FullName.ShouldEqual(TestClass + ".Fail");
            fail.Output.Lines().ShouldEqual("Console.Out: Fail", "Console.Error: Fail");
            fail.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            fail.Status.ShouldEqual(CaseStatus.Failed);
            fail.Exception.FailedAssertion.ShouldBeFalse();
            fail.Exception.Type.ShouldEqual("Fixie.Tests.FailureException");
            fail.Exception.StackTrace
                .CleanStackTraceLineNumbers()
                .ShouldEqual(At("Fail()"));
            fail.Exception.Message.ShouldEqual("'Fail' failed!");

            failByAssertion.Name.ShouldEqual(TestClass + ".FailByAssertion");
            failByAssertion.MethodGroup.FullName.ShouldEqual(TestClass + ".FailByAssertion");
            failByAssertion.Output.Lines().ShouldEqual("Console.Out: FailByAssertion", "Console.Error: FailByAssertion");
            failByAssertion.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            failByAssertion.Status.ShouldEqual(CaseStatus.Failed);
            failByAssertion.Exception.FailedAssertion.ShouldBeTrue();
            failByAssertion.Exception.Type.ShouldEqual("Should.Core.Exceptions.EqualException");
            failByAssertion.Exception.StackTrace
                .CleanStackTraceLineNumbers()
                .ShouldEqual(At("FailByAssertion()"));
            failByAssertion.Exception.Message.Lines().ShouldEqual(
                "Assert.Equal() Failure",
                "Expected: 2",
                "Actual:   1");

            skipWithReason.Name.ShouldEqual(TestClass + ".SkipWithReason");
            skipWithReason.MethodGroup.FullName.ShouldEqual(TestClass + ".SkipWithReason");
            skipWithReason.Output.ShouldBeNull();
            skipWithReason.Duration.ShouldEqual(TimeSpan.Zero);
            skipWithReason.Status.ShouldEqual(CaseStatus.Skipped);
            skipWithReason.Reason.ShouldEqual("Skipped with reason.");

            skipWithoutReason.Name.ShouldEqual(TestClass + ".SkipWithoutReason");
            skipWithoutReason.MethodGroup.FullName.ShouldEqual(TestClass + ".SkipWithoutReason");
            skipWithoutReason.Output.ShouldBeNull();
            skipWithoutReason.Duration.ShouldEqual(TimeSpan.Zero);
            skipWithoutReason.Status.ShouldEqual(CaseStatus.Skipped);
            skipWithoutReason.Reason.ShouldBeNull();

            var assemblyCompleted = listener.AssemblyCompletions.Single();
            assemblyCompleted.Name.ShouldEqual("Fixie.Tests");
            assemblyCompleted.Location.ShouldEqual(assembly.Location);
        }

        public class StubCaseCompletedListener :
            Handler<AssemblyStarted>,
            Handler<CaseCompleted>,
            Handler<AssemblyCompleted>
        {
            public List<AssemblyStarted> AssemblyStarts { get; set; } = new List<AssemblyStarted>();
            public List<CaseCompleted> Cases { get; set; } = new List<CaseCompleted>();
            public List<AssemblyCompleted> AssemblyCompletions { get; set; } = new List<AssemblyCompleted>();

            public void Handle(AssemblyStarted message) => AssemblyStarts.Add(message);
            public void Handle(CaseCompleted message) => Cases.Add(message);
            public void Handle(AssemblyCompleted message) => AssemblyCompletions.Add(message);
        }
    }
}