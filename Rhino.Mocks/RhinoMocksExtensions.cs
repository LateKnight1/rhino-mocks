#if DOTNET35
using System;
using System.Collections.Generic;
using Rhino.Mocks.Constraints;
using Rhino.Mocks.Exceptions;
using Rhino.Mocks.Generated;
using Rhino.Mocks.Interfaces;
using System.Linq.Expressions;

namespace Rhino.Mocks
{
    public static class RhinoMocksExtensions
    {
        [ThreadStatic]
        internal static IList<Expression> argumentPredicates;

        public static IMethodOptions<VoidType> Expect<T>(this T mock, Action<T> action)
        {
            return Expect<T, VoidType>(mock, t =>
            {
                action(t);
                return null;
            });
        }

        public static IMethodOptions<R> Expect<T, R>(this T mock, Func<T, R> action)
        {
            IMockedObject mockedObject = MockRepository.GetMockedObject(mock);
            MockRepository mocks = mockedObject.Repository;
            mocks.BackToRecord(mock, BackToRecordOptions.None);
            action(mock);
            IMethodOptions<R> options = LastCall.GetOptions<R>();
            options.TentativeReturn();
            mocks.Replay(mock);
            return options;
        }

        public static IMethodOptions<object> Stub<T>(this T mock, Action<T> action)
        {
            return Stub<T, object>(mock, t =>
            {
                action(t);
                return null;
            });
        }
        public static IMethodOptions<R> Stub<T, R>(this T mock, Func<T, R> action)
        {
            return Expect(mock, action).Repeat.Times(0, 1);
        }

        public static void AssertWasCalled<T>(this T mock, Action<T> action)
        {
            argumentPredicates = new List<Expression>();
            AssertWasCalled(mock, action, delegate(IMethodOptions<object> options)
            {
                var constraints = new List<AbstractConstraint>();
                foreach (var expression in argumentPredicates)
                {
                    constraints.Add(new LambdaConstraint(expression));
                }
                options.Constraints(constraints.ToArray());
            });
        }

        public static void AssertWasCalled<T>(this T mock, Action<T> action, Action<IMethodOptions<object>> setupConstraints)
        {
            try
            {
                IMockedObject mockedObject = MockRepository.GetMockedObject(mock);
                MockRepository mocks = mockedObject.Repository;
                T mockToRecordExpectation = mocks.DynamicMock<T>(mockedObject.ConstructorArguments);
                action(mockToRecordExpectation);
                AssertExactlySingleExpectaton(mocks, mockToRecordExpectation);
                
                IMethodOptions<object> lastMethodCall = mocks.LastMethodCall<object>(mockToRecordExpectation);
                lastMethodCall.TentativeReturn();
                if (setupConstraints != null)
                {
                    setupConstraints(lastMethodCall);
                }
                ExpectationsList expectationsToVerify = mocks.Replayer.GetAllExpectationsForProxy(mockToRecordExpectation);
                if (expectationsToVerify.Count == 0)
                    throw new InvalidOperationException("The expectation was removed from the waiting expectations list, did you call Repeat.Any() ? This is not supported in AssertWasCalled()");
                IExpectation expected = expectationsToVerify[0];
                ICollection<object[]> argumentsForAllCalls = mockedObject.GetCallArgumentsFor(expected.Method);

                foreach (object[] args in argumentsForAllCalls)
                {
                    if (expected.IsExpected(args))
                        return;
                }


                throw new ExpectationViolationException("Expectd that " + expectationsToVerify[0].ErrorMessage +
                                                        " would be called, but is was it was not found on the actual calls made on the mocked object");

            }
            finally
            {
                argumentPredicates = null;
            }
        }

        private static void AssertExactlySingleExpectaton<T>(MockRepository mocks, T mockToRecordExpectation)
        {
            if (mocks.Replayer.GetAllExpectationsForProxy(mockToRecordExpectation).Count == 0)
                throw new InvalidOperationException("No expectations were setup to be verified, ensure that the method call in the action is a virtual (C#) / overridable (VB.Net) method call");

            if (mocks.Replayer.GetAllExpectationsForProxy(mockToRecordExpectation).Count > 1)
                throw new InvalidOperationException("You can only use a single expectation on AssertWasCalled(), use separate calls to AssertWasCalled() if you want to verify several expectations");
        }

        public class VoidType
        {
            private VoidType()
            {

            }
        }
    }

    public static class Arg<T>
    {
        public static T Matches(Expression<Predicate<T>> predicate)
        {
            if (RhinoMocksExtensions.argumentPredicates == null)
            {
                throw new InvalidOperationException("Cannot pass explicit delegate to setup the expectation and also use Arg<T>.Matches");
            }
            RhinoMocksExtensions.argumentPredicates.Add(predicate);

            return default(T);
        }
    }
}
#endif