using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Company.TestProject.Shared;
using Company.TestProject.Shared.DTO;

namespace Company.TestProject.DataAccess
{
    public class ProductRepository : BaseRepository, IProductRepository
    {
        public const string ActivitySourceName = nameof(ProductRepository);

        private static readonly Version Version = typeof(ProductRepository).Assembly.GetName().Version;
        private static readonly ActivitySource ProductRepositoryActivitySource = new ActivitySource(ActivitySourceName, Version.ToString());

        public ProductRepository(string connectionString)
            : base(connectionString)
        {
        }

        public Product GetById(Guid id)
        {
            return this.TraceAction(() =>
            {
                const string sql = @"SELECT	Id
                                ,		Name
                                ,		CreatedOn
                                FROM	dbo.Product
                                WHERE	Id = @id";

                return this.Query<Product>(sql, new { id = id }).FirstOrDefault();
            });
        }

        public List<Product> GetProducts(string filter)
        {
            return this.TraceAction(() =>
            {
                const string sql = @"SELECT	Id
                                ,		Name
                                ,		CreatedOn
                                FROM	dbo.Product
                                WHERE	Name like @filter";

                return this.Query<Product>(sql, new { filter = $"%{filter}%" });
            });
        }

        public void InsertProduct(Product product)
        {
            this.TraceAction(() =>
            {
                const string sql = @"INSERT INTO dbo.Product(Id, Name, CreatedOn) VALUES (@Id, @Name, @CreatedOn)";
                var prms = new
                {
                    id = product.Id,
                    Name = product.Name,
                    CreatedOn = product.CreatedOn
                };

                this.Execute(sql, prms);
            });
        }

        private void TraceAction(Action method, [CallerMemberName] string callerMemberName = null)
        {
            var activity = ProductRepositoryActivitySource.StartActivity($"{nameof(ProductRepository)}.{callerMemberName}", ActivityKind.Internal);

            try
            {
                method();
            }
            finally
            {
                activity?.Stop();
            }
        }

        private T TraceAction<T>(Func<T> method, [CallerMemberName] string callerMemberName = null)
        {
            var activity = ProductRepositoryActivitySource.StartActivity($"{nameof(ProductRepository)}.{callerMemberName}", ActivityKind.Internal);

            try
            {
                return method();
            }
            finally
            {
                activity?.Stop();
            }
        }
    }
}
