Worker Bee Visual System - Swarm-to-Giant-Bee Design
Document Version: 1.0
Date: December 30, 2025
Status: Design Specification - Ready for Implementation

Overview
This document outlines the visual system for worker bees in Bee Colony Expansion. The system uses a humorous "swarm-to-giant-bee" approach where individual bees cluster together, then merge into progressively larger single bees as worker count increases.

Core Concept
Visual Phases
Phase 1: Individual Swarm (1-5 workers per tile)

Each worker displays as a small sphere (0.3 scale)
Slight random offset from tile center creates organic cluster
Each bee bobs independently (out of sync)
Color-coded by assignment (yellow = Hive, magenta = Flower)

Phase 2: Giant Bee (6+ workers per tile)

Workers "merge" into single large sphere
Sphere grows infinitely with worker count (comedic effect)
Darker color variant to distinguish from individuals
Single synchronized bobbing motion


Technical Specifications
Inspector Parameters
csharp[Header("Swarm Settings")]
swarmThreshold = 5                  // Merge happens at 6th worker
individualBeeScale = 0.3f           // Size of individual bees
swarmSpreadRadius = 0.3f            // Random offset range from center

[Header("Giant Bee Settings")]
giantBeeBaseScale = 0.5f            // Starting size when merged
giantBeeGrowthRate = 0.1f           // Scale increase per worker
// No max scale - infinite growth for comedy
```

### Color Specifications

**Individual Swarm Bees:**
- Hive workers: `Color.yellow` (current)
- Flower workers: `Color.magenta` (current)

**Giant Bees:**
- Hive workers: Darker yellow (reduce brightness by ~30%)
- Flower workers: Darker magenta (reduce brightness by ~30%)
- Implementation: Multiply RGB values by 0.7

---

## Positioning System

### Individual Swarm Bees (≤5 workers)

**Base Position:**
```
tileWorldPosition + Vector3.up * heightAboveTile
```

**Random Offset (Deterministic):**
- XZ plane: ±swarmSpreadRadius (random based on worker index)
- Y axis: ±0.1 units (slight height variation)
- Pattern consistent per worker index (deterministic randomness)

**Example Distribution:**
```
Worker 1: Center + (0.2, 0.05, 0.15)
Worker 2: Center + (-0.25, -0.08, 0.1)
Worker 3: Center + (0.1, 0.03, -0.3)
Worker 4: Center + (-0.15, -0.05, -0.2)
Worker 5: Center + (0.3, 0.07, 0.05)
```

### Giant Bee (6+ workers)

**Position:**
```
tileWorldPosition + Vector3.up * heightAboveTile
```
- Centered exactly on tile (no offset)

**Scale Formula:**
```
scale = giantBeeBaseScale + (workerCount - swarmThreshold) * giantBeeGrowthRate

Examples:
6 workers:  0.5 + (6-5) * 0.1 = 0.6
10 workers: 0.5 + (10-5) * 0.1 = 1.0
20 workers: 0.5 + (20-5) * 0.1 = 2.0
50 workers: 0.5 + (50-5) * 0.1 = 5.0 (hilariously huge!)

Animation Behavior
Individual Swarm Bees
Bobbing Motion (Out of Sync):
csharp// Each bee has unique phase offset
float phaseOffset = workerIndex * 0.5f;
float bobOffset = Mathf.Sin((Time.time + phaseOffset) * bobSpeed) * bobAmount;

position.y = baseHeight + bobOffset;
Visual Effect:

Creates organic "buzzing swarm" appearance
Bees bob at different times
More lifelike and dynamic

Giant Bee
Bobbing Motion (Synchronized):
csharp// Single unified motion
float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;

position.y = baseHeight + bobOffset;
Optional Enhancement (Future):

Slow rotation: transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
Adds extra visual interest to "super bee"


Transition Logic
Swarm → Giant Bee (Crossing Threshold)
Trigger: Worker count increases from 5 to 6
Process:

Detect worker count > threshold on tile
Destroy all individual bee GameObjects for that tile
Instantiate single giant bee GameObject
Apply darker color variant
Calculate initial scale based on worker count
Log: "Workers merged into giant bee at [coord]!"

No Animation: Instant transition (can polish later)
Giant Bee Growth
Trigger: Additional workers assigned to tile
Process:

Detect worker count increase
Recalculate giant bee scale
Apply new scale to existing giant bee GameObject
Log: "Giant bee grew! Now {workerCount} workers"

Note: No removal/shrinking functionality in MVP

Data Structures
Tracking Per-Tile Visualization
csharp// Track which tiles use giant bee mode
private Dictionary<Vector2Int, bool> tileUsesGiantBee;

// Store giant bee GameObjects
private Dictionary<Vector2Int, GameObject> giantBeeObjects;

// Group workers by tile
private Dictionary<Vector2Int, List<WorkerBee>> workersByTile;
```

### Update Workflow
```
1. Get all active workers from ResourceManager
2. Group workers by assignedTileCoordinate
3. For each tile:
   a. Count workers on this tile
   b. If count ≤ 5:
      - Ensure individual swarm exists
      - Update positions/animations
   c. If count > 5:
      - Check if giant bee exists
      - If not: Create giant bee, destroy individual bees
      - Update giant bee scale
      - Update giant bee animation
```

---

## Visual Examples

### Progression Visualization
```
1 Worker:
        ●
    =========

3 Workers:
      ●   ●
        ●
    =========

5 Workers (At Threshold):
    ●   ●   ●
      ●   ●
    =========

6 Workers (MERGE!):
        ◉
    =========
    (Slightly bigger, darker color)

15 Workers:
        ⬤
    =========
    (Large chonky bee!)

50 Workers:
        ⚫
    =========
    (MASSIVE ABSOLUTE UNIT)
```

---

## Color Reference

### Current Colors (Individual Swarm)
- Hive: `RGB(255, 255, 0)` - Bright yellow
- Flower: `RGB(255, 0, 255)` - Bright magenta

### Giant Bee Colors (Darkened)
- Hive Giant: `RGB(178, 178, 0)` - Dark yellow/gold
  - Calculation: `Color.yellow * 0.7f`
- Flower Giant: `RGB(178, 0, 178)` - Dark magenta/purple
  - Calculation: `Color.magenta * 0.7f`

---

## Implementation Checklist

### Phase 1: Core Structure
- [ ] Add inspector variables to WorkerVisualizer
- [ ] Create tracking dictionaries
- [ ] Implement worker grouping by tile coordinate

### Phase 2: Individual Swarm System
- [ ] Generate deterministic random offsets per worker
- [ ] Position individual bees with spread pattern
- [ ] Implement out-of-sync bobbing animation
- [ ] Apply correct colors (yellow/magenta)

### Phase 3: Giant Bee System
- [ ] Create giant bee instantiation logic
- [ ] Implement scale calculation formula
- [ ] Apply darkened color variants
- [ ] Implement synchronized bobbing

### Phase 4: Transition Logic
- [ ] Detect threshold crossing
- [ ] Destroy individual bees when merging
- [ ] Create giant bee on merge
- [ ] Handle giant bee growth on worker addition

### Phase 5: Testing
- [ ] Test with 1 worker per tile
- [ ] Test with 3-5 workers per tile (swarm appearance)
- [ ] Test crossing threshold (5→6 transition)
- [ ] Test giant bee growth (6→10→20→50)
- [ ] Test multiple tiles simultaneously
- [ ] Verify colors appear correct

---

## Future Enhancements (Post-MVP)

### Visual Polish
- Smooth transition animation when merging
- Particle effect on merge ("swarm condensing")
- Giant bee rotation animation
- Scale pulsing effect (breathing)

### Advanced Features
- Giant bee facial expression (content/happy with more workers)
- Size-based effects (very large bees cast shadows?)
- Sound effects (buzzing gets deeper with giant bees)

### Performance Optimization
- Object pooling for bee GameObjects
- LOD system for distant tiles
- Culling for off-screen bees

---

## Design Philosophy

**Why This System Works:**

1. **Clarity:** Immediate visual feedback on worker distribution
   - Few workers = small cluster
   - Many workers = BIG BEE

2. **Humor:** The infinite growth creates comedic moments
   - "Look at my absolute unit of a bee!"
   - Encourages experimentation

3. **Performance:** Fewer GameObjects at scale
   - 50 individual bees = 50 GameObjects
   - Giant bee = 1 GameObject

4. **Customization:** Inspector controls allow easy tuning
   - Adjust threshold to find sweet spot
   - Modify growth rate for balance

5. **Simplicity:** Clear rules, no complex state management
   - Count workers → Pick visualization mode
   - No edge cases or complicated transitions

---

## Testing Scenarios

### Scenario 1: Single Tile Progression
```
Start: 0 workers
Spawn 1st: Small bee appears with offset
Spawn 2nd: Two bees cluster, bobbing out of sync
Spawn 3rd-5th: Swarm grows, still individual
Spawn 6th: MERGE! Giant bee appears (darker color)
Spawn 7th-10th: Giant bee grows steadily
Spawn 20th: Massive comedic bee
```

### Scenario 2: Multiple Tiles
```
Hive (10 workers): Large yellow giant bee
Flower A (3 workers): Small magenta swarm
Flower B (8 workers): Medium magenta giant bee
Visual contrast clear between tiles
```

### Scenario 3: Mixed Assignment
```
Start game with auto-assignment
Watch swarms form on different tiles
Observe merges happening independently
Confirm colors correct per tile type

Success Criteria
The system is complete when:
✅ Individual bees spawn with deterministic offsets
✅ Individual bees bob out of sync
✅ Swarm mode works for 1-5 workers per tile
✅ Giant bee appears at 6 workers
✅ Giant bee uses darker color variant
✅ Giant bee scale grows with worker count
✅ Giant bee bobs in sync
✅ No scale cap (infinite growth works)
✅ Multiple tiles can have different visualization modes
✅ System handles 50+ workers gracefully
✅ Performance remains smooth

End of Document

Quick Reference
Default Values:

Swarm Threshold: 5
Individual Scale: 0.3
Spread Radius: 0.3
Giant Base Scale: 0.5
Growth Rate: 0.1
Color Multiplier: 0.7 (for darkening)

Key Formulas:

Giant Scale: 0.5 + (count - 5) * 0.1
Bob Phase: workerIndex * 0.5
Darker Color: originalColor * 0.7f