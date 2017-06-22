using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using System.Collections.Generic;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.FileSystems;
using System.IO;
using Microsoft.Owin.Extensions;
using Autofac;
using Nustache.Core;
using Nustache.Compilation;
using Microsoft.Owin.Diagnostics;
using System.Globalization;

namespace WebApplication1
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    public class Middleware1 : OwinMiddleware
    {
        public Middleware1(OwinMiddleware next) : base(next)
        {

        }

        public async override Task Invoke(IOwinContext context)
        {
            await context.Response.WriteAsync("<!-- Middleware1 class before -->\n");
            await Next.Invoke(context);
            await context.Response.WriteAsync("<!-- Middleware1 class after -->\n");
        }
    }
    public class Middleware2 : OwinMiddleware
    {
        public Middleware2(OwinMiddleware next) : base(next)
        {

        }

        public async override Task Invoke(IOwinContext context)
        {
            await context.Response.WriteAsync("<!-- Middleware2 class before -->\n");
            await Next.Invoke(context);
            await context.Response.WriteAsync("<!-- Middleware2 class after -->\n");
        }
    }

    public class ErrorMiddleware
    {
        private AppFunc next;
        private Func<object, string> compiled;
        public ErrorMiddleware(AppFunc next)
        {
            this.next = next;

            var template = new Template();
            template.Load(File.OpenText(@"C:\Users\maciej\documents\visual studio 2017\Projects\OwinTests\WebApplication1\generic-error.html"));
            compiled = template.Compile(new { code = 0, resource = "" }.GetType(), null);
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var context = new OwinContext(environment);
            var @params = context.Request.QueryString.Value.Split(';');
            int code = int.Parse(@params[0]);
            string resource = @params[1];

            context.Response.StatusCode = code;

            await context.Response.WriteAsync(compiled(new { code, resource }));

        }
    }
    public class ExtendedCookieOptions : CookieOptions
    {
        public TimeSpan? MaxAge { get; set; }

    }
    public static class IOwinResponseExtensions
    {
        public static Task SendHtmlFileAsync(this IOwinResponse @this, string fileName)
        {
            @this.ContentType = "text/html";
            return @this.SendFileAsync(fileName);
        }

        /// <summary>
        /// Add a new cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public static void AppendCookie(this IHeaderDictionary Headers, string key, string value, ExtendedCookieOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);
            bool expiresHasValue = options.Expires.HasValue;
            bool maxAgeHasValue = options.MaxAge.HasValue;

            string setCookieValue = string.Concat(
                Uri.EscapeDataString(key),
                "=",
                Uri.EscapeDataString(value ?? string.Empty),
                !domainHasValue ? null : "; domain=",
                !domainHasValue ? null : options.Domain,
                !pathHasValue ? null : "; path=",
                !pathHasValue ? null : options.Path,
                !expiresHasValue ? null : "; expires=",
                !expiresHasValue ? null : options.Expires.Value.ToString("ddd, dd-MMM-yyyy HH:mm:ss ", CultureInfo.InvariantCulture) + "GMT",
                !maxAgeHasValue ? null : "; max-age=",
                !maxAgeHasValue ? null : options.MaxAge.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                !options.Secure ? null : "; secure",
                !options.HttpOnly ? null : "; HttpOnly");
            Headers.AppendValues("Set-Cookie", setCookieValue);
        }
    }


    public class Startup
    {
        private const string LoggedOnCookie = "logged";
        private const string RequestedWithHeader = "X-Requested-With";
        public void Configuration(IAppBuilder app)
        {

            app.UseErrorPage(ErrorPageOptions.ShowAll);
            app.UseWelcomePage("/");
            app.UseSendFileFallback();

            var builder = new ContainerBuilder();

            builder.RegisterType<Middleware2>();
            builder.RegisterType<Middleware1>();

            app.Map("/login", configureMap =>
            {
                configureMap.Run(context =>
                {
                    if (context.Request.Method == "POST")
                    {
                        ExtendAuthentication(context);
                        context.Response.StatusCode = 204;
                        context.Response.Headers.Append("X-Location", "/index");
                        return Task.CompletedTask;
                    }
                    if (context.Request.Method == "GET")
                    {
                        context.Response.Headers.Append("Cache-Control", "no-store");
                        context.Response.Cookies.Delete(LoggedOnCookie, new CookieOptions() { HttpOnly = true, Path = "/" });

                        return context.Response.SendHtmlFileAsync("web\\login.html");
                    }

                    return Task.CompletedTask;

                });
            });

            app.Map("/index", configureMap =>
            {
                configureMap.Use(Authorize).UseStageMarker(PipelineStage.Authorize);
                configureMap.Use(SlideAuthentication);
                configureMap.Run(context =>
                {
                    if (context.Request.Method == "GET")
                    {
                        context.Response.Headers.Append("Cache-Control", "no-store");
                        return context.Response.SendHtmlFileAsync("web\\index.html");
                    }
                    return Task.CompletedTask;
                });
            });

            app.Map("/explicit-extend-cookie", configureMap =>
            {
                configureMap.Use(Authorize).UseStageMarker(PipelineStage.Authorize);
                configureMap.Use(SlideAuthentication);
                configureMap.Run(context =>
                {
                    context.Response.StatusCode = 204;
                    context.Response.ContentType = "text/plain";
                    return Task.CompletedTask;
                });
            });
            app.Map("/logout", configureMap =>
            {
                configureMap.Run(context =>
                {
                    context.Response.Cookies.Delete(LoggedOnCookie, new CookieOptions() { HttpOnly = true, Path = "/" });

                    context.Response.StatusCode = 204;

                    return Task.CompletedTask;
                });
            });
            app.Map("/errors", configureMap =>
            {
                configureMap.Use<ErrorMiddleware>();
            }).UseStageMarker(PipelineStage.MapHandler);

            app.Map("/ajax-extend-cookie", configureMap =>
            {

                configureMap.Use(Authorize).UseStageMarker(PipelineStage.Authorize);
                configureMap.Use(SlideAuthentication);
                configureMap.Run(App);
            }).UseStageMarker(PipelineStage.MapHandler);
            app.Map("/ajax-not-extend-cookie", configureMap =>
            {

                configureMap.Use(Authorize).UseStageMarker(PipelineStage.Authorize);
                configureMap.Run(App);
            }).UseStageMarker(PipelineStage.MapHandler);
            app
                .Use(AppFuncMiddleware)
                .UseAutofacMiddleware(builder.Build())
                .Use(ErrorLogger)
                .UseStageMarker(PipelineStage.Authenticate)
                .UseStaticFiles(new StaticFileOptions { FileSystem = new PhysicalFileSystem("web") }).UseStageMarker(PipelineStage.MapHandler);
        }

        private static void ExtendAuthentication(IOwinContext context)
        {
            context.Response.Headers.AppendCookie(LoggedOnCookie, "true", new ExtendedCookieOptions() { HttpOnly = true, MaxAge = TimeSpan.FromSeconds(2), Path = "/" });
        }

        public Task Authorize(IOwinContext context, Func<Task> next)
        {
            if (!IsAuthorized(context))
            {
                if (IsAjaxCall(context))
                {
                    context.Response.StatusCode = 403;
                    return Task.CompletedTask;

                }

                context.Response.Redirect("/login");
                return Task.CompletedTask;
            }
            return next();

        }

        public async Task SlideAuthentication(IOwinContext context, Func<Task> next)
        {

            await next();

            if (IsAuthorized(context))
            {
                ExtendAuthentication(context);
            }

        }

        private static bool IsAjaxCall(IOwinContext context)
        {
            return "XMLHttpRequest".Equals(context.Request.Headers.Get(RequestedWithHeader), StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsAuthorized(IOwinContext context)
        {
            return "true".Equals(context.Request.Cookies[LoggedOnCookie], StringComparison.InvariantCultureIgnoreCase);
        }

        public AppFunc AppFuncMiddleware(AppFunc next)
        {
            AppFunc appFunc = async env =>
            {
                var context = new OwinContext(env);
                await context.Response.WriteAsync("<!-- AppFuncMiddleware before -->\n");

                await next(env);

                await context.Response.WriteAsync("<!-- AppFuncMiddleware after -->\n");

            };
            return appFunc;
        }
        static int i = 0;
        public async Task ErrorLogger(IOwinContext context, Func<Task> next)
        {

            await context.Response.WriteAsync("<!-- ErrorLogger before -->\n");

            try
            {
                await next();

                await context.Response.WriteAsync("<!-- ErrorLogger after -->\n");

                i++;
            }
            catch (Exception)
            {
                await context.Response.WriteAsync("<!-- ErrorLogger Error -->\n");
                throw;
            }

        }
        public async Task App(IOwinContext context)
        {
            if (context.Request.QueryString.Value.Equals("throw"))
            {
                throw new Exception();
            }
            if (context.Request.QueryString.Value.Equals("404"))
            {
                context.Response.StatusCode = 404;
                return;
            }
            if (context.Request.QueryString.Value.Equals("500"))
            {
                context.Response.StatusCode = 500;
                return;
            }
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("App handler\n");
        }
    }
}
