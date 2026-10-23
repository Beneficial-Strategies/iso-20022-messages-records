// Example 03: the record implements an interface, and has a JSON form.
// Shows:  that a record is accepted wherever its interface is asked for, that two records with the same members are equal, and the JSON round trip.
// Needs:  only BeneficialStrategies.Iso20022.Messages.Records (which brings the interfaces).
// Prints: the identification through the interface, whether two records are equal, the JSON of a message, and whether it reads back equal.
using BeneficialStrategies.Iso20022.Components;
using BeneficialStrategies.Iso20022.pacs;
using BeneficialStrategies.Iso20022.Serialization;

// <snippet interface>
static string Describe(IGenericIdentification3 id) => $"{id.Identification} / {id.Issuer?.ToString() ?? "no issuer"}";

var a = new GenericIdentification3 { Identification = "ID-1", Issuer = "BANK" };
var b = new GenericIdentification3 { Identification = "ID-1", Issuer = "BANK" };

Console.WriteLine(Describe(a));            // ID-1 / BANK
Console.WriteLine(a == b);                 // True: a record is compared by its members
// </snippet>

// <snippet json>
var message = new MultilateralSettlementRequestV02
{
    GroupHeader = new GroupHeader104 { MessageIdentification = "MSG-001", CreationDateTime = "2026-01-01T00:00:00", NumberOfSettlementRequests = "0" },
    SettlementRequest = [],
};

var json = Iso20022JsonSerializer.SerializeToString(message);
Console.WriteLine(json);
Console.WriteLine(Iso20022JsonSerializer.Deserialize<MultilateralSettlementRequestV02>(json) == message);   // True
// </snippet>
