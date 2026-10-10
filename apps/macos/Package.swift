// swift-tools-version: 5.9
import PackageDescription
let package = Package(name: "LocalRemoteDesktop", platforms: [.macOS(.v13)], products: [.executable(name: "LocalRemoteDesktop", targets: ["LocalRemoteDesktop"])], targets: [.executableTarget(name: "LocalRemoteDesktop")])
