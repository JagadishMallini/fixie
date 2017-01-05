﻿namespace Fixie.Tests.Execution
{
    using System;
    using System.Collections.Generic;
    using Fixie.Execution;
    using Fixie.Internal;
    using Should;
    using static Utility;

    public class CaseCompletedTests
    {
        public void ShouldDescribeCaseCompletedMessages()
        {
            var listener = new StubCaseCompletedListener();
            var convention = SampleTestClassConvention.Build();

            using (new RedirectedConsole())
                Run<SampleTestClass>(listener, convention);

            listener.Log.Count.ShouldEqual(5);

            var skipWithReason = (CaseSkipped)listener.Log[0];
            var skipWithoutReason = (CaseSkipped)listener.Log[1];
            var fail = (CaseFailed)listener.Log[2];
            var failByAssertion = (CaseFailed)listener.Log[3];
            var pass = listener.Log[4];

            pass.Name.ShouldEqual(FullName<SampleTestClass>() + ".Pass");
            pass.Class.FullName.ShouldEqual(FullName<SampleTestClass>() + "");
            pass.Method.Name.ShouldEqual("Pass");
            pass.Output.Lines().ShouldEqual("Console.Out: Pass", "Console.Error: Pass");
            pass.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            pass.Status.ShouldEqual(CaseStatus.Passed);

            fail.Name.ShouldEqual(FullName<SampleTestClass>() + ".Fail");
            fail.Class.FullName.ShouldEqual(FullName<SampleTestClass>() + "");
            fail.Method.Name.ShouldEqual("Fail");
            fail.Output.Lines().ShouldEqual("Console.Out: Fail", "Console.Error: Fail");
            fail.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            fail.Status.ShouldEqual(CaseStatus.Failed);
            fail.Exception.Type.ShouldEqual("Fixie.Tests.FailureException");
            fail.Exception.StackTrace
                .CleanStackTraceLineNumbers()
                .Lines()
                .ShouldEqual("'Fail' failed!", At<SampleTestClass>("Fail()"));
            fail.Exception.Message.ShouldEqual("'Fail' failed!");

            failByAssertion.Name.ShouldEqual(FullName<SampleTestClass>() + ".FailByAssertion");
            failByAssertion.Class.FullName.ShouldEqual(FullName<SampleTestClass>() + "");
            failByAssertion.Method.Name.ShouldEqual("FailByAssertion");
            failByAssertion.Output.Lines().ShouldEqual("Console.Out: FailByAssertion", "Console.Error: FailByAssertion");
            failByAssertion.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            failByAssertion.Status.ShouldEqual(CaseStatus.Failed);
            failByAssertion.Exception.Type.ShouldEqual("Should.Core.Exceptions.EqualException");
            failByAssertion.Exception.StackTrace
                .CleanStackTraceLineNumbers()
                .Lines()
                .ShouldEqual(
                    "Assert.Equal() Failure",
                    "Expected: 2",
                    "Actual:   1",
                    At<SampleTestClass>("FailByAssertion()"));
            failByAssertion.Exception.Message.Lines().ShouldEqual(
                "Assert.Equal() Failure",
                "Expected: 2",
                "Actual:   1");

            skipWithReason.Name.ShouldEqual(FullName<SampleTestClass>() + ".SkipWithReason");
            skipWithReason.Class.FullName.ShouldEqual(FullName<SampleTestClass>() + "");
            skipWithReason.Method.Name.ShouldEqual("SkipWithReason");
            skipWithReason.Output.ShouldBeNull();
            skipWithReason.Duration.ShouldEqual(TimeSpan.Zero);
            skipWithReason.Status.ShouldEqual(CaseStatus.Skipped);
            skipWithReason.Reason.ShouldEqual("Skipped with reason.");

            skipWithoutReason.Name.ShouldEqual(FullName<SampleTestClass>() + ".SkipWithoutReason");
            skipWithoutReason.Class.FullName.ShouldEqual(FullName<SampleTestClass>() + "");
            skipWithoutReason.Method.Name.ShouldEqual("SkipWithoutReason");
            skipWithoutReason.Output.ShouldBeNull();
            skipWithoutReason.Duration.ShouldEqual(TimeSpan.Zero);
            skipWithoutReason.Status.ShouldEqual(CaseStatus.Skipped);
            skipWithoutReason.Reason.ShouldBeNull();
        }

        public class StubCaseCompletedListener :
            Handler<CaseSkipped>,
            Handler<CasePassed>,
            Handler<CaseFailed>
        {
            public List<CaseCompleted> Log { get; set; } = new List<CaseCompleted>();

            public void Handle(CaseSkipped message) => Log.Add(message);
            public void Handle(CasePassed message) => Log.Add(message);
            public void Handle(CaseFailed message) => Log.Add(message);
        }
    }
}
