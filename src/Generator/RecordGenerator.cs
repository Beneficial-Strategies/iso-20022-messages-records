using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BeneficialStrategies.Iso20022.Messages.Records.Generator;

/// <summary>
/// Writes the records of the model from the interfaces of the referenced BeneficialStrategies.Iso20022.Messages assembly
/// (its metadata only) and its XML documentation file, which the project passes as an additional file.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class RecordGenerator : IIncrementalGenerator
{
    const string InterfacesAssembly = "BeneficialStrategies.Iso20022.Messages";
    const string Root = "BeneficialStrategies.Iso20022";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var xml = context.AdditionalTextsProvider
            .Where(t => t.Path.EndsWith("BeneficialStrategies.Iso20022.Messages.xml"))
            .Select((t, ct) => t.GetText(ct)?.ToString() ?? "")
            .Collect();
        context.RegisterSourceOutput(context.CompilationProvider.Combine(xml), (spc, pair) => Emit(spc, pair.Left, pair.Right));
    }

    static void Emit(SourceProductionContext spc, Compilation compilation, ImmutableArray<string> xml)
    {
        IAssemblySymbol? asm = null;
        foreach (var r in compilation.References)
            if (compilation.GetAssemblyOrModuleSymbol(r) is IAssemblySymbol a && a.Name == InterfacesAssembly) { asm = a; break; }
        if (asm == null) return;

        var docs = new Dictionary<string, (string Summary, string Remarks)>();
        if (xml.Length > 0 && xml[0].Length > 0)
            foreach (var m in XDocument.Parse(xml[0]).Descendants("member"))
            {
                var name = (string?)m.Attribute("name"); if (name == null) continue;
                docs[name] = (Inner(m.Element("summary")), Inner(m.Element("remarks")));
            }

        var types = new List<INamedTypeSymbol>();
        Collect(asm.GlobalNamespace, types);
        var model = new Model(compilation, asm, types, docs);
        foreach (var t in types)
        {
            var text = model.Write(t);
            if (text != null) spc.AddSource((t.ContainingNamespace.ToDisplayString() + "." + t.Name).Replace("<", "_") + ".g.cs", SourceText.From(text, Encoding.UTF8));
        }
    }

    static string Inner(XElement? e)
    {
        if (e == null) return "";
        var sb = new StringBuilder();
        foreach (var n in e.Nodes()) sb.Append(n.ToString());
        return sb.ToString().Trim('\r', '\n');
    }

    static void Collect(INamespaceSymbol ns, List<INamedTypeSymbol> into)
    {
        foreach (var m in ns.GetMembers())
        {
            if (m is INamespaceSymbol child) Collect(child, into);
            else if (m is INamedTypeSymbol t && t.TypeKind == TypeKind.Interface && t.DeclaredAccessibility == Accessibility.Public) into.Add(t);
        }
    }
}
