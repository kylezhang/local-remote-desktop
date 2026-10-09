// swift-tools-version: 5.9
import PackageDescription
let package = Package(name: "LocalRemote", platforms: [.macOS(.v13)], products: [.executable(name: "LocalRemote", targets: ["LocalRemote"])], targets: [.executableTarget(name: "LocalRemote")])
