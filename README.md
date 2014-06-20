# TypeMapping
Configure and perform object-to-object mappings.

## Overview
Software written for the modern world involves passing objects between layers. When crossing layers, objects' values are copied to other objects, often to avoid exposing implementation details across layers. An example might be creating presentation objects for the UI from data objects returned from the data layer. You might end up writing code that looks like this:

    Presentation p = new Presentation();
    p.UserName = user.Name;
    p.AccountType = user.Type;
    // ... 40 other properties ..
    
Writing this code can be tedious and might even need repeated. The `TypeMapping` library aims to simplify mapping between objects by allowing you to configure a mapping in one place and refer back to it as many times as needed.

    DefineMap.From<User>().To<Presentation>()
        .Map(user => user.Name, p => p.UserName)
        .Map(user => user.Type, p => p.AccountType);
        
The code sample above would be used to define a mapping from a `User` object to `Presentation` object. Once you have an actual `User` object, you can create a `Presentation` object simply by calling:

    Presentation presentation = Map.From<User>(user).To<Presentation>();
    
This code assumes that the given user is not null and that the `Presentation` object is default-constructible.

## Identifiers
If you need to define a mapping between the same types multiple times with slightly different mechanics, you can pass an identifier to `To<T>` to uniquely distinguish them. When actually performing the mapping, pass the same identifier to `To<T>`.

## Construction
If your destination type is not default constructible, you have two options: you can pass the pre-constructed object to the `To<T>` method when mapping -or- you can call the `Construct` method.

    DefineMap.From<User>().To<Presentation>()
        .Construct(user => new Presentation())
        
It can make sense to call `Construct` even if you know you'll be passing the object every time. It adds flexibility and can actually improve performance.
        
## Assignment
Technically, you can do a lot of initialization in the `Construct` method. However, someone might want to pass a pre-constructed object to the `To<T>` method. In that case, any custom logic in the `Construct` method will be ignored. If you plan on setting a lot of values to constant values, use the `Map` methods accepting values:

    DefineMap.From<User>().To<Presentation>()
        .Construct(user => new Presentation())
        .Map(up => up.UserName, HttpContext.Current.User.Identity.Name)
        .Map(up => up.TimeStamp, () => DateTime.UtcNow);

Note that usually the right-hand side of the assignments are not re-generated each time a `Presentation` object is mapped to. You can use the second overload, the overload accepting an `Action<TProp>` to force the value to be evaluated every time.

## One-to-one Mapping
If you need to map a single value from one object to another, use the `Map` methods. One overload allows you to specify the source and destination properties:

    DefineMap.From<User>().To<Presentation>()
        .Map(user => user.Name, p => p.UserName);
        
Another overload allows for more complex mapping:

    DefineMap.From<User>().To<Presentation>()
        .Map(user => user.Name, (p, name) => p.UserName = name);
        
This is a general-purpose implementation of the first overload. It allows you to do things like call methods:

    DefineMap.From<User>().To<Presentation>()
        .Map(user => user.Name, (p, name) => p.SetName(name));
        
The second overload is slightly harder to read but is required sometimes. In most cases, it also boosts performance.

## One-to-Many and Many-to-Many Mappings
What about one-to-many mappings or many-to-many mappings? `MapMany` makes this possible.

Imagine you wanted to convert a `List<User>` to a `List<Presentation>`. Assuming you've already created a mapping from a `User` to a `Presentation`, the following code can easily accomplish this:

    DefineMap.From<List<User>>().To<List<Presentation>>()
        .MapMany((ps, user) => ps.Add(Map.From<User>(user).To<Presentation>()));
        
The lambda is simply converting the current `User` to a `Presentation` and adding it to the `List<Presentation>`.

This code can be improved by setting the `Presentation` list's capacity up-front:

    DefineMap.From<List<User>>().To<List<Presentation>>()
        .Construct(users => new List<Presentation>(users.Count))
        .MapMany((ps, user) => ps.Add(Map.From<User>(user).To<Presentation>()));
        
There are even overloads that would make it possible to map from a `List<User>` to a `Presentation[]`:

    DefineMap.From<List<User>>().To<Presentation[]>()
        .Construct(users => new Presentation[users.Count])
        .MapMany((ps, index, user) => ps[index] = Map.From<User>(user).To<Presentation>());
        
There are also overloads of `MapMany` that will map so long as a condition holds true. For instance, here's some code that will map from a `SqlDataReader` to a `List<User>`.

    DefineMap.From<SqlDataReader>().To<List<User>>()
        .MapMany(reader => reader.Read(), (users, record) => users.Add(Map.From<IDataRecord>(record).To<User>()));
        
    DefineMap.From<IDataRecord>().To<User>()
        .Map(record => record.GetInt32(0), user => user.Id)
        .Map(record => record.GetString(1), user => user.Name)
        .Map(record => record.GetDateTime(2), user => user.Created);
        
There are two important things that note from the previous example. First, notice that the mapping from `IDataRecord` to `User` is defined after the mapping from `SqlDataReader` to `List<User>`. Even though the first definition depends on the second, they can be defined in any order. Just make sure you have everything defined *before* you map!

The second thing to notice is that when mapping `IDataRecord` to `User`, I provided the generic argument to `From<T>`. Normally, specifying the generic argument to `From<T>` is optional because it can be inferred from the source object. However, in this case, it is **required**! If I didn't provide it, an error would have occurred because there is no mapping from `SqlDataReader` to `User`. Technically, I could have used `SqlDataReader` instead of `IDataRecord` when defining the second mapping; however, restricting the interface makes this method more reusable and prevents silly mistakes. The thing to take away from all this is that you should *always* explicitly specify the generic arguments.

## Bridge
Probably the most complex feature in *TypeMapping* is the `Bridge` method. `Bridge` will handle the scenario where you've defined a mapping from `A -> B` and from `B -> C` and now you want to define `A -> C` (the associative property). For this to work, simply define the mapping `A -> B` and then call `Bridge`, passing the results of defining `B -> C`:

    // Define a mapping from List<User> to User[]
    var list2Array = DefineMap.From<List<User>>().To<User[]>()
        .Construct(users => new User[users.Count])
        .MapMany((array, index, user) => array[index] = user);
        
    // Define a mapping from a DataReader to a List<User>, then DataReader to a User[].
    DefineMap.From<SqlDataReader>().To<List<User>>()
        .MapMany(reader => reader.Read(), (users, record) => users.Add(Map.From<IDataRecord>(record).To<User>()))
        .Associate(list2Array);
        
The second definition is actually two definitions in one! Until `Bridge` is called, a mapping from `SqlDataReader` to `List<User>` is being defined. The call to `Bridge` automatically defines a new mapping, which can then be further configured.

The only consideration is that the middle type (`List<User>` in the example) must be default constructible or `Construct` must be called. `Bridge` essentially eliminates the middle step from an otherwise long-winded mapping process. It assumes that it can create whatever it needs to avoid those middle steps. You can always configure the resulting mapping to fill in any gaps.

## Many-to-one Mappings
The only scenario we didn't talk about was mapping from many objects into one. Suppose you have a `Summary` object that simply acts as a container for aggregated data. Objects like `Summary` are actually fairly common when building presentation objects (view models) or generating service responses.

The simplest solution is to define mappings from one of your sources to a collection in the aggregate object. In that case, simply doing an assignment would be easier.

Another option is to map to your aggregate object, but then use `MapMany` to map to the aggregate collection. You just need to remember to pass the aggregate object to `To<T>` when performing the mapping; otherwise, each call to `Map` will create a new aggregate object.

    Summary summary = new Summary();
    Map.From<List<Order>>(orders).To<Summary>(summary)
        .MapMany(orders => orders.Select(o => o.Total), (summary, total) => summary.OrderTotals.Add(total));
        .Map(orders => orders.Sum(o => o.Total), summary => summary.GrandTotal);

A third option is create a simple container object for all of your sources, where each is just a simple property. Then define a mapping from your container object to the aggregate object, using `Map` and `MapMany` where needed. With this option you have define an entire type but the mapping definition is easier to read and write.

## License
If you are looking for a license, you won't find one. The software in this project is free, as in "free as air". Feel free to use my software anyway you like. Use it to build up your evil war machine, swindle old people out of their social security or crush the souls of the innocent.

I love to hear how people are using my code, so drop me a line. Feel free to contribute any enhancements or documentation you may come up with, but don't feel obligated. I just hope this code makes someone's life just a little bit easier.
