using System;
using System.Collections.Generic;
using Company.TestProject.Tests.Acceptance.Context;
using Company.TestProject.WebApiClient;
using Company.TestProject.WebApiClient.DTO;

namespace Company.TestProject.Tests.Acceptance.API
{
    public class ProductServiceClientWrapper
    {
        private readonly ProductServiceContext productServiceContext;
        private readonly ProductServiceClient productServiceClient;
        public ProductServiceClientWrapper(ProductServiceContext productServiceContext)
        {
            this.productServiceContext = productServiceContext ?? throw new ArgumentNullException(nameof(productServiceContext));

            var serviceUrl = ConfigurationManager.Configuration.GetSection("AppSettings:ServiceUrl").Value;
            this.productServiceClient = new ProductServiceClient(serviceUrl);
            NUnit.Framework.TestContext.Progress.WriteLine($"Host: {serviceUrl}");
        }

        public List<Product> GetProducts(string filter = null)
        {
            return this.productServiceClient.GetProducts(filter);
        }
    }
}
