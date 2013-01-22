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
    
This code assumes that the given user is not null and that the `Presentation` object is default constructible.

## Construct
If your destination type is not default constructible, you have two options: you can pass the pre-constructed object to the `To<T>` method when mapping -or- you can call the `Construct` method.

    DefineMap.From<User>().To<Presentation>()
        .Construct(user => new Presentation())
        
It can make sense to call `Construct` even if you know you'll be passing the object every time. It add flexibility and can actually improve performance. 
        
## Assign
Technically, you can do a lot of initialization in the `Construct` method. However, someone might want to pass a pre-constructed object to the `To<T>` method. In that case, any custom logic in the `Construct` method will be ignored. If you plan on setting a lot of values to constant values, use the `Assign` method:

    DefineMap.From<User>().To<Presentation>()
        .Construct(user => new Presentation())
        .Assign(up => up.UserName = HttpContext.Current.User.Identity.Name) 
        .Assign(up => up.TimeStamp = DateTime.UtcNow());

Note that the right-hand side of the assignments are re-generated each time a `Presentation` object is mapped to. Otherwise, all of your `Presentation` objects would have the same user and timestamp.

## Map
If you need to map a single value from one object to another, use the `Map` methods. One overload allows you to specify the source and destination properties:

    DefineMap.From<User>().To<Presentation>()
        .Map(user => user.Name, p => p.UserName);
        
The other overload allows you for more complex mapping:

    DefineMap.From<User>().To<Presentation>()
        .Map(user => user.Name, (p, name) => p.UserName = name);
        
This is a general-purpose implementation of the first overload. It allows you to do things like call methods:

    DefineMap.From<User>().To<Presentation>()
        .Map(user => user.Name, (p, name) => p.SetName(name));
        
The second overload is slightly harder to read but is required sometimes. In most cases, it also boosts performance.

## MapMany
What about one-to-many mappings or many-to-many mappings? `MapMany` makes this possible.

Imagine you wanted to convert a `List<User>` to a `List<Presentation>`. Assuming you've already created a mapping from a `User` to a `Presentation`, the following code can easily accomplish this:

    DefineMap.From<List<User>>().To<List<Presentation>>()
        .MapMany(users => users, (user, ps) => ps.Add(Map.From<User>(user).To<Presentation>()));
        
The first lambda is retrieving a collection from the source. In this case, the source *is* the collection! The second lambda is simply converting the current `User` to a `Presentation` and adding it to the `List<Presentation>`.

This code can be improved by setting the `Presentation` list's capacity up-front:

    DefineMap.From<List<User>>().To<List<Presentation>>()
        .Construct(users => new List<Presentation>(users.Count))
        .MapMany(users => users, (user, ps) => ps.Add(Map.From<User>(user).To<Presentation>()));
        
There are even overloads that would make it possible to map from a `List<User>` to a `Presentation[]`:

    DefineMap.From<List<User>>().To<Presentation[]>()
        .Construct(users => new Presentation[users.Count])
        .MapMany(users => users, (user, ps, index) => ps[index] = Map.From<User>(user).To<Presentation>());
        
There are also overloads of `MapMany` that will map so long as a condition holds true. For instance, here's some code that will map from a `SqlDataReader` to a `List<User>`.

    DefineMap.From<SqlDataReader>().To<List<User>>()
        .MapMany(reader => reader.Read(), (reader, users) => users.Add(Map.From<IDataRecord>(reader).To<User>()));
        
    DefineMap.From<IDataRecord>().To<User>()
        .Map(record => record.GetInt32(0), user => user.Id)
        .Map(record => record.GetString(1), user => user.Name)
        .Map(record => record.GetDateTime(2), user => user.Created);
        
There are two important things that note from the previous example. First, notice that the mapping from `IDataRecord` to `User` is defined after the mapping from `SqlDataReader` to `List<User>`. Even though the first definition depends on the second, they can be defined in any order. Just make sure you have everything defined *before* you map!

The second thing to notice is that when mapping `IDataRecord` to `User`, I provided the generic argument to `From<T>`. Normally, specifying the generic argument to `From<T>` is optional because it can be infered from the source object. However, in this case, it is **required**! If I didn't provide it, an error would have occurred because there is no mapping from `SqlDataReader` to `User`. Technically, I could have used `SqlDataReader` instead of `IDataRecord` when defining the second mapping; however, restricting the interface makes this method more reusable and prevents access to `IDataReader`-only methods. The thing to take away from all this is that you should *always* explicitly specify the generic arguments.

