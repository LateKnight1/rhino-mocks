#if DOTNET35
using Castle.Core.Interceptor;
#elif NETSTANDARD2_0
using Castle.DynamicProxy;
# endif
using Rhino.Mocks.Impl.InvocationSpecifications;
using Rhino.Mocks.Interfaces;

namespace Rhino.Mocks.Impl.Invocation.Specifications
{
    ///<summary>
    ///</summary>
    public class IsAPropertyInvocation : ISpecification<IInvocation>
    {
        IMockedObject proxyInstance;

        ///<summary>
        ///</summary>
        public IsAPropertyInvocation(IMockedObject proxy_instance)
        {
            proxyInstance = proxy_instance;
        }

        ///<summary>
        ///</summary>
        public bool IsSatisfiedBy(IInvocation item)
        {
            return proxyInstance.IsPropertyMethod(item.GetConcreteMethod());
        }
    }
}