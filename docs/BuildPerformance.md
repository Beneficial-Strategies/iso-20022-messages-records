# Build performance

Moved here from the monorepo's CLAUDE.md, where it described the compile of the hand-kept records. The records are now written by a source generator while the package is compiled; measured on the same model, the generator build takes about 2 minutes and 7 GB (the hand-kept records took 2:45 and 8.2 GB). The reasoning below is still the explanation of why a full build of the model is heavy.

## The text

A clean or touch-any-file build of `BeneficialStrategies.Iso20022.Common` takes roughly 80-100
seconds **per TFM** — confirmed via `-clp:PerformanceSummary` to be almost entirely the `Csc` task
itself (~80s), not restore (~3.5s) or anything else. This is inherent to compiling one ~24,000-file
single-assembly project — Roslyn recompiles the whole assembly on any change (C# has no true
per-file incremental compilation within one project), and this project's "everything in one
package" design means that's ~24K files, not a handful. It is not a flag you can toggle away
without restructuring into multiple smaller assemblies, which is a real architectural trade-off
(current design vs. more, smaller published surface) — not something to do casually as a build-speed
fix.

Practical mitigations for interim/dev-loop builds (all confirmed against this repo, not
theoretical):
- **Build only the one project you're iterating on**, not the whole solution:
  `dotnet build Common`, not `dotnet build iso20022.sln`.
- **Build a single TFM while iterating**, not both: `dotnet build -f net8.0` (or whichever you're
  actively targeting). Now that the library multi-targets two TFMs, building both on every save
  doubles the ~80-100s cost for no benefit until you're actually checking cross-TFM behavior.
  Reserve full multi-TFM builds/tests for pre-commit verification (this is exactly the difference
  between the day-to-day workflow used throughout this project's history and the
  `snapshot-sync-verify` skill's Milestone builds, which intentionally use `--no-incremental` and
  build everything for an accurate final signal).
- **`--no-restore`** on repeat invocations within a session saves the ~3.5s restore tax when
  dependencies haven't changed — small, but free.
- Test *execution* itself is not the bottleneck (the full 6,600+ test suite runs in ~1 second) —
  only the build that precedes it is slow. `dotnet test --filter` to a narrower set doesn't
  meaningfully speed up your local loop; skipping unnecessary rebuilds does.
