import Foundation
public typealias CCPBKDFAlgorithm = UInt32
public typealias CCPseudoRandomAlgorithm = UInt32
public let kCCPBKDF2: UInt32 = 2, kCCPRFHmacAlgSHA256: UInt32 = 3, kCCSuccess: Int32 = 0
@_silgen_name("EVP_sha256") func sha256() -> OpaquePointer
@_silgen_name("PKCS5_PBKDF2_HMAC") func pbkdf(_ password: UnsafePointer<Int8>?, _ length: Int32, _ salt: UnsafePointer<UInt8>?, _ saltLength: Int32, _ rounds: Int32, _ digest: OpaquePointer, _ outputLength: Int32, _ output: UnsafeMutablePointer<UInt8>?) -> Int32
public func CCKeyDerivationPBKDF(_ algorithm: CCPBKDFAlgorithm, _ password: UnsafePointer<Int8>?, _ length: Int, _ salt: UnsafePointer<UInt8>?, _ saltLength: Int, _ prf: CCPseudoRandomAlgorithm, _ rounds: UInt32, _ output: UnsafeMutablePointer<UInt8>?, _ outputLength: Int) -> Int32 {
    guard algorithm == 2, prf == 3 else { return -1 }
    return pbkdf(password,Int32(length),salt,Int32(saltLength),Int32(rounds),sha256(),Int32(outputLength),output) == 1 ? 0 : -1
}
