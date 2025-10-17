using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Fengb3.EasyCodeBuilder;
using Microsoft.CodeAnalysis;

namespace Docker.Dotnet.UI.SourceGenerator;

[Generator]
public class CsvImmutableDataSourceGenerator : IIncrementalGenerator
{
    public const string FileSuffix = ".table.csv";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.AdditionalTextsProvider
            .Where(f => Path.GetFileName(f.Path).EndsWith(FileSuffix))
            .Collect();

        context.RegisterSourceOutput(provider, GenerateCode);
    }

    private void GenerateCode(SourceProductionContext context, ImmutableArray<AdditionalText> files)
    {
        foreach (var file in files)
        {
            GenerateCodeForSingleFile(context, file);
        }
    }

    private void GenerateCodeForSingleFile(SourceProductionContext context, AdditionalText file)
    {
        var fileName = Path.GetFileName(file.Path);
        var className = Path.GetFileNameWithoutExtension(fileName.Replace(FileSuffix, ""));

        var text = file.GetText(context.CancellationToken)?.ToString();
        if (string.IsNullOrEmpty(text))
            return;

        var lineSeparators = new string[] { "\n", "\r\n", "\r" };

        var lines = file.GetText(context.CancellationToken)?.ToString().Split(
            lineSeparators,
            StringSplitOptions.None
        );

        // first line is header
        var headers = lines?[0].Split(',');
        if (headers == null || headers.Length < 2)
            return;

        var properties = new List<PropertyInfo>();
        for (var i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            if (string.IsNullOrEmpty(header)) continue;

            if (header.Contains("#")) continue; // skip

            var propertyInfo = new PropertyInfo();

            var cuts = new string[2];
            var trimmedHeader = header;

            // starting with [PK] is Primary Key
            if (header.StartsWith("[Key]"))
            {
                propertyInfo.IsPrimaryKey = true;
                trimmedHeader = header.Substring(5); // remove [PK]
            }

            cuts = trimmedHeader.Split(':');
            propertyInfo.TypeName = cuts[1];
            propertyInfo.csvName = cuts[1];
            propertyInfo.Name = ToUpperCamelCase(cuts[0]);
            propertyInfo.Sequence = i;

            properties.Add(propertyInfo);
        }

        // data begins from second line
        var dataValues = new List<Dictionary<string, string>>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;
            
            if(line.StartsWith("#")) continue; // skip comment line

            var values = line.Split(',');
            var dataItem = new Dictionary<string, string>();
            foreach (var propertyInfo in properties)
            {
                var value = values.Length > propertyInfo.Sequence ? values[propertyInfo.Sequence] : "";
                dataItem[propertyInfo.Name] = value;
            }

            dataValues.Add(dataItem);
        }

        var cb = new CSharpCodeBuilder();

        cb.Using(
            "System",
            "System.Collections.Generic"
        );

        cb.Class(
            className, cls =>
            {
                cls.Class("Data", innerCls =>
                {
                    foreach (var propertyInfo in properties)
                    {
                        innerCls.Property(propertyInfo.TypeName, propertyInfo.Name);
                    }

                    innerCls.Constructor("Data", ctor =>
                    {
                        foreach (var propertyInfo in properties)
                        {
                            ctor.AppendLine($"this.{propertyInfo.Name} = {ToLowerCamelCase(propertyInfo.Name)};");
                        }
                    }, string.Join(", ", properties.Select(p => $"{p.TypeName} {ToLowerCamelCase(p.Name)}").ToArray()));
                });
                
                cls.Property($"Dictionary<{properties.FirstOrDefault(p => p.IsPrimaryKey)?.TypeName ?? "object"}, Data>", "Items", "public static");

                cls.Constructor(className, ctor =>
                    {
                        ctor <<= $"Items = new({dataValues.Count});";
                        var pkIsString = properties.FirstOrDefault(p => p.IsPrimaryKey)?.TypeName == "string";
                        foreach (var dataValue in dataValues)
                        {
                            var keyValue = dataValue[properties.FirstOrDefault(p => p.IsPrimaryKey)?.Name ?? "null"];
                            if (pkIsString)
                            {
                                keyValue = $"@\"{keyValue.Replace("\"", "\"\"")}\"";
                            }
                            ctor <<= $"Items.Add({keyValue}, new Data(" +
                                string.Join(", ", properties.Select(p => 
                                    {
                                        var val = dataValue[p.Name];
                                        if (p.TypeName == "string")
                                        {
                                            return $"@\"{val.Replace("\"", "\"\"")}\"";
                                        }
                                        else if (p.TypeName == "char")
                                        {
                                            return $"'{val}'";
                                        }
                                        else if (p.TypeName == "bool")
                                        {
                                            return (val.ToLower() == "true" || val == "1") ? "true" : "false";
                                        }
                                        else if (string.IsNullOrEmpty(val))
                                        {
                                            return "default";
                                        }
                                        else
                                        {
                                            return val;
                                        }
                                    })
                                )
                            + "));";
                        }
                    }, "",
                    "static"
                );
            },
            "public static"
        );

        context.AddSource($"{fileName}.g.cs", cb.ToString());
    }

    public string ToUpperCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(new char[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
        }

        return string.Join("", words);
    }

    public string ToLowerCamelCase(string input)
    {
        var words = input.Split(new char[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = char.ToLower(words[i][0]) + words[i].Substring(1).ToLower();
        }

        return string.Join("", words);
    }
}

public class PropertyInfo
{
    public string csvName { get; set; }= string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; }= string.Empty;
    public bool IsPrimaryKey { get; set; }
    public int Sequence { get; set; }
}