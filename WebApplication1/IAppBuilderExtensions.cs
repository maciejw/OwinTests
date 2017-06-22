
using Owin;
using System;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1
{

    using AppFunc = Func<IDictionary<string, object>, Task>;
    public static class IAppBuilderExtensions
    {
        public static IAppBuilder Use(this IAppBuilder app, Func<AppFunc, AppFunc> appFuncMiddleware)
        {
            return app.Use(middleware: appFuncMiddleware);
        }
    }
}
