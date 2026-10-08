# Handoff — MESHRA for Unity

Written 2026-08-04, continuing work on the project.
Updated 2026-10-08: Rebranded to MESHRA (Team CODE SYNERGY for Innohacks 4.0), tailored for the Indian Gaming & AVGC ecosystem.

The deliverable is a Unity package, `dev.meshra.unity-connector`, that puts an in-editor real-time 3D asset studio & automated rigging engine inside Unity (`Ctrl/Cmd + Shift + P`).

---

## 1. Where things are

| | |
| --- | --- |
| Repo | `github.com/SKYGOD07/MESHRA` — **public** |
| The package | The repo root. This repo is the package, nothing else |
| This document | `Documentation~/HANDOFF.md`. The `~` keeps Unity from importing it |
| Install URL | `https://github.com/SKYGOD07/MESHRA.git` |

## 2. What it does

An in-editor asset gallery window (`Tools ▸ MESHRA ▸ Browse Assets`, `Ctrl/Cmd + Shift + P`) that browses the catalogue, exposes each asset's knobs with zero-latency hybrid baking (~0.05ms vertex morphing, ~20-140ms QuickJS local baking), previews the result, and imports it with `COLOR_0` vertex colors baked in. Plus `MeshRACharacterAnimation` bone-binding for automated character rigging without joint hierarchy distortion.


**The design rule that matters:** nothing is invented client-side. Every label, range,
step, option and palette entry is read from the asset's published schema at
`/cdn/{id}-params.json`. A knob added on the server shows up in Unity without a package
release. Keep it that way — it is the reason this is a connector and not a reimplementation.

## 3. What the server taught us

These were established empirically against the live API. They are load-bearing assumptions
in the client, so if the server changes, these are what break.

- **The remix endpoint bakes every `affects: geometry` knob** — `range`, `choice` and
  `toggle` alike. What decides it is `affects`, not the type, and a missing `affects` reads
  as `colors` server-side. Only colour-affecting values are ignored. A dropped knob returns
  the baseline GLB rather than an error, so failure looks like "nothing happened".

  *Re-verified 2026-08-13 by hashing responses. This corrects the original note, which said
  only `range` knobs were baked; that was true when written and the client hid `choice` and
  `toggle` controls because of it. On `brick-church-6cf1af`: `towerHeight` `"12"` and
  `"18"` and `rose=false` each return a distinct GLB.*
- **Choice values are compared strictly.** Options are published as strings — `"12"`,
  `"15"`, `"18"` — and sending the number `12` matches nothing and returns the baseline.
  This is the easiest way to reintroduce a silent no-op.
- **Range values are snapped before they are cached.** `remix_snap` puts a value on a
  40-step grid (or whole numbers for a count-style knob, integer bounds spanning ≤ 8), but
  the cache is keyed on what was *asked for*. Off-grid requests therefore pay for bakes
  that on-grid ones get free, so the client mirrors the formula exactly — including PHP's
  round-half-away-from-zero, which .NET does not do by default.
- **Colour is therefore applied locally**, by matching each authored default hex to the
  vertices carrying it and rewriting vertex colours in-process. This is why colourways are
  instant and cost no quota. It also means colour correctness depends on the palette in the
  schema matching the GLB's actual vertex colours.
- **Colourway presets often omit the default option.** `presets` lists the *alternative*
  schemes; the default one is the asset's authored colours, already carried by each colour
  knob's own default. Requiring every option to name a preset hid the whole colourway
  control on assets that only ship alternatives.
- **Palette entries changed shape mid-development** from `["#hex"]` to `[{hex, share}]`.
  The parser accepts both, and `#RGB` shorthand. If the shape changes again, that parser is
  where to look — it broke every asset load when it happened.
- **Quota is per hour and unauthenticated sessions hit it.** A 429 opens a dialog
  explaining the limit with a link to create an account; the runtime path degrades to the
  nearest cached variant rather than stalling.
- **`/api/me`** reports tier and remaining budget. Note a naming inconsistency never
  resolved: the endpoint is `/api/me` but tier naming elsewhere uses `who_am_i`.

## 4. Measurements worth not re-deriving

- Server bake: **~120 ms** round trip.
- Local bake via QuickJS on Quest 3: **41.5 ms** — three frames at 72 Hz, a visible hitch.
  The same module in Node is **0.36 ms**; the gap is the interpreter, not the work. This is
  why local baking is opt-in and off by default.
- Vertex morphing between two bakes: **~0.05 ms**. **14 of 32** range knobs on the live
  catalogue are topology-preserving and therefore morphable.
- Catalogue: 480 assets as of 2026-08-13 (~285 when this was first written), averaging
  ~742 triangles. Paged 50 at a time, so it walks 10 pages.

## 5. State of the work

Done and committed:

- MIT `LICENSE.md`, `Third Party Notices.md` (three.js MIT notice in full), `license` field
  in `package.json`.
- `CHANGELOG.md` documenting changes.
- Local baking moved out of the package into `Samples~/LocalBaking`. It carried a 343 KB
  trimmed three.js build in a `Resources/` folder, which Unity includes in **every**
  consumer's player build whether referenced or not. As a sample it is opt-in.
- `Samples~/RuntimeApi` — spawn and remix from script.
- README: documented local baking and the three ways geometry gets rebuilt.

> **Not compiled or run.** Open the project in Unity and run the EditMode
> tests (section 8) before trusting it. The tests were updated to the new behaviour, so a
> stale assumption should show up as a failure rather than silently.

## 6. What is left

1. **Compile it and run the tests.** Verify the RE:Mesh rebranding has not broken any
   assembly or namespace references.
2. **Tag `v1.0.0`** once the tests pass, so the install URL can be pinned with `#v1.0.0`.
3. **Verify on macOS.** The specific risks to check:
   - PuerTS 3.0.2's `PapiQuickjs.bundle` is a **universal binary** (`x86_64 + arm64`), so
     local baking works natively on Apple Silicon and under an Intel editor alike.
   - The package has no `Process.Start`, no `DllImport`, no registry or `SpecialFolder`
     access, no platform conditionals, and no backslash path literals; every path is built
     with `Path.Combine`, and the `file:` package URLs use forward slashes as UPM requires.
   - `REMeshTar` was checked against both real archives for the two things that break on
     a case-insensitive filesystem: names colliding only by case, and absolute or `../`
     entries. Neither archive has any.
   - The vertex-colour shader is a plain vertex/fragment pass over `UnityCG` with no
     surface-shader or built-in-lighting dependency, which is what lets it compile for Metal.

   **The open macOS question is Gatekeeper.** Files written by our own installer do not get
   the `com.apple.quarantine` attribute; files a browser downloads do. If a Mac user reports
   the engine never registering, `xattr -d com.apple.quarantine` on the bundle is the first
   thing to try.
4. **Decide on distribution.** The git URL works today and is clean now that the package
   has its own repo. OpenUPM is the next step and wants version tags plus an OSI licence,
   both of which are in place bar the tag.

## 6b. Decided against

- **An FBX export button.** Unity's own FBX Exporter (`com.unity.formats.fbx`) already
  exports any imported prefab, vertex colours included: it writes an
  `FbxLayerElementVertexColor` layer when the mesh reports `HasValidVertexColors()`, which
  is the only part that matters here, since a RE:Mesh asset is one untextured mesh whose
  entire look is `COLOR_0`. Building it in would wrap an existing right-click menu item
  and pull a native Autodesk SDK into a package that has two lightweight managed
  dependencies. Documented in the README's *Exporting to FBX* section instead.

## 7. Gotchas that will bite

- **PowerShell 5.1 mangles double quotes** passed to `git`. Always use `git commit -F <file>`.
- **Shaders used only at runtime get stripped.** Nothing in a scene references glTFast's
  shaders when assets are downloaded at play time, so a build drops them and models render
  with the magenta error shader. Consumers shipping a build must add them to Always
  Included Shaders.
- **Every asset needs a `.meta`, and the package cannot fix it itself.** Unity writes
  missing `.meta` files for assets it owns, but a package installed from a git URL is
  *immutable*, so it cannot. `Tools~/check-package.py` fails CI on either a missing or
  orphaned `.meta`; run it before pushing.
- **Test assemblies and `defineConstraints`.** `UNITY_INCLUDES_TESTS` silently skips the
  assembly; the tests just never run and nothing reports it.
- **A package's tests need `testables`** in the host project's manifest, or they silently
  do not run. See section 8.

## 8. Running the tests

This repo is a package, not a project, so the tests need a host project to run in. Make an
empty Unity 6000.0 project, add the package to its `Packages/manifest.json` (either the git
URL, or `file:` pointing at a local clone while you iterate), then:

```bash
Unity.exe -batchmode -nographics -projectPath <host-project> \
  -runTests -testPlatform EditMode -testCategory "!Network" \
  -testResults results.xml
```

A package's tests only run if the project opts in: add
`"testables": ["dev.remesh.unity-connector"]` to that `manifest.json`, or the Test Runner will
show nothing and report no error. `UNITY_INCLUDES_TESTS` in `defineConstraints` fails the
same silent way.

Offline tests, plus live tests marked
`[Category("Network")]` that need a reachable API and a key, excluded by the filter above.
The key is read from `REMESH_API_KEY`, a gitignored
`remesh.key`, or `EditorPrefs`. **Never commit it**; `remesh.key` and `.env` are
already ignored.
