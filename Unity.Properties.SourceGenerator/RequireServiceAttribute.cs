using System;

namespace Unity.Properties.SourceGenerator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RequireServiceAttribute : Attribute
    {
        public RequireServiceAttribute(Type serviceType )
        {
        }
    }

    public interface ITestService
    {
        
    }
    public interface ITestService2
    {
        
    }
}