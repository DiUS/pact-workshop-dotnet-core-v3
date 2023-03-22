using System.Collections.Generic;
using Provider.Model;

namespace Provider.Repositories;

public interface IProductRepository
{
    public List<Product> List();
    public Product Get(int id);

    public void SetState(List<Product> product);
}