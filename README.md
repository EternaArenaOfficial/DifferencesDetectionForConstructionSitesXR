# DifferencesDetectionForConstructionSitesXR
An XR application built with Unity for real-time comparison and progress tracking on construction sites. It detects discrepancies between on-site structures and reference BIM models using spatial mapping and Mixed Reality visualization.

# Features
- QR Code-based model loading using Microsoft’s QR Tracking package.

- Reference model placement (pipes, installations, structures).

- Real-time spatial mapping and voxel-based difference detection.

- Week-based task segmentation with toggleable visibility and progress status.

- Progress tracking per model (e.g., Pipe1 50%, Pipe2 100%).

- CSV export of work progress for reporting and documentation.

# Built With
- Unity 2020.3.42f1

- Mixed Reality Toolkit (MRTK) 2.8

- Microsoft QR Code Tracking Package

FOR: HoloLens 2

# How It Works
Scan a QR Code

Each QR code contains identifying text corresponding to a construction model (e.g., "PipeSetA").
Once scanned, the app places the associated reference mesh at the correct position in the physical world.

Load & Place Reference Models
Reference models can be individual elements (pipes, conduits) or full-scale sections of a construction site.
Placement is precise thanks to QR-based anchoring and spatial understanding.

Detect Differences
The app uses Unity's spatial mapping + a voxel comparison algorithm to detect differences between:
- The reference model
- The scanned real-world geometry

Differences (e.g., missing or misaligned installations) are visualized in the XR interface.

Track Progress
Construction elements are grouped by weekly progress milestones.
Users can toggle week groups ON/OFF and mark them as complete.
Completed elements turn green, indicating success.

Generate CSV Report
Upon exiting or on-demand, a .csv file is generated:
Name,Percentage,Status
Pipe1,50%,Non Complete
Pipe2,100%,Complete
