# UnitySensors HDRP Extension

This repository provides HDRP compatibility and extensions for UnitySensors.

It depends on:

https://github.com/Field-Robotics-Japan/UnitySensors/tree/v3.0.0b

---

## Overview

UnitySensors is originally designed for Unity Built-in Render Pipeline.
This project adapts and extends it for:

- Unity 2023
- High Definition Render Pipeline (HDRP)
- Custom depth extraction using HDRP Custom Pass
- ROS2 integration compatibility

This repository does **not** replace UnitySensors.
It builds on top of the official v3.0.0b release.

---

## Dependency

UnitySensors v3.0.0b

## Unity Physics-based Sensors

Some sensors require Unity’s physics loop for consistent, deterministic sampling (e.g., sensors derived from rigidbody motion).
For those sensors, this project introduces and uses **`UnityPhysicsSensor`** as a common base pattern.

### Why `UnityPhysicsSensor`?

- Uses **`Rigidbody`** as the primary data source when physics-based motion is required
- Samples and publishes in **`FixedUpdate()`** to align with Unity’s physics timestep
- Keeps a clear separation between:
  - **Physics sampling (FixedUpdate)**  
  - **ROS publishing / visualization (Update / message callbacks)**

This helps maintain stable sensor output under HDRP rendering load and avoids frame-rate dependent sensor jitter.