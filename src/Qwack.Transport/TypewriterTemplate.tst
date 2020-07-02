${
    // Enable extension methods by adding using Typewriter.Extensions.*
    using Typewriter.Extensions.Types;

    // Uncomment the constructor to change template settings.
    //Template(Settings settings)
    //{
    //    settings.IncludeProject("Project.Name");
    //    settings.OutputExtension = ".tsx";
    //}

    // Custom extension methods can be used in the template by adding a $ prefix e.g. $LoudName
    string LoudName(Property property)
    {
        return property.Name.ToUpperInvariant();
    }

    Template(Settings settings)
    {
        settings.OutputFilenameFactory = (file) => {
            if (file.Classes.Any()){
                var className = file.Classes.First().Name;
                return $"GeneratedTypeScript\\{className}.ts";
            }
            if (file.Enums.Any()){
                var className = file.Enums.First().Name;
                return $"GeneratedTypeScript\\{className}.ts";
            }
            return file.Name;
        };
    }

    string Imports(Class c){
                var imports = c.Properties
                                .Where(p=>!p.Type.IsPrimitive || p.Type.IsEnum)
                                .Where(p=>p.Type.Name!="T")
                                .Select(p=> $"import {{ {StripGenericArguement(CleanupName(p.Type.Name))} }} from './{StripGenericArguement(CleanupName(p.Type.Name))}';");
                var importssGeneric = c.Properties
                                .Where(x=>x.Type.IsGeneric)                
                                .SelectMany(x=>x.Type.TypeArguments)
                                .Where(x=>!x.IsPrimitive)
                                .Where(p=>p.Name!="T")
                                .Select(p=> $"import {{ {StripGenericArguement(CleanupName(p.Name))} }} from './{StripGenericArguement(CleanupName(p.Name))}';");
                return imports.Concat(importssGeneric)
                                .Distinct()
                                .Concat(new [] {c.BaseClass!=null?$"import {{ {CleanupName(c.BaseClass.Name)} }} from './{CleanupName(c.BaseClass.Name)}';":null})
                                .Where(x=>x!=null && !x.StartsWith("import { T }") && !x.StartsWith("import { number }") && !x.StartsWith("import { string }"))
                                .Aggregate("", (all,import) => $"{all}{import}\r\n")
                                .TrimStart();
    }
   

    string CustomProperties(Class c) => c.Properties
                                        .Select(p=> $"\t {p.name}: {CleanupName(p.Type.Name, false)};")
                                        .Aggregate("", (all,prop) => $"{all}{prop}\r\n")
                                        .TrimEnd();

    string ClassName(Class c) => c.Name + (c.IsGeneric ? "<T>" : "" );

    string Inherits(Class c) => c.BaseClass != null && !string.IsNullOrWhiteSpace(c.BaseClass.Name) ?" extends " + c.BaseClass.Name:"";

    string CleanupName(string propertyName, bool? removeArray = true){
        if (removeArray.HasValue && removeArray.Value) {
            propertyName = propertyName.Replace("[]","");
        }
        return propertyName;
    }

    string StripGenericArguement(string type) {
        if(!type.Contains("<") || !type.Contains(">"))
            return type;
        var ix1 = type.IndexOf('<');
        var ix2 = type.IndexOf('>') + 1;
        return type.Remove(ix1,ix2-ix1);
    }
}
$Classes(*)[$Imports
export interface $ClassName $Inherits {
$CustomProperties
}]
$Enums(*)[export enum $Name { $Values[
    $name = $Value][,]
}]

