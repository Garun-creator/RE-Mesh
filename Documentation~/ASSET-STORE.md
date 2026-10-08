# Asset Store / Distribution Notes

Everything needed to distribute this package. This document covers submission guidelines,
dependency compliance, listing copy, and the `.unitypackage` build process.

---

## 1. Blockers

### 1a. Programmatic package installation — **handled**

Quote the two clauses separately, because running them together is how this was misread the
first time:

> **2.5.1.d** *"Submissions do not contain any scripts that, upon import and at any other
> point, automatically and/or without user consent redirect users outside the Unity Editor,
> such as a website or other hyperlinks/deep links."*
>
> **2.5.1.e** *"Offerings must not programmatically add, update, or remove packages in user
> projects, except for packages included in the offering's own Asset Store product."*
> — [Submission Guidelines](https://assetstore.unity.com/publishing/submission-guidelines)

"Automatically and/or without user consent" qualifies **d**, not **e**. A button the user
chose to press is a fine answer to d and no answer at all to e.

The rule is **declare, don't install**, and 5.2.c limits what you may declare to *"Unity
packages or other packages already included in the same published product"*. PuerTS is
Tencent's, which closes both doors — and leaves the third: make it ours by vendoring.

| Feature | Resolution |
| --- | --- |
| *Install PuerTS for me* | **Gone.** The engine is vendored into the package (`Tools~/vendor-puerts.py`), so there is nothing to install and nothing to ask permission for. |
| `Tools ▸ RE:Mesh ▸ Update Package` | **Stripped from any store build.** The store delivers its own updates; a store install would fetch a second copy alongside the imported files. |

`Tools~/make-store-package.py` drops that file, cuts any region marked `// <store-strip>`,
then *searches the result* for `Client.Add`, `Client.AddAndRemove`, `packages-lock.json` and
any orphaned reference the strip left behind, and exits non-zero if it finds one.

```bash
python3 "Tools~/make-store-package.py" ../remesh-store-build
```

What is vendored is deliberately less than the whole engine: desktop x64 natives only (4.4 MB,
all marked Editor-only), managed source verbatim, and none of the Android/iOS/WebGL binaries,
the websocket addon, or the IL2CPP generator. `Third Party Notices.md` carries the BSD 3-Clause
notice, which is what clause 2 asks of a binary redistribution.

### 1b. The animation clips — **decided: keep them, ship free**

The character animation feature downloads animation data into the user's project.

**Decision: keep the pack, and the package is free.** If a complaint ever arrives, the fix
is already scoped — the retargeting code does not care where clips come from, only that the
bone names are compatible, so a CC0 locomotion set drops in without touching anything else.

### 1d. Validation: demo scene and offline documentation — **handled**

The Asset Store Tools validator checks:

- `CheckDemoScenes` collects every `.unity` file under the paths you selected and accepts a
  scene whose root-object count is **anything other than zero, or exactly an untouched camera
  plus an untouched light**. Content is never inspected. Three roots passes.
- `CheckDocumentation` collects every `.txt`, `.pdf`, `.html`, `.rtf` and `.md` under those
  paths, and accepts one that either ends in **`.pdf`** or has the literal word
  **"documentation"** somewhere in its text. Nothing else is read.

- **`Demo/RE:Mesh Demo.unity`** — camera, key light, and an object called
  *START HERE - RE:Mesh* whose Inspector lists the four steps with a button that opens the
  gallery. Three root objects, so it passes.
- **`Documentation/REMesh-Manual.pdf`** and **`.html`** — numbered sections with a
  table of contents, covering install, the demo scene, browsing, remixing, importing,
  re-editing, characters, local rebuilds, keys, scripting and troubleshooting.

**When validating, select `Assets/REMesh` itself**, not a subfolder.

### 1e. Attribution and licence notes

three.js is MIT and PuerTS is BSD 3-Clause; both licences require the notice to travel with
the code. `Third Party Notices.md` satisfies this requirement. Any store build that strips
these notices must also strip the vendored engine itself — they travel together.

`Tools~/make-store-package.py` handles this automatically: it sheds and verifies it shed.

---

## 2. What is already compliant

- **Dependencies.** `com.unity.cloud.gltfast` and `com.unity.nuget.newtonsoft-json` are both
  Unity Registry packages, correctly declared in `package.json`.
- **Third-party notices.** `Third Party Notices.md` carries three.js's MIT notice in full.
- **Licence file.** `LICENSE.md`, MIT.
- **Minimum editor version.** `package.json` says `6000.0`, above the 2021.3 LTS floor.
- **Size.** Under a megabyte, against a 700 MB UPM ceiling.
- **Documentation.** `README.md` covers install, the gallery, knobs, local baking, rigs and
  animation, and FBX export.

---

## 3. Listing copy

**Title** (keep under 50 characters)

```
MESHRA — In-Editor 3D Asset Studio & Auto-Rigging
```

**Summary / short description**

```
In-Editor Real-Time 3D Asset Studio & Automated Rigging Engine for Unity, built for the Indian Gaming & AVGC ecosystem. Zero-latency hybrid baking & auto-rigging.
```

**Description**

```
MESHRA (by Team CODE SYNERGY) brings an in-editor real-time 3D asset studio and automated rigging engine directly into Unity, engineered to solve high 3D licensing costs, hardware barriers, and tier-2/3 network constraints across the Indian Gaming & AVGC ecosystem.

Open Tools > MESHRA > Browse Assets (Ctrl/Cmd + Shift + P) to search the catalogue, parametrically tweak 3D models with zero-latency hybrid baking (~0.05ms vertex morphing, ~20-140ms QuickJS local baking), and instantiate Unity prefabs directly into your scene.

WHAT YOU GET
• Native Unity Editor Asset Gallery (Ctrl/Cmd + Shift + P)
• Zero-Latency Hybrid Baking Engine (~0.05ms vertex morphing & QuickJS local execution)
• MeshRACharacterAnimation bone-binding pipeline for automated character rigging without joint distortion
• glTFast COLOR_0 vertex color pipeline for single-draw-call rendering
• Runtime API for spawning & parametric tweaking at play time

REQUIREMENTS
Unity 6000.0 or newer. Depends on glTFast (com.unity.cloud.gltfast) and Newtonsoft JSON (com.unity.nuget.newtonsoft-json).

Source code & Innohacks 4.0 Pitch Deck available on GitHub: https://github.com/SKYGOD07/MESHRA
```


**Category**: `Tools ▸ Modeling` (alternative: `Tools ▸ Utilities`)

**Keywords**: `3d models`, `low poly`, `asset browser`, `parametric`, `procedural`,
`glb`, `gltf`, `editor tool`, `prototyping`, `vertex color`

---

## 3b. The .unitypackage

`Tools~/make-unitypackage.py` builds one without Unity — a .unitypackage is a gzipped tar
laid out by GUID, which is a format, not a ritual. Paths are rewritten to `Assets/REMesh/…`
and `~` folders are dropped, since Unity ignores those wherever they land.

```bash
python3 "Tools~/make-unitypackage.py" REMesh.unitypackage
```

It is attached to each GitHub release, which is the answer to "where do I get the Unity
package" for anyone who does not want the git URL.

### Submit as a UPM package, not a .unitypackage

**This is the format decision, and it is not close.** A `.unitypackage` carries no dependency
information — it is a bag of files, not a manifest — so a buyer who imports one gets every
RE:Mesh assembly failing to compile against glTFast and Newtonsoft JSON until they install both
by hand. UPM publishing resolves declared dependencies automatically.

`package.json` is already submission-ready: `name`, `version`, `displayName`, `description`,
`unity`, plus `documentationUrl`, `changelogUrl`, `licensesUrl`, `author`, `keywords`, `samples`
and the two `dependencies`.

1. Enrol at [cloud.unity.com/assetstore/publisher](https://cloud.unity.com/assetstore/publisher).
2. Create a product draft and **reserve the technical name `dev.remesh.unity-connector`** — the
   uploader rejects a package whose `name` does not match it.
3. **Window ▸ Tools ▸ Asset Store ▸ Validator**, set **Validation Type: UPM**, run it.
4. **Window ▸ Tools ▸ Asset Store ▸ Uploader ▸ UPM Packages**, upload.

Ceiling is 550 MB; we are about 5 MB.

---

## 4. Images

Generated into `Documentation~/store/`, at the sizes the store asks for:

| File | Size | Where it shows |
| --- | --- | --- |
| `icon-160x160.png` | 160 × 160 | Icon grid |
| `card-420x280.png` | 420 × 280 | Search results |
| `cover-1950x1300.png` | 1950 × 1300 | Product page header |

**Screenshots are still needed, and they are the part that sells it:**

1. The gallery with the grid populated and a model in the preview
2. The remix screen mid-edit, showing sliders beside a large model
3. A model in a scene with the Inspector open on its knobs
4. A character with the animation dropdown open

Take them at 1920 × 1080 or larger, on the dark editor skin.

---

## 5. Submission checklist

- [x] **Price settled** — free
- [x] **Demo scene and offline documentation** — in every build, validated against the
      validator's own rules
- [x] `Third Party Notices.md` current — PuerTS BSD 3-Clause, reproduced verbatim
- [x] `package.json` submission-ready — every required and recommended field, both dependencies

Left to do, in order:

- [ ] **Reserve the technical name `dev.remesh.unity-connector`** in the product draft.
- [ ] Unzip `REMesh-UPM.zip` into a fresh Unity 6000.0 project's `Packages/`, so the manifest
      lands at `Packages/dev.remesh.unity-connector/package.json`. Package Manager resolves glTFast
      and Newtonsoft on its own.
- [ ] Compile with **no errors and no warnings**
- [ ] Every feature exercised once: browse, remix, import, place, re-edit, animate
- [ ] **Validator**, Validation Type: **UPM**
- [ ] **Four screenshots**
- [ ] Description names both Unity Registry dependencies and the internet requirement
- [ ] **Uploader ▸ UPM Packages**, upload
- [ ] Version tagged in git so the submission maps to a commit
