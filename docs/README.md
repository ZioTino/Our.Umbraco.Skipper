# ![Skipper](/assets/icon-sm.png?raw=true) Our.Umbraco.Skipper
An Umbraco package that lets you specify nodes and document types that will be excluded from Umbraco's URL generation.

This project is exclusively for Umbraco 9+. This project is a port for v9, taken from the [original Umbraco-VirtualNodes](https://github.com/sotirisf/Umbraco-VirtualNodes) by [sotirisf](https://github.com/sotirisf).

## Installation
```
dotnet add package Our.Umbraco.Skipper
dotnet build
dotnet run
```

## Multiple skip support
Our.Umbraco.Skipper can work no problem with multiple nodes to be skipped, both one following another or not.  
Some examples (between parentheses the URL segment):
```
root (en)
    products (products)
        category (category) <-- this is Skipper's work
            product1 (product1)
            product2 (product2)
            product3 (product3)
```
Product URLs will be for example `/en/products/product1`.  
Skipper's work URLs **without** [Hide Skipper's work](#hide-skippers-work):
- Category: `/en/products/category`

Skipper's work URLs **with** [Hide Skipper's work](#hide-skippers-work):
- Category: **No URL**
```
root (en)
    services (services) <-- this is Skipper's work
        products (products)
            category (category) <-- this is Skipper's work
                product1 (product1)
                product2 (product2)
                product3 (product3)
```
Product URLs will be for example `/en/products/product2`.
Skipper's work URLs **without** [Hide Skipper's work](#hide-skippers-work):
- Services: `/en/services`
- Category: `/en/products/category`
  
Skipper's work URLs **with** [Hide Skipper's work](#hide-skippers-work):
- Services: **No URL**
- Category: **No URL**
```
root (en)
    services (services) <-- this is Skipper's work
        products (products) <-- this is Skipper's work
            category (category)
                product1 (product1)
                product2 (product2)
                product3 (product3)
```
Product URLs will be for example `/en/category/product3`.
Skipper's work URLs **without** [Hide Skipper's work](#hide-skippers-work):
- Services: `/en/services`
- Category: `/en/products`
  
Skipper's work URLs **with** [Hide Skipper's work](#hide-skippers-work):
- Services: **No URL**
- Category: **No URL**


## Configuration
Skipper's configuration uses for the most part the new .NET 5 IConfiguration, so we will modify for the most part the file appsettings.json.
Configuration will work between environments, so if you want to change some options for development, you can do it.

### Root configuration node
The main configuration node is inside Umbraco's configuration (at the same level of CMS configuration, for reference):
```json
{
    "$schema": "",
    "Umbraco": {
        "Skipper": {
            // Skipper's config goes here
        }
    }
}
```

### Aliases
Same process of [sotirisf](https://github.com/sotirisf)'s old v8: you specify doc type aliases in the configuration, and all nodes will be treated by Skipper:
```json
{
    "$schema": "",
    "Umbraco": {
        "Skipper": {
            "Aliases": [
                "myDocTypeAlias1",
                "myDocTypeAlias2"
            ]
        }
    }
}
```

### Hide Skipper's work
By default Skipper will skip the specified segment URL for the node's children, but for the node itself it will still generate the URL.
You can force Skipper to return 404 for all nodes, this way:
```json
{
    "$schema": "",
    "Umbraco": {
        "Skipper": {
            "HideSkipperWork": true
        }
    }
}
```

## Additional options
To prevent infinite looping, I've added a check that after 50 (by default) iterations, while loops will break automatically. If you have some troubles on large-scale websites, you might need to increment this value:
```json
{
    "$schema": "",
    "Umbraco": {
        "Skipper": {
            "WhileLoopMaxCount": 50
        }
    }
}
```

## Reserved property aliases
Working the same way as specified in the [Special Property Type aliases for routing](https://our.umbraco.com/documentation/reference/routing/routing-properties), i've added some property aliases configuration to granularly control each node.
### umbracoUrlSkipper
Creating a property alias with this name and using a True/False property editor lets you control for the node if it should be controller by Skipper or not.
### umbracoHideSkipperWork
Creating a property alias with this name and using a True/False property editor lets you control for the node (if it's being controlled by Skipper) if the node should be hidden (and no URL will be generated) or not.

## Duplicates check
Consider the following example:
```
articles
    skipperWork1
        article1
        article2
    skipperWork2
```
Supposing that `skipperWork1` and `skipperWork2` are Skipper's work, the path for article1 will be `articles/article1`. But what if we add a new article named article1 under `skipperWork2`?  
Our.Umbraco.Skipper checks nodes on save and changes their names accordingly to protect your URLs from this.  
So, if you saved a new node named `article1` under `skipperWork2` it would become:
```
articles
    skipperWork1
        article1
        article2
    skipperWork2
        article1 (1) 
```
And then if you create another node named `article1` back under `skipperWork1` it would become `article1 (2)`:
```
articles
    skipperWork1
        article1
        article2
        article1 (2)
    skipperWork2
        article1 (1) 
```
This ensures that there will be no duplicate URLs.
Diffrently from [sotirisf](https://github.com/sotirisf)'s old v8, **Our.Umbraco.Skipper will check for ALL Skipper's work nodes**, so you don't have to worry even if the nodes are on different levels but reference to the same root node.