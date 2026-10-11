import Foundation
import CommonCrypto
struct PairingCredential {
    static let rounds: UInt32 = 600_000
    static let marker = Data("LOCALREMOTE-PAIRING-PASSWORD-V1:".utf8)
    let text: String
    let key: Data
    let salt: Data?
    static func validate(_ password: String) throws {
        guard (8...64).contains(password.utf8.count), password.utf8.allSatisfy({ $0 > 32 && $0 < 127 }) else {
            throw NSError(domain: "LocalRemoteDesktop.Pairing", code: 1, userInfo: [NSLocalizedDescriptionKey:"密码需为 8–64 位英文字母、数字或符号，不含空格。"])
        }
    }
    static func validateNew(_ password: String) throws {
        try validate(password)
        guard password.utf8.count == 8 else {
            throw NSError(domain: "LocalRemoteDesktop.Pairing", code: 1, userInfo: [NSLocalizedDescriptionKey:"新密码需为 8 位英文字母、数字或符号，不含空格。"])
        }
    }
    static func password(_ text: String, salt: Data) throws -> PairingCredential {
        try validate(text)
        guard salt.count == 16 else { throw NSError(domain: "LocalRemoteDesktop.Pairing", code: 2) }
        var key = Data(count: 32)
        let result = key.withUnsafeMutableBytes { output in
            salt.withUnsafeBytes { saltBytes in
                text.withCString { password in
                    CCKeyDerivationPBKDF(CCPBKDFAlgorithm(kCCPBKDF2), password, text.utf8.count,
                        saltBytes.bindMemory(to: UInt8.self).baseAddress, salt.count,
                        CCPseudoRandomAlgorithm(kCCPRFHmacAlgSHA256), rounds,
                        output.bindMemory(to: UInt8.self).baseAddress, 32)
                }
            }
        }
        guard result == kCCSuccess else { throw NSError(domain: "LocalRemoteDesktop.Pairing", code: Int(result)) }
        return PairingCredential(text: text, key: key, salt: salt)
    }
    static func decode(_ data: Data) throws -> PairingCredential {
        if data.count == 32 { return PairingCredential(text: data.map { String(format:"%02x",$0) }.joined(), key: data, salt: nil) }
        guard data.starts(with: marker), data.count > marker.count + 16,
              let text = String(data: data.dropFirst(marker.count + 16), encoding: .utf8) else {
            throw NSError(domain: "LocalRemoteDesktop.Pairing", code: 3, userInfo: [NSLocalizedDescriptionKey:"已保存的配对信息无效，不会自动替换。"])
        }
        return try password(text, salt: Data(data.dropFirst(marker.count).prefix(16)))
    }
    var encoded: Data { if let salt { return Self.marker + salt + Data(text.utf8) }; return key }
    static func generated(using bytes: Data) -> String {
        precondition(bytes.count == 8)
        let alphabet = Array("ABCDEFGHJKLMNPQRSTUVWXYZ23456789".utf8)
        return String(bytes: bytes.map { alphabet[Int($0 & 31)] }, encoding: .utf8)!
    }
}
