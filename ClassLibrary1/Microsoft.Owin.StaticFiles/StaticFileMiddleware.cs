// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin;

namespace Modified.Microsoft.Owin.StaticFiles
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Enables serving static files for a given request path
    /// </summary>
    public class StaticFileMiddleware
    {
        private readonly StaticFileOptions _options;
        private readonly PathString _matchUrl;
        private readonly AppFunc _next;

        /// <summary>
        /// Creates a new instance of the StaticFileMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">The configuration options.</param>
        public StaticFileMiddleware(AppFunc next, StaticFileOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (options.ContentTypeProvider == null)
            {
                throw new ArgumentException("Resources.Args_NoContentTypeProvider");
            }
            if (options.FileSystem == null)
            {
                options.FileSystem = new PhysicalFileSystem("." + options.RequestPath.Value);
            }

            _next = next;
            _options = options;
            _matchUrl = options.RequestPath;
        }

        /// <summary>
        /// Processes a request to determine if it matches a known file, and if so, serves it.
        /// </summary>
        /// <param name="environment">OWIN environment dictionary which stores state information about the request, response and relevant server state.</param>
        /// <returns></returns>
        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

            var fileContext = new StaticFileContext(context, _options, _matchUrl);
            if (fileContext.ValidateMethod()
                && fileContext.ValidatePath()
                && fileContext.LookupContentType()
                && await fileContext.LookupFileInfo())
            {
                fileContext.ComprehendRequestHeaders();

                switch (fileContext.GetPreconditionState())
                {
                    case StaticFileContext.PreconditionState.Unspecified:
                    case StaticFileContext.PreconditionState.ShouldProcess:
                        {
                            if (fileContext.IsHeadMethod)
                            {
                                await fileContext.SendStatusAsync(Constants.Status200Ok);
                                return;
                            }
                            if (fileContext.IsRangeRequest)
                            {
                                await fileContext.SendRangeAsync();
                                return;
                            }
                            await fileContext.SendAsync();
                            return;
                        }

                    case StaticFileContext.PreconditionState.NotModified:
                        {
                            await fileContext.SendStatusAsync(Constants.Status304NotModified);
                            return;
                        }

                    case StaticFileContext.PreconditionState.PreconditionFailed:
                        {
                            await fileContext.SendStatusAsync(Constants.Status412PreconditionFailed);
                            return;
                        }

                    default:
                        throw new NotImplementedException(fileContext.GetPreconditionState().ToString());
                }
            }

            await _next(environment);
        }
    }
}
