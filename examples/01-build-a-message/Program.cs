// Example 01: build a message and write it as XML.
// Shows:  that a record is built with its required members (the compiler asks for them), with the primitive types of ISO 20022, and written as XML.
// Needs:  only BeneficialStrategies.Iso20022.Messages.Records.
// Prints: the pacs.029 message as XML.
using BeneficialStrategies.Iso20022.Amounts;
using BeneficialStrategies.Iso20022.Components;
using BeneficialStrategies.Iso20022.pacs;
using BeneficialStrategies.Iso20022.Serialization;

// <snippet build>
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
// </snippet>

// <snippet write>
Console.WriteLine(Iso20022XmlSerializer.SerializeToString(message));
// </snippet>
