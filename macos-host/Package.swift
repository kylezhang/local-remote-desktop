// swift-tools-version: 5.9
import PackageDescription
let package = Package(name: "RemoteMacLAN", platforms: [.macOS(.v13)], products: [.executable(name: "RemoteMacLAN", targets: ["RemoteMacLAN"])], targets: [.executableTarget(name: "RemoteMacLAN")])
