using System;
using System.Collections.Generic;
using Company.TestProject.Shared.DTO;

namespace Company.TestProject.Shared
{
    public interface IProductRepository
    {
        List<Product> GetProducts(string filter);
        void InsertProduct(Product product);
        Product GetById(Guid id);
    }
}
