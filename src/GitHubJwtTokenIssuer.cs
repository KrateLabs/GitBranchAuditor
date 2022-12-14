namespace GitBranchAuditor
{
    public static class GitHubJwtTokenIssuer
    {
        public static string GenerateToken(IConfiguration config)
        {

            var appId = config.GetValue<int>("GITBranchAuditor_APP_ID");
            var appPrivateKey = config["GITBranchAuditor_PRIVATE_KEY"];

            // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
            var generator = new GitHubJwt.GitHubJwtFactory(
                new GitHubJwt.StringPrivateKeySource(appPrivateKey),
                new GitHubJwt.GitHubJwtFactoryOptions
                {
                    AppIntegrationId = appId, // The GitHub App Id
                    ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
                }
            );

            var jwtToken = generator.CreateEncodedJwtToken();
            return jwtToken;
        }
    }
}
