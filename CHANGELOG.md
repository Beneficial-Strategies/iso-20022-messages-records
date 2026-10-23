# Changelog

The versions of this package have three parts, `design.breaking.compatible`: the first changes with a redesign, the second with a change that breaks existing users, the third with every other change. Newest first. Each entry is written from the issues closed since the previous release.

## 1.0.0-alpha (2026-10-23)

* Added: The first publication of the package: the records of every ISO 20022 message, component and choice, **written at compile time by a source generator from the interfaces of `BeneficialStrategies.Iso20022.Messages`**, instead of typed by hand. It replaces `BeneficialStrategies.Iso20022` (the records package of the earlier monorepo, whose last version is 0.7.0-alpha); the namespaces are unchanged, so code that used it changes only its package reference.
* Added: The generator, with unit tests that need no mocks (a small interface assembly is compiled in memory, the generator is run on it, and the output is checked as text, by compiling it and by reflection), and the tests of the records that were `Common.Tests` in the monorepo, with their history.
* Changed: Every choice has its variants as known types and as JSON derived types, with the variant name as the discriminator. 125 of the 4,341 choices of the earlier package had no discriminator, which was an omission in the typed-by-hand code, not a design; their JSON now carries `$type` like the others.
* Changed: Versions have three parts, `design.breaking.compatible`: the first changes with a redesign, the second with a change that breaks existing users, the third with every other change. This is not Semantic Versioning.
