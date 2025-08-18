# DifferencesDetectionForConstructionSitesXR

[![Unity Version](https://img.shields.io/badge/Unity-2020.3.42f1-blue)](https://unity.com/releases/editor/whats-new/2020.3.42) 
[![MRTK Version](https://img.shields.io/badge/MRTK-2.8-green)](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/) 
[![Platform](https://img.shields.io/badge/Platform-HoloLens2-lightgrey)]()

An **XR application** built with **Unity** for **real-time progress tracking and discrepancy detection on construction sites**.  
It allows construction teams to compare on-site structures against reference BIM models, visualize missing or misaligned components, and generate progress reports.

---

## 🛠 Features

- **QR Code-Based Model Loading**  
  Scan QR codes to automatically load and place reference models at the correct location on-site.

- **Reference Model Placement**  
  Place individual construction elements (pipes, conduits) or entire sections with high precision.

- **Real-Time Spatial Mapping & Difference Detection**  
  Uses Unity's **Spatial Mapping** + **voxel-based comparison** to detect discrepancies between reference models and real-world scanned geometry.

- **Week-Based Task Segmentation**  
  Toggle visibility of construction elements grouped by weekly milestones.

- **Progress Tracking Per Model**  
  Mark elements as complete or incomplete. Completed items turn **green**.

- **CSV Export**  
  Export construction progress in `.csv` format for reporting and documentation.

---

## 🏗 Built With

- [Unity 2020.3.42f1](https://unity.com/releases/editor/whats-new/2020.3.42)  
- [Mixed Reality Toolkit (MRTK) 2.8](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/)  
- [Microsoft QR Code Tracking Package](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/features/ux/qr-code-tracking)  
- **Target Platform:** HoloLens 2  

---

## How It Works

### Scan a QR Code
Each QR code contains a unique identifier (e.g., `"PipeSetA"`) corresponding to a reference model.  
When scanned, the app anchors the reference mesh at the correct physical location.

### Load & Place Reference Models
- Models can be **individual elements** (pipes, conduits) or **full sections** of a construction site.  
- Placement is **precise** thanks to QR-based anchoring and spatial understanding.

### Detect Differences
- Combines **spatial mapping** with a **voxel comparison algorithm**.  

### Track Progress
- Group elements by **weekly milestones**.  
- Toggle week groups ON/OFF and mark tasks as complete.  
- Completed elements turn **green**.

### Generate CSV Report
- Export progress in `.csv` for documentation.  
Example:
Name,Percentage,Status
Pipe1,50%,Non Complete
Pipe2,100%,Complete

---

## 🚀 Getting Started

### Prerequisites
- **Unity 2020.3.42f1 (LTS)**  
- **Visual Studio 2019/2022** with **UWP development** workload  
- **HoloLens 2 device** or **HoloLens 2 Emulator**  
- **MRTK 2.8** installed in Unity  
- Microsoft **QR Code Tracking Package**

### Installation
1. Clone the repository:
```bash
git clone https://github.com/yourusername/DifferencesDetectionForConstructionSitesXR.git
```
2. Open the project in Unity 2020.3.42f1.
3. Import **MRTK 2.8** and **QR Code Tracking Package** if not included.
4. Build and deploy to **Hololens 2** using UWP.

---

## Usage Tips
- Ensure QR codes are printed clearly and placed on stable surfaces.
- For large reference models, break them into smaller sections to improve voxel detection performance.
- Use proper lighting conditions for accurate spatial mapping.

---

## References & Resources
- Unity Spatial Mapping: https://docs.unity3d.com/Manual/SpatialMapping.html
- MRTK Documentation: https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/
- HoloLens 2 Development Guide: https://learn.microsoft.com/en-us/windows/mixed-reality/develop/
- Voxel-Based Mesh Comparison Concepts: https://www.sciencedirect.com/topics/engineering/voxel
