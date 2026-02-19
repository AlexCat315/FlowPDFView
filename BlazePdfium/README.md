[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Maven Central](https://img.shields.io/maven-central/v/io.github.blazepdf/blaze-pdfium.svg?label=blaze-pdfium)](https://central.sonatype.com/artifact/io.github.blazepdf/blaze-pdfium)
[![Maven Central](https://img.shields.io/maven-central/v/io.github.blazepdf/blaze-pdfviewer.svg?label=blaze-pdfviewer)](https://central.sonatype.com/artifact/io.github.blazepdf/blaze-pdfviewer)

# BlazePdfium

## 简介

BlazePdfium 是一个高性能的 Android PDF 渲染库，基于 Google PDFium 原生库开发。该项目是 [barteksc/PdfiumAndroid](https://github.com/barteksc/PdfiumAndroid) 和 [barteksc/AndroidPdfViewer](https://github.com/DImuthuUpe/AndroidPdfViewer) 的维护分支。

## 项目组成

- **libPdfium**: PDFium 原生库绑定，提供底层 PDF 渲染功能
- **libPdfViewer**: Android PDF 查看器组件，提供高级 PDF 显示功能

## 版本与构建规范

- 单一版本源：`BlazePdfium/gradle.properties` 中的 `blaze.version`
- 统一构建入口：在 `BlazePdfium/` 运行 `./gradlew buildAll`
- `buildAll` 会执行：
  1. 清理 `FlowPDFView.Android.Binding/Jars` 旧版 Blaze AAR
  2. 构建 `libPdfium` / `libPdfViewer` 的 `release` AAR
  3. 拷贝版本化产物到 Binding 目录
  4. 校验目标产物存在且非空

示例：

```bash
cd BlazePdfium
./gradlew buildAll
./gradlew -Pblaze.version=1.1.0 buildAll
./scripts/release-android.sh 1.1.0
```

---

## 功能增强

### 新增功能

- **支持 16 KB 页面大小**  
  确保与现代使用更大内存页面的 Android 设备兼容。  
  ([Android 文档](https://developer.android.com/guide/practices/page-sizes))

### 问题修复

- **首页渲染问题**  
  修复了当首页高度较小时，首页渲染不完整的 bug。  
  这会导致偏移量计算错误和部分渲染问题，特别是在启用 *snap page* 或缩放返回后。

---

## Enhancements

### Added

- **Support for 16 KB page sizes**  
  Ensures compatibility with modern Android devices that use larger memory pages.  
  ([Android documentation](https://developer.android.com/guide/practices/page-sizes))

### Fixed

- **First page rendering issue**  
  Resolved a bug where the first page rendered incompletely when its height was small.  
  This caused incorrect offset calculations and partial rendering, particularly with *snap page* enabled or
  after zooming back.

---

## 安装

### 添加依赖

在项目的 `build.gradle` 中添加：

```groovy
implementation 'io.github.blazepdf:blaze-pdfium:1.0.0'
```

### ProGuard 配置

如果使用 ProGuard 进行代码混淆和压缩，需要在 `proguard-rules.pro` 文件中添加：

```proguard
-keep class com.blaze.pdfium.** { *; }
```

---

## libPdfium 使用示例

Pdfium 库提供了底层的 PDF 渲染功能，支持打开 PDF 文档、获取页面信息、渲染页面等。

```kotlin
fun openPdf(context: Context, file: File, password: String? = null) {
    val iv: ImageView = findViewById(R.id.imageView)
    val fd = ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY)
    val page = 0
    try {
        PdfiumCore(context).use {
            val pdfDocument: PdfDocument = it.newDocument(fd, password)
            pdfDocument.openPage(page)
            val width: Int = it.getPageWidthPoint(page)
            val height: Int = it.getPageHeightPoint(page)
            // ARGB_8888 - 最佳质量，高内存占用，可能导致 OutOfMemoryError
            // RGB_565 - 质量稍差，内存占用减半
            val bitmap: Bitmap = createBitmap(width, height, Bitmap.Config.ARGB_8888)
            it.renderPageBitmap(page, bitmap, 0, 0, width, height)
            // 如需渲染注释和表单字段，可在方法最后添加 'true' 参数
            iv.setImageBitmap(bitmap)
            printInfo(pdfDocument)
        }
    } catch (ex: IOException) {
        Log.e(TAG, ex.localizedMessage, ex)
        ex.printStackTrace()
    }
}

fun printInfo(doc: PdfDocument) {
    val meta: PdfDocument.Meta = doc.metaData
    Log.v(TAG, "标题 = ${meta.title}")
    Log.v(TAG, "作者 = ${meta.author}")
    Log.v(TAG, "主题 = ${meta.subject}")
    Log.v(TAG, "关键词 = ${meta.keywords}")
    Log.v(TAG, "创建者 = ${meta.creator}")
    Log.v(TAG, "生产器 = ${meta.producer}")
    Log.v(TAG, "创建日期 = ${meta.creationDate}")
    Log.v(TAG, "修改日期 = ${meta.modDate}")
    printBookmarksTree(doc.bookmarks, "-")
}

fun printBookmarksTree(tree: List<PdfDocument.Bookmark>, sep: String) {
    for (b in tree) {
        Log.v(TAG, "书签 $sep ${b.title}, 页码: ${b.pageIndex}")
        if (b.hasChildren) {
            printBookmarksTree(b.children, "$sep-")
        }
    }
}
```

---

# libPdfViewer

Android PDF 查看器组件，基于 libPdfium 构建，提供现代化的 PDF 查看体验。支持 API 24+。

## 安装

在项目的 `build.gradle` 中添加：

```groovy
implementation 'io.github.blazepdf:blaze-pdfviewer:1.0.1'
```

## 在布局中使用 PDFView

```xml
<com.blaze.pdfviewer.PDFView
        android:id="@+id/pdfView"
        android:layout_height="match_parent"
        android:layout_width="match_parent" />
```

## 加载 PDF 文件

所有可用配置选项（带默认值）：

```kotlin
pdfView.fromUri(Uri)              // 从 URI 加载
pdfView.fromFile(File)           // 从文件加载
pdfView.fromBytes(ByteArray)     // 从字节数组加载
pdfView.fromStream(InputStream) // 从输入流加载（流会被写入字节数组 - 原生代码无法使用 Java 流）
pdfView.fromSource(DocumentSource) // 从自定义数据源加载
pdfView.fromAsset(String)        // 从 Assets 资源加载
    .pages(0, 2, 1, 3, 3, 3)    // 过滤和排序页面（默认显示所有页面）
    .enableSwipe(true)           // 是否启用滑动手势切换页面
    .swipeHorizontal(false)      // 水平滑动
    .enableDoubletap(true)       // 启用双击缩放
    .defaultPage(0)              // 默认显示的页面
    .onDraw(onDrawListener)      // 在当前页面上绘制（通常在屏幕中间可见）
    .onDrawAll(onDrawListener)   // 在所有页面上绘制，仅对可见页面调用
    .onLoad(onLoadCompleteListener) // 文档加载完成后调用
    .onPageChange(onPageChangeListener) // 页面改变时调用
    .onPageScroll(onPageScrollListener) // 页面滚动时调用
    .onError(onErrorListener)    // 发生错误时调用
    .onPageError(onPageErrorListener) // 页面渲染错误时调用
    .onRender(onRenderListener)  // 首次渲染完成后调用
    .onTap(onTapListener)        // 单击事件，返回 true 表示已处理
    .enableAnnotationRendering(false) // 渲染注释（如评论、颜色或表单）
    .password(null)              // PDF 密码
    .scrollHandle(null)          // 滚动句柄
    .enableAntialiasing(true)    // 改善低分辨率屏幕的渲染效果
    .spacing(0)                  // 页面间距（dp），设置背景色可定义间距颜色
    .autoSpacing(false)          // 自动添加间距以适应屏幕
    .linkHandler(DefaultLinkHandler) // 链接处理器
    .pageFitPolicy(FitPolicy.WIDTH) // 页面适应策略
    .fitEachPage(true)           // 适应每个页面到视图
    .nightMode(false)            // 夜间模式
    .pageSnap(true)              // 页面吸附到屏幕边界
    .pageFling(false)            //  fling 切换单页（类似 ViewPager）
    .load()
```

注意：`pages` 是可选的，用于过滤和排序 PDF 页面。

## 滚动句柄

使用 PDFView **不需要**必须在 RelativeLayout 中，可以使用任何布局。

通过 `Configurator#scrollHandle()` 方法注册滚动句柄，需要实现 **ScrollHandle** 接口。

库中提供了默认实现：
- `.scrollHandle(DefaultScrollHandle(this))` - 默认在右侧（垂直滚动时）或底部（水平滚动时）
- `.scrollHandle(DefaultScrollHandle(this, true))` - 放置在左侧或顶部

也可以自定义实现 **ScrollHandle** 接口。

## 文档数据源

文档数据源是 PDF 文档的提供者，每个提供者实现 **DocumentSource** 接口。预定义的数据源位于 **com.blaze.pdfviewer.source.DocumentSource** 包中，可作为创建自定义数据源的参考。

使用简写方法：

```kotlin
pdfView.fromUri(Uri)       // 从 URI 加载
pdfView.fromFile(File)     // 从文件加载
pdfView.fromBytes(ByteArray)  // 从字节数组加载
pdfView.fromStream(InputStream) // 从输入流加载
pdfView.fromAsset(String)  // 从 Assets 加载
```

自定义数据源可使用 `pdfView.fromSource(DocumentSource)` 方法。

## 链接处理

默认使用 **DefaultLinkHandler**：
- 点击同一文档内的页面链接 → 跳转到目标页面
- 点击外部 URI 链接 → 在默认应用中打开

可自定义实现 **LinkHandler** 接口并通过 `Configurator#linkHandler(LinkHandler)` 方法设置。

## 页面适应策略

支持三种页面适应屏幕的模式：

- **WIDTH**: 最宽页面的宽度等于屏幕宽度
- **HEIGHT**: 最高页面的高度等于屏幕高度
- **BOTH**: 基于最宽和最高的页面，每个页面都缩放到完全可见

除选择的策略外，每个页面都会相对于其他页面进行缩放。

使用 `Configurator#pageFitPolicy(FitPolicy)` 设置，默认策略为 **WIDTH**。

## 高级选项

### Bitmap 质量

默认情况下，生成的位图使用 `RGB_565` 格式压缩以减少内存消耗。
可通过 `pdfView.useBestQuality(true)` 强制使用 `ARGB_8888`。

### 双击缩放

有三个缩放级别：最小（默认 1）、中间（默认 1.75）和最大（默认 3）。
- 第一次双击 → 放大到中间级别
- 第二次双击 → 放大到最大级别
- 第三次双击 → 缩小到最小级别

可在级别之间切换继续缩放。

更改缩放级别：

```kotlin
fun setMinZoom(minZoom: Float)
fun setMidZoom(midZoom: Float)
fun setMaxZoom(maxZoom: Float)
```

### 为什么无法从 URL 打开 PDF？

下载文件是长时间运行的过程，需要考虑 Activity 生命周期、配置变更、数据清理和缓存，因此创建此类模块可能会作为新库提供。

### 如何在配置变更后显示上次打开的页面？

需要存储当前页码，然后使用 `pdfView.defaultPage(page)` 设置，参考示例应用。

### 如何适应屏幕宽度（如屏幕方向更改时）？

使用 `FitPolicy.WIDTH` 策略，或在渲染时适配特定页面：

```kotlin
onRender(object : OnRenderListener {
    override fun onInitiallyRendered(totalPages: Int) {
        pdfView.fitToWidth(pageIndex)
    }
})
```

### 如何像 ViewPager 一样滚动单页？

使用以下配置组合获得类似 ViewPager 的滚动和 fling 行为：

```kotlin
pdfView.swipeHorizontal(true)
    .pageSnap(true)
    .autoSpacing(true)
    .pageFling(true)
```

---

# 许可证

```
Copyright 2021 blaze 

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```


优化清单（按优先级）


[P0] 修复 native 互斥锁双重解锁风险。mainJNILib.cpp (line 42) 在 lock_guard 生命周期内手动 unlock，导致 V8 状态损坏，引起 "Check failed: current_state != V8StartupState::kPlatformDisposed" 崩溃。（需重新编译 native 库）
