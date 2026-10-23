# Test patterns

Moved here from the monorepo's CLAUDE.md. The tests of the records are in `tests/Records.Tests`; the XSD tests skip unless `ISO20022_XSD_DIR` is set.

### Validating against ISO's XSDs

ISO's message schemas are not in the repository (they are ISO's). Download the message definition archives from iso20022.org,
unzip the `*.xsd` into one folder, and set `ISO20022_XSD_DIR` to it: `IsoXsdValidationTests` then reads each sample of `TestData`,
writes it back and validates the output with `XmlSchemaSet`. `ISO20022_XSD_SCAN=1` also runs `IsoXsdStructureTests`, which compares
the record model of every message with the structure of its XSD (element order and names, multiplicity, choice alternatives) and
writes `structure-report.txt` and `structure-problems.tsv` into the folder. Without the variable both are skipped. What fails today is
in #41 (and #39 for the choices). Put derived files in `_work/<project>/xsd/`, not in the repository.

### Embedded XML Sample Files

XML sample files live in `src/Common.Tests/TestData/` and are
compiled as `EmbeddedResource`. Every file **must** be validated against the official ISO 20022
XSD **before** it is committed.

**Validation requirement (mandatory):**

1. Obtain the XSD from `https://www.iso20022.org/sites/default/files/schemas/<message-id>.xsd`
   (e.g. `camt.056.001.10.xsd`).
2. Validate the XML file against the XSD using an external tool (e.g. `xmllint --schema`
   or an online validator such as freeformatter.com/xml-validator.html).
3. Record the result in the file's header comment:
   ```xml
   <!--
     XSD VALIDATION STATUS: VALID
     Validated : YYYY-MM-DD
     Schema    : https://www.iso20022.org/sites/default/files/schemas/<message-id>.xsd
   -->
   ```
4. If the file cannot be validated externally, set the status to `PENDING EXTERNAL VALIDATION`
   and add a TODO comment explaining why (e.g. amounts excluded due to Ccy-attribute mismatch).

**Amount elements** use the ISO XSD `simpleContent+Ccy-attribute` pattern:
`<Amt Ccy="EUR">47250.00</Amt>` — NOT child elements. The library's `Iso20022XmlSerializer`
handles this correctly for all types in the `BeneficialStrategies.Iso20022.Amounts` namespace.

### Embedded resource naming

The manifest resource name is built from the `<RootNamespace>` (not the assembly name):
`{RootNamespace}.TestData.{filename}` where path separators become dots.
For this project: `BeneficialStrategies.Iso20022.TestData.<filename>`
