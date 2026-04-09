using WireMock.Server;
using Request = WireMock.RequestBuilders.Request;
using Response = WireMock.ResponseBuilders.Response;

namespace AssetHub.Shared {
    public static class AssetHubMockApi {
        public static void StartApi() {

            var server = WireMockServer.Start(9090);

            Console.WriteLine($"WireMock running on {server.Urls[0]}");

            //
            // 🔐 TOKEN ENDPOINT
            //
            server.Given(
                Request.Create()
                    .WithPath("/oauth/token")
                    .UsingPost()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new {
                        access_token = "mock-token",
                        token_type = "Bearer",
                        expires_in = 5400
                    })
            );

            //
            // 📦 CATEGORY LOOKUP
            //
            server.Given(
                Request.Create()
                    .WithPath("/v1/categories")
                    .UsingGet()
                    .WithHeader("Authorization", "Bearer mock-token")
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new {
                        data = new[]
                        {
                new { id = 1, name = "Earthmoving" }
                        }
                    })
            );

            //
            // 🔍 ASSET SEARCH (dedup check)
            //
            server.Given(
                Request.Create()
                    .WithPath("/v1/projects/*/assets")
                    .UsingGet()
                    .WithHeader("Authorization", "Bearer mock-token")
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new {
                        data = Array.Empty<object>() // no match = safe to create
                    })
            );

            //
            // ➕ ASSET CREATE
            //
            server.Given(
                Request.Create()
                    .WithPath("/v1/projects/*/assets")
                    .UsingPost()
                    .WithHeader("Authorization", "Bearer mock-token")
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(201)
                    .WithBodyAsJson(new {
                        data = new {
                            id = "asset-001",
                            assetId = "Caterpillar-320-SN-9901"
                        }
                    })
            );

            //
            // ✏️ ASSET UPDATE
            //
            server.Given(
                Request.Create()
                    .WithPath("/v1/projects/*/assets/*")
                    .UsingPut()
                    .WithHeader("Authorization", "Bearer mock-token")
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new {
                        data = new {
                            id = "asset-001",
                            updated = true
                        }
                    })
            );

            //
            // 🖼 PHOTO UPLOAD
            //
            server.Given(
                Request.Create()
                    .WithPath("/v1/projects/*/assets/*/photo")
                    .UsingPost()
                    .WithHeader("Authorization", "Bearer mock-token")
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
            );

            //
            // ❌ Example failure (for resilience testing)
            //
            server.Given(
                Request.Create()
                    .WithPath("/v1/projects/*/assets")
                    .UsingPost()
                    .WithHeader("Authorization", "Bearer mock-token")
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(500)
            );
        }
    }
}
