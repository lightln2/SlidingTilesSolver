# Complete Breadth-First Search of the Sliding Tile Puzzles

The 4 x 4 and 8 x 2 [Fifteen Sliding Tile Puzzles](https://en.wikipedia.org/wiki/15_puzzle) have over ten trillion states that can be reached from initial position.
This program performs complete breadth-first search on them in single-tile and multi-tile move metric, and finds puzzle radius and width at all depths.

The algorithm is described in detail [here](https://www.researchgate.net/publication/362559309_Complete_Solution_of_the_Fifteen_Sliding_Tile_Puzzles_on_a_Personal_Computer). To achieve best possible performance, the program uses
- special asymmetric encoding of puzzle states
- [Frontier Search](https://www.researchgate.net/publication/220430520_Linear-Time_Disk-Based_Implicit_Graph_Search) algorithm
- [ILGPU library](https://www.ilgpu.net/) for GPU calculations
- aggressive optimizations ([VByte encoding](https://arxiv.org/abs/1709.08990), SSE intrinsics, bit tricks etc.)

Minimum requirements:
- NVidia GPU card
- 8TB free disk space
- 16GB RAM

Running time is a few days, depending on the hardware available.

# Results:

**Single-tile moves**

([OEIS A151944](http://oeis.org/A151944) sequence)

| Cells | Puzzle | Radius |
|:-----:|:------:|:------:|
| 4     | 2 x 2  | 6      |
| 6     | 3 x 2  | 21     |
| 8     | 4 x 2  | 36     |
| 9     | 3 x 3  | 31     |
| 10    | 5 x 2  | 55     |
| 12    | 4 x 3  | 53     |
| 12    | 6 x 2  | 80     |
| 14    | 7 x 2  | 108    |
| 15    | 5 x 3  | 84     |
| 16    | 4 x 4  | 80     |
| 16    | 8 x 2  | 140    |


**Multi-tile moves**

| Cells | Puzzle | Radius |
|:-----:|:------:|:------:|
| 4     | 2 x 2  | 6      |
| 6     | 3 x 2  | 20     |
| 8     | 4 x 2  | 25     |
| 9     | 3 x 3  | 24     |
| 10    | 5 x 2  | 36     |
| 12    | 4 x 3  | 33     |
| 12    | 6 x 2  | 41     |
| 14    | 7 x 2  | 52     |
| 15    | 5 x 3  | 44     |
| 16    | 4 x 4  | 43     |
| 16    | 8 x 2  | 57     |


## License

- **[MIT license](https://lightln2.github.io/SlidingTiles/license.txt)**
- Copyright 2022 © lightln2
