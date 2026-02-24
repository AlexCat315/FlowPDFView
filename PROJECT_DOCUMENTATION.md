# FlowPDFView Project Documentation

## 1. Project Overview

**FlowPDFView** is a cross-platform PDF rendering library built on **.NET MAUI**, supporting Android, iOS, macOS, and Windows. The project uses a layered architecture design that separates core abstractions, control implementations, and platform-specific code for easy maintenance and extension.

### Tech Stack
- **Framework**: .NET MAUI 10.0
- **Target Platforms**: Android (API 35+), iOS 13+, macOS Catalyst 15+, Windows
- **Language**: C# 12
- **Null Safety**: Enabled (Nullable Reference Types)

---

## 2. Project Structure

```
FlowPDFView/
├── Flow.PDFView.Core/                 # Core abstraction layer (platform-agnostic)
│   ├── Abstractions/
│   │   ├── IPdfViewCore.cs           # Core interface definition
│   │   ├── PdfSource.cs              # PDF source abstraction (file/URI/stream/bytes/asset)
│   │   ├── EventArgs.cs              # Event argument definitions
│   │   ├── PdfModels.cs              # Data models (search results/outline/text selection)
│   │   ├── FitPolicy.cs              # Page fit policy enum
│   │   ├── PdfDisplayMode.cs         # Display mode enum
│   │   ├── ScrollOrientation.cs      # Scroll orientation enum
│   │   └── PdfSourceTypeConverter.cs # Type converter
│   └── Class1.cs                     # Namespace placeholder
│
├── Flow.PDFView.Controls/             # Controls configuration layer
│   ├── PdfViewDefaults.cs             # Default configuration constants
│   └── Class1.cs                     # Namespace placeholder
│
├── FlowPDFView/                        # Main library project
│   ├── PdfView.cs                     # Main control implementation (inherits View)
│   ├── IPdfView.cs                    # Public interface (facade)
│   ├── PageAppearance.cs              # Page appearance configuration
│   ├── AppBuilderExtensions.cs        # MAUI extension methods
│   │
│   ├── Interfaces/
│   │   └── IPdfViewPlatformFeatures.cs  # Platform features interface
│   │
│   ├── DataSources/                   # PDF source implementations (main project)
│   │   ├── IPdfSource.cs             # Source interface
│   │   ├── FilePdfSource.cs           # File source
│   │   ├── HttpPdfSource.cs          # HTTP source
│   │   ├── AssetPdfSource.cs         # Asset source
│   │   ├── ByteArrayPdfSource.cs     # Byte array source
│   │   └── PdfTempFileHelper.cs       # Temporary file helper
│   │
│   ├── Helpers/
│   │   └── DesiredSizeHelper.cs      # Size calculation helper
│   │
│   ├── Events/
│   │   └── PageChangedEventArgs.cs   # Page changed event
│   │
│   └── Platforms/                     # Platform-specific implementations
│       ├── Android/
│       │   ├── PdfViewAndroid.cs      # Android wrapper class
│       │   ├── PdfViewHandler.cs      # MAUI Handler
│       │   ├── ScreenHelper.cs        # Screen density helper
│       │   ├── Resources/layout/
│       │   │   └── card_view.xml      # Page card layout
│       │   └── Common/
│       │       ├── PdfBitmapAdapter.cs      # RecyclerView adapter
│       │       ├── CardViewHolder.cs       # ViewHolder
│       │       ├── ZoomableRecyclerView.cs # Zoomable RecyclerView
│       │       └── ZoomableLinearLayoutManager.cs
│       │
│       ├── iOS/
│       │   ├── PdfViewiOS.cs         # iOS wrapper class
│       │   └── PdfViewHandler.cs      # MAUI Handler
│       │
│       ├── MacCatalyst/
│       │   └── PdfViewHandler.cs      # macOS Handler
│       │
│       └── Windows/
│           └── PdfViewHandler.cs      # Windows Handler
│
├── FlowPDFView.Android.Binding/        # Android native binding
│   └── (Auto-generated Java binding code)
│
└── ExampleMauiApp/                     # Sample application
    ├── MainPage.xaml(.cs)             # Sample page
    ├── App.xaml(.cs)                  # App entry
    ├── MauiProgram.cs                  # MAUI program configuration
    └── Platforms/                      # Platform-specific configuration
```

---

## 3. Architecture Design

### 3.1 Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    User (App Developer)                     │
├─────────────────────────────────────────────────────────────┤
│  FlowPDFView (Main Library)                               │
│  ├── PdfView.cs - MAUI control with BindableProperty      │
│  ├── IPdfView.cs - Public facade interface                │
│  └── DataSources/ - PDF source implementations            │
├─────────────────────────────────────────────────────────────┤
│  Flow.PDFView.Controls                                    │
│  └── PdfViewDefaults.cs - Default configuration constants │
├─────────────────────────────────────────────────────────────┤
│  Flow.PDFView.Core (Core Abstractions)                   │
│  ├── IPdfViewCore.cs - Core interface                    │
│  ├── PdfSource abstraction + implicit   .cs - PDF source │
│  ├── EventArgs.cs - Event arguments                       │
│  ├── PdfModels.cs - Data models                          │
│  └── Enums (FitPolicy, PdfDisplayMode, ScrollOrientation)│
├─────────────────────────────────────────────────────────────┤
│  Platforms (Platform Implementations)                     │
│  ├── Android/ - PdfViewAndroid + Handler + Components    │
│  ├── iOS/    - PdfViewiOS + Handler                     │
│  ├── MacCatalyst/ - Handler                             │
│  └── Windows/ - Handler + PdfPig library                 │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 MAUI Handler Architecture

The project uses **MAUI Handlers** for cross-platform support:

1. **PdfView** (MAUI Controls) - Defines cross-platform properties
2. **PdfViewHandler** (Platform) - Maps properties to native controls
3. **PdfViewiOS/PdfViewAndroid** (Platform Wrapper) - Wraps native PDF rendering

```
XAML: <pdf:PdfView Source="document.pdf" />
         ↓
MAUI Handler Mapping
         ↓
Platform-specific implementation:
  - Android: PdfKit.Android (Blaze PDF viewer binding)
  - iOS/macOS: PdfKit (native framework)
  - Windows: PdfPig (third-party library)
```

---

## 4. Core Features

### 4.1 PDF Source (PdfSource)

Supports multiple PDF sources via static factory methods and implicit conversion:

```csharp
// Method 1: Implicit conversion from string
pdfView.Source = "https://example.com/doc.pdf";
pdfView.Source = "file:///storage/doc.pdf";
pdfView.Source = "asset://pdfs/document.pdf";
pdfView.Source = "document.pdf";  // Auto-recognized as asset

// Method 2: Factory methods
pdfView.Source = PdfSource.FromFile("/path/to/doc.pdf");
pdfView.Source = PdfSource.FromUri(new Uri("https://..."));
pdfView.Source = PdfSource.FromStream(stream);
pdfView.Source = PdfSource.FromBytes(byteArray);
pdfView.Source = PdfSource.FromAsset("document.pdf");
```

### 4.2 Core Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Source | PdfSource? | null | PDF source |
| EnableZoom | bool | true | Enable zoom gestures |
| EnableSwipe | bool | true | Enable swipe gestures (page turning) |
| EnableTapGestures | bool | false | Enable tap gestures |
| EnableLinkNavigation | bool | true | Enable link navigation |
| Zoom | float | 1.0 | Current zoom level |
| MinZoom | float | 1.0 | Minimum zoom level |
| MaxZoom | float | 10.0 | Maximum zoom level |
| PageSpacing | int | 10 | Page spacing |
| FitPolicy | FitPolicy | Width | Page fit policy |
| DisplayMode | PdfDisplayMode | SinglePageContinuous | Display mode |
| ScrollOrientation | PdfScrollOrientation | Vertical | Scroll direction |
| DefaultPage | int | 0 | Default page to display |
| EnableAntialiasing | bool | true | Antialiasing (Android only) |
| UseBestQuality | bool | true | Best quality rendering |
| EnableAnnotationRendering | bool | true | Render annotations |

### 4.3 Events

| Event | Args | Description |
|-------|------|-------------|
| DocumentLoaded | DocumentLoadedEventArgs | Document finished loading |
| PageChanged | PageChangedEventArgs | Page changed |
| Error | PdfErrorEventArgs | Error occurred |
| LinkTapped | LinkTappedEventArgs | Link tapped |
| Tapped | PdfTappedEventArgs | Page tapped |
| Rendered | RenderedEventArgs | Rendering completed |
| AnnotationTapped | AnnotationTappedEventArgs | Annotation tapped |
| SearchResultsFound | PdfSearchResultsEventArgs | Search results found |
| SearchProgress | PdfSearchProgressEventArgs | Search progress |

### 4.4 Methods

```csharp
// Navigate to specified page
pdfView.GoToPage(5);

// Reload document
pdfView.Reload();

// Search functionality
var results = await pdfView.SearchAsync("keyword");
pdfView.HighlightSearchResults(true);
pdfView.GoToSearchResult(0);
pdfView.ClearSearch();
```

---

## 5. Platform Implementation Details

### 5.1 Android

- **Native Library**: Blaze PDF Viewer (via Java binding)
- **Rendering**: `Com.Blaze.Pdfviewer.PDFView`
- **List**: `RecyclerView` + `PdfBitmapAdapter`
- **Zoom**: Custom `ZoomableRecyclerView`
- **Handler**: `PdfViewHandler` (partial class)

### 5.2 iOS/macOS

- **Native Framework**: PdfKit
- **Rendering**: `PdfKit.PdfView`
- **Gestures**: `UITapGestureRecognizer`, `UIPanGestureRecognizer`
- **Search**: Uses `PdfDocument.GetSelection()` API

### 5.3 Windows

- **Rendering Library**: PdfPig (UglyToad.PdfPig)
- **Display**: `ScrollViewer` + `Image` list
- **Text Extraction**: PdfPig built-in text extraction

---

## 6. Usage Examples

### 6.1 XAML Usage

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:pdf="clr-namespace:Flow.PDFView;assembly=Flow.PDFView">
    <pdf:PdfView x:Name="PdfViewer"
                 Source="https://example.com/document.pdf"
                 EnableZoom="True"
                 EnableSwipe="True"
                 PageSpacing="10"
                 FitPolicy="Width" />
</ContentPage>
```

### 6.2 C# Code Usage

```csharp
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        
        // Listen to events
        PdfViewer.DocumentLoaded += OnDocumentLoaded;
        PdfViewer.PageChanged += OnPageChanged;
        PdfViewer.Error += OnError;
        
        // Set source
        PdfViewer.Source = "asset://pdfs/sample.pdf";
        
        // Or use code
        PdfViewer.Source = PdfSource.FromFile(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "document.pdf"));
    }
    
    private void OnDocumentLoaded(object sender, DocumentLoadedEventArgs e)
    {
        Console.WriteLine($"Document loaded, {e.PageCount} pages total");
    }
}
```

---

## 7. Build and Release

### 7.1 Project Reference Relationships

```
Flow.PDFView (Main Library)
    ├── Flow.PDFView.Core
    ├── Flow.PDFView.Controls
    └── FlowPDFView.Android.Binding (Android only)
```

### 7.2 Target Frameworks

- `net10.0` - Default/shared code
- `net10.0-android` - Android specific
- `net10.0-ios` - iOS specific
- `net10.0-maccatalyst` - macOS Catalyst
- `net10.0-windows` - Windows specific (extra dependency: PdfPig)

### 7.3 Build Commands

```bash
# Build all platforms
dotnet build

# Build specific platform
dotnet build -f net10.0-android
dotnet build -f net10.0-ios

# Publish
dotnet publish -f net10.0-android
dotnet publish -f net10.0-ios
```

---

## 8. Key Design Decisions

### 8.1 Why Use PdfSource Abstraction?

- **Type Safety**: Different sources require different handling
- **Lazy Loading**: Streams and byte arrays need different lifecycle management
- **Platform Adaptation**: Different platforms handle sources differently

### 8.2 Why Use Handlers Instead of Renderer?

MAUI Handlers is the new recommended approach, compared to the old Renderer:
- More lightweight
- Simpler API
- Easier to maintain

### 8.3 Default Value Management

- `PdfViewDefaults.cs` centrally manages default values
- Easy to modify and document uniformly
- Supports independent Controls module publishing

---

## 9. Notes

### 9.1 Platform Requirements

| Platform | Min Version | Notes |
|----------|-------------|-------|
| Android | API 35 | Requires Blaze PDF binding |
| iOS | 13.0 | Uses PdfKit |
| macOS Catalyst | 15.0 | Uses PdfKit |
| Windows | 10+ | Uses PdfPig |

### 9.2 Permission Requirements

**Android** (`AndroidManifest.xml`):
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
```

### 9.3 Known Limitations

- Windows search functionality is incomplete
- Some deprecated APIs use `#pragma warning disable` to suppress warnings

---

## 10. Extension Development

### Adding New Properties

1. Add property definition in `IPdfViewCore.cs`
2. Add `BindableProperty` and property wrapper in `PdfView.cs`
3. Add mapping method in platform `PdfViewHandler.cs`
4. Implement logic in platform wrapper class
5. Add default value in `PdfViewDefaults.cs` (if applicable)

### Adding New Platforms

1. Create `Platforms/<Platform>/` directory
2. Create `<Platform>PdfView.cs` wrapper class
3. Create `PdfViewHandler.cs` processor
4. Add conditional reference in main project csproj
