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
5. **Measure. Then measure again after.** Three of my own conclusions on this
   branch were wrong, and only measuring caught them — see sections 7, 9 and 10.

Section 14 is a different subject: how clicking and dragging work when there are no
elements to click, which is the part of canvas rendering that has no SVG equivalent.

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
| Everything at once, per pointer move | 730 ms | 10.3 ms |

The last row is the one that matters most, because it's the only one measured the way
a user experiences it: main-thread time per pointer move with a world imported, 600
nodes on screen and both overlays on. Sections 10 and 11 explain why it was so much
worse than the per-frame figures suggested.

With lanes, their bounding boxes and their block cells all switched on, a 600-node
layout now runs at 3.95 ms/frame and 7.10 ms of main-thread time per pointer move at
fit zoom, 8.08 ms zoomed in. Add an imported world underneath and it is 6.17 ms/frame
and 10.28 ms per move, of which the two block layers are 0.95 ms and the lane outlines
0.04 ms.

## The tool that made this possible

Before any of it: `RenderProfiler`. It times every frame and, separately, every
renderer inside that frame. The bottom-right overlay in the app shows it live.

This matters more than it sounds. "The canvas feels slow" is not something you can
act on. "`MapBlocksRenderer` costs 94.67 ms of a 103.76 ms frame" tells you exactly
which twenty lines to read. Almost all of the work below was *finding* which line
mattered; the fixes themselves are mostly small.

It has one important blind spot, which I did not discover until section 10: it
measures the time spent *telling* Skia what to draw, not the time Skia spends
actually colouring pixels. That turned out to hide a whole second problem.

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

At this stage I kept one rectangle per cell rather than merging neighbours, on the
grounds that merging would change how the layer looks. That was true *while the cells
were outlined*, and section 11 is the story of it becoming false. The output here is
pixel-identical — I compared screenshots.

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
## 10. My profiler was blind to half the cost

By this point every renderer looked cheap. Then I ran everything at once — imported
world, 600-node graph, both overlays, zoomed out — and the profiler reported a
comfortable 5.24 ms/frame while the browser reported **730 ms of main-thread time
per mouse move**. Both numbers were correct. They were measuring different things.

Narrowing it down by turning things on and off:

| scene | profiler said | browser said |
|---|---|---|
| graph only | 3.20 ms/frame | 6.5 ms per move |
| graph + overlays, zoomed **out** | 3.37 ms/frame | **344 ms per move** |
| graph + overlays, zoomed **in** | 2.78 ms/frame | 6.1 ms per move |
| small graph + overlays | 1.65 ms/frame | 5.4 ms per move |

The same scene was 56× cheaper zoomed in than zoomed out, and the profiler barely
noticed. So the cost tracked **how much screen area got filled**, not how many calls
were made — and my instrument could not see it.

The reason is worth understanding, because it applies to any Skia or canvas work.
Drawing happens in two stages. When you call `DrawPath`, Skia *records* what you
asked for and returns quickly. The actual colouring of pixels happens later, when
the frame is flushed to the GL context — after `RenderProfiler` has already stopped
its timer. So the profiler measures the cost of *asking*, which was exactly the right
instrument for sections 1 through 6, and is blind to the cost of *doing*.

**A profiler tells you about the thing it measures, not about everything that is
slow.** Two instruments disagreeing is a finding, not a glitch. The honest reading
was that I had a second, independent problem I had not been measuring at all.

## 11. The blocks should have been filled all along

The second problem turned out not to be a performance problem at heart. It was a
drawing bug that happened to be expensive.

```csharp
var blockPaint = context.Cache.GetPaint(
    context.Options.CellFillStyle,     // "Fill"
    SKPaintStyle.Stroke,               // ...used to stroke
    1f, context.Viewport.Scale);
```

Every cell was drawn as an *outline* using a colour called `CellFillStyle`. A filled
cell was the intent; the layer was meant to read as ground and instead read as a
hatch pattern.

That mismatch is also the whole explanation for section 10's mystery. The outline is
stroked one screen pixel wide, and the stroke straddles the cell's border — half
inside, half outside. Zoom out until a cell is *smaller* than one screen pixel and
the outline is now **wider than the thing it outlines**, so every cell paints over
all of its neighbours. 64,000 cells each smearing over their neighbours is an
enormous amount of pixel work to produce a flat grey area.

Filling instead of stroking removes that entirely — a filled cell covers exactly its
own square and nothing else.

And filling unlocks a second saving that outlining had blocked. Earlier I had
deliberately *not* merged neighbouring cells, and wrote in the code that merging
"would change how the block layer looks". With outlines that was true: merge two
outlined cells and the shared border between them disappears. With fills it is no
longer true — **two filled squares that touch cover exactly the same pixels as one
rectangle spanning both.** So `BlockGeometry` now merges runs of neighbouring cells
along each row. The imported world's 64,269 cells collapse to **2,452 rectangles**,
with identical output.

Merging stops at tile boundaries, which gives up a little merging to keep the
viewport culling from section 1. Both still apply.

| measured zoomed out, per pointer move | before | after |
|---|---|---|
| imported world | 11.91 ms | 5.92 ms |
| graph + both overlays | 344.64 ms | 6.44 ms |
| everything at once | 730.24 ms | 9.12 ms |

The profiler and the browser now agree to within a few milliseconds, which is how
you know the hidden cost is actually gone rather than merely moved.

Filling changed what can be seen *through* the block layer, so the paint order had
to move with it. Solid ground can't sit on top of the reference overlays or it
buries them. The order is now: ground (background, blocks), then the overlays that
are only useful over the ground (chunk grid, mirror axis, regions, lane outlines),
then the graph.

Two general points from this one:

- **A performance symptom can be a correctness bug in disguise.** I had measured
  this layer, diagnosed it, cached it, tiled it and culled it — all useful, all
  real — without noticing that it was drawing the wrong thing. The paint style was
  right there in the line I edited twice.
- **"I can't optimise this without changing the output" deserves re-checking when
  the output changes.** My reason for not merging cells was sound when I wrote it and
  obsolete the moment the layer became filled.

## 12. Dashes that were never drawn

`RenderStyle.LineDash` was set to `[5]` on the bridgeable edge style and on the
mirror line style — and nothing anywhere read the property. No dash effect was ever
created, so both drew solid, and bridgeable edges were indistinguishable from
walkable ones.

Two details made this more than a one-liner.

The dash has to be **part of the paint cache key**. The walkable and bridgeable edge
styles are otherwise identical — same colour, same stroke, same width — so they would
have shared a single cached paint and one dash setting would have leaked onto the
other.

And dash lengths are screen distances, like stroke widths, so the intervals get
divided by the zoom. That means the dash effect depends on the zoom and can't be
built once at startup — which is exactly the trap that made the original
`PaintCache` leak native memory (section covered under the first commit). So
`PaintCache` keeps **one effect per pattern** and rebuilds it when the zoom changes,
releasing the previous one. Bounded, not accumulating.

Measured: dashes are 5px at fit zoom and 4px four zoom steps further in — constant on
screen, as intended, rather than growing with the map.

## 13. Two bugs found by screenshotting the overlays

Everything above was measured with both overlays on, but I had only ever
*screenshotted* them off. Taking the picture found two bugs the timings could never
have shown.

The first: **editing Lane Width moved the purple outlines and left the filled cells
behind.** Measured at width 4 and then 12, the outlines grew to span 144px while the
fill stayed 59px. Nudging a node with the arrow keys did the same thing - the lane
drew where the node used to be. The cause:

```csharp
if (e.EdgeBlocks.Count == 0)                     // only ever computed once
    e.EdgeBlocks = Rectangle.DiscretePointsInsideRect(...);
```

Once computed, never revisited, so every caller that moved a node or changed the
width had to remember to ask for a refresh - and two of them did not. Worse, Generate
Schematic computes its cells fresh from the current width, so the preview was
showing something the export would not produce.

`Edge.BlocksFor(laneWidth)` now derives them on demand and remembers the result
against the only things it depends on: the two endpoint positions and the width. A
change to any of them recomputes that one edge and nothing else. This is the
cache-invalidation lesson from the rest of the branch pointing the other way - the
earlier fixes *added* caches keyed on a revision, and this one already existed, keyed
on nothing at all. An unkeyed cache is just a stale value with extra steps.

The second I have **not** changed, because it would alter generated schematics and
that is your call. A lane of declared width 4 fills **5 cells**, not 4 - measured as
59px of fill against a 48px outline. `DiscretePointsInsideRect` keeps a cell when its
centre is within `width / 2` of the line, inclusive, so at an even width the cells
exactly `width / 2` away on *both* sides qualify and you get `width + 1`. It has
always behaved this way and I preserved it exactly; filling the cells is simply the
first time it became visible, because the fill now visibly overshoots its own
outline. Whether width 4 should mean 4 cells or 5 is a decision about what the number
means.

## 14. Making shapes clickable, and moving them

Nothing above this point was about interaction, and interaction is where a canvas
differs from SVG most sharply. This section works through it on the map.xml regions —
the rectangles, circles and cylinders — because they started out as pure decoration
and ended up pickable and draggable.

### There is nothing to attach a handler to

With SVG you write `onclick` on a `<rect>` and the browser tells you which element was
hit. **A canvas has no elements.** By the time a rectangle is on screen it is pixels;
the shape is gone. So the browser cannot answer "what did I click?" and something else
has to.

That something is the data. Each shape answers for itself:

```csharp
public abstract bool Contains(Vector2 planePoint, float tolerance = 0f);
```

For a region this is barely a new idea — a region in the game *is* a question about
whether it contains a block, so `Contains` is what it already meant. A rectangle
compares against its bounds, a circle against its radius, a compound region asks its
children. Nodes already worked this way (`FindNodeWithin`), which is why hover worked
before any of this.

The consequence worth internalising: **on a canvas, hit-testing is a feature you own.**
It is not free and it is not given to you. The upside is that you control it exactly —
no `elementFromPoint`, no invisible padding elements, no fighting `pointer-events`.

### Three rules that decide whether it feels right

**Search backwards.** Shapes overlap, and paint order decides what a person sees on
top. So picking must run in the reverse of paint order, or clicking a small circle
drawn over a big rectangle selects the rectangle:

```csharp
for (int index = Items.Count - 1; index >= 0; index--)
    if (Items[index].Contains(planePoint, tolerance)) return Items[index];
```

**Tolerance belongs in screen space.** A click never lands exactly on a shape, so it
needs slack. Express that slack in *world* units and a small region becomes
unclickable precisely when it is smallest on screen. So it is pixels, divided by the
zoom:

```csharp
Regions.Pick(worldPosition, RegionPickPixels / _viewport.Scale);
```

That is the same division stroke widths already use, and the same reason: the number
is meaningful to a person's eye and a hand, which live in screen space, not in blocks.

**Picking follows what was drawn, not what the data means.** A *negative* region means
"everything except these children". Its `Contains` nonetheless reports what the
children cover — because the children are what got painted, and a click can only
sensibly land on something visible. Where the drawing and the semantics disagree, the
drawing wins, because that is what the person is pointing at.

### Dragging: convert once, then think in blocks

The whole gesture lives in world coordinates. The pointer is converted at the
boundary, and after that nothing in the drag knows about pixels:

```csharp
var desiredOffset = Round(worldPosition - _startWorldPosition);
var step = desiredOffset - _appliedOffset;
if (step == Vector2.Zero) return false;
_region.Translate(step);
```

Three things fall out of that for free. The drag behaves identically at any zoom and
any pan, because the offset is in blocks. **Snapping is just `Round`** — no snap grid,
no tolerance table, because the world unit *is* the block. And the early return means
a pointer move that does not change the snapped offset repaints nothing.

Snapping the *offset* rather than the position is deliberate: a region whose corner
sits at x = 12.5 keeps its half-block when you move it three blocks left. Snapping the
position would silently straighten coordinates the author chose.

### One undo entry per gesture

A drag produces a hundred pointer moves. Pushing an action per move fills the history
with a hundred entries for one gesture, and undo becomes useless.

So the region moves live, and how far it has moved is tracked separately. On release
the live movement is *rewound* and the total handed to the history stack, which
re-applies it:

```csharp
region.Translate(-offset);                              // undo the live drag
return new MoveRegionAction(regions, region, offset);   // ...then let history apply it
```

That looks redundant and is not. It keeps one meaning of "the history stack owns this
change", so `Execute` and `Undo` are exact inverses and redo needs no special case.
Verified in the browser: drag lands `red-spawn` at −124,−93, undo returns −150,−110,
redo returns −124,−93.

### A moving layer can still be cached

This is where interaction meets everything earlier in this document. `RegionRenderer`
was the last layer still drawing one shape at a time, at about 1.5 ms per frame for 41
regions — the per-call cost from section 4, unbatched.

Batching it looks like it should conflict with dragging: a cached path is a snapshot,
and these shapes move. It does not, because the cache is keyed on a revision the move
bumps:

```csharp
public int Revision { get; private set; }
public void MarkChanged() => Revision++;
```

Drag a region and the revision moves, the batch rebuilds once, and the old outline
does not linger. **1.5 → 0.06 ms/frame.** Hovering and selecting do *not* bump it —
those draw the one highlighted shape again on top of the batch that already contains
it, the same trick as the hovered node in section 4, so pointing at things never
invalidates anything.

And the cursor is set by calling out to JS rather than through a Blazor-bound `style`
attribute, because a bound attribute would put a component re-render back on every
pointer move — undoing section 7 for a cosmetic detail.

### What this costs, measured

| | |
|---|---|
| region picking, 41 regions | 0.02 ms per pointer move |
| RegionRenderer, batched and cached | 0.06 ms/frame (was 1.5) |
| script time per move, XML mode vs Layout mode | 4.41 vs 3.99 ms |

Interaction is essentially free here. One honest limitation: `Pick` is a linear scan,
where node hover uses a grid. At 41 regions the scan is 0.02 ms and a grid would be
ceremony. At a few thousand it would become section 8 again — and the fix is already
written, in `Graph.FindNodeWithin`.

## 15. What I did not change

- **Lane cells and terrain cells are the same grey.** With "Show blocks" on over an
  imported world you cannot tell a lane from the ground. They shared a colour before
  this work too (both were the same hatch), so nothing regressed — but now that they
  are solid, giving lanes their own colour would make the overlay much more useful.
  That is a palette decision, so it is yours.
- **`RenderStyle.Radius` defaults to 6** while every real style sets 0.4. Since the
  units are world units — blocks — a fallback node would render 15× too large. Any
  node type without an explicit style (`WoolEntry`, `Frontline`, `Hub`, `Corridor`
  and the rest) hits that default.

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
