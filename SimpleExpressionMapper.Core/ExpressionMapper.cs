using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleExpressionMapper.Core;

public interface IMapper
{
    //Мэппер простой, один метод для конвертирования и кэширования конвертируемых типов
    TDestination Map<TSource, TDestination>(TSource source); 
}

public class ExpressionMapper : IMapper
{
    //Словарь для хранения делегатов производящих мэппинг
    private readonly ConcurrentDictionary<TypesMapStorage, Delegate?> _delegatesStorage = new();
    
    //Основной метод для вызова мэппинга.
    //TSource - исходный тип для конвертации
    //TDestination - тип в который будем конвертировать
    public TDestination Map<TSource, TDestination>(TSource source)
    {
       var creatorDelegate = GetCreatorDelegate<TSource, TDestination>();
       
       if(creatorDelegate == null)
           throw new NullReferenceException("The creator delegate is null.");
       
       return creatorDelegate(source);
    }

    //Метод, который возвращает делегат для мэппинга.
    private Func<TSource, TDestination>? GetCreatorDelegate<TSource, TDestination>()
    {
        //Получаем ключ для словаря.
        var key = new TypesMapStorage(typeof(TSource), typeof(TDestination));
        
        //Если делегата еще нет, то он создается и сохраняется в словарь _delegatesStorage.
        //Если есть нужный делегат в словаре, то просто возвращаем его.
        if (_delegatesStorage.TryGetValue(key, out var creatorDelegate))
        {
            return creatorDelegate as Func<TSource, TDestination>;
        }
        else
        {
            var newCreatorDelegate = CompileCreatorDelegate<TSource, TDestination>();
            _delegatesStorage.GetOrAdd(key, newCreatorDelegate);
            
            return newCreatorDelegate;
        }
    }

    //Метод для компиляции делегата конвертации из дерева выражений
    private static Func<TSource, TDestination> CompileCreatorDelegate<TSource, TDestination>()
    {
        //Извлекаем свойства из исходного типа(класса) и типа в который будем конвертировать.
        //Проверяем, что свойство результирующего типа не только для чтения.  
        var sourceTypeProperties = typeof(TSource).GetProperties(BindingFlags.Instance|BindingFlags.Public);
        var destinationTypeProperties = typeof(TDestination).GetProperties(BindingFlags.Instance|BindingFlags.Public)
            .Where(p=> p.CanWrite)
            .ToList();

        //Сопоставляем совпадающие имена свойства исходного и результирующего типа.
        //И проверяем что тип свойства в обоих классах одинаковый (например int и int).
        var propertiesPair = sourceTypeProperties
            .Join(destinationTypeProperties,
                s => s.Name,
                d => d.Name,
                (s, d) => (sourceProperty: s, destinationProperty: d))
            .Where(p => p.sourceProperty.PropertyType.IsAssignableTo(p.destinationProperty.PropertyType))
            .ToList();
         
        //Выражение для создания параметра лямбды, по сути он будет компилироваться в такое лямбда выражение: (source) => ...
        var sourceTypeParameter = Expression.Parameter(typeof(TSource), "source");
        
        //Выражение для присвоения значения свойств исходного типа свойствам результирующего типа. 
        var propertiesBindings = propertiesPair.Select(p=>
            Expression.Bind(p.destinationProperty, Expression.Property(sourceTypeParameter, p.sourceProperty)));
        
        //Выражение для создания объекта результирующего типа, и назначения свойств 
        var createInstanceExpression  = Expression.MemberInit(Expression.New(typeof(TDestination)), propertiesBindings);
        
        //Компиляция делегата для мэппинга
        return Expression.Lambda<Func<TSource, TDestination>>(createInstanceExpression, sourceTypeParameter).Compile();
    }
}

//А это у нас ключ для кэширования в словарь делегата. Он состоит из исходного и результирующего типа.
//Как вы помните метод GetHashCode(и Equals) в record переопределяется автоматически.
public record TypesMapStorage(Type SourceType, Type DestinationType);