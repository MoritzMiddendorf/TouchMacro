You are an expert Android app developer proficient in C# and Visual Studio 2022, specializing in developing modern Android applications using .NET MAUI. Your task is to build a fully functional Android app based strictly on the following specifications:

App Overview
Primary Purpose: A tap macro tool enabling users to record screen taps and playback (simulate) them automatically.

Target Audience: Gamers primarily, but generally useful for anyone needing tap automation.

Supported Devices: Android smartphones and tablets.

Core Functionalities (Initial Version)
Macro Recording

Start/stop recording taps via overlay controls.

Record precise tap coordinates (X,Y) and timing intervals between taps.

Macro Playback (Simulation):

Replay recorded taps accurately at original positions and intervals.

Overlay UI:

Minimalistic overlay controls always accessible above other running apps:

Start/stop recording

Play/stop playback

Overlay should remain draggable, responsive, unobtrusive.

Non-overlay UI (Settings Screen):

Simple settings screen accessible via main app icon; basic initially.

Technical Requirements
Programming Language & IDE: C# with Visual Studio 2022 using .NET MAUI framework.

Authentication & Backend: None required. Fully local execution.

Data Storage: Local storage only (SQLite or simple file storage) to persist macros.

UI/UX Design Guidelines
Modern, clean, minimalistic design with intuitive usability.

Accessibility & Performance
Smooth performance during recording/playback without noticeable lag.

Compatibility across various Android devices/tablets.

Permissions & Hardware Requirements
Clearly identify all necessary permissions:

Overlay permission ("Display over other apps")

Accessibility service permission (if required for simulating taps)

Storage permission for saving/loading macros locally
Clearly document why each permission is needed in code comments.

Monetization Strategy
No monetization initially; completely free without ads or purchases. Possibly add a donate button later.

Testing & Debugging
Write clean, modular, well-commented code optimized for readability and maintainability. Include debugging statements/logging clearly.

Deliverables & Project Structure
Provide structured .NET MAUI project files compatible with Visual Studio on Windows 11:

Logical separation into components:

Overlay UI component

Macro recording/playback logic component

Local storage/data management component

Basic settings UI component