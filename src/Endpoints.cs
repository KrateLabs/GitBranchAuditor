using GitBranchAuditor.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Octokit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GitBranchAuditor
{
    public static class Endpoints
    {
        public static void ConfigureEndpoints(this WebApplication app)
        {
            app.MapPost("/api/events", ProcessEvent)
                .WithDisplayName("ProcessEvent")
                .Produces(200)
                .Produces(400)
                .WithOpenApi();
        }

        public static async Task<IResult> ProcessEvent(IConfiguration config, HttpRequest req, [FromBody] JsonElement jsonBody)
        {
            var eventType = req.Headers["x-github-event"];
            if (eventType != "create")
            {
                return Results.BadRequest("Unsupported github webhook event type.");
            }

            var signatureExist = req.Headers.TryGetValue("x-hub-signature-256", out StringValues payloadSignature);
            if (!signatureExist)
            {
                return Results.BadRequest("Missing or Invalid request signature detected.");
            }

            var signatureValue = payloadSignature.FirstOrDefault();
            if (String.IsNullOrEmpty(signatureValue) || !VerifySignature(signatureValue, jsonBody.GetRawText()))
            {
                return Results.BadRequest("Missing or Invalid request signature detected.");
            }

            var createEvent = jsonBody.Deserialize<CreateEvent>();
            if (createEvent == null || createEvent.installation?.id <= 0)
            {
                return Results.BadRequest($"Invalid github webhook payload received for eventType: {eventType}");
            }

            //var payload = await req.GetRawBodyAsync();
            //var jsonPayload = jsonBody.ToString();
            //var isEqual = jsonPayload == payload;

            //var createdRefType = jsonBody.GetProperty("ref_type").GetString();
            //var createdRefName = jsonBody.GetProperty("ref").GetString();

            var jwtToken = GitHubJwtTokenIssuer.GenerateToken(config);

            // Use the JWT as a Bearer token
            var appClient = new GitHubClient(new ProductHeaderValue("GitBranchAuditor"))
            {
                Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
            };

            // Create an Installation token for Installation Id 123
            var response = await appClient.GitHubApps.CreateInstallationToken(createEvent.installation.id);

            // NOTE - the token will expire in 1 hour!
            var expiration = response.ExpiresAt;

            // Create a new GitHubClient using the installation token as authentication
            var installationClient = new GitHubClient(new ProductHeaderValue($"GitBranchAuditor-{createEvent.installation.id}"))
            {
                Credentials = new Credentials(response.Token)
            };

            var userInfo = await installationClient.User.Get(createEvent?.sender.login);
            var targetEmail = userInfo.Email;

            return Results.Ok();
        }

        private static bool VerifySignature(string gitHubSignature, string payload)
        {
            string ToHex(byte[] bytes) => String.Concat(Array.ConvertAll(bytes, x => x.ToString("x2")));

            var isSigatureValid = false;
            var keyBytes = GetBytes("c4b47e1a06ebd4e0ea500944a2b92c95");
            using (HMACSHA256 hmSha256 = new HMACSHA256(keyBytes))
            {
                var messageBytes = GetBytes(payload);
                var hashBytes = hmSha256.ComputeHash(messageBytes);
                var hash = "sha256=" + ToHexString(hashBytes);
                var hexits = $"sha256={ToHex(hashBytes)}";

                isSigatureValid = hash == gitHubSignature;
            }
            return isSigatureValid;
        }

        private static byte[] GetBytes(string text) => new UTF8Encoding().GetBytes(text);

        private static string ToHexString(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                builder.AppendFormat("{0:x2}", b);
            }

            return builder.ToString();
        }

        private static async Task<string> GetRawBodyAsync(this HttpRequest request, Encoding? encoding = default)
        {
            if (!request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                request.EnableBuffering();
            }

            request.Body.Position = 0;

            var reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8);

            var body = await reader.ReadToEndAsync().ConfigureAwait(false);

            request.Body.Position = 0;

            return body;
        }
    }
}
