using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoMasters.E2ETests.Infrastructure;

public abstract class E2ETestBase
{
    protected MockGroup2ApiServer MockServer { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    [TestInitialize]
    public virtual void Setup()
    {
        MockServer = new MockGroup2ApiServer();
        Client = new HttpClient(MockServer)
        {
            BaseAddress = new Uri("https://api-cartes-g2.fldsio.com/")
        };
    }

    [TestCleanup]
    public virtual void Cleanup()
    {
        Client.Dispose();
        MockServer.Dispose();
    }

    protected void SetAuthorizationToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
