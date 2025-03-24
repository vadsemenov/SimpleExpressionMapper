using SimpleExpressionMapper.Core;

namespace SimpleExpressionMapper.Main;

class Program
{
    static void Main(string[] args)
    {
        var order = new Order
        {
            OrderId = 1,
            OrderName = "OrderName",
            OrderDate = DateTime.Now
        };

        var mapper = new ExpressionMapper();

        var orderDto = mapper.Map<Order, OrderDto>(order);
        var orderDto1 = mapper.Map<Order, OrderDto>(order);
        var orderDto2 = mapper.Map<Order, OrderDto2>(order);

        Console.Read();
    }

    public class Order
    {
        public int OrderId { get; set; }

        public string OrderName { get; set; } = null!;

        public DateTime OrderDate { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }

        public string OrderName { get; set; } = null!;

        public DateTime OrderDate { get; set; }
    }

    public class OrderDto2
    {
        public int OrderId { get; set; }

        public string OrderName { get; set; } = null!;

        public DateTime OrderDate { get; set; }
    }
}