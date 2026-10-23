// Example 02: read XML into a record, and what is wrong with what you were sent.
// Shows:  that the reader fills the record or lists every problem it finds, each with the path of the element.
// Needs:  only BeneficialStrategies.Iso20022.Messages.Records.
// Prints: the message read from valid XML, and the problems found in invalid XML.
using BeneficialStrategies.Iso20022.pacs;
using BeneficialStrategies.Iso20022.Serialization;

// <snippet read>
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
// </snippet>

// <snippet problems>
var broken = xml.Replace("<MsgId>MSG-001</MsgId>", "").Replace("<NbOfSttlmReqs>1</NbOfSttlmReqs>", "<NbOfSttlmReqs>many</NbOfSttlmReqs>");

foreach (var issue in Iso20022XmlSerializer.TryRead<MultilateralSettlementRequestV02>(broken).Issues)
    Console.WriteLine($"{issue.Code} {issue.Path}: {issue.Reason}");
// </snippet>
