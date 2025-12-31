# SpatialSlides XR Client

SpatialSlides is a system that integrates immersive authoring directly into slide-based presentation workflows.

This XR client connects to the companion Python Middleware to synchronize 3D content with PowerPoint presentations.

For the Python middleware source code and full system documentation, visit the main repository: **[SpatialSlides](https://github.com/JungWhoNam/SpatialSlides)**

## Project Structure

The core assets are located in `Assets/SlidesXR`. The project includes two types of scenes used in the evaluation:

* **Widgets - [Name]**: The full SpatialSlides system (Guided Mode). These scenes include the `SlideSyncController`, the authoring `Widget`, and the connection to PowerPoint.
* **Baseline - [Name]**: The standalone viewer (Unguided Mode) used as a control condition.

## Setup & Requirements

### Prerequisites
* **Unity 2022.3+**
* **Meta Quest 3** (Required for Passthrough/Mixed Reality).
* **Python Middleware**: This client requires the server to be running. See the [SpatialSlides_PythonMiddleware](https://github.com/JungWhoNam/SpatialSlides_PythonMiddleware) repository.

### Networking Configuration
The client expects the Python server to be listening on `localhost` (or the host PC's IP if running wirelessly):
* **Port 5557**: SUB Socket (Receives `AnimationStep`, `CurrentViewRefs`).
* **Port 5558**: PUSH Socket (Sends `CreateView` commands).

*Note: These ports are defined in `NetMQClient.cs`.*

## How to Run
1.  Ensure the **Python Middleware** is running and connected to PowerPoint.
2.  Open this project in Unity.
3.  Navigate to `Assets/SlidesXR`.
4.  Open one of the **Widget** scenes (e.g., `Widgets - king`).
5.  Press **Play** (using Quest Link) or Build to the headset.

## Controls & Interaction

### Authoring Mode (Edit)
* **Snapshot:** Place the model inside the **Reference Frame** (Wireframe Box). Click the **Capture (+)** button on the virtual UI to send the view to PowerPoint.
* **Link:** Click the checkmark to link a captured view to the active slide.
* **Navigation:** Advancing the slide in PowerPoint will automatically animate the model to the captured state.

### Exploration Mode
* **Grab & Move:** Grab the model using hand tracking to detach it from the presentation flow. The Reference Frame will appear to indicate synchronization is broken.
* **Resume:** Place the model back near the Reference Frame to re-sync with the slide deck.
