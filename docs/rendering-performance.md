# Why the canvas was slow, and what fixed it

This is a walkthrough of the rendering work on this branch, written to be read
start to finish. Each section is one change: what was slow, why it was slow, what
replaced it, and what the measurement said.

There are only about five ideas here, and they repeat. If you take nothing else
away, take these:

1. **Don't redo work whose answer hasn't changed.** Most of the slowness was the
   same calculation repeated 60 times a second.
2. **Don't draw what nobody can see.** Off-screen work costs exactly as much as
   on-screen work unless you skip it.
3. **Ask the drawing library to do a lot at once, not a little many times.** In
   WebAssembly the cost is in *asking*, not in the drawing.
4. **A loop that runs per node, per frame is a hot loop.** Small waste inside one
   is not small.
5. **Measure. Then measure again after.** Two of my own guesses on this branch
   were wrong, and only measuring caught them.

Every number below was measured in a real browser through
`tools/canvas-bench.mjs`. Two things to know about the numbers up front:

- The machine has no GPU. Rendering goes through SwiftShader, a software
  renderer. Anything limited by *filling pixels* is much worse here than on your
  machine; anything limited by *running code* is roughly comparable. Where it
  matters, I say so.
- The test data is a generated world (`tools/make-sample-world.py`, 64,269 floor
  blocks) and a generated node grid (`--graph 30x20` — 600 nodes, 1,150 edges),
  because the real map corpus isn't reachable from this container.

## Headline

| Scene | Before | After |
|---|---|---|
| Imported world, 64k blocks | 103.8 ms/frame (10 fps) | 2.7 ms/frame |
| 600 nodes, 1,150 edges | 105.9 ms/frame | 2.5 ms/frame |
| Same, both overlays on | 327.8 ms/frame (3 fps) | 2.7 ms/frame |
| Laying out 1,150 edges | 7,314 ms | ~400 ms |
| Finding the hovered node, 2,400 nodes | 10.06 ms per pointer move | 0.20 ms |
| Blazor re-renders during a 60-move pan | 64 | 3 |

## The tool that made this possible

Before any of it: `RenderProfiler`. It times every frame and, separately, every
renderer inside that frame. The bottom-right overlay in the app shows it live.

This matters more than it sounds. "The canvas feels slow" is not something you can
act on. "`MapBlocksRenderer` costs 94.67 ms of a 103.76 ms frame" tells you exactly
which twenty lines to read. Almost all of the work below was *finding* which line
mattered; the fixes themselves are mostly small.

A caveat I only discovered at the end, discussed in the last section: this profiler
measures the time spent *telling* Skia what to draw, not the time Skia spends
actually colouring pixels. That distinction turned out to matter.

## 1. The block layer: rebuilding 64,000 rectangles every frame

`MapBlocksRenderer` was **94.67 ms of a 103.76 ms frame** — 90% of it.

The old code, once per frame:

```csharp
SKPath blockList = new();
foreach (var block in context.Map.Blocks)      // 64,269 of them
    blockList.AddRect(SKRect.Create(block.X, block.Y, 1, 1));
context.Surface.Canvas.DrawPath(blockList, blockPaint);
```

Two separate problems.

**It rebuilt geometry that never changes.** Those blocks are loaded once when you
import a world and then sit there. Describing all 64,269 of them to Skia is real
work, and it was being redone 60 times a second to produce a byte-for-byte
identical result each time.

**It had no idea what was on screen.** There *is* a check in the old loop:

```csharp
if (centerX <= context.LimitX && centerY <= context.LimitY)
```

but `LimitX`/`LimitY` are half the *map's* width and height. That asks "is this
block inside the map?" — and every block is, by definition. It reads like culling
but rejects nothing. Zoom into a corner and all 64,269 blocks were still sent.

The replacement is `BlockGeometry`. Two ideas:

- **Build once, keep it.** The paths are built when the block set changes and
  reused otherwise. `Map.BlocksRevision` is a counter that goes up whenever the
  blocks are replaced, and the cache compares against it. That's all a cache needs:
  a cheap way to ask "is what I'm holding still correct?"
- **Split it into tiles.** The blocks go into square buckets 32 units across, one
  path each. Each frame draws only the buckets that overlap the visible rectangle
  (`Viewport.VisibleWorldRect`). Without tiles, caching alone would still hand Skia
  the whole map every frame.

Result: **94.67 → 0.49 ms**, and 0.25 ms when zoomed in.

Those two numbers separate the two ideas, which is why I measured both. At
fit-the-whole-map zoom every tile is visible, so culling does nothing and the
0.49 ms is *entirely* the caching. The drop to 0.25 ms when zoomed in is the
culling. Neither fix alone would have been enough.

I kept one rectangle per cell rather than merging neighbours, because merging would
change how the layer looks. The output is pixel-identical — I compared screenshots.

## 2. A debug print in the drawing loop

`RegionRenderer` was 8.53 ms/frame. The whole reason:

```csharp
foreach (var region in context.MapElement.Regions.Items)
{
    Console.WriteLine(region.Id);   // <-- 41 times, 60 times a second
    RenderRegion(context, region);
}
```

In WebAssembly, `Console.WriteLine` is not cheap. It leaves the .NET runtime and
calls into the browser's console — about 0.2 ms each time. Forty-one regions is
8.5 ms of a frame spent printing text nobody reads.

Deleting one line returned 8% of the frame. Worth remembering: a print statement
inside a loop that runs every frame is a performance bug, not just untidiness. It's
also invisible in the source — nothing about `Console.WriteLine` looks expensive.

## 3. Two strings per node, per frame

With 600 nodes and 1,150 edges on screen, drawing them cost **105.85 ms/frame**:
`EdgeRenderer` 56.37, `NodeRenderer` 49.06.

Part of it was this, once per node per frame:

```csharp
var style = context.Options.GetStyle(node.Type.ToString().ToLower());
...
switch (style.Shape.ToLower()) { case "circle": ... }
```

`node.Type` is already an enum — a number. `ToString()` turns it into a string,
`ToLower()` allocates a second string, then the dictionary hashes that string to
find the style. Then the shape does it again. Four allocations and two string
hashes per node per frame, to answer a question the caller already knew the answer
to.

The fix is to key on the enum: `RenderStyle.Shape` becomes a `NodeShape` enum and
`RenderingOptions` gets `GetNodeStyle(Node.NodeType)` / `GetEdgeStyle(Edge.EdgeType)`.
The three styles that were never per-type — the grid line and the two mirror
styles — became plain properties, because looking those up by name was pure
indirection.

Result: **105.85 → 65.63 ms/frame.** Roughly 40 ms of frame time was string
handling.

This is the lesson I'd generalise most carefully. Converting an enum to a
lowercase string is *nothing* — nanoseconds — and in code that runs once you should
absolutely write whatever is clearest. It became 40 ms because it sat in a loop
over 600 nodes running 60 times a second. **Location, not the operation, is what
made it expensive.**

## 4. Asking Skia 1,150 times instead of once

After the string fix, `EdgeRenderer` was still 30.06 ms for 1,150 edges — about
**26 microseconds per line**. Drawing a line is not 26 microseconds of work. So
the time was going somewhere other than drawing.

It was the boundary. SkiaSharp is a .NET wrapper around Skia, a C++ library. Every
call — `DrawLine`, `DrawCircle`, `AddRect` — has to leave the .NET world and enter
the native one. In WebAssembly that crossing is expensive relative to the work
behind it. `NodeRenderer` was worse: two calls per node plus, for every diamond
node, a brand-new `SKPath` allocated and never disposed.

So the cost scaled with **how many times we asked**, not with how much we drew.

`GraphGeometry` collects all edges of one type into a single path and all nodes of
one type into a single path, then issues one call per path. Two edge types and
three node types means a frame makes a fixed handful of calls whether the layout
has 10 nodes or 10,000. The paths depend only on the graph, so they're keyed on a
new `Graph.Revision` counter and rebuilt only when the layout changes — panning,
zooming and hovering leave them alone.

Result: **65.63 → 2.10 ms/frame.** EdgeRenderer 30.06 → 0.37, NodeRenderer
35.15 → 1.26.

Two details worth calling out, because both are the kind of thing that bites later:

- **`Graph.Revision` has to include node positions.** Nudging a node with the
  arrow keys changes no edges and adds no nodes, but it does move something the
  cached path drew. `Map.MoveNode` bumps the revision for exactly that reason. A
  cache is only as good as its "has this changed?" test, and getting that test
  wrong gives you stale pixels — a much more annoying bug than slowness.
- **The hovered node is drawn twice, deliberately.** It needs a different outline
  colour, but rebuilding the whole batch every time the pointer moves would throw
  away the caching entirely. So it's drawn *again* on top of the batch that already
  contains it. Antialiasing is off and the geometry is identical, so the second
  draw covers the first exactly. The general shape: keep the cached thing keyed on
  something slow-changing, and handle the fast-changing exception separately.

## 5. The same mistake one level down, in plain maths

Building the 30x20 grid took **7.3 seconds** — not a frame problem, but the same
thinking. `CalculateEdgeBlocks` works out which cells each edge's lane covers:

```csharp
// swept the bounding box and, for every cell:
public static bool InsideRect(Vector2 a, Vector2 b, Vector2 point, double width)
{
    var corners = FindRectCorners(a, b, width);   // allocates a List, every cell
    var rect = AxisAlignedBoundingBox(corners);   // recomputed, every cell
    if (!rect.Contains(point)) return false;
    ...
}
```

The lane's corners and bounding box don't depend on which cell you're testing.
They were recomputed for all ~190,000 cell tests, producing the same answer every
time, allocating a list each time.

Computing them once before the sweep took it to **427 ms — 17× faster.** Same
answers. `FindRectCorners` now returns a `RectCorners` struct with the corners
named instead of a `List<Vector2>` indexed by position, so nothing is allocated
and callers stop writing `corners[2]`. The distance test compares squared
distances so the sweep pays no square roots.

Schematic generation goes through the same function and got the same speedup for
free.

This is fix #1 wearing different clothes: **work whose result doesn't vary across
a loop doesn't belong inside the loop.** Once you've seen the pattern in the
drawing code, you start seeing it everywhere.

## 6. The overlays: every earlier mistake at once

With "Show blocks" and "Show bounding box" both on, the 30x20 grid rendered at
**327.79 ms/frame** — three frames a second.

`EdgeBlocksRenderer` was 223.14 ms. Per frame, per edge, it built a fresh path
holding one rect per cell of that edge's lane: about 150,000 rects, rebuilt 60
times a second, with the same fake map-bounds cull as the original block layer.
That is precisely the problem `BlockGeometry` already solved — so it now *uses*
`BlockGeometry`. The edge cells are flattened into one list when the graph changes
and handed to the same tiled, cached, culled code. One implementation, two callers.

`EdgeBoundingBoxRenderer` was 102.55 ms: four `DrawLine` calls per edge, 4,600
native calls a frame. The outlines became one cached path in `GraphGeometry`, keyed
on the lane width *as well as* the graph revision — the width is a map setting that
the graph revision knows nothing about.

Result: **327.79 → 2.70 ms/frame.** EdgeBlocks 223.14 → 0.51, bounding boxes
102.55 → 0.03.

The reason this section is short is the interesting part. By this point the two
remaining slow renderers were slow for reasons already diagnosed, and one of them
needed no new code at all — just recognising that its data had the same shape as
another layer's. **The same fix keeps working because these were never five
different problems.**

## 7. Blazor was re-rendering the whole sidebar on every mouse move

By now frames cost ~2.5 ms, so I turned to interaction cost. I added a counter for
how often the Blazor component re-renders. A 60-move pan caused **64 component
re-renders**.

The cause was that pointer movement was handled *twice*:

```razor
<div id="canvasContainer" @onmousemove="OnMouseMove" ...>
```

plus a JavaScript handler on the canvas calling `JSOnMouseMove` to pan. The
difference between those two paths matters a lot:

- When a **Blazor event handler** returns, Blazor re-renders the component and
  re-diffs all its markup — every sidebar control, on every mouse move.
- A **`[JSInvokable]` method** called from JavaScript does not.

Moving the pointer changes nothing the sidebar displays, so all that diffing
produced no visible change. Hover detection moved into `JSOnMouseMove`, which now
pans and updates the hover in one handler and repaints once if either changed. The
`@onmousemove` binding is gone. Clicks and keypresses stayed on Blazor bindings —
those *do* change what the sidebar shows (the selected node's coordinates are
displayed there) and they're infrequent.

**Here I got something wrong, and it's instructive.** My first measurement was
wall-clock time around the pan, and after the fix it didn't improve — it looked
like the fix did nothing. But wall clock was measuring the *test harness*: each
simulated mouse move is a round trip into the browser, and each repaint waits for
an animation frame. Those dwarfed the app's own work.

The right instrument is the browser's own `ScriptDuration`, read from the CDP
Performance domain — actual main-thread time running code. By that measure:

| | before | after |
|---|---|---|
| component re-renders per 60-move pan | 64 | 3 |
| main-thread script time per move | 10.6 ms | 6.6 ms |

Real, and about 38% less work per pointer move. **A measurement that includes
things you don't control will hide the effect you're looking for.** I nearly
concluded the fix was worthless.

## 8. Finding the hovered node: measuring before optimising

`GetClosestNode` measures the distance to *every* node to find the nearest, then
hover throws that away unless it's within 0.4 units. Called on every pointer move.
This looks obviously wasteful — but "obviously wasteful" is how you end up
optimising something that doesn't matter, so I timed it:

| nodes | hover lookup | whole frame |
|---|---|---|
| 150 | 0.62 ms | 1.53 ms |
| 600 | 2.78 ms | 2.55 ms |
| 2,400 | **10.06 ms** | 6.13 ms |

Perfectly linear, and at 2,400 nodes it cost *more than drawing the entire canvas*.
Worth fixing after all.

About 4 microseconds per node for one distance comparison is far slower than that
loop would be in a desktop .NET app. The reason is that this code runs on the
**interpreted** WebAssembly runtime — Blazor doesn't compile your C# to native
machine code by default. Per-element loops are expensive enough there that
algorithmic choices matter at sizes where you'd normally ignore them. That's
probably the single most transferable fact in this document for Blazor work.

`Graph.FindNodeWithin` puts the nodes into a grid of one-unit cells and checks only
the cells the search radius touches, so the cost stops depending on layout size.
The index rebuilds lazily when the revision changes.

It returns the same node the old code did: if the globally nearest node is within
the radius then it's also the nearest node within the radius, and if it isn't, both
versions answer "nothing". `GetClosestNode` stayed for the two click handlers,
which genuinely want the nearest node at any distance and run once per click.

Result: **10.06 → 0.20 ms, and now flat in node count.** Script time per move at
2,400 nodes: 18.87 → 8.50 ms.

## 9. Deleting something instead of optimising it

`Render(RenderTrigger trigger)` took a reason for the repaint at seventeen call
sites — `RenderTrigger.Pan`, `RenderTrigger.NodeHover`, and so on — and ignored it
completely. The body only ever called `Invalidate()`.

My first instinct was that this was a missed opportunity: surely repeated
invalidations in one frame should be collapsed, and surely a hover should only
redraw the node layer. So I read SkiaSharp's JavaScript:

```js
if (this.renderLoopRequest !== 0)
    return;                     // already scheduled for the next frame
```

`SKGLView` **already** collapses repeated `Invalidate()` calls into a single paint.
My assumption was wrong. And making a hover repaint only the node layer would mean
compositing each layer into its own surface — a lot of machinery for a frame that
now costs 2.5 ms.

So the honest change was to delete the parameter and the enum. They implied the
canvas had logic for deciding what to redraw. It doesn't, and pretending otherwise
is worse than a gap, because the next person reading it will trust it. If per-layer
invalidation is ever worth building, the reason for a repaint can come back then —
with behaviour behind it.

**Not every performance idea survives contact with the source.** Two of mine didn't
(this one and the wall-clock measurement in §7). Reading the library beats guessing
about it.

## 10. What is still slow, and the limit of my own profiler

Running everything at once — imported world, 600-node graph, both overlays, zoomed
out — the profiler reported a comfortable 5.24 ms/frame. But the browser reported
**730 ms of main-thread script time per mouse move.** Those cannot both describe
the same work.

Narrowing it down:

| scene | profiler says | browser says |
|---|---|---|
| graph only | 3.20 ms/frame | 6.5 ms per move |
| graph + overlays, zoomed **out** | 3.37 ms/frame | **344 ms per move** |
| graph + overlays, zoomed **in** | 2.78 ms/frame | 6.1 ms per move |
| small graph + overlays | 1.65 ms/frame | 5.4 ms per move |

The same scene is 56× cheaper zoomed in than zoomed out, while the profiler barely
notices the difference. So the cost tracks **how much screen area gets filled**,
not how many calls are made.

The explanation is a limitation of my own instrument. `RenderProfiler` times how
long it takes to *tell* Skia what to draw. Skia records those commands quickly and
colours the actual pixels later, when the frame is flushed to the GL context —
after the profiler has stopped its timer. So the profiler is blind to fill cost.

What's expensive is genuine overdraw. The block layers draw one outlined rectangle
per cell — around 214,000 of them for this scene — and neighbouring cells each
paint over the other's shared edge. When the whole map is on screen a cell is under
one pixel wide, so all that outlining resolves to a flat grey area that could have
been filled once.

**Two important caveats before you read 344 ms as your experience:** this machine
has no GPU, and SwiftShader does that filling on the CPU, where it is far worse
than on real hardware. And it only happens with "Show blocks" on while zoomed out.
The default view after importing a world is milder — about 12 ms per move zoomed
out versus 4 ms zoomed in.

I have **not** fixed this, deliberately, because every fix changes what you see:

- **Level of detail** — when a cell is smaller than about two screen pixels, draw
  the merged silhouette as a filled shape instead of per-cell outlines. At that
  zoom the outlines aren't resolvable anyway, so it should look the same and be
  dramatically cheaper. This is what I'd do.
- **Merge runs of adjacent cells** into fewer, larger rectangles. Cheaper at every
  zoom, but the interior grid lines disappear.
- **Draw the layer as a bitmap**, one pixel per cell, in a single call. Cheapest of
  all, and the appearance changes most.

Which one is right depends on what the block layer is *for*, which is your call,
not mine.

Two smaller things I noticed and left alone, both cosmetic rather than performance:

- `NodeRenderer` is registered *before* `EdgeRenderer`, so edges paint over nodes
  and every node has lines crossing it. Probably not intended, but changing it
  changes the picture.
- `RenderStyle.LineDash` is set for bridgeable edges and the mirror line but never
  used — nothing applies a dash effect, so dashed lines render solid.

## How to check any of this yourself

```bash
# Terminal 1
dotnet run --project MinecraftLayoutEditor.WebApp -c Release --launch-profile http

# Terminal 2
python3 tools/make-sample-world.py /tmp/sample-world     # once

node tools/canvas-bench.mjs --frames 60 --world /tmp/sample-world
node tools/canvas-bench.mjs --frames 60 --graph 30x20 --layers
node tools/canvas-bench.mjs --frames 60 --graph 60x40 --zoom 3
```

`--graph <columns>x<rows>` fills the map with a connected node grid, `--layers`
switches on both overlays, `--zoom N` scrolls in N steps before measuring. The live
figures are also in the overlay at the bottom right of the app.

To attribute a regression: check `avgFrameMs` and the per-renderer breakdown first;
if those look fine but the app feels slow, compare `script time per move` — the gap
between them is fill cost, per the section above.

One setup note that will bite on a fresh machine: SkiaSharp's WebAssembly package
needs the **`wasm-tools` workload** (`dotnet workload install wasm-tools`) so
`libSkiaSharp.a` gets linked into the runtime. Without it the build *succeeds* with
only a warning and then fails at runtime. Don't install it with `sudo` — that
changes `HOME` and the workload lands somewhere the build won't look.
