# This repository

The records of every ISO 20022 message, component and choice, written at compile time from the interfaces of iso-20022-messages by a Roslyn source generator: immutable, compared by value, read and written as XML and JSON. (NuGet package `BeneficialStrategies.Iso20022.Messages.Records`). It is public.

## Layout

* `src/Records` the package: a project with **no source files of its own**. The records are written while it is compiled, by the generator, from the interfaces of `BeneficialStrategies.Iso20022.Messages` (and the XML documentation next to that assembly).
* `src/Generator` the Roslyn source generator (a netstandard2.0 analyzer, not packed); `tests/Generator.Tests` its unit tests: a small interface assembly is compiled in memory, the generator is run on it, and the output is checked as text, by compiling it and by reflection. No mocks.
* `tests/Records.Tests` the tests of the records (about 270; they take several minutes, because the whole model is loaded). Those that need ISO's material skip unless `ISO20022_XSD_DIR` (ISO's message schemas) or `ISO20022_SNAPSHOT_FILE` (a `spec-snapshot.tsv`) is set.
* `examples/NN-name` console projects that use the packed package, as a consumer does; `expected-output.txt` beside each is what it prints.
* `README.md` is also the package README: it is cut at the line `<!-- end of package readme -->` when packed, and its code blocks are filled from the examples.
* `tools/` the release and package scripts, the license check, `run-examples.sh` and `sync-readme.py`.
* `.beneficial/` workspace metadata and the logo files.

## Working here

* Background: `docs/BuildPerformance.md` (why a full build of the model is heavy, and what it costs now) and `docs/TestPatterns.md` (validating against ISO's XSDs, embedded XML samples and their resource names).

* **Never edit a generated record, and there is none to edit**: a change to the model is made in the repository of the interfaces (iso-20022-messages); a change to what a record looks like is made in the generator, with a test in `tests/Generator.Tests`. A full build compiles the whole model (about two minutes, several gigabytes of memory).
* Never commit, push or open a pull request unless asked in that turn. Work on a branch; every pull request is linked to an issue (`Closes #n`).
* Never commit `.claude/`. Generated or derived files go under `artifacts/` (ignored) or a `_work/` folder outside the repository, not into it.
* Build and test before proposing a change: `dotnet build -c Release` (warnings are errors) and `dotnet test -c Release --no-build`; then `tools/run-examples.sh` and `python3 tools/sync-readme.py --check`.
* Every public type and member has an XML documentation comment; the build fails on a missing one.
* The README is public text. Never write in it, in an issue, a pull request, a commit message or a release note the name or the content of anything that is not public.
* The version has three parts, `design.breaking.compatible`, and is not Semantic Versioning (see `CONTRIBUTING.md`). Do not change `<Version>` by hand: the release workflow does.
* Examples: types the consumer writes start with `My` and are in a `MyCompany` namespace; data is plainly fictitious; an example is never renumbered, a new one takes the next number.
