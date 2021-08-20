using System;
using System.Collections.Generic;
using provider.Model;

namespace provider.Repositories
{
    public interface IProductRepository
    {
        public List<Product> List();
        public Product Get(int id);

        public void SetState(List<Product> product);
    }
}
