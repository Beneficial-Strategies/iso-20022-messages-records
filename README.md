![Beneficial Strategies](https://raw.githubusercontent.com/Beneficial-Strategies/iso-20022-messages-records/main/.beneficial/logo.png)

# BeneficialStrategies.Iso20022.Messages.Records

The records of every ISO 20022 message, component and choice, written at compile time from the interfaces of iso-20022-messages by a Roslyn source generator: immutable, compared by value, read and written as XML and JSON.

**The ready-made C# classes for every ISO 20022 message**: 3,339 financial-services messages, and every component and choice they are made of, as immutable records with XML and JSON serialization. They are not typed by hand: a source generator writes them, while this package is compiled, from the interfaces of [BeneficialStrategies.Iso20022.Messages](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Messages), so the records and the interfaces cannot disagree. The namespaces are those of the earlier package `BeneficialStrategies.Iso20022`, which this one replaces.

Each record **implements the interface of its shape** (from [BeneficialStrategies.Iso20022.Messages](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Messages)), and the interfaces are what the rest of the family is built on. So a record from this package works with the validators, the readers and everything else, and when you do not want 3,339 messages' worth of classes, the same tools work with classes you write or have made for you. See [Where the records fit](#where-the-records-fit) at the end. The records are built from [the primitive types](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Primitives), strongly typed all the way down, so dirty data that does not fit the ISO 20022 specification cannot get into your system through them in the first place.

## Why ISO 20022

[ISO20022](https://iso20022.org) is fast becoming the defacto [inter-partner financial communication language](https://businesspaymentscoalition.org/wp-content/uploads/0217-bpc-intro-iso20022.pdf) for sending financial information. It is being [adopted in the US](https://businesspaymentscoalition.org/wp-content/uploads/0517-us-adoption-iso20022.pdf) and also [being adopted world-wide](https://medium.datadriveninvestor.com/world-digital-currencies-are-already-here-cbdcs-bc756bd899b9).
It's not just for banks any more. Experts are encouraging corporations to hasten with a [migration strategy](https://blogs.opentext.com/what-does-iso-20022-mean-for-corporates/). There is even work being done to pass [tokenized monetary value](https://ripple.com/insights/iso-20022-shaping-the-future-of-cross-border-payments/) itself across across the wire using this standard, causing the money/value to settle at the same moment the message itself settles.

Even if you plan on changing toolsets somewhere down the road, it would be wise to start adapting your domain model to this new standard.

## What this library provides

This package covers all currently defined messages in the ISO specification, plus both XML and JSON serialization and deserialization. Great attention has been paid to:

- **Intellisense-friendly**: packaged so your IDE has access to source and commentary on all types, giving you context on how each class or field should be used.
- **Compiler-enforced**: `required` properties ensure the compiler helps you construct valid ISO 20022 messages rather than discovering missing data at runtime.
- **Immutable records**: full value-equivalence throughout; thread-safe by design.
- **Strongly-typed dates and codes**: no string-typed enums, no ambiguous date formats.
- **Multiplicity-correct**: where a field allows multiple values, it's a collection; where it's optional, it's nullable.
- **Strongly-typed identifiers**: industry-specific identifiers like CUSIP, SEDOL, IBAN, and BIC are strongly typed, not bare strings; because the implementation is strongly-typed all the way down to the primitives, dirty data that doesn't fit the ISO 20022 spec can never be introduced into your system in the first place.

## How to use

### Build a message

The compiler asks for every required member, so a message that is missing data does not compile. The members are the primitive types of ISO 20022, which convert from strings and numbers and refuse what the standard does not allow.

<!-- snippet: examples/01-build-a-message/Program.cs#build -->
```csharp
var message = new MultilateralSettlementRequestV02
{
    GroupHeader = new GroupHeader104
    {
        MessageIdentification = "MSG-001",
        CreationDateTime = "2026-01-01T00:00:00",
        NumberOfSettlementRequests = "1",
    },
    SettlementRequest =
    [
        new MultilateralSettlementRequest3
        {
            InstructionIdentification = "INSTR-001",
            MovementRecord =
            [
                new MovementRecord2
                {
                    Identification = "MOV-001",
                    Amount = new AmountAndDirection5 { Amount = new ActiveCurrencyAndAmount { Currency = "EUR", Amount = 100m } },
                },
            ],
        },
    ],
};
```
<!-- /snippet -->

And write it as XML:

<!-- snippet: examples/01-build-a-message/Program.cs#write -->
```csharp
Console.WriteLine(Iso20022XmlSerializer.SerializeToString(message));
```
<!-- /snippet -->

Try the [project](https://github.com/Beneficial-Strategies/iso-20022-messages-records/tree/main/examples/01-build-a-message) yourself.

### Read it back, and what is wrong with what you were sent

<!-- snippet: examples/02-read-it-back/Program.cs#read -->
```csharp
var xml = """
<Document xmlns="urn:iso:std:iso:20022:tech:xsd:pacs.029.001.02">
  <MulSttlmReq>
    <GrpHdr><MsgId>MSG-001</MsgId><CreDtTm>2026-01-01T00:00:00</CreDtTm><NbOfSttlmReqs>1</NbOfSttlmReqs></GrpHdr>
    <SttlmReq><InstrId>INSTR-001</InstrId></SttlmReq>
  </MulSttlmReq>
</Document>
""";

var result = Iso20022XmlSerializer.TryRead<MultilateralSettlementRequestV02>(xml);

if (result.Succeeded)
    Console.WriteLine($"read: {result.Message!.GroupHeader.MessageIdentification}");   // read: MSG-001
```
<!-- /snippet -->

Nothing is thrown for bad input: the result is the message or the list of problems, each with the path of the element it is about.

<!-- snippet: examples/02-read-it-back/Program.cs#problems -->
```csharp
var broken = xml.Replace("<MsgId>MSG-001</MsgId>", "").Replace("<NbOfSttlmReqs>1</NbOfSttlmReqs>", "<NbOfSttlmReqs>many</NbOfSttlmReqs>");

foreach (var issue in Iso20022XmlSerializer.TryRead<MultilateralSettlementRequestV02>(broken).Issues)
    Console.WriteLine($"{issue.Code} {issue.Path}: {issue.Reason}");
```
<!-- /snippet -->

Try the [project](https://github.com/Beneficial-Strategies/iso-20022-messages-records/tree/main/examples/02-read-it-back) yourself.

### The record is an implementation of an interface, and has a JSON form

Every record implements the interface of its shape, and the interface is what the rest of the family is written against. A record is accepted wherever the interface is asked for, and two records with the same members are equal.

<!-- snippet: examples/03-the-interface-and-json/Program.cs#interface -->
```csharp
static string Describe(IGenericIdentification3 id) => $"{id.Identification} / {id.Issuer?.ToString() ?? "no issuer"}";

var a = new GenericIdentification3 { Identification = "ID-1", Issuer = "BANK" };
var b = new GenericIdentification3 { Identification = "ID-1", Issuer = "BANK" };

Console.WriteLine(Describe(a));            // ID-1 / BANK
Console.WriteLine(a == b);                 // True: a record is compared by its members
```
<!-- /snippet -->

The same record has a JSON form, with the ISO naming:

<!-- snippet: examples/03-the-interface-and-json/Program.cs#json -->
```csharp
var message = new MultilateralSettlementRequestV02
{
    GroupHeader = new GroupHeader104 { MessageIdentification = "MSG-001", CreationDateTime = "2026-01-01T00:00:00", NumberOfSettlementRequests = "0" },
    SettlementRequest = [],
};

var json = Iso20022JsonSerializer.SerializeToString(message);
Console.WriteLine(json);
Console.WriteLine(Iso20022JsonSerializer.Deserialize<MultilateralSettlementRequestV02>(json) == message);   // True
```
<!-- /snippet -->

Try the [project](https://github.com/Beneficial-Strategies/iso-20022-messages-records/tree/main/examples/03-the-interface-and-json) yourself.

## More

* Documentation, release notes and issues: https://github.com/Beneficial-Strategies/iso-20022-messages-records
* License: MIT. Status: stable.
* Targets net8.0 and net10.0.

### Where the records fit

The records are the ready-made implementation. They are the convenient way to get started, and nothing else in the family requires them:

| You want | Package |
|---|---|
| the rules the types cannot express, as FluentValidation validators for these messages | [BeneficialStrategies.Iso20022.Messages.Validation](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Messages.Validation) (business-rule validation: constraints the C# type system alone can't express) |
| to validate XML or JSON text, a document or a queue payload, with every problem reported | [BeneficialStrategies.Iso20022.Validation.Xml](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Validation.Xml) and [.Xml.Payloads](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Validation.Xml.Payloads) |
| the same tools **without** the records (your own classes, or classes made for you on the fly) | [BeneficialStrategies.Iso20022.Messages](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Messages) and [BeneficialStrategies.Iso20022.Factory](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Factory) |
| only the primitive types, not the message records | [BeneficialStrategies.Iso20022.Primitives](https://www.nuget.org/packages/BeneficialStrategies.Iso20022.Primitives) (this package depends on it; the namespaces are the same in both) |

### What's notably missing

- XML Schema Validation (the XML written by this library is checked against ISO's schemas in this repository's own tests, but there is no runtime schema validation)

**Known limitations:**

- Digital signatures not yet supported
- External content not yet supported
- External schema not yet supported

### Why is this approach better?

The most common quick-and-dirty approach to serializing these messages is to use the only marginally supported [Microsoft XSD tool](https://learn.microsoft.com/en-us/dotnet/standard/serialization/xml-schema-definition-tool-xsd-exe) to generate a data proxy. If you only have a few messages to support, that might be a worthy approach. Here are the reasons you might not want to:

- Most of the classes generated in the proxy are repeats that are also used in other messages.
    - You get zero reuse across messages.
    - If you have a certain type of Name and Address object in one message, you cannot assign it to another object in a different message because they are two different data types.
    - If you end up using many ISO messages, you get code glut over time since there are multiple copies of many objects.
- There is zero documentation burned into the proxy contents.
- If there's something wrong about the way it generates, there's little you can do to fix it.
- It is difficult to create frameworks around these proxy objects because there are no interfaces in common and no shared data objects.

### A word about the future

[Beneficial Strategies](https://www.beneficialstrategies.com/iso20022/) maintains this package as an ongoing output of building and testing the [Beneficial Strategies ISO 20022 MCP Server](https://beneficialstrategies.com/product/mcp-server/) — expect updates as the ISO 20022 organization publishes new snapshots of the standard. Updates and fixes generally fall into these categories:

- ISO20022 standard update — feature branch
- Improvements in metadata
- Correction of cardinality or data relationships — defect branch
- Tweaking of compile directives across versions

For anyone having a dependency on this package, the changes should be easy to maintain. When you update the reference to the new version, the compiler will let you know immediately if it sees any breaking changes.

Questions or feedback? Reach us at support@beneficialstrategies.com.

<!-- end of package readme -->

## Building and contributing

```
dotnet build -c Release            # about two minutes and several gigabytes: the whole model is compiled
dotnet test -c Release --no-build  # the generator tests, and the tests of the records
tools/run-examples.sh              # packs the package, then builds and runs every example against it
python3 tools/sync-readme.py       # fills the code blocks above from the examples
```

The records are not in this repository: they are written while the package is compiled, by the generator in `src/Generator`, from the interfaces of `BeneficialStrategies.Iso20022.Messages`. A change to the model is made in the interfaces (the repository iso-20022-messages), not here; a change to what a record looks like is made in the generator, with a test of it in `tests/Generator.Tests`. Two groups of tests need ISO's material, which is not in any repository: set `ISO20022_XSD_DIR` to a folder of ISO's message schemas for the schema tests, and `ISO20022_SNAPSHOT_FILE` to a `spec-snapshot.tsv` for the choice-variant tests. A bug report or a change starts with an issue; a bug fix names the test that failed before it. See CONTRIBUTING.md.
