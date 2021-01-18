using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Company.TestProject.DataAccess;
using Company.TestProject.Service;
using Company.TestProject.Shared;
using Company.TestProject.Shared.DTO;
using NUnit.Framework;

namespace Company.TestProject.Tests.Integration
{
    [TestFixture]
    public class ProductServiceTest : IntegrationTestBase
    {
        private IProductService productService;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            this.productService = new ProductService(new ProductRepository(this.connectionString));
        }

        [Test]
        public void CanGetProducts()
        {
            // Arrange
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                CreatedOn = DateTime.Now
            };

            // Act
            this.productService.InsertProduct(product);

            // Assert
            var productInDatabase = this.productService.GetById(product.Id);

            Assert.That(productInDatabase, Is.Not.Null);
            Assert.That(productInDatabase.Id, Is.EqualTo(product.Id));
            Assert.That(productInDatabase.Name, Is.EqualTo(productInDatabase.Name));
            Assert.That(productInDatabase.CreatedOn, Is.EqualTo(productInDatabase.CreatedOn));
        }
    }
}
