// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "WindowOpacityTuner",
    platforms: [.macOS(.v13)],
    targets: [
        .executableTarget(
            name: "WindowOpacityTuner",
            path: "Sources/WindowOpacityTuner"
        ),
    ]
)
