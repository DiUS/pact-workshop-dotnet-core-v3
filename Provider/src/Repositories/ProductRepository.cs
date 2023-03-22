using System.Collections.Generic;
using Provider.Model;

namespace Provider.Repositories;

public class ProductRepository : IProductRepository
{
    private List<Product> State { get; set; }

    public ProductRepository()
    {
        State = new List<Product>()
        {
            new Product(9, "GEM Visa", "CREDIT_CARD", "v2"),
            new Product(10, "28 Degrees", "CREDIT_CARD", "v1")
        };
    }

    public void SetState(List<Product> state)
    {
        State = state;
    }

    List<Product> IProductRepository.List()
    {
        return State;
    }

    public Product Get(int id)
    {
        return State.Find(p => p.id == id);
    }
}