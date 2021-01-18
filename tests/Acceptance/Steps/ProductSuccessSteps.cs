using System;
using System.Linq;
using Company.TestProject.Tests.Acceptance.API;
using Company.TestProject.Tests.Acceptance.Context;
using Company.TestProject.WebApiClient.DTO;
using NUnit.Framework;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Company.TestProject.Tests.Acceptance.Steps
{
    [Binding]
    public class ProductSuccessSteps
    {
        private readonly ProductServiceContext context;
        private readonly ProductServiceClientWrapper productServiceClientWrapper;

        public ProductSuccessSteps(ProductServiceContext context, ProductServiceClientWrapper productServiceClientWrapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.productServiceClientWrapper = productServiceClientWrapper ?? throw new ArgumentNullException(nameof(productServiceClientWrapper));
        }

        [Given(@"The user is logged in to the platform")]
        public void GivenTheUserIsLoggedInToThePlatform()
        {
        }

        [When(@"Get the list of products")]
        public void WhenGetTheListOfProducts()
        {
            this.context.Products = this.productServiceClientWrapper.GetProducts();
        }

        [Then(@"the platform return the products:")]
        public void ThenThePlatformReturnTheProducts(Table table)
        {
            var expectedProducts = table.CreateSet<Product>().ToList();

            Assert.AreEqual(expectedProducts.Count, this.context.Products.Count);

            foreach (var expectedProduct in expectedProducts)
            {
                var product = this.context.Products.FirstOrDefault(o => o.Name == expectedProduct.Name);
                Assert.IsNotNull(product);
            }
        }
    }
}
