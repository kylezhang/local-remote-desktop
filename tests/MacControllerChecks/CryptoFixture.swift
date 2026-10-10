// Linux-only CryptoKit-shaped test adapter. Actual crypto uses OpenSSL; native
// CryptoKit/Apple SDK acceptance remains a separate test gate.
import Foundation
@_silgen_name("fixture_hmac") private func hmac(_ key: UnsafeRawPointer?, _ length: Int32, _ data: UnsafeRawPointer?, _ count: Int32, _ output: UnsafeMutableRawPointer?) -> Int32
@_silgen_name("fixture_equal") private func equal(_ a: UnsafeRawPointer?, _ b: UnsafeRawPointer?, _ length: Int32) -> Int32
@_silgen_name("fixture_gcm") private func gcm(_ encrypt: Int32, _ key: UnsafeRawPointer?, _ nonce: UnsafeRawPointer?, _ aad: UnsafeRawPointer?, _ aadSize: Int32, _ data: UnsafeRawPointer?, _ count: Int32, _ output: UnsafeMutableRawPointer?, _ tag: UnsafeMutableRawPointer?) -> Int32
public struct SymmetricKey {
    public enum Size { case bits256 }
    let bytes: Data
    public init(data: Data) { bytes = data }
    public init(size: Size) { bytes = Data((0..<32).map { _ in UInt8.random(in: 0...255) }) }
}
public enum SHA256 {}
public enum FixtureError: Error { case invalid }
public enum HMAC<T> {
    public static func authenticationCode(for data: Data, using key: SymmetricKey) -> Data {
        var output = Data(count: 32)
        let ok = key.bytes.withUnsafeBytes { k in data.withUnsafeBytes { d in output.withUnsafeMutableBytes { o in
            hmac(k.baseAddress,Int32(key.bytes.count),d.baseAddress,Int32(data.count),o.baseAddress)
        } } }; precondition(ok == 1); return output
    }
    public static func isValidAuthenticationCode(_ proof: Data, authenticating data: Data, using key: SymmetricKey) -> Bool {
        let expected = authenticationCode(for:data,using:key)
        guard proof.count == expected.count else { return false }
        return proof.withUnsafeBytes { a in expected.withUnsafeBytes { b in equal(a.baseAddress,b.baseAddress,Int32(proof.count)) == 1 } }
    }
}
public enum AES { public enum GCM {
    public struct Nonce {
        let data: Data
        public init(data: Data) throws { guard data.count == 12 else { throw FixtureError.invalid }; self.data = data }
    }
    public struct SealedBox {
        let nonce: Nonce
        public let ciphertext: Data, tag: Data
        public init(nonce: Nonce, ciphertext: Data, tag: Data) throws {
            guard tag.count == 16 else { throw FixtureError.invalid }; self.nonce = nonce; self.ciphertext = ciphertext; self.tag = tag
        }
    }
    private static func cipher(_ data: Data, key: SymmetricKey, nonce: Nonce, aad: Data, tag: inout Data, encrypt: Bool) throws -> Data {
        guard key.bytes.count == 32 else { throw FixtureError.invalid }
        var output = Data(count:data.count+16)
        let ok = key.bytes.withUnsafeBytes { k in nonce.data.withUnsafeBytes { n in aad.withUnsafeBytes { a in
            data.withUnsafeBytes { d in output.withUnsafeMutableBytes { o in tag.withUnsafeMutableBytes { t in
                gcm(encrypt ? 1 : 0,k.baseAddress,n.baseAddress,a.baseAddress,Int32(aad.count),d.baseAddress,Int32(data.count),o.baseAddress,t.baseAddress)
            } } }
        } } }; guard ok == 1 else { throw FixtureError.invalid }; return output.prefix(data.count)
    }
    public static func seal(_ data: Data, using key: SymmetricKey, nonce: Nonce, authenticating aad: Data) throws -> SealedBox {
        var tag = Data(count:16); let encrypted = try cipher(data,key:key,nonce:nonce,aad:aad,tag:&tag,encrypt:true)
        return try SealedBox(nonce:nonce,ciphertext:encrypted,tag:tag)
    }
    public static func open(_ box: SealedBox, using key: SymmetricKey, authenticating aad: Data) throws -> Data {
        var tag = box.tag; return try cipher(box.ciphertext,key:key,nonce:box.nonce,aad:aad,tag:&tag,encrypt:false)
    }
} }
