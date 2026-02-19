import com.vanniktech.maven.publish.AndroidSingleVariantLibrary
import org.jetbrains.kotlin.gradle.dsl.JvmTarget

plugins {
    alias(libs.plugins.android.library)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.dokka)
    alias(libs.plugins.vanniktech.maven.publish)
}

val blazeGroup = providers.gradleProperty("blaze.group").orElse("io.github.blazepdf").get()
val blazeVersion = providers.gradleProperty("blaze.version").orElse("1.0.0").get()
val blazeCompileSdk = providers.gradleProperty("blaze.compileSdk").map(String::toInt).orElse(36).get()
val blazeMinSdk = providers.gradleProperty("blaze.minSdk").map(String::toInt).orElse(21).get()
val blazeBuildTools = providers.gradleProperty("blaze.buildToolsVersion").orElse("36.1.0").get()
val blazeNdk = providers.gradleProperty("blaze.ndkVersion").orElse("29.0.14206865").get()
val bindingJarsDir = rootProject.file(
    providers.gradleProperty("blaze.binding.jars.dir")
        .orElse("../FlowPDFView.Android.Binding/Jars")
        .get()
)

android {
    namespace = "com.blaze.pdfviewer"
    compileSdk = blazeCompileSdk
    buildToolsVersion = blazeBuildTools
    ndkVersion = blazeNdk

    defaultConfig {
        minSdk = blazeMinSdk
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(getDefaultProguardFile(name = "proguard-android-optimize.txt"), "proguard-rules.pro")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    lint {
        checkAllWarnings = true
        warningsAsErrors = false
        abortOnError = false
        disable.addAll(elements = setOf("TypographyFractions", "TypographyQuotes", "Typos"))
    }

    kotlin {
        compilerOptions {
            jvmTarget.set(JvmTarget.JVM_17)
            freeCompilerArgs.addAll(
                "-opt-in=kotlin.RequiresOptIn",
                "-opt-in=org.readium.r2.shared.InternalReadiumApi"
            )
        }
    }
}

dependencies {
    implementation(project(":libPdfium"))
    implementation(libs.androidx.core.ktx)
    implementation(libs.coroutines.android)
    implementation(libs.coroutines.core)
    implementation(libs.coroutines.play.services)
}



// Copy AAR to Binding project after build
tasks.register<Copy>("copyAarToBinding") {
    group = "distribution"
    description = "Copy libPdfViewer release AAR to Binding project"
    dependsOn(tasks.named("assembleRelease"))
    from(layout.buildDirectory.file("outputs/aar/libPdfViewer-release.aar"))
    into(bindingJarsDir)
    rename { "blaze-pdfviewer-$blazeVersion.aar" }
}

mavenPublishing {
    configure(
        platform = AndroidSingleVariantLibrary(
            variant = "release",
            sourcesJar = true,
            publishJavadocJar = true,
        )
    )
    publishToMavenCentral()
    signAllPublications()

    coordinates(
        groupId = blazeGroup,
        artifactId = "blaze-pdfviewer",
        version = blazeVersion
    )

    pom {
        name.set("BlazePDFViewer")
        description.set("Android view for displaying PDFs rendered with PdfiumAndroid")
        inceptionYear.set("2026")
        url.set("https://github.com/blazepdf/BlazePdfium/")

        licenses {
            license {
                name.set("The Apache License, Version 2.0")
                url.set("http://www.apache.org/licenses/LICENSE-2.0.txt")
                distribution.set("http://www.apache.org/licenses/LICENSE-2.0.txt")
            }
        }

        developers {
            developer {
                id.set("blazepdf")
                name.set("Blaze Team")
                email.set("cyke0315@gmail.com")
                url.set("https://github.com/AlexCat315/")
                roles.set(listOf("owner", "developer"))
            }
        }

    }
}
