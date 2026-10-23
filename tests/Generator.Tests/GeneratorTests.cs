using System.Reflection;
using BeneficialStrategies.Iso20022.Messages.Records.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace BeneficialStrategies.Iso20022.Messages.Records.Generator.Tests;

/// <summary>
/// The generator needs no mocks: a tiny Messages assembly is compiled in memory, the generator is run on a compilation
/// that references it (CSharpGeneratorDriver), and the output is checked as text and by compiling and reflecting on it.
/// </summary>
public class GeneratorTests
{
    static readonly string PrimitivesDll = typeof(BeneficialStrategies.Iso20022.SimpleTypes.Max35Text).Assembly.Location;

    static byte[] LastInterfaces;

    static IEnumerable<MetadataReference> BclReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is "System.Private.CoreLib.dll" or "System.Runtime.dll" or "System.Collections.dll" or "System.Linq.dll" or "System.ComponentModel.Primitives.dll" or "System.Runtime.Serialization.Primitives.dll" or "System.Text.Json.dll" or "netstandard.dll" or "System.ComponentModel.Annotations.dll" or "System.Memory.dll" or "System.Runtime.Extensions.dll")
            .Select(p => MetadataReference.CreateFromFile(p));

    /// <summary>Compiles interface source into an assembly named like the real Interfaces assembly.</summary>
    static MetadataReference Interfaces(string source, out string xml)
    {
        var tree = CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; global using BeneficialStrategies.Iso20022.Framework; global using BeneficialStrategies.Iso20022.SimpleTypes; global using BeneficialStrategies.Iso20022.Metadata;\n" + source);
        var comp = CSharpCompilation.Create("BeneficialStrategies.Iso20022.Messages", new[] { tree },
            BclReferences().Append(MetadataReference.CreateFromFile(PrimitivesDll)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var ms = new MemoryStream();
        var r = comp.Emit(ms);
        Assert.True(r.Success, string.Join("\n", r.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        xml = "<doc><members></members></doc>";
        LastInterfaces = ms.ToArray();
        return MetadataReference.CreateFromImage(LastInterfaces);
    }

    static (string[] Sources, Compilation Output, IEnumerable<Diagnostic> Diagnostics) Run(string interfaceSource, string docsXml = null)
    {
        var iface = Interfaces(interfaceSource, out var xml);
        var comp = CSharpCompilation.Create("BeneficialStrategies.Iso20022.Messages.Records", new SyntaxTree[0],
            BclReferences().Append(iface).Append(MetadataReference.CreateFromFile(PrimitivesDll)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var driver = CSharpGeneratorDriver.Create(new RecordGenerator().AsSourceGenerator())
            .AddAdditionalTexts(System.Collections.Immutable.ImmutableArray.Create<AdditionalText>(new Doc("BeneficialStrategies.Iso20022.Messages.xml", docsXml ?? xml)));
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(comp, out var output, out var diags);
        var sources = driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()).ToArray();
        return (sources, output, diags.Concat(output.GetDiagnostics()));
    }

    sealed class Doc : AdditionalText
    {
        readonly string _t; public Doc(string path, string text) { Path = path; _t = text; }
        public override string Path { get; }
        public override SourceText GetText(CancellationToken ct = default) => SourceText.From(_t);
    }

    const string Component = @"
namespace BeneficialStrategies.Iso20022.Components {
  [IsoId(""c1"")] [IsoDisplayName(""Party Thing 1"")]
  public interface IPartyThing1 : IIsoComponent {
    [IsoXmlTag(""Nm"")] Max35Text Name { get; }
    [IsoXmlTag(""Id"")] Max35Text? Id { get; }
    [IsoXmlTag(""Kid"")] IPartyThing1? Child { get; }
    [IsoList(IsoListKind.Value)] [IsoXmlTag(""Kids"")] IEnumerable<IPartyThing1> Children { get; }
    [IsoList(IsoListKind.SimpleValue)] [IsoXmlTag(""Tags"")] IEnumerable<Max35Text> Tags { get; }
  }
}";

    static string Text(string[] sources, string type) => sources.Single(s => s.Contains("record " + type + " "));

    [Fact]
    public void AComponent_BecomesARecordWithRequiredInitMembersAndListsThatStartEmpty()
    {
        var (sources, output, diags) = Run(Component);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        var t = Text(sources, "PartyThing1");
        Assert.Contains("public partial record PartyThing1 : global::BeneficialStrategies.Iso20022.Components.IPartyThing1", t);
        Assert.Contains("public required global::BeneficialStrategies.Iso20022.SimpleTypes.Max35Text Name { get; init; }", t);
        Assert.Contains("Max35Text? Id { get; init; }", t);                                      // nullable: not required
        Assert.Contains("ValueList<global::BeneficialStrategies.Iso20022.Components.PartyThing1> Children { get; init; } = [];", t);
        Assert.Contains("SimpleValueList<global::BeneficialStrategies.Iso20022.SimpleTypes.Max35Text> Tags { get; init; } = [];", t);
        Assert.Contains("[global::System.ComponentModel.DisplayName(@\"Party Thing 1\")]", t);   // from [IsoDisplayName]
        Assert.Contains("IPartyThing1 BeneficialStrategies.Iso20022.Components.IPartyThing1.Child => Child;", t.Replace("global::", "").Replace("IPartyThing1?", "IPartyThing1"));
    }

    [Fact]
    public void TheGeneratedAssembly_CompilesAndBehavesLikeARecordWithValueEquality()
    {
        var (_, output, diags) = Run(Component);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        using var ms = new MemoryStream();
        Assert.True(output.Emit(ms).Success);
        var interfacesBytes = LastInterfaces;
        var alc = new System.Runtime.Loader.AssemblyLoadContext("generated", isCollectible: true);
        alc.Resolving += (c, n) => n.Name == "BeneficialStrategies.Iso20022.Messages" ? c.LoadFromStream(new MemoryStream(interfacesBytes))
            : n.Name == "BeneficialStrategies.Iso20022.Primitives" ? c.LoadFromAssemblyPath(PrimitivesDll) : null;
        var asm = alc.LoadFromStream(new MemoryStream(ms.ToArray()));
        var type = asm.GetType("BeneficialStrategies.Iso20022.Components.PartyThing1");
        Assert.True(type != null, "type not found; types: " + string.Join(",", asm.GetTypes().Select(x => x.FullName)));
        var a = Activator.CreateInstance(type); var b = Activator.CreateInstance(type);
        Assert.Equal(a, b);                                   // two empty instances: lists compare by items
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(type.GetProperty("Children") != null, "no Children property; properties: " + string.Join(",", type.GetProperties().Select(x => x.Name)));
        Assert.True(type.GetProperty("Children").GetValue(a) != null, "Children is null");   // the list starts empty, not null
    }

    [Fact]
    public void AChoice_GetsItsVariantsAsKnownTypes_AndEachVariantDerivesFromIt()
    {
        var src = @"
namespace BeneficialStrategies.Iso20022.Choices {
  [IsoId(""ch1"")] [IsoDisplayName(""Reason 1 Choice"")] public interface IReason1Choice : IIsoChoice { }
}
namespace BeneficialStrategies.Iso20022.Choices.Reason1Choice {
  [IsoId(""v1"")] [IsoDisplayName(""Code"")] public interface ICode : IReason1Choice, IIsoChoiceVariant { [IsoXmlTag(""Cd"")] Max35Text Value { get; } }
  [IsoId(""v2"")] [IsoDisplayName(""Proprietary"")] public interface IProprietary : IReason1Choice, IIsoChoiceVariant { [IsoXmlTag(""Prtry"")] Max35Text Value { get; } }
}";
        var (sources, output, diags) = Run(src);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        var choice = Text(sources, "Reason1Choice_");
        Assert.Contains("public abstract partial record Reason1Choice_", choice);
        Assert.Contains("KnownType(typeof(global::BeneficialStrategies.Iso20022.Choices.Reason1Choice.Code))", choice);
        Assert.Contains("JsonDerivedType(typeof(global::BeneficialStrategies.Iso20022.Choices.Reason1Choice.Proprietary), \"Proprietary\")", choice);
        Assert.Contains("public partial record Code : global::BeneficialStrategies.Iso20022.Choices.Reason1Choice_, global::BeneficialStrategies.Iso20022.Choices.Reason1Choice.ICode", Text(sources, "Code"));
    }

    [Fact]
    public void AMessage_GetsItsConstantsFromTheIsoMessageAttribute()
    {
        var src = @"
namespace BeneficialStrategies.Iso20022.xmpl {
  [IsoId(""m1"")] [IsoDisplayName(""Example V01"")]
  [IsoMessage(""xmpl.001.001.01"", ""Xmpl"", ""urn:example:xmpl.001.001.01"", DocumentElementName = ""Envelope"")]
  public interface IExampleV01 : IOuterRecord { [IsoXmlTag(""Nm"")] Max35Text Name { get; } }
}";
        var (sources, _, diags) = Run(src);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        var t = Text(sources, "ExampleV01");
        Assert.Contains("public const string IsoIdentifier = @\"xmpl.001.001.01\";", t);
        Assert.Contains("public const string XmlTag = @\"Xmpl\";", t);
        Assert.Contains("public const string DocumentElementName = @\"Envelope\";", t);      // the nvlp case
    }

    [Fact]
    public void AnAmountMember_ChecksItsFacetsWhenSet()
    {
        var src = @"
namespace BeneficialStrategies.Iso20022.Components {
  [IsoId(""a1"")] [IsoDisplayName(""Amount Thing 1"")]
  public interface IAmountThing1 : IIsoComponent { [IsoXmlTag(""Amt"")] [IsoAmount(18, 5)] decimal Amount { get; } }
}";
        var (sources, _, diags) = Run(src);
        Assert.Empty(diags.Where(d => d.Severity == DiagnosticSeverity.Error));
        var t = Text(sources, "AmountThing1");
        Assert.Contains("get => _amount;", t);
        Assert.Contains("IsoAmountFacets.Check(typeof(AmountThing1), nameof(Amount), value)", t);
        Assert.Contains("private decimal _amount;", t);
    }

    [Fact]
    public void TheSameInputGivesTheSameOutput()
    {
        var a = Run(Component).Sources; var b = Run(Component).Sources;
        Assert.Equal(a, b);
    }

    [Fact]
    public void AMemberWithoutADescription_GetsAnEmptySummary_BecauseEveryPublicMemberNeedsAComment()
    {
        var t = Text(Run(Component).Sources, "PartyThing1");
        Assert.Contains("/// <summary>\n    /// </summary>".Replace("\n", Environment.NewLine), t);
    }

    [Fact]
    public void TheDocumentationOfAnInterfaceMember_IsCopiedToTheRecord()
    {
        var xml = "<doc><members><member name=\"P:BeneficialStrategies.Iso20022.Components.IPartyThing1.Name\"><summary>The name of the party.</summary><remarks>Usage: short.</remarks></member></members></doc>";
        var t = Text(Run(Component, xml).Sources, "PartyThing1");
        Assert.Contains("/// The name of the party.", t);
        Assert.Contains("/// Usage: short.", t);
    }
}
