using Modified.Microsoft.Owin.StaticFiles;
using Microsoft.Owin.Testing;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Owin.FileSystems;

namespace ClassLibrary1
{
    public class StartupWithETagProvider
    {
        public void Configuration(IAppBuilder app)
        {
            IFileSystem fileSystem = new PhysicalFileSystem(".");

            StaticFileOptions staticFileOptions = new StaticFileOptions()
            {
                FileSystem = fileSystem,
                GetCustomETag = new ETagProvider(fileSystem).GetETag,
            };
            app.UseStaticFiles(staticFileOptions);
        }
    }
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            IFileSystem fileSystem = new PhysicalFileSystem(".");

            StaticFileOptions staticFileOptions = new StaticFileOptions()
            {
                FileSystem = fileSystem,
            };
            app.UseStaticFiles(staticFileOptions);
        }
    }


    public class StaticFileTests
    {
        [Fact]
        public async Task Serve_existing_file_with_etag_crc()
        {
            using (var server = TestServer.Create<StartupWithETagProvider>())
            {
                var response = await server.HttpClient.GetAsync("/test.txt");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.NotNull(response.Headers.ETag);
                Assert.NotNull(response.Content.Headers.LastModified);

                using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/test.txt"))
                {
                    httpRequestMessage.Headers.IfNoneMatch.Add(response.Headers.ETag);
                    httpRequestMessage.Headers.IfModifiedSince = response.Content.Headers.LastModified.Value.AddHours(-1);

                    var responseCached = await server.HttpClient.SendAsync(httpRequestMessage);
                    Assert.Equal(HttpStatusCode.NotModified, responseCached.StatusCode);
                    Assert.NotNull(responseCached.Headers.ETag);
                    Assert.NotNull(responseCached.Content.Headers.LastModified);

                }
            }
        }
        [Fact]
        public async Task Serve_existing_file_with_etag_crc_no_crc()
        {
            using (var server = TestServer.Create<StartupWithETagProvider>())
            {
                var response = await server.HttpClient.GetAsync("/test-no-crc.txt");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.NotNull(response.Headers.ETag);
                Assert.NotNull(response.Content.Headers.LastModified);

                using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/test-no-crc.txt"))
                {
                    httpRequestMessage.Headers.IfNoneMatch.Add(response.Headers.ETag);
                    httpRequestMessage.Headers.IfModifiedSince = response.Content.Headers.LastModified.Value.AddHours(-1);

                    var responseCached = await server.HttpClient.SendAsync(httpRequestMessage);
                    Assert.Equal(HttpStatusCode.OK, responseCached.StatusCode);
                    Assert.NotNull(responseCached.Headers.ETag);
                    Assert.NotNull(responseCached.Content.Headers.LastModified);

                }
            }
        }
        [Fact]
        public async Task Serve_existing_file_with_default_etag()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var response = await server.HttpClient.GetAsync("/test.txt");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.NotNull(response.Headers.ETag);
                Assert.NotNull(response.Content.Headers.LastModified);

                using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/test.txt"))
                {
                    httpRequestMessage.Headers.IfNoneMatch.Add(response.Headers.ETag);
                    httpRequestMessage.Headers.IfModifiedSince = response.Content.Headers.LastModified.Value.AddHours(-1);

                    var responseCached = await server.HttpClient.SendAsync(httpRequestMessage);
                    Assert.Equal(HttpStatusCode.OK, responseCached.StatusCode);
                    Assert.NotNull(responseCached.Headers.ETag);
                    Assert.NotNull(responseCached.Content.Headers.LastModified);

                }
            }
        }
        [Fact]
        public async Task Dont_serve_non_existing_file()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var response = await server.HttpClient.GetAsync("/test1.txt");

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            }
        }
    }
}
