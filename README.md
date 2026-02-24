[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
# Flow.PDFView
`Flow.PDFView` is a cross-platform .NET MAUI PDF control supporting Android, iOS, MacCatalyst (Windows is still a basic implementation).

## Core Capabilities
-   Native PDF rendering (not WebView)
-   Multiple data source loading: `Uri` / Local File / Asset / Byte Array / Stream
-   Page control: Zoom, Swipe direction, Display mode, Jump to page
-   Events: Document loaded, Page changed, Error, Tap, Link tap
-   Search API (Unified entry)
    -   `SearchAsync`
    -   `GoToSearchResult`
    -   `ClearSearch`
    -   `HighlightSearchResults`
    -   `SearchResultsFound` / `SearchProgress`

## Platform Feature Matrix
| Feature | Android | iOS | MacCatalyst | Windows |
|---|---|---|---|---|
| Basic rendering/Page turn/Zoom | ✅ | ✅ | ✅ | ✅ |
| Unified Search API | ✅ | ✅ | ✅ | ✅ |
| Search result navigation | ✅ | ✅ | ✅ | ✅ |
| Search result highlighting | ✅ | ✅ | ✅ | ✅ |

## Repository Structure
```
FlowPDFView/
├── FlowPDFView/ # Core MAUI control library
├── FlowPDFView.Android.Binding/ # Android native binding layer
├── BlazePdfium/ # Android PDF engine source code & build
└── ExampleMauiApp/ # Demo application
```

## Local Integration (ProjectReference)
In your MAUI project, directly reference the local library project:
```xml
<ItemGroup>
  <ProjectReference Include="..\FlowPDFView\Flow.PDFView.csproj" />
</ItemGroup>
```
And register it in `MauiProgram.cs`:
```csharp
builder
  .UseMauiApp<App>()
  .UseMauiPdfView();
```

## Local Android Dependency (BlazePdfium)
If you modify the `BlazePdfium` version or source code, please rebuild and ensure the binding project uses the latest artifacts:
1.  Execute `./gradlew buildAll` in the `BlazePdfium/` directory.
2.  This task will automatically clean old Blaze AARs, copy the new AAR to `FlowPDFView.Android.Binding/Jars/`, and complete verification.
3.  Rebuild:
```bash
cd BlazePdfium
./gradlew buildAll
cd ..
dotnet build ExampleMauiApp/ExampleMauiApp.csproj -f net10.0-android
```

## Quick Usage
```xml
<pdf:PdfView x:Name="PdfViewer" EnableZoom="True" EnableSwipe="True" DisplayMode="SinglePageContinuous" ScrollOrientation="Vertical" />
```
```csharp
PdfViewer.Source = new UriPdfSource(new Uri(""));
PdfViewer.GoToPage(3);
var results = await PdfViewer.SearchAsync("pdf", new PdfSearchOptions { Highlight = true, SearchAllPages = true, MaxResults = 200 });
if (results.Count > 0)
{
    PdfViewer.GoToSearchResult(0);
}
```

## Demo Application
Please refer to `ExampleMauiApp/README.md`:
-   URL / Local file loading
-   Page turn and Zoom
-   Search + Previous/Next result navigation
-   Toolbar overlay interaction

## Build Verification (This Repository)
```bash
dotnet build FlowPDFView/Flow.PDFView.csproj -f net10.0-android
dotnet build FlowPDFView/Flow.PDFView.csproj -f net10.0-ios
dotnet build FlowPDFView/Flow.PDFView.csproj -f net10.0-maccatalyst
dotnet build ExampleMauiApp/ExampleMauiApp.csproj -f net10.0-android
```
