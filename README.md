# WFC-Procedural-Generation
How the WFC generator works

The basic idea is pretty simple. You give it a small example picture, it chops that picture into tiny squares, learns which squares are allowed to sit next to each other, and then builds a new, bigger picture piece by piece while following those rules. The result looks like the original up close but is never an exact copy.

## WFC Pipeline

First, it reads the image. Unity hands over the PNG’s pixels and every distinct colour gets a number, so from then on the picture is just a grid of numbers.

Next, it cuts the image into patterns. A small N×N window (the “kernel”, for example 3×3) slides over every spot in the image, and each square it sees becomes a pattern. Duplicates are merged into one, and it remembers how often each one showed up. If you want, it can also add rotated and mirrored copies of every pattern.

Then it works out the neighbour rules. For every pair of patterns it checks whether pattern B can sit to the right of pattern A, above it, and so on. They’re allowed to be neighbours if their overlapping edge matches exactly, and how wide that edge is comes from the overlap setting.

After that it sets up the output grid. The output is split into cells, and each cell will eventually hold one pattern. At the start, every cell could still be anything.

Then it plants a seed: a random cell gets a random piece of the input image to start from.

Now the main loop begins. It picks the next cell (which one depends on the heuristic you chose) and randomly chooses one of the patterns still allowed there, with more common patterns being more likely. Then it propagates: the neighbouring cells drop any patterns that no longer fit, which can make their neighbours drop some too, rippling outward across the grid. This repeats until every cell is decided.

If it gets stuck, meaning some cell ends up with no options left (a “contradiction”), it starts over with a new seed derived from the original. It keeps trying up to the max attempts.

Finally, it draws the result. Each cell’s pattern is painted into a texture, which is shown on a sprite in the scene. When animating, cells that aren’t decided yet show a blurry average of what they could still become, and they sharpen as the picture fills in.

## Editor Parameters 
- Kernel Size (2 to 16) is the size of the squares
- Overlap (1 to N−1) is how many columns or rows two neighbours must share.
- Symmetry (1 to 8) adds rotated and mirrored copies
- Periodic Input treats the input as tileable, so patterns can wrap around its edges 
- Duplicates decides how repeated patterns are weighted: WeightByFrequency makes common patterns show up more often, while Uniform makes all patterns equally likely

- Output Width and Output Height set the size of the generated image in pixels
- Periodic Output makes the result tile seamlessly

- Heuristic is the order the picture gets built in
- MinimumEntropy always fills the most constrained cell first
- BreadthFirst grows outward from the start in rings
- DepthFirst snakes away along a path and then fills in the gaps
- Seed controls the randomness, so the same seed gives the same picture every time
- Randomize Seed picks a new seed on each run
- Max Attempts is how many times it may restart before giving up

- Pixels Per Unit controls how big the result appears in the scene
- Show Superposition turns the blurry “still deciding” preview on or off while animating
- Undecided Color is the colour of empty cells when that preview is off
- Generate On Start makes it generate automatically when you press Play
- Animate lets you watch it build step by step, and Steps Per Frame controls how fast that goes
