namespace GitBranchAuditor
{
    public static class Endpoints
    {
        public static void ConfigureEndpoints(this WebApplication app)
        {
            app.MapGet("/api/events", () => ProcessEvent)
                .WithName("ProcessEvent")
                .Produces(200)
                .Produces(400)
                .WithOpenApi();
        }

        public static async Task<IResult> ProcessEvent(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentNullException(nameof(payload));
            }

            return Results.Ok();
        }
    }
}
