plugins {
    alias(libs.plugins.android.library) apply false
    alias(libs.plugins.kotlin.android) apply false
}

val blazeVersion: String = providers.gradleProperty("blaze.version").orElse("1.0.0").get()
val bindingJarsDirPath: String = providers.gradleProperty("blaze.binding.jars.dir")
    .orElse("../FlowPDFView.Android.Binding/Jars")
    .get()
val bindingJarsDir = rootProject.file(bindingJarsDirPath)

val expectedPdfiumAar = "blaze-pdfium-$blazeVersion.aar"
val expectedPdfViewerAar = "blaze-pdfviewer-$blazeVersion.aar"

tasks.register<Delete>("cleanBindingAars") {
    group = "distribution"
    description = "Remove stale Blaze AARs from FlowPDFView.Android.Binding/Jars"

    delete(
        fileTree(bindingJarsDir).matching {
            include("blaze-pdfium-*.aar")
            include("blaze-pdfviewer-*.aar")
        }
    )
}

tasks.register("copyAarsToBinding") {
    group = "distribution"
    description = "Build both Android libraries and copy versioned AARs to Binding project"

    dependsOn("cleanBindingAars")
    dependsOn(project(":libPdfium").tasks.named("assembleRelease"))
    dependsOn(project(":libPdfViewer").tasks.named("assembleRelease"))
    dependsOn(project(":libPdfium").tasks.named("copyAarToBinding"))
    dependsOn(project(":libPdfViewer").tasks.named("copyAarToBinding"))
}

tasks.register("verifyBindingAars") {
    group = "verification"
    description = "Validate expected versioned AARs exist in Binding project"
    dependsOn("copyAarsToBinding")

    doLast {
        val pdfium = bindingJarsDir.resolve(expectedPdfiumAar)
        val viewer = bindingJarsDir.resolve(expectedPdfViewerAar)

        if (!pdfium.exists()) {
            throw GradleException("Missing AAR: ${pdfium.path}")
        }
        if (!viewer.exists()) {
            throw GradleException("Missing AAR: ${viewer.path}")
        }
        if (pdfium.length() <= 0L || viewer.length() <= 0L) {
            throw GradleException("AAR size validation failed under ${bindingJarsDir.path}")
        }
    }
}

tasks.register("buildAll") {
    group = "build"
    description = "Build Android artifacts, copy to binding project, and validate outputs"
    dependsOn("verifyBindingAars")
}
