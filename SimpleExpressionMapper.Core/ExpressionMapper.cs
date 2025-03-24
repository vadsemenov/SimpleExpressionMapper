using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleExpressionMapper.Core;

public interface IMapper
{
    TDestination Map<TSource, TDestination>(TSource source); 
}

public class ExpressionMapper : IMapper
{
    private readonly ConcurrentDictionary<TypesMapStorage, Delegate?> _delegatesStorage = new();
    
    public TDestination Map<TSource, TDestination>(TSource source)
    {
       var creatorDelegate = GetCreatorDelegate<TSource, TDestination>();
       
       if(creatorDelegate == null)
           throw new NullReferenceException("The creator delegate is null.");
       
       return creatorDelegate(source);
    }

    private Func<TSource, TDestination>? GetCreatorDelegate<TSource, TDestination>()
    {
        var key = new TypesMapStorage(typeof(TSource), typeof(TDestination));

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

    private static Func<TSource, TDestination> CompileCreatorDelegate<TSource, TDestination>()
    {
        var sourceTypeProperties = typeof(TSource).GetProperties(BindingFlags.Instance|BindingFlags.Public);
        var destinationTypeProperties = typeof(TDestination).GetProperties(BindingFlags.Instance|BindingFlags.Public)
            .Where(p=> p.CanWrite)
            .ToList();

        var propertiesPair = sourceTypeProperties
            .Join(destinationTypeProperties,
                s => s.Name,
                d => d.Name,
                (s, d) => (sourceProperty: s, destinationProperty: d))
            .Where(p => p.sourceProperty.PropertyType.IsAssignableTo(p.destinationProperty.PropertyType))
            .ToList();
         
        var sourceTypeParameter = Expression.Parameter(typeof(TSource), "source");
        
        var propertiesBindings = propertiesPair.Select(p=>
            Expression.Bind(p.destinationProperty, Expression.Property(sourceTypeParameter, p.sourceProperty)));
        
        var createInstanceExpression  = Expression.MemberInit(Expression.New(typeof(TDestination)), propertiesBindings);
        
        return Expression.Lambda<Func<TSource, TDestination>>(createInstanceExpression, sourceTypeParameter).Compile();
    }
}

public record TypesMapStorage(Type SourceType, Type DestinationType);