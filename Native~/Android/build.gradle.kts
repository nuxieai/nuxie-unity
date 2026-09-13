plugins {
    id("com.android.library") version "8.10.1"
    id("org.jetbrains.kotlin.android") version "2.0.21"
    id("maven-publish")
}
android {
    namespace = "ai.nuxie.unity"
    compileSdk = 36
    defaultConfig { minSdk = 23; consumerProguardFiles("consumer-rules.pro") }
    compileOptions { sourceCompatibility = JavaVersion.VERSION_17; targetCompatibility = JavaVersion.VERSION_17 }
    kotlinOptions { jvmTarget = "17" }
    publishing { singleVariant("release") }
}
val nativePins = groovy.json.JsonSlurper().parse(file("../../NATIVE-PINS.json")) as Map<*, *>
val androidRevision = (nativePins["android"] as Map<*, *>)["revision"] as String
dependencies {
    implementation("ai.nuxie:nuxie-android:0.2.0-$androidRevision")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.9.0")
}
afterEvaluate {
    publishing {
        publications { create<MavenPublication>("bridge") { from(components["release"]); groupId = "ai.nuxie"; artifactId = "nuxie-unity-bridge"; version = "0.2.0" } }
        repositories { maven { name = "Package"; url = uri("../../.native/maven") } }
    }
}
