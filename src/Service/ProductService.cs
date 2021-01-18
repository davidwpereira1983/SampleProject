using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Company.TestProject.Shared;
using Company.TestProject.Shared.DTO;

namespace Company.TestProject.Service
{
    public class ProductService : IProductService
    {
        public const string ActivitySourceName = nameof(ProductService);

        private static readonly Version Version = typeof(ProductService).Assembly.GetName().Version;
        private static readonly ActivitySource ImageReceiverServiceActivitySource = new ActivitySource(ActivitySourceName, Version.ToString());

        private readonly IProductRepository productRepository;

        public ProductService(IProductRepository productRepository)
        {
            this.productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public Product GetById(Guid id)
        {
            return this.TraceAction(() =>
            {
                return this.productRepository.GetById(id);
            });
        }

        public List<Product> GetProducts(string filter)
        {
            return this.TraceAction(() =>
            {
                return this.productRepository.GetProducts(filter);
            });
        }

        public void InsertProduct(Product product)
        {
            this.TraceAction(() =>
            {
                this.productRepository.InsertProduct(product);
            });
        }

        private T TraceAction<T>(Func<T> method, [CallerMemberName] string callerMemberName = null)
        {
            var activity = ImageReceiverServiceActivitySource.StartActivity($"{nameof(ProductService)}.{callerMemberName}", ActivityKind.Internal);

            try
            {
                return method();
            }
            finally
            {
                activity?.Stop();
            }
        }

        private void TraceAction(Action method, [CallerMemberName] string callerMemberName = null)
        {
            var activity = ImageReceiverServiceActivitySource.StartActivity($"{nameof(ProductService)}.{callerMemberName}", ActivityKind.Internal);

            try
            {
                method();
            }
            finally
            {
                activity?.Stop();
            }
        }
    }
}
