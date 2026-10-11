import Foundation
@main enum Check {
    static func main() async throws {
        let salt=Data(0..<16), known="0dca4c657c16ea4e595de60ab5d6bad618ea62de9184dc41c08e40051905d87c"
        let value=try PairingCredential.password("Office-LAN#42",salt:salt)
        precondition(value.key.map { String(format:"%02x",$0) }.joined() == known)
        let restored=try PairingCredential.decode(value.encoded)
        precondition(restored.text == value.text && restored.key == value.key && restored.salt == salt)
        let legacy=try PairingCredential.decode(Data(repeating:7,count:32))
        precondition(legacy.salt == nil && legacy.encoded == legacy.key && legacy.text.count == 64)
        for text in ["1234567",String(repeating:"a",count:65),"hello world","中文password","abc\0defgh"] {
            do { _=try PairingCredential.password(text,salt:salt); fatalError("invalid password accepted") } catch { }
        }
        for data in [Data(), Data(repeating:0,count:60)] {
            do { _=try PairingCredential.decode(data); fatalError("corrupt key silently reset") } catch { }
        }
        let generated=PairingCredential.generated(using:Data(0..<8))
        precondition(generated.count == 8 && !generated.contains("0") && !generated.contains("1"))
        try PairingCredential.validateNew("Office#2")
        for text in ["1234567", "123456789", "Office-LAN#42", "abc defg", "中文abcdef"] {
            do { try PairingCredential.validateNew(text); fatalError("invalid new password accepted") } catch { }
        }
        let prep=SharingPreparation<PairingCredential>()
        _=try await prep.readKey { legacy }
        prep.replaceCachedKey(value)
        let updated=try await prep.readKey { fatalError("updated cache reread store") }
        precondition(updated.text == value.text)
        print("PASS production Swift password KDF/independent vector, record roundtrip, legacy key, policy/generator, corrupt rejection and save-cache replacement")
    }
}
