using SimpleExpressionMapper.Core;

//Исходный объект класса
var order = new Order
{
	OrderId = 1,
	OrderName = "OrderName",
	OrderDate = DateTime.Now
};

var mapper = new ExpressionMapper();

var orderDto = mapper.Map<Order, OrderDto>(order);
//Так как делегат уже создан, при втором вызове с одинаковыми типами,
//он достанется из кэша (словаря)
var orderDto1 = mapper.Map<Order, OrderDto>(order);
var orderDto2 = mapper.Map<Order, OrderDto2>(order);

Console.Read();

//Исходный тип
public class Order
{
	public int OrderId { get; set; }
	public string OrderName { get; set; } = null!;
	public DateTime OrderDate { get; set; }
}

//Результирующий тип
public class OrderDto
{
	public int OrderId { get; set; }
	public string OrderName { get; set; } = null!;
	public DateTime OrderDate { get; set; }
}

//Еще один результирующий тип 
public class OrderDto2
{
	public int OrderId { get; set; }
	public string OrderName { get; set; } = null!;
	public DateTime OrderDate { get; set; }
}
