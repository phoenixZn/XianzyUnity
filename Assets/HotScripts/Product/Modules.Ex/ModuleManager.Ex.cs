using System;


namespace Xease
{
        
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SkipModuleAutoRegisterAttribute : Attribute
    {
    }
    

}