# Shimmer Controls — Extraction Guide for Pivots

> **Audience:** an automated agent (or developer) working **inside the Pivots repository**
> (`intxp/ip/Pivots`, default branch `main`) who will port the WinUI (Composition) and Win2D
> shimmer controls out of DevWinUI and into `Pivots.Controls`, with correct attribution and
> Component Governance.
>
> **You have access to the DevWinUI source** on the fork/branch below, so this guide *references
> file paths* rather than embedding code. Open the referenced files directly.

---

## 0. TL;DR / What to do

1. Copy the shimmer source files (Section 3) from the DevWinUI fork into a new
   `src/Controls/Shimmer/` folder in `Pivots.Controls`.
2. Re-namespace `DevWinUI` → `Pivots.Controls`; fix XAML `xmlns` and the style-merge mechanism
   (Pivots uses a **manual** `MergedDictionaries` in `Generic.xaml`, *not* DevWinUI's XAMLTools
   auto-merge — Section 6).
3. Add the `Microsoft.Graphics.Win2D` `PackageReference` to `Controls.csproj` (the version is
   already centrally pinned — Section 5).
4. Add attribution: file headers, `src/Controls/THIRD-PARTY-NOTICES.md`, and the root
   `cgmanifest.json` (Section 7). **This is a hard requirement.**
5. Decide the masked-skeleton scope (Section 4) — the Composition `ShimmerMaskView` pulls in the
   `RedirectVisualView` base class (third-party, from *cnbluefire*); the **Win2D**
   `Win2DShimmerMaskView` is self-contained and needs no extra base class.
6. Build (`Packaged`/`Debug`, ARM64) and validate with a throwaway harness page (Section 8).

---

## 1. Source coordinates

| Item | Value |
|------|-------|
| Upstream repo | `https://github.com/ghost1372/DevWinUI` — **MIT**, © 2023 Mahdi Hosseini |
| Fork (the code lives here) | `https://github.com/brentlopez/DevWinUI` |
| Branch | `add-shimmer-panel-controls` |
| Commit (HEAD at extraction time) | `39589892` (`395898921b6b8eca44240ce5fb1694d7b106ddea`) |
| Local checkout (if available) | `D:\brentlopez\Projects\3p\DevWinUI` |

> All `dev/DevWinUI/...` paths below are repo-relative to that fork/branch.

---

## 2. Background — what these controls are

A **shimmer** is the animated, diagonal "sweep" of light that travels across a skeleton/placeholder
while content loads. This work adds a **generalized, content-agnostic** shimmer (the pre-existing
DevWinUI `Shimmer`/`ShimmerTextBlock` only shimmered *text*) with **two interchangeable backends that
share the same math and the same public property surface**:

- **Composition backend** (Windows UI Composition / the XAML visual layer) — dependency-free,
  lightweight, the recommended default for plain placeholders.
- **Win2D backend** (`Microsoft.Graphics.Canvas`) — per-frame GPU drawing; use when you are already
  rendering with Win2D or want the shimmer to be part of a Win2D scene.

Both backends share **`ShimmerSweep`** (all angle/keyframe/gradient/easing math) so they look
identical, and expose the same nine properties: `Duration`, `IsActive`, `Angle`, `BandWidth`,
`HighlightOpacity`, `HighlightColor`, `Easing`, `GradientShape`, `ReverseGradient`.

Four controls in total:

| Control | Backend | Role |
|---------|---------|------|
| `ShimmerPanel` | Composition | `ContentControl`; overlays the sweep on **any** child content. |
| `ShimmerMaskView` | Composition | Single sweep **masked by the alpha** of the child placeholder shapes (skeleton card). Derives from `RedirectVisualView`. |
| `Win2DShimmerPanel` | Win2D | Win2D equivalent of `ShimmerPanel`. |
| `Win2DShimmerMaskView` | Win2D | Masked skeleton built from a **device-independent shape list** (no visual capture, no extra base class). |

Accessibility: the sweep is gated on **reduced-motion** (`UISettings.AnimationsEnabled`) and
**high-contrast**; when motion is disabled the resting placeholder/skeleton is shown without
animation. (See `ShimmerSweep.ShouldAnimate()`.)

---

## 3. File inventory (what to copy)

### 3a. Core set — always extract (Composition `ShimmerPanel` + shared math + both Win2D controls)

| # | File (on fork/branch) | ~Lines | Notes |
|---|-----------------------|-------:|-------|
| 1 | `dev/DevWinUI/Controls/Native/Others/ShimmerSweep.cs` | 233 | `internal static` shared math. Keep `internal`. |
| 2 | `dev/DevWinUI/Controls/Native/Others/ShimmerSweepController.cs` | 96 | Owns the Composition brush sweep lifecycle. `internal sealed partial`. |
| 3 | `dev/DevWinUI/Controls/Native/Others/ShimmerEasing.cs` | 15 | **public enum**. |
| 4 | `dev/DevWinUI/Controls/Native/Others/ShimmerGradientShape.cs` | 19 | **public enum**. |
| 5 | `dev/DevWinUI/Controls/Native/Others/ShimmerPanel.cs` | 311 | Composition `ContentControl`. |
| 6 | `dev/DevWinUI/Controls/Win2DAndComposition/ShimmerCanvas/ShimmerSweepRenderer.cs` | 76 | Win2D drawable. `internal sealed partial`. |
| 7 | `dev/DevWinUI/Controls/Win2DAndComposition/ShimmerCanvas/Win2DShimmerPanel.cs` | 282 | Win2D `ContentControl`. |
| 8 | `dev/DevWinUI/Controls/Win2DAndComposition/ShimmerCanvas/Win2DShimmerMaskView.cs` | 308 | Win2D masked skeleton. |
| 9 | `dev/DevWinUI/Controls/Win2DAndComposition/ShimmerCanvas/Win2DShimmerMaskShape.cs` | 21 | Public shape model. |
| 10 | `dev/DevWinUI/Controls/Win2DAndComposition/ShimmerCanvas/Win2DShimmerMaskShapeKind.cs` | 11 | Public enum. |

Styles (control templates):

| # | File | ~Lines | Notes |
|---|------|-------:|-------|
| 11 | `dev/DevWinUI/Themes/Styles/Controls/ShimmerPanel.xaml` | 19 | `ShimmerPanel` template. |
| 12 | `dev/DevWinUI/Themes/Styles/Win2d/Win2DShimmerPanel.xaml` | 21 | `Win2DShimmerPanel` template (hosts a `CanvasControl`). |
| 13 | `dev/DevWinUI/Themes/Styles/Win2d/Win2DShimmerMaskView.xaml` | 16 | `Win2DShimmerMaskView` template. |

### 3b. Optional — only if you want the **Composition** masked skeleton (`ShimmerMaskView`)

| # | File | ~Lines | Notes |
|---|------|-------:|-------|
| 14 | `dev/DevWinUI/Controls/Native/Others/ShimmerMaskView.cs` | 267 | Derives from `RedirectVisualView`. |
| 15 | `dev/DevWinUI/Controls/Native/OpacityMaskView/RedirectVisualView.cs` | 282 | **Third-party base class (cnbluefire).** Captures the child into a `CompositionSurfaceBrush`. |
| 16 | `dev/DevWinUI/Themes/Styles/Controls/RedirectVisualView.xaml` | 38 | Template for `RedirectVisualView` (parts: `LayoutRoot`, `ChildPresenterContainer`, `ChildPresenter`, `ChildHost`, `OpacityMaskContainer`). Self-contained. |

> See Section 4 for the scope decision. If you skip 3b, you still have full masked-skeleton support
> via the **Win2D** `Win2DShimmerMaskView` (#8–10).

### 3c. Reference only — **do not** copy (DevWinUI.Gallery-specific)

These are the demo pages. They are not part of `Pivots.Controls`, but they are the **best worked
examples** of how to consume every property and build skeleton-card layouts — read them when writing
your harness/usage:

- `dev/DevWinUI.Gallery/Views/Pages/Features/ShimmerPanelPage.xaml` (+ `.cs`)
- `dev/DevWinUI.Gallery/Views/Pages/Win2d/Win2DShimmerPanelPage.xaml` (+ `.cs`)

---

## 4. Scope decision: the Composition `ShimmerMaskView` / `RedirectVisualView` question

`ShimmerMaskView` (Composition) produces a "skeleton card" where a **single** sweep is visible *only*
through the alpha of the placeholder shapes. To do that it captures the child visual tree into a
`CompositionSurfaceBrush` and masks the sweep with it — that capture machinery is the
**`RedirectVisualView`** base class, which DevWinUI attributes to **cnbluefire** (a separate
third-party author; see Section 7).

| Option | What you get | Cost |
|--------|--------------|------|
| **A — Lean (recommended default)** | `ShimmerPanel` (Composition) + `Win2DShimmerPanel` + `Win2DShimmerMaskView` (masked skeleton). | No `RedirectVisualView`, **no extra third-party author** to attribute. The masked skeleton is Win2D-rendered. |
| **B — Full fidelity** | All four controls, incl. Composition `ShimmerMaskView`. | Must also copy `RedirectVisualView.cs` + its style, **and add a second attribution (cnbluefire)** to `THIRD-PARTY-NOTICES.md` and `cgmanifest.json`. |

**Recommendation:** Start with **Option A** unless a product surface specifically needs a
*Composition* (non-Win2D) masked skeleton. Win2D is already a Pivots dependency, so
`Win2DShimmerMaskView` adds no new dependency and no new third-party author, and it covers the same
skeleton-card use case.

---

## 5. Dependencies & coupling

**Good news: coupling to DevWinUI is effectively zero.** The shimmer source files use only framework
namespaces (`Microsoft.UI.Xaml.*`, `Microsoft.Graphics.Canvas.*`, `System.Numerics`,
`System.Diagnostics`). There are **no** DevWinUI helper/extension/service calls in the core set
(verified by inspecting the `using` directives and bodies).

| Dependency | Status in Pivots | Action |
|------------|------------------|--------|
| `Microsoft.Graphics.Win2D` | Already centrally pinned (`Directory.Packages.props`: `1.4.0`) and used by `Pivots.Graphics` / `Pivots.DeveloperTooling`. **Not** yet referenced by `Controls.csproj`. | Add `<PackageReference Include="Microsoft.Graphics.Win2D" />` (NO `Version` — Pivots uses Central Package Management) to `src/Controls/Controls.csproj`. |
| Windows App SDK / WinUI | Provided via `..\WinAppSDK.Common.props` (already imported by `Controls.csproj`). | None. |
| `CommunityToolkit.Labs.WinUI.DependencyPropertyGenerator` | Referenced by `Controls.csproj`. | Optional: you *may* convert the manual `DependencyProperty.Register` calls to `[GeneratedDependencyProperty]`, but it is not required (Section 9, FAQ). |
| Composition (`Microsoft.UI.Composition`, `ElementCompositionPreview`) | Part of WinUI; no package. | None. |

The controls are **self-contained for theming**: they compute their highlight/skeleton colors from
`ActualTheme` (`ShimmerSweep.ResolveRgb` / `ResolveSkeletonColor`) and do **not** reference external
`ThemeResource` keys. (The *gallery examples* reference `ControlAltFillColorTertiaryBrush` and a
page-local `ShimmerSkeletonBrush`, but those are consumer-side decoration, not control-side
dependencies.)

---

## 6. Target placement in Pivots

### 6a. Project / folder / namespace

- **Project:** `src/Controls/Controls.csproj` (`RootNamespace` = `Pivots.Controls`).
- **Folder:** create `src/Controls/Shimmer/` and place all `.cs` files there.
- **Namespace:** change `namespace DevWinUI;` → `namespace Pivots.Controls;` in every copied file.
  (Pivots uses a single flat `Pivots.Controls` namespace, matching existing controls such as
  `WorkspaceCarousel`, `AppMessagesHost`.)
- **`sealed`:** Pivots controls are typically `public sealed partial class`. The two `ContentControl`
  controls and the Win2D mask view can be `sealed`. **Do not** seal `RedirectVisualView` if you take
  Option B (other types derive from it upstream; `ShimmerMaskView` derives from it here).
- **Keep `partial`** on every control/helper (Native-AOT friendliness; also required by the DP
  source generator if you adopt it).

### 6b. Styles — **this is the biggest mechanical difference from DevWinUI**

DevWinUI auto-merges every `Themes/Styles/**/*.xaml` into `Generic.xaml` at build time via the
`XAMLTools.MSBuild` package. **Pivots does not do this.** Pivots maintains a single, hand-written
`src/Controls/Themes/Generic.xaml` and pulls in separate style files with an explicit
`MergedDictionaries` entry — exactly like the Ribbon control:

```xml
<!-- existing precedent in src/Controls/Themes/Generic.xaml -->
<ResourceDictionary.MergedDictionaries>
  <ResourceDictionary Source="ms-appx:///Controls/Ribbon/RibbonStyle.xaml" />
</ResourceDictionary.MergedDictionaries>
```

**Recommended approach:** consolidate the three shimmer style dictionaries (#11–13, and #16 if
Option B) into **one** file `src/Controls/Shimmer/ShimmerStyles.xaml`, then add:

```xml
<ResourceDictionary.MergedDictionaries>
  <ResourceDictionary Source="ms-appx:///Controls/Ribbon/RibbonStyle.xaml" />
  <ResourceDictionary Source="ms-appx:///Controls/Shimmer/ShimmerStyles.xaml" />  <!-- add -->
</ResourceDictionary.MergedDictionaries>
```

(Alternatively, paste the `<Style>` blocks directly into `Generic.xaml`. A separate merged file keeps
the diff localized and mirrors the Ribbon precedent — prefer it.)

XAML edits inside the copied styles:

- Change every `xmlns:local="using:DevWinUI"` → `xmlns:local="using:Pivots.Controls"`.
- The Win2D templates declare `xmlns:wind2="using:Microsoft.Graphics.Canvas.UI.Xaml"` — keep as-is
  (or rename the prefix to match Pivots convention; the existing DevWinUI `CircleIcon` uses `win2d`).
- Each control's two-part style pattern (a keyed `Default…Style` + an implicit `BasedOn` style) is
  compatible with Pivots; keep it. Ensure each control keeps `DefaultStyleKey = typeof(<Control>)` in
  its constructor (already present).
- If you build a **single** `ShimmerStyles.xaml`, make sure to keep all the `x:Key` default-style
  keys unique (they already are: `DefaultShimmerPanelStyle`, `DefaultWin2DShimmerPanelStyle`,
  `DefaultWin2DShimmerMaskViewStyle`, and `DefaultRedirectVisualViewStyle` for Option B).

> ⚠️ **Do not** look for a `Themes/Styles/` folder convention in Pivots — there isn't one. Everything
> funnels through `Generic.xaml` (inline or via `MergedDictionaries`).

---

## 7. Attribution & governance (REQUIRED)

The shimmer code is being **copied from** the DevWinUI fork. DevWinUI is **MIT**, so reuse is
permitted *provided the copyright notice and permission notice are preserved*. Do all three of the
following.

### 7a. Per-file header

Add an attribution header to the top of every copied `.cs`/`.xaml` file. Pivots' own files start with
`// Copyright (c) Microsoft Corporation. All rights reserved.`; for these third-party-derived files,
prepend an origin note, e.g.:

```csharp
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Adapted from DevWinUI (https://github.com/ghost1372/DevWinUI), MIT License,
// Copyright (c) 2023 Mahdi Hosseini.
// (Option B only) RedirectVisualView is derived from cnbluefire (https://github.com/cnbluefire).
```

### 7b. `src/Controls/THIRD-PARTY-NOTICES.md`

This file already exists and has a worked example (the WCT Ribbon section). **Append** a new section.
Use this template (drop the cnbluefire bullet/section if you took Option A):

```markdown
## DevWinUI - Shimmer Controls

- **Source:** https://github.com/ghost1372/DevWinUI
- **License:** MIT
- **Copyright:** Copyright (c) 2023 Mahdi Hosseini

The following files are adapted from DevWinUI and used under the terms of the MIT License:

- `Shimmer/ShimmerPanel.cs`
- `Shimmer/ShimmerSweep.cs`
- `Shimmer/ShimmerSweepController.cs`
- `Shimmer/ShimmerEasing.cs`
- `Shimmer/ShimmerGradientShape.cs`
- `Shimmer/ShimmerSweepRenderer.cs`
- `Shimmer/Win2DShimmerPanel.cs`
- `Shimmer/Win2DShimmerMaskView.cs`
- `Shimmer/Win2DShimmerMaskShape.cs`
- `Shimmer/Win2DShimmerMaskShapeKind.cs`
- `Shimmer/ShimmerStyles.xaml`
<!-- Option B only: -->
- `Shimmer/ShimmerMaskView.cs`
- `Shimmer/RedirectVisualView.cs`  (originally from cnbluefire — see below)

### MIT License

<paste the standard MIT license text here — copy the exact block already present
 in this file's Ribbon section>
```

For **Option B**, also add a short section noting that `RedirectVisualView` originates from
**cnbluefire** (`https://github.com/cnbluefire`). DevWinUI only references the author by URL in a code
comment — **verify the exact upstream repository and its license** (it is MIT in cnbluefire's WinUI
projects) before merging, and record it.

### 7c. Root `cgmanifest.json` (Component Governance)

Append a component entry mirroring the existing Ribbon entry's shape. Record the **commit the code was
taken from** (the fork HEAD, `39589892`) in the `Comments`, and point `RepositoryUrl` at the canonical
upstream:

```json
{
  "Component": {
    "Type": "Git",
    "Git": {
      "RepositoryUrl": "https://github.com/ghost1372/DevWinUI",
      "CommitHash": "<resolve the upstream DevWinUI commit these controls were branched from>"
    }
  },
  "DevelopmentDependency": false,
  "Comments": "Shimmer controls adapted into src/Controls/Shimmer/ (MIT). Taken from fork brentlopez/DevWinUI@39589892 (branch add-shimmer-panel-controls)."
}
```

> The shimmer controls are **new code authored on a DevWinUI fork** (so there is no single upstream
> `ghost1372` commit that contains them). Two acceptable governance stances — confirm with whoever
> owns Component Governance for Pivots:
> 1. Treat it as adapted-from-DevWinUI and record the upstream repo + the nearest upstream base commit
>    (plus the fork commit in `Comments`), as above; **or**
> 2. If your policy treats first-party-authored-on-a-fork code as first-party, you may only need the
>    `THIRD-PARTY-NOTICES.md` entry for the *patterns/base class* it derives from (DevWinUI, and
>    cnbluefire for Option B). When in doubt, include the `cgmanifest.json` entry — it is harmless and
>    conservative.

**Bottom line:** at minimum, ship the per-file headers **and** the `THIRD-PARTY-NOTICES.md` entry
(DevWinUI MIT; + cnbluefire for Option B). Add the `cgmanifest.json` entry unless governance tells you
otherwise.

---

## 8. Build & validate

Pivots build (from `pivots-build-system` conventions; ARM64 dev machine):

```powershell
# from the worktree/repo root
dotnet restore src/PivotsApp/PivotsApp.csproj -p:Platform=ARM64
dotnet build   src/Controls/Controls.csproj   -c Debug -p:Platform=ARM64 --no-restore
```

(Or build the whole app: `dotnet build src/PivotsApp/PivotsApp.csproj -c Packaged -p:Platform=ARM64`.)

**Validation checklist:**

1. `Controls.csproj` compiles with the new `Win2D` `PackageReference` and the `Shimmer/` files.
2. `Generic.xaml` parses (the `MergedDictionaries` source path resolves; styles apply).
3. Drop a throwaway harness onto any page (mirror the gallery pages in 3c). Minimal usage:

   ```xml
   xmlns:controls="using:Pivots.Controls"
   ...
   <!-- Composition: shimmer over any content -->
   <controls:ShimmerPanel Width="120" Height="120" CornerRadius="8" IsActive="True">
       <Image Source="ms-appx:///Assets/photo-placeholder.png" Stretch="UniformToFill" />
   </controls:ShimmerPanel>

   <!-- Win2D: masked skeleton card -->
   <Border Padding="12" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
           BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" BorderThickness="1" CornerRadius="8">
       <controls:Win2DShimmerMaskView Width="320" Height="72" IsActive="True">
           <controls:Win2DShimmerMaskShape Kind="Ellipse"          X="0"  Y="12" Width="48"  Height="48" />
           <controls:Win2DShimmerMaskShape CornerRadius="4"        X="60" Y="20" Width="248" Height="12" />
           <controls:Win2DShimmerMaskShape CornerRadius="4"        X="60" Y="40" Width="180" Height="12" />
       </controls:Win2DShimmerMaskView>
   </Border>
   ```

4. **What "correct" looks like:** content/skeleton is visible with a *faint* diagonal highlight band
   travelling across it (~20% opacity by default). If you see **solid black rounded rectangles**, read
   the CanvasControl note in Section 9 — that is the signature failure of using the wrong Win2D host.
5. Toggle OS "Show animations" off (or High Contrast on) → the sweep should stop and only the resting
   skeleton remain.

---

## 9. FAQ / gotchas (hard-won — read before you start)

**Q. Why does the Win2D control use `CanvasControl` and not `CanvasAnimatedControl`?**
`CanvasAnimatedControl` renders to an **opaque swap chain** that ignores a transparent `ClearColor`,
so it paints **solid black over the content** behind it (the original "black rectangles" bug).
`CanvasControl` is `SurfaceImageSource`-backed and composites with transparency. Per-frame redraws are
driven by subscribing to `CompositionTarget.Rendering` and calling `Invalidate()`. **Do not** "simplify"
this back to `CanvasAnimatedControl`. (DevWinUI's `CircleIcon` makes the same `CanvasControl` choice.)

**Q. Is it safe to read DependencyProperties inside the Win2D `Draw` handler?**
Yes — with `CanvasControl`, `Draw` runs on the **UI thread**, so reading DPs / `ActualTheme` is fine.
(This would *not* be true for `CanvasAnimatedControl`, whose `Draw` runs on a render thread and throws
`RPC_E_WRONG_THREAD` if you touch DPs — another reason the control uses `CanvasControl`.) The code
still snapshots parameters into fields for tidy, consistent reads.

**Q. The controls go blank after being hidden and shown again (Flyout/TabView/cached page/ListView).**
That was a fixed bug — make sure you copy the **current** versions. `OnApplyTemplate` is *not*
re-invoked on reparent, so the controls keep their `CanvasControl` wired across unload→reload (they
release only the per-frame GPU brush/geometry, which rebuild lazily on the next `Draw`) and re-acquire
the canvas in `OnLoaded` if needed. See `WireCanvas()` / `UpdateAnimationState()` in
`Win2DShimmerPanel.cs` and `Win2DShimmerMaskView.cs`.

**Q. Does theme switching at runtime work?**
Yes. `OnActualThemeChanged` routes through `UpdateAnimationState()`, which also re-evaluates the
reduced-motion / high-contrast gate. Preserve that wiring.

**Q. Why is the sweep angle/speed independent of control size?**
`ShimmerSweep` renders in **absolute (pixel) gradient space** with a fixed pixel velocity and a 45°
projection onto the bounding box, so angle, band width and speed don't distort with aspect ratio.
`Duration` is the time to cross a fixed reference distance and is clamped to `[1 ms, 24 days]` to avoid
zero/overflow. Don't "normalize" the gradient back to relative space.

**Q. Manual `DependencyProperty.Register` vs the Pivots DP source generator?**
The copied code uses manual `DependencyProperty.Register` (portable, no dependency). Pivots references
`CommunityToolkit.Labs.WinUI.DependencyPropertyGenerator`, so you *may* convert to
`[GeneratedDependencyProperty]` for house-style consistency — but it's optional and orthogonal to the
extraction. If you convert, keep the same property names, types, defaults, and the
`OnParameterChanged`/`OnIsActiveChanged`/`OnAppearanceChanged` callbacks.

**Q. Do I need any special skeleton brushes?**
No, for the controls themselves — they derive highlight/skeleton colors from `ActualTheme`
(`Win2DShimmerMaskView` uses `ShimmerSweep.ResolveSkeletonColor`, default dark `#FF424242` / light
`#FFDADADA`). For the **Composition `ShimmerMaskView`** (Option B), the *consumer* supplies the
placeholder shapes and should fill them with an **opaque** brush (the sweep is masked by the shapes'
alpha; semi-transparent fills make it faint).

**Q. Disposal / leaks?**
Already handled and reviewed: `ShimmerSweepController` and `ShimmerSweepRenderer` are `IDisposable`
and dispose their Composition/Win2D resources; the Win2D mask view disposes its cached `CanvasGeometry`;
`CompositionTarget.Rendering` is unsubscribed on stop/unload; device-loss is handled in
`OnCreateResources` (drops cached brush/geometry, rebuilt lazily). Keep these intact.

**Q. `Win2DShimmerMaskView` caches its geometry — what if `Shapes` changes after load?**
By contract, set `Shapes` **before** load. The geometry is (re)built lazily and on device-loss; if you
need post-load mutation, invalidate the cached `_maskGeometry` when the collection changes.

**Q. Anything else DevWinUI-specific to strip?**
No service locator, no `GetService`, no DevWinUI extension methods are used. The only non-framework
type the Composition mask view needs is `RedirectVisualView` (Option B).

---

## 10. Suggested commit / branch shape in Pivots

- Branch: `user/brentlopez/shimmer-controls` (no work item yet; create one if your process requires it).
- Keep the extraction in a single, reviewable commit (or two: "add Shimmer controls" + "attribution").
- Reference this guide and the fork commit (`brentlopez/DevWinUI@39589892`) in the PR description.
- Make sure `THIRD-PARTY-NOTICES.md` + `cgmanifest.json` changes are in the **same** PR as the code.

---

## 11. Quick checklist

- [ ] `src/Controls/Shimmer/` created; core `.cs` files copied (Section 3a) [+ 3b if Option B].
- [ ] `namespace DevWinUI;` → `namespace Pivots.Controls;` in every file.
- [ ] Styles consolidated into `Controls/Shimmer/ShimmerStyles.xaml`; `xmlns:local` updated; added to
      `Generic.xaml` via `MergedDictionaries`.
- [ ] `Microsoft.Graphics.Win2D` `PackageReference` (no `Version`) added to `Controls.csproj`.
- [ ] Per-file attribution headers added.
- [ ] `src/Controls/THIRD-PARTY-NOTICES.md` updated (DevWinUI MIT [+ cnbluefire for Option B]).
- [ ] Root `cgmanifest.json` updated (or governance-confirmed not needed).
- [ ] Builds `Debug`/ARM64; harness shows a faint moving highlight (not black); reduced-motion stops it.
- [ ] PR links this guide + the source fork/commit.
