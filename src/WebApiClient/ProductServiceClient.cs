using System.Collections.Generic;
using System.Net.Http;
using Company.TestProject.WebApiClient.DTO;
using RestSharp;

namespace Company.TestProject.WebApiClient
{
    public class ProductServiceClient
    {
        private readonly IRestClient restClient;
        public ProductServiceClient(string baseUrl)
        {
            this.restClient = new RestClient(baseUrl);
        }

        public List<Product> GetProducts(string filter)
        {
            var request = new RestRequest($"product?filter={filter}", Method.GET)
            {
                RequestFormat = DataFormat.Json
            };

            IRestResponse<List<Product>> response = this.restClient.Execute<List<Product>>(request);

            if (!response.IsSuccessful)
            {
                throw new HttpRequestException($"Http request wasn't successful. StatusCode: ({response.StatusCode}),  ResponseStatus: ({response.ResponseStatus}), Content: ({response.Content})");
            }

            return response.Data;
        }
    }
}
