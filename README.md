# TypeMapping
Configure and perform object-to-object mapping.

## In-Progress
I am currently thinking about where this project is going. Initially, I created `While` and `ForEach` functions for handle one-to-many relationships. Since then, I've decided I don't like this approach. The `ForEach` method is particularly confusing because there's no way to access the items as they're iterated.

I might instead think of something entirely different. Also, there's no way right now to build a single object from a collection. So, I wouldn't start using this project just yet.

## Overview
Software written for the modern world involves passing objects between layers. When crossing layers, objects' values are copied t other objects, often to avoid exposing implementation details across layers. An example might be creating presentation objects from data objects returned from the data layer. You might end up writing code that looks like this:

    UserPresentation up = new UserPresentation();
    up.UserName = user.Name;
    up.AccountType = user.Type;
    // ... 40 other properties ..
    
Writing this code can be tedious and might even need repeated in multiple places. The `TypeMapping` library aims to simplify mapping between objects by allowing you to configure a mapping in one place and refer back to it as many times as needed.

    DefineMap.From<User>().To<UserPresentation>()
        .Map(user => user.Name, up => up.UserName)
        .Map(user => user.Type, up => up.AccountType);
        
The code sample aove would be used to define a mapping from a `User` object to `UserPresentation` object. Once you have an actual `User` object, you can create a `UserPresentation` object simply by calling:

    UserPresentation presentation = Map.From<User>(user).To<UserPresentation>();
    
This code assumes that the given user name is not null and that the `UserPresentation` object is default constructible.    

## Construct
If your destination type is not default constructible, you have two options: you can pass the pre-constructed object to the `To<T>` method when mapping -or- you can call the `Construct` method.

    DefineMap.From<User>().To<UserPresentation>()
        .Construct(user => new UserPresentation())
        
## Assign
Technically, you can do a lot of initialization in the `Construct` method. However, someone might want to pass a pre-constructed object to the `To<T>` method. In that case, any custom logic in the `Construct` method will be skipped. If you plan on setting a lot of values to constant values, use the `Assign` method:

    DefineMap.From<User>().To<UserPresentation>()
        .Construct(user => new UserPresentation())
        .Assign(up => up.UserName = HttpContext.Current.User.Name) 
        .Assign(up => up.TimeStamp = DateTime.UtcNow());
