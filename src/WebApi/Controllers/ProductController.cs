using System;
using System.Collections.Generic;
using AutoMapper;
using Company.TestProject.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Company.TestProject.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService productService;
        private readonly IMapper mapper;

        public ProductController(IProductService productService, IMapper mapper)
        {
            this.productService = productService ?? throw new ArgumentNullException(nameof(productService));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        public List<WebApiClient.DTO.Product> Get([FromQuery]string filter)
        {
            List<Shared.DTO.Product> products = this.productService.GetProducts(filter);
            return this.mapper.Map<List<WebApiClient.DTO.Product>>(products);
        }
    }
}
