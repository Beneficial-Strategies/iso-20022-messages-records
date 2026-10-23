using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BeneficialStrategies.Iso20022.Messages.Records.Generator;

sealed class Model
{
    readonly IAssemblySymbol _asm;
    readonly Dictionary<string, (string Summary, string Remarks)> _docs;
    readonly HashSet<INamedTypeSymbol> _types;
    readonly Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> _variants = new(SymbolEqualityComparer.Default);

    public Model(Compilation c, IAssemblySymbol asm, List<INamedTypeSymbol> types, Dictionary<string, (string, string)> docs)
    {
        _asm = asm; _docs = docs; _types = new HashSet<INamedTypeSymbol>(types, SymbolEqualityComparer.Default);
        foreach (var t in types)
            if (Kind(t) == K.Variant)
                foreach (var b in t.Interfaces)
                    if (Kind(b) == K.Choice) { if (!_variants.TryGetValue(b, out var l)) _variants[b] = l = new(); l.Add(t); }
    }

    enum K { Other, Message, Component, Choice, Variant }
    static K Kind(INamedTypeSymbol t)
    {
        var names = new HashSet<string>(t.AllInterfaces.Select(i => i.Name));
        if (names.Contains("IIsoChoiceVariant")) return K.Variant;
        if (names.Contains("IIsoChoice")) return K.Choice;
        if (names.Contains("IIsoComponent")) return K.Component;
        if (names.Contains("IOuterRecord")) return K.Message;
        return K.Other;
    }

    bool IsModel(ITypeSymbol t) => t is INamedTypeSymbol n && n.TypeKind == TypeKind.Interface && SymbolEqualityComparer.Default.Equals(n.ContainingAssembly, _asm);

    // the record for a model interface
    string RecordName(INamedTypeSymbol i)
    {
        var name = i.Name.Substring(1) + (Kind(i) == K.Choice ? "_" : "");
        return "global::" + i.ContainingNamespace.ToDisplayString() + "." + name;
    }

    static bool Compiler(AttributeData a)
    {
        var ns = a.AttributeClass!.ContainingNamespace.ToDisplayString();
        return ns == "System.Runtime.CompilerServices" || ns == "Microsoft.CodeAnalysis";
    }

    static string Q(string s) => "@\"" + s.Replace("\"", "\"\"") + "\"";

    string TypeText(ITypeSymbol t, bool asRecord)
    {
        if (t is INamedTypeSymbol n)
        {
            if (n.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return TypeText(n.TypeArguments[0], asRecord) + "?";
            if (asRecord && IsModel(n)) return RecordName(n);
            if (n.IsGenericType)
                return "global::" + n.ContainingNamespace.ToDisplayString() + "." + n.Name + "<" + string.Join(", ", n.TypeArguments.Select(a => TypeText(a, asRecord))) + ">";
        }
        return t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
    }

    string Constant(TypedConstant c)
    {
        if (c.Kind == TypedConstantKind.Enum)
        {
            var et = (INamedTypeSymbol)c.Type!;
            foreach (var f in et.GetMembers().OfType<IFieldSymbol>())
                if (f.HasConstantValue && Equals(f.ConstantValue, c.Value)) return "global::" + et.ToDisplayString() + "." + f.Name;
            return "(" + et.ToDisplayString() + ")" + c.Value;
        }
        if (c.Kind == TypedConstantKind.Type) return "typeof(" + ((ITypeSymbol)c.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")";
        if (c.Value is string s) return Q(s);
        if (c.Value is bool b) return b ? "true" : "false";
        return Convert.ToString(c.Value, System.Globalization.CultureInfo.InvariantCulture)!;
    }

    string Attribute(AttributeData a, string? rename = null)
    {
        var cls = a.AttributeClass!;
        var name = rename ?? ("global::" + cls.ContainingNamespace.ToDisplayString() + "." + cls.Name.Replace("Attribute", ""));
        var args = a.ConstructorArguments.Select(Constant).Concat(a.NamedArguments.Select(n => n.Key + " = " + Constant(n.Value))).ToList();
        return "[" + name + (args.Count > 0 ? "(" + string.Join(", ", args) + ")" : "") + "]";
    }

    static void Doc(StringBuilder sb, string indent, (string Summary, string Remarks) d)
    {
        // The ISO model has no description for a few types and members: the interface then carries an empty summary, and so does the record,
        // because every public member must have a documentation comment.
        if (d.Summary.Length == 0) sb.Append(indent).AppendLine("/// <summary>").Append(indent).AppendLine("/// </summary>");
        else { sb.Append(indent).AppendLine("/// <summary>"); foreach (var l in d.Summary.Split('\n')) sb.Append(indent).Append("/// ").AppendLine(l.TrimEnd('\r').Trim()); sb.Append(indent).AppendLine("/// </summary>"); }
        if (d.Remarks.Length > 0) { sb.Append(indent).AppendLine("/// <remarks>"); foreach (var l in d.Remarks.Split('\n')) sb.Append(indent).Append("/// ").AppendLine(l.TrimEnd('\r').Trim()); sb.Append(indent).AppendLine("/// </remarks>"); }
    }

    (string, string) DocOf(string id) => _docs.TryGetValue(id, out var d) ? d : ("", "");

    public string? Write(INamedTypeSymbol i)
    {
        var kind = Kind(i);
        if (kind == K.Other) return null;
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>Written by the record generator from the interfaces of iso-20022-messages; do not edit.</auto-generated>");
        sb.AppendLine("#nullable enable");
        var ns = i.ContainingNamespace.ToDisplayString();
        sb.Append("namespace ").Append(ns).AppendLine(";").AppendLine();
        var typeName = i.Name.Substring(1) + (kind == K.Choice ? "_" : "");
        Doc(sb, "", DocOf("T:" + ns + "." + i.Name));

        // type attributes
        var typeAttrs = new List<string>();
        foreach (var a in i.GetAttributes())
        {
            var n = a.AttributeClass!.Name;
            if (Compiler(a)) continue;
            if (n == "IsoDisplayNameAttribute") typeAttrs.Add("[global::System.ComponentModel.DisplayName(" + Constant(a.ConstructorArguments[0]) + ")]");
            else if (n == "IsoMessageAttribute") { }
            else typeAttrs.Add(Attribute(a));
        }
        if (kind == K.Choice && _variants.TryGetValue(i, out var vs))
            foreach (var v in vs.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                var vt = "global::" + v.ContainingNamespace.ToDisplayString() + "." + v.Name.Substring(1);
                typeAttrs.Add("[global::System.Runtime.Serialization.KnownType(typeof(" + vt + "))]");
            }
        if (kind == K.Choice && _variants.TryGetValue(i, out var vs2))
            foreach (var v in vs2.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                var vt = "global::" + v.ContainingNamespace.ToDisplayString() + "." + v.Name.Substring(1);
                typeAttrs.Add("[global::System.Text.Json.Serialization.JsonDerivedType(typeof(" + vt + "), \"" + v.Name.Substring(1) + "\")]");
            }
        foreach (var a in typeAttrs) sb.AppendLine(a);

        // declaration
        string baseList;
        if (kind == K.Variant)
        {
            var choice = i.Interfaces.First(b => Kind(b) == K.Choice);
            baseList = RecordName(choice) + ", " + "global::" + ns + "." + i.Name;
        }
        else baseList = "global::" + ns + "." + i.Name;
        sb.Append(kind == K.Choice ? "public abstract partial record " : "public partial record ").Append(typeName).Append(" : ").AppendLine(baseList);
        sb.AppendLine("{");

        if (kind == K.Message)
        {
            var im = i.GetAttributes().First(a => a.AttributeClass!.Name == "IsoMessageAttribute");
            var id = (string)im.ConstructorArguments[0].Value!; var tag = (string)im.ConstructorArguments[1].Value!; var dns = (string)im.ConstructorArguments[2].Value!;
            var elementName = "Document";
            foreach (var na in im.NamedArguments) if (na.Key == "DocumentElementName") elementName = (string)na.Value.Value!;
            sb.AppendLine("    /// <summary>\n    /// The official ISO 20022 designation for this version of this message.\n    /// </summary>").AppendLine("    public const string IsoIdentifier = " + Q(id) + ";").AppendLine();
            sb.AppendLine("    /// <summary>\n    /// The ISO specified XML tag that should be used for standardized serialization of this message.\n    /// </summary>").AppendLine("    public const string XmlTag = " + Q(tag) + ";").AppendLine();
            sb.AppendLine("    /// <summary>\n    /// The ISO specified XML namespace that should be used for standardized serialization of this message type.\n    /// </summary>").AppendLine("    public const string DocumentNamespace = " + Q(dns) + ";").AppendLine();
            sb.AppendLine("    /// <summary>\n    /// The ISO specified XML element name that must surround the inner content to achieve standardized serialization.\n    /// </summary>").AppendLine("    public const string DocumentElementName = " + Q(elementName) + ";").AppendLine();
            sb.AppendLine("    /// <summary>\n    /// The XML namespace in which this message is delivered.\n    /// </summary>").AppendLine("    public static string IsoXmlNamspace => DocumentNamespace;").AppendLine();
        }

        var explicitImpls = new List<string>();
        foreach (var p in i.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic))
        {
            Doc(sb, "    ", DocOf("P:" + ns + "." + i.Name + "." + p.Name));
            string? listKind = null;
            foreach (var a in p.GetAttributes())
            {
                if (Compiler(a)) continue;
                if (a.AttributeClass!.Name == "IsoListAttribute") { listKind = a.ConstructorArguments[0].Value is int v && v == 1 ? "Simple" : "Value"; continue; }
                sb.Append("    ").AppendLine(Attribute(a));
            }
            var pt = p.Type;
            var ifaceTypeText = TypeText(pt, false) + (pt.IsReferenceType && p.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "");
            bool isList = pt is INamedTypeSymbol ln && ln.IsGenericType && ln.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";
            if (isList)
            {
                var elem = ((INamedTypeSymbol)pt).TypeArguments[0];
                var listType = (listKind == "Simple" ? "global::BeneficialStrategies.Iso20022.SimpleValueList<" : "global::BeneficialStrategies.Iso20022.ValueList<") + TypeText(elem, true) + ">";
                sb.Append("    public ").Append(listType).Append(' ').Append(p.Name).AppendLine(" { get; init; } = [];").AppendLine();
                explicitImpls.Add(TypeText(pt, false) + " " + ns.Replace("global::", "") + "." + i.Name + "." + p.Name + " => " + p.Name + ";");
            }
            else
            {
                var isNullable = pt.IsReferenceType ? p.NullableAnnotation == NullableAnnotation.Annotated : pt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
                var recText = TypeText(pt, true) + (pt.IsReferenceType && isNullable ? "?" : "");
                if (!isNullable && recText == "decimal" && p.GetAttributes().Any(a => a.AttributeClass!.Name == "IsoAmountAttribute"))
                {
                    // the amount facets (digits) are checked when the value is set
                    var field = "_" + char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1);
                    sb.Append("    public required decimal ").AppendLine(p.Name).AppendLine("    {");
                    sb.Append("        get => ").Append(field).AppendLine(";");
                    sb.Append("        init => ").Append(field).Append(" = global::BeneficialStrategies.Iso20022.Framework.IsoAmountFacets.Check(typeof(").Append(typeName).Append("), nameof(").Append(p.Name).AppendLine("), value);");
                    sb.AppendLine("    }").AppendLine();
                    sb.Append("    private decimal ").Append(field).AppendLine(";").AppendLine();
                }
                else
                sb.Append("    public ").Append(isNullable ? "" : "required ").Append(recText).Append(' ').Append(p.Name).AppendLine(" { get; init; }").AppendLine();
                if (recText != ifaceTypeText) explicitImpls.Add(ifaceTypeText + " " + ns + "." + i.Name + "." + p.Name + " => " + p.Name + ";");
            }
        }
        foreach (var e in explicitImpls)
        {
            // "T global::NS.IName.Prop => Prop;"  (explicit interface implementation)
            sb.Append("    ").AppendLine(e);
        }
        sb.AppendLine("}");
        return sb.ToString();
    }
}
