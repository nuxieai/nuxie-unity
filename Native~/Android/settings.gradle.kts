pluginManagement { repositories { google(); mavenCentral(); gradlePluginPortal() } }
dependencyResolutionManagement { repositories { google(); mavenCentral(); maven { url = uri("../../.native/maven") } } }
rootProject.name = "nuxie-unity-bridge"
