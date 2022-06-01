using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace TestAuthClient
{
    class Program
    {
        private static async Task Main()
        {
            var httpClientHandler = new HttpClientHandler();            
            httpClientHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls13;
            //handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            //handler.ClientCertificates.Add(new System.Security.Cryptography.X509Certificates.X509Certificate2("Simcode_IdentityServer.pfx", "identityserver"));
            // needed for self signed sertificates
            //httpClientHandler.ServerCertificateCustomValidationCallback =
            //    (httpRequestMessage, cert, cetChain, policyErrors) =>
            //    {
            //        return true;
            //    };            

            // discover endpoints from metadata
            var httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(100000),
                
            };

            var discoveryDocumentResponse = await httpClient.GetDiscoveryDocumentAsync("http://localhost:60050");
            if (discoveryDocumentResponse.IsError)
            {
                Console.WriteLine(discoveryDocumentResponse.Error);
                return;
            }

            // request token
            var tokenResponse = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = discoveryDocumentResponse.TokenEndpoint,
                ClientId = "userfront",
                Scope = "openid custom.profile userapi",
                UserName = "1",//"pcadmin",
                Password = "1"
            });
            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            // call api
            var apiHttpClient = new HttpClient(httpClientHandler);
            apiHttpClient.SetBearerToken(tokenResponse.AccessToken);
            var responseInfo = await apiHttpClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = discoveryDocumentResponse.UserInfoEndpoint,
                Token = tokenResponse.AccessToken
            });
            if (responseInfo.IsError)
            {
                Console.WriteLine(responseInfo.Error);
            }
            else
            {
                Console.WriteLine("User claims:");
                foreach (var claim in responseInfo.Claims)
                {
                    Console.WriteLine("{0} - {1}", claim.Type, claim.Value);
                }
            }

            /*var response = await apiClient.GetAsync("http://localhost:5000/api/v1/projects");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JObject.Parse(content));
            }*/

            /*var introClient = new HttpClient();
            var introResponse = await introClient.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = disco.IntrospectionEndpoint,
                ClientId = "userapi",
                ClientSecret = "intro",
                Token = tokenResponse.AccessToken
            });
            if (introResponse.IsError)
            {
                Console.WriteLine(introResponse.Error);
            }
            else
            {
                Console.WriteLine($"Is active: {introResponse.IsActive}");
            }*/
        }
    }
}
