﻿using System;
using System.Linq;
using Fixie.Conventions;
using Fixie.Discovery;
using Fixie.Results;

namespace Fixie
{
    public class ExecutionModel
    {
        readonly Listener listener;
        readonly ConfigModel config;
        readonly string conventionName;

        readonly AssertionLibraryFilter assertionLibraryFilter;

        readonly Func<Case, bool> skipCase;
        readonly Func<Case, string> getSkipReason;
        readonly Action<Case[]> orderCases;
        
        public ExecutionModel(Listener listener, Convention convention)
        {
            this.listener = listener;
            config = convention.Config;
            conventionName = convention.GetType().FullName;
            
            assertionLibraryFilter = new AssertionLibraryFilter(config.AssertionLibraryTypes);

            skipCase = config.SkipCase;
            getSkipReason = config.GetSkipReason;
            orderCases = config.OrderCases;
        }

        public ConventionResult Run(Type[] candidateTypes)
        {
            var executionPlan = new ExecutionPlan(config);
            var caseDiscoverer = new CaseDiscoverer(config);
            var conventionResult = new ConventionResult(conventionName);

            foreach (var testClass in caseDiscoverer.TestClasses(candidateTypes))
            {
                var classResult = new ClassResult(testClass.FullName);

                var cases = caseDiscoverer.TestCases(testClass);
                var casesBySkipState = cases.ToLookup(skipCase);
                var casesToSkip = casesBySkipState[true];
                var casesToExecute = casesBySkipState[false].ToArray();
                foreach (var @case in casesToSkip)
                    classResult.Add(Skip(@case));

                if (casesToExecute.Any())
                {
                    orderCases(casesToExecute);

                    var caseExecutions = executionPlan.Execute(testClass, casesToExecute);

                    foreach (var caseExecution in caseExecutions)
                        classResult.Add(caseExecution.Exceptions.Any() ? Fail(caseExecution) : Pass(caseExecution));
                }

                conventionResult.Add(classResult);
            }

            return conventionResult;
        }

        CaseResult Skip(Case @case)
        {
            var result = new SkipResult(@case, getSkipReason(@case));
            listener.CaseSkipped(result);
            return CaseResult.Skipped(result.Case.Name, result.Reason);
        }

        CaseResult Pass(CaseExecution caseExecution)
        {
            var result = new PassResult(caseExecution);
            listener.CasePassed(result);
            return CaseResult.Passed(result.Case.Name, result.Duration);
        }

        CaseResult Fail(CaseExecution caseExecution)
        {
            var result = new FailResult(caseExecution, assertionLibraryFilter);
            listener.CaseFailed(result);
            return CaseResult.Failed(result.Case.Name, result.Duration, result.ExceptionSummary);
        }
    }
}