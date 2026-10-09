// Linux-only type/transport fixture. Uses the system OpenSSL tool for test
// SHA256; this is NOT Apple's CryptoKit implementation or native SDK validation.
import Foundation
public struct SHA256 {
    private var data = Data()
    public init() {}
    public mutating func update(data: Data) { self.data.append(data) }
    public func finalize() -> [UInt8] {
        let path = FileManager.default.temporaryDirectory.appendingPathComponent("localremote-test-hash-" + UUID().uuidString)
        try! data.write(to: path); defer { try? FileManager.default.removeItem(at: path) }
        let process = Process(), pipe = Pipe()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/openssl")
        process.arguments = ["dgst","-sha256","-binary",path.path]; process.standardOutput = pipe
        try! process.run(); let digest = pipe.fileHandleForReading.readDataToEndOfFile(); process.waitUntilExit()
        precondition(process.terminationStatus == 0 && digest.count == 32); return Array(digest)
    }
}
