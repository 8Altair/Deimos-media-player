# Deimos Media Player

A lightweight, feature-rich media player built with C# and WPF for playing audio, video, and displaying images.

## About

Deimos Media Player is a modern desktop media player application that provides an intuitive interface for managing and playing multimedia files. It supports multiple audio and video formats with advanced features like shuffle and repeat modes, embedded artwork extraction, and a responsive user interface designed for seamless playback control and media management.

## Features

### 🎵 Multi-Format Support
- **Audio Formats**: MP3, FLAC, WAV, WMA, M4A
- **Video Formats**: MP4, AVI, WMV
- **Image Formats**: PNG, JPG, JPEG

### 🎨 Media Management
- Load media files from any user-specified directory
- Add, edit, and remove media items from the playlist
- Edit media metadata including title, artist, and album information
- Automatic metadata extraction from media files using TagLib#

### 🎧 Playback Features
- Play, pause, and stop controls
- Volume adjustment with slider control
- Shuffle mode for randomized playback
- Repeat mode for looping tracks
- Previous/Next track navigation
- Automatic duration detection for audio and video files
- Embedded artwork extraction and caching from audio files

### 🖼️ Visual Features
- Display and view image files (PNG, JPG, JPEG)
- Automatic artwork extraction from audio metadata
- Fallback cover art for media without embedded artwork
- Clean and intuitive user interface
- Image preview with shuffle capability

### 📝 Playlist Management
- Add sample media items for testing
- Edit individual media properties
- Remove unwanted items from playlist
- Display current playing status

## Installation & Setup

### Requirements
- Windows OS (WPF application)
- .NET Framework 4.7+ or .NET 6+
- Rider or compatible C# IDE for building

### Building from Source

1. Open the project in IDE
2. Restore NuGet packages
3. Build the solution
4. Run the Deimos.UI application

## Usage Guide

### Loading Media

1. **Initialize Media Directory**:
   - Use the `LoadMediaFilesFromDirectory()` method in MainViewModel
   - Specify any directory containing your media files
   - The application automatically scans and catalogs all supported files

2. **Browse & Select**:
   - View all detected media files in the playlist
   - Select individual items to view metadata
   - Preview artwork and duration information

### Playback Control

1. **Play Media**:
   - Select a media item from the playlist
   - Click the Play button or use the Play/Pause command
   - Track information appears in the "Now Playing" label

2. **Adjust Settings**:
   - Use the volume slider to control playback volume
   - Enable/disable shuffle for random playback order
   - Enable/disable repeat to loop the current track
   - Use Previous/Next buttons to navigate tracks

### Managing Your Playlist

1. **Add Media**:
   - Click "Add Media" button to open file picker
   - Browse and select audio, video, or image files
   - Enter or verify metadata (title, artist, album, duration)
   - Confirm to add item to playlist

2. **Edit Media**:
   - Select an item in the playlist
   - Click "Edit Media" to open editor window
   - Modify title, artist, album, or cover artwork
   - Update duration if necessary
   - Save changes to apply

3. **Remove Media**:
   - Select an item in the playlist
   - Click "Remove" to delete from playlist
   - Confirm removal action

## Technical Architecture

### Project Structure

```
Deimos.UI/
├── Models/
│   ├── MediaFile.cs           # Core media item data model
│   ├── MediaExtensions.cs     # Supported file extension constants
│   └── RelayCommand.cs        # MVVM command implementation
├── ViewModels/
│   └── MainViewModel.cs       # Primary application view model
├── Services/
│   └── MediaPlayback.cs       # Media playback engine and logic
├── Windowing/
│   ├── MainWindow.xaml        # Primary application window
│   ├── AddMediaWindow.xaml    # Media addition dialog
│   └── EditMediaWindow.xaml   # Media editing dialog
├── Assets/
│   ├── Icons/                 # UI icon resources
│   └── Default_cover/         # Default artwork assets
└── App.xaml                   # Application configuration
```

### Architecture Pattern

**MVVM (Model-View-ViewModel)**
- Separation of concerns between UI and logic
- Data binding for reactive UI updates
- Command pattern for user interactions

### Key Components

#### MediaFile Model
Represents individual media items with properties:
- Title, Artist, Album
- FilePath, ImagePath
- Duration, IsPlaying status
- Property change notifications for UI binding

#### MediaPlayback Service
Handles all playback operations:
- Loading media from user-specified directories
- Artwork extraction from tagged files
- URI resolution and validation
- Playback control (play, pause, stop)
- Volume management
- Format detection and validation

#### MainViewModel
Orchestrates application logic:
- Playlist management
- Playback state synchronization
- Shuffle and repeat mode handling
- Image preview scheduling
- Command routing and execution

## Core Functionality Details

### Media Discovery & Loading

The application provides flexible media discovery through the `LoadMediaFilesFromDirectory()` method:
- Scans specified directory for supported formats
- Validates file extensions against supported types
- Extracts metadata using TagLib# library
- Creates MediaFile objects with complete information
- Handles errors gracefully with debug logging

### Metadata Extraction

Automatic metadata extraction includes:
- Title from file tags or filename
- Artist and Album information
- Duration calculation
- Embedded artwork detection and extraction
- Fallback to default artwork when unavailable

### Playback Engine

URI-based playback system supporting:
- Local file paths
- Network URIs
- Embedded resources (pack:// protocol)
- Format detection and validation
- Media element integration
- Error handling and recovery

### Artwork Management

Comprehensive artwork handling:
- Embedded artwork extraction from audio files
- Cached storage in application cache directory
- Automatic cleanup and organization
- Support for embedded resources
- Fallback artwork for unsupported formats

## Supported Media Formats

| Type | Extensions |
|------|-----------|
| **Audio** | .mp3, .flac, .wav, .wma, .m4a |
| **Video** | .mp4, .avi, .wmv |
| **Images** | .png, .jpg, .jpeg |

## Development Notes

### Dependencies
- **TagLib#**: For audio metadata extraction and artwork handling
- **WPF**: Windows Presentation Foundation for UI
- **.NET Framework/Core**: Runtime environment

### Key Technologies
- C# 9.0+ language features
- MVVM pattern implementation
- Data binding and commands
- ObservableCollection for dynamic lists
- DispatcherTimer for UI scheduling
- File I/O and path management

### Debug Output

The application provides comprehensive debug logging:
- Media loading progress and statistics
- Playback events and state changes
- Metadata extraction details
- Artwork handling operations
- Error conditions and recovery attempts

## Performance Considerations

### Efficient Media Loading
- Directory scanning with file type filtering
- Lazy metadata extraction only when needed
- Cached artwork prevents redundant extraction
- Asynchronous operations where applicable

### Memory Management
- ObservableCollection for dynamic item management
- Proper resource cleanup on application close
- Artwork caching to avoid repeated extraction
- DispatcherTimer cleanup for image preview scheduling

### UI Responsiveness
- Command pattern prevents UI blocking
- Debounced input handling for text changes
- Efficient data binding
- Minimal UI updates

## Troubleshooting

### Media Files Not Loading
- Verify file format is in the supported list
- Check file path is accessible and valid
- Ensure sufficient permissions on directory
- Check debug output for specific error messages

### Artwork Not Displaying
- Verify image file exists at specified path
- Check image format is supported (.png, .jpg, .jpeg)
- Ensure file has read permissions
- TagLib# may fail on corrupted metadata - check file integrity

### Playback Issues
- Verify media file format is supported
- Check file is not corrupted
- Ensure volume is not muted
- Review debug output for specific errors

### Performance Issues
- Reduce number of items in large playlists
- Check system resources (disk, memory)
- Verify artwork cache directory has space
- Consider directory with fewer media files

## License

This project is licensed under the MIT License - allowing free use, modification, and distribution for both personal and commercial purposes.

---

## ⚠️ Disclaimer

**Deimos Media Player is a WPF learning/portfolio project and is not an official commercial product.** This is a demonstration application built as a desktop development exercise. It is provided as-is for educational and personal use purposes. Users should test the application thoroughly before using it with important media files or in production environments.
