import com.vanniktech.maven.publish.AndroidSingleVariantLibrary
import org.jetbrains.kotlin.gradle.dsl.JvmTarget

plugins {
    alias(libs.plugins.android.library)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.dokka)
    alias(libs.plugins.vanniktech.maven.publish)
    alias(libs.plugins.signing)
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
    namespace = "com.blaze.pdfium"
    compileSdk = blazeCompileSdk
    buildToolsVersion = blazeBuildTools
    ndkVersion = blazeNdk

    defaultConfig {
        minSdk = blazeMinSdk

        @Suppress("UnstableApiUsage")
        externalNativeBuild {
            cmake {
                arguments.addAll(
                    elements = listOf(
                        "-DANDROID_STL=c++_shared",
                        "-DANDROID_PLATFORM=android-${minSdk}",
                        "-DANDROID_ARM_NEON=TRUE",
                    )
                )

                cppFlags("-std=c++17", "-frtti", "-fexceptions")
            }
        }

        ndk {
            abiFilters += listOf("arm64-v8a", "armeabi-v7a", "x86", "x86_64")
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(getDefaultProguardFile(name = "proguard-android-optimize.txt"), "proguard-rules.pro")
        }
    }

    externalNativeBuild {
        cmake {
            version = "4.1.2"
            path(path = "src/main/cpp/CMakeLists.txt")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlin {
        compilerOptions {
            jvmTarget.set(JvmTarget.JVM_17)
        }
    }

    lint {
        checkAllWarnings = true
        warningsAsErrors = false
        abortOnError = false
        disable.addAll(elements = setOf("TypographyFractions", "TypographyQuotes", "Typos"))
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.coroutines.android)
    implementation(libs.coroutines.core)
    implementation(libs.coroutines.play.services)
}


// Copy AAR to Binding project after build
tasks.register<Copy>("copyAarToBinding") {
    group = "distribution"
    description = "Copy libPdfium release AAR to Binding project"
    dependsOn(tasks.named("assembleRelease"))
    from(layout.buildDirectory.file("outputs/aar/libPdfium-release.aar"))
    into(bindingJarsDir)
    rename { "blaze-pdfium-$blazeVersion.aar" }
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
        artifactId = "blaze-pdfium",
        version = blazeVersion
    )

    pom {
        name.set("BlazePdfium")
        description.set("Blaze Pdfium Library for Android (API 21 binding)")
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
