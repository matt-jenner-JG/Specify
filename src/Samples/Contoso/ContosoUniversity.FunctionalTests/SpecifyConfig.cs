﻿using Specify;
using Specify.Autofac;
using Specify.Configuration;

namespace ContosoUniversity.FunctionalTests
{
    public class SpecifyConfig : SpecifyConfiguration
    {
        public override IDependencyResolver GetDependencyResolver()
        {
            return new AutofacDependencyResolver();
        }
    }
}
