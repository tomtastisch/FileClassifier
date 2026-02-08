using System.Reflection;
using System.Text;
using Tomtastisch.FileClassifier;

namespace FileTypeDetectionLib.Tests.Contracts;

[Trait("Category", "ApiContract")]
public sealed class PublicApiContractSnapshotTests
{
    private static readonly string SnapshotPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Contracts", "public-api.snapshot.txt"));

    [Fact]
    public void PublicApiSurface_MatchesSnapshot()
    {
        var lines = BuildPublicApiLines();
        var current = string.Join('\n', lines) + '\n';

        if (Environment.GetEnvironmentVariable("UPDATE_PUBLIC_API_SNAPSHOT") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath)!);
            File.WriteAllText(SnapshotPath, current, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        var expected = File.ReadAllText(SnapshotPath).Replace("\r\n", "\n");
        Assert.Equal(expected, current);
    }

    private static string[] BuildPublicApiLines()
    {
        var assembly = typeof(FileTypeDetector).Assembly;
        var lines = new List<string>();

        var publicTypes = assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace == "Tomtastisch.FileClassifier")
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        foreach (var type in publicTypes)
        {
            lines.Add($"T:{GetTypeKind(type)} {FormatType(type)}");

            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                         .OrderBy(DescribeMethodBase, StringComparer.Ordinal))
            {
                lines.Add($"M:{FormatType(type)}.{DescribeMethodBase(ctor)}");
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                         .Where(method => method.DeclaringType == type && !method.IsSpecialName)
                         .OrderBy(DescribeMethodBase, StringComparer.Ordinal))
            {
                lines.Add($"M:{FormatType(type)}.{DescribeMethodBase(method)}:{FormatType(method.ReturnType)}");
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                         .Where(property => property.DeclaringType == type)
                         .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                lines.Add($"P:{FormatType(type)}.{property.Name}:{FormatType(property.PropertyType)}");
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                         .Where(field => field.DeclaringType == type && !field.IsSpecialName && field.Name != "value__")
                         .OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                lines.Add($"F:{FormatType(type)}.{field.Name}:{FormatType(field.FieldType)}");
            }

            foreach (var @event in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                         .Where(@event => @event.DeclaringType == type)
                         .OrderBy(@event => @event.Name, StringComparer.Ordinal))
            {
                lines.Add($"E:{FormatType(type)}.{@event.Name}:{FormatType(@event.EventHandlerType!)}");
            }

            if (type.IsEnum)
            {
                foreach (var name in Enum.GetNames(type).OrderBy(name => name, StringComparer.Ordinal))
                {
                    lines.Add($"V:{FormatType(type)}.{name}");
                }
            }
        }

        return lines.OrderBy(line => line, StringComparer.Ordinal).ToArray();
    }

    private static string DescribeMethodBase(MethodBase method)
    {
        var parameterTypes = method.GetParameters()
            .Select(parameter => FormatType(parameter.ParameterType))
            .ToArray();
        return $"{method.Name}({string.Join(",", parameterTypes)})";
    }

    private static string GetTypeKind(Type type)
    {
        if (type.IsEnum) return "enum";
        if (type.IsClass) return "class";
        if (type.IsValueType) return "struct";
        if (type.IsInterface) return "interface";
        return "type";
    }

    private static string FormatType(Type type)
    {
        if (type.IsGenericParameter) return type.Name;

        if (type.IsArray)
        {
            return $"{FormatType(type.GetElementType()!)}[]";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tickIndex = name.IndexOf('`');
        if (tickIndex >= 0)
        {
            name = name[..tickIndex];
        }

        var args = type.GetGenericArguments()
            .Select(FormatType)
            .ToArray();
        return $"{name}<{string.Join(",", args)}>";
    }
}
