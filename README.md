# StudioSoundPro

**Open-Source C# Digital Audio Workstation (DAW) for macOS (Apple Silicon)**

StudioSoundPro is a cross-platform Digital Audio Workstation built with C# (.NET 9) and Avalonia UI, targeting macOS ARM devices first. The project follows a strict test-driven development approach with one feature implemented at a time, each gated by comprehensive unit tests and manual verification.

## 🎯 Project Status

**Current Version**: v0.1.0 - Feature #1 Complete  
**Target Platform**: macOS (Apple Silicon / M1+)  
**Framework**: .NET 9, Avalonia UI 11.3.8  
**License**: MIT

## ✅ Feature #1: Audio Device Abstraction

**Status**: COMPLETED ✅  
**Implementation Date**: November 4, 2025

### Purpose
Establish foundation for audio I/O by creating a robust abstraction layer for audio devices, including enumeration, selection, and basic audio callback functionality.

### Implementation
- **Core Interfaces**: `IAudioDevice`, `IAudioDeviceService`, `IAudioEngine`, `ISineGenerator`
- **Audio Device Service**: Mock implementation with device enumeration and selection
- **Audio Engine**: Simplified engine with state management and callback support
- **Sine Wave Generator**: Produces test tones at configurable frequency and amplitude (-12 dBFS)
- **CLI Tool**: Command-line interface for device listing and audio testing
- **UI Integration**: Full Avalonia UI with device selection, audio controls, and output meters

### Architecture

```
src/
├─ StudioSoundPro.Core/           # Core interfaces and audio components
│  └─ Audio/
│     ├─ IAudioDevice.cs         # Device abstraction interface
│     ├─ IAudioDeviceService.cs  # Device enumeration service interface
│     ├─ IAudioEngine.cs         # Audio processing engine interface
│     ├─ ISineGenerator.cs       # Audio generator interface
│     └─ SineGenerator.cs        # Sine wave implementation
├─ StudioSoundPro.AudioIO/        # Audio I/O implementations
│  └─ PortAudio/
│     ├─ PortAudioDevice.cs      # Device implementation
│     ├─ PortAudioDeviceService.cs # Mock device service
│     └─ PortAudioEngine.cs      # Simplified audio engine
├─ StudioSoundPro.UI/             # Avalonia UI application
│  ├─ ViewModels/
│  │  ├─ AudioTestViewModel.cs   # Audio testing UI logic
│  │  └─ ViewModelBase.cs        # Base viewmodel with disposal
│  └─ Views/
│     ├─ AudioTestView.axaml     # Main audio test interface
│     └─ MainWindow.axaml        # Application main window
└─ StudioSoundPro.CLI/            # Command-line tools
   └─ Program.cs                  # Device listing and testing CLI
```

### Key Features Implemented
1. **Device Enumeration**: List all available audio devices with capabilities
2. **Device Selection**: Choose specific devices for input/output
3. **Audio Engine**: Start/stop audio processing with callback support
4. **Sine Wave Generation**: Configurable frequency (20-20kHz) and amplitude (-60 to 0 dB)
5. **Real-time Metering**: Visual output level monitoring
6. **CLI Tools**: Command-line device listing and audio testing
7. **Cross-platform UI**: Native Avalonia interface with responsive controls

### Verification Results

#### Unit Tests ✅
- **Total Tests**: 7 passed, 0 failed
- **Coverage**: Core audio functionality
- **Test Results**:
  - `SineGenerator_produces_expected_frequency_and_level()` ✅
  - `SineGenerator_disabled_produces_silence()` ✅
  - `SineGenerator_reset_clears_phase()` ✅
  - `AudioDeviceService_ListDevices_returns_at_least_one_output()` ✅
  - `AudioDeviceService_GetDefaultOutputDevice_returns_valid_device()` ✅
  - `AudioEngine_StartStop_changes_state_and_invokes_callback()` ✅
  - `AudioEngine_requires_initialization_before_start()` ✅

#### Manual Testing ✅
- **Device Enumeration**: Successfully lists mock audio devices
- **UI Functionality**: All controls responsive and functional
- **Audio Generation**: Sine wave generator produces expected frequencies
- **State Management**: Audio engine state changes propagate correctly
- **Error Handling**: Graceful error handling and user feedback

#### CLI Testing ✅
```bash
# Device listing
dotnet run --project src/StudioSoundPro.CLI devices

# Output:
StudioSoundPro CLI - Audio Device Manager
=====================================
Available Audio Devices:
-----------------------
ID: 00 | Default Output Device [DEFAULT OUT]
      Input channels: 0, Output channels: 2
      Default sample rate: 48000 Hz

ID: 01 | Default Input Device [DEFAULT IN]
      Input channels: 2, Output channels: 0
      Default sample rate: 48000 Hz

ID: 02 | Built-in Output
      Input channels: 0, Output channels: 2
      Default sample rate: 48000 Hz

Default output device: Default Output Device
```

### Performance Characteristics
- **Engine Latency**: Mock implementation (real PortAudio integration planned)
- **UI Responsiveness**: Smooth 60fps UI updates with real-time level meters
- **Memory Usage**: Minimal allocations in audio callback path
- **CPU Usage**: Low impact for sine wave generation

### Technical Notes
- **Mock Implementation**: Current PortAudio integration is simplified for rapid development
- **Real-time Safety**: Audio callback designed for lock-free operation
- **Error Handling**: Comprehensive exception handling with user-friendly messages
- **Resource Management**: Proper disposal patterns for audio resources

### Future Improvements
- Full PortAudio integration with native device APIs
- ASIO driver support for ultra-low latency
- Multi-channel device support
- Device hot-plug detection
- Advanced audio routing capabilities

---

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- macOS (Apple Silicon recommended)
- Git

### Quick Start
```bash
# Clone repository
git clone https://github.com/reyisjones/StudioSoundPro.git
cd StudioSoundPro

# Build solution
dotnet build

# Run UI application
dotnet run --project src/StudioSoundPro.UI

# Run CLI tools
dotnet run --project src/StudioSoundPro.CLI devices
```

### Running Tests
```bash
# Run all unit tests
dotnet test

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## 📋 Roadmap

Following the [SystemGuidance](SystemGuidance.md) framework, features are implemented in strict order:

- [x] **Feature #1**: Audio Device Abstraction ✅
- [ ] **Feature #2**: Transport & Clock
- [ ] **Feature #3**: Track/Clip Model  
- [ ] **Feature #4**: Timeline View
- [ ] **Feature #5**: WAV Import/Export
- [ ] **Feature #6**: Mixer
- [ ] **Feature #7**: Latency Compensation

Each feature must pass:
- Unit tests (≥80% coverage)
- Manual functional testing
- Performance baseline
- Code quality gates

## 🏗️ Architecture

StudioSoundPro follows clean architecture principles:

- **Core**: Domain models and interfaces
- **AudioIO**: Platform-specific audio implementations  
- **UI**: Cross-platform Avalonia interface
- **CLI**: Command-line tools and utilities

## 🧪 Quality Assurance

- **Test Framework**: xUnit with FluentAssertions
- **Coverage**: Coverlet for code coverage analysis
- **Static Analysis**: StyleCop.Analyzers, SecurityCodeScan
- **Performance**: BenchmarkDotNet for performance regression testing

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Follow the [SystemGuidance](SystemGuidance.md) development framework
2. One feature at a time, fully tested before moving on
3. Maintain cross-platform compatibility
4. Keep performance and security as top priorities

---

**StudioSoundPro** - Building the future of open-source digital audio workstations, one feature at a time.

