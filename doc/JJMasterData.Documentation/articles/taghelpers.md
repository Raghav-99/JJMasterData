# TagHelpers

TagHelpers are a simple way to re-use HTML components. <br>
JJMasterData have some built-in TagHelpers.<br>
<br>
The big advantage is to encapsulate the "bootstrap" components, 
so the html code is generated according to the parameterized version of bootstrap.
With this we hope to eliminate the pain of updating bootstrap versions.

## Usage
At `_ViewImports.cshtml`
```csharp
@addTagHelper *, JJMasterData.Web
```
At your Razor page

### Example 1
```html
<jj-formview element-name="ElementName"/>
```
In the example above the html of an entire CRUD is generated at runtime, 
where "ElementName" is the name of the preconfigured data dictionary.

<br>

### Example 2
```html
 <jj-title title="My title" subtitle="Foo"/>
```
In the example above the following HTML code is generated
```html
<h1>
    My Title
    <small>Foo</small>
</h1>
```

<br>

You can check all available TagHelpers at our [API Reference](https://portal.jjconsulting.com.br/jjdoc/lib/JJMasterData.Web.TagHelpers.html)
