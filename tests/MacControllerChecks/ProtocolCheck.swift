import Foundation
#if os(Linux)
import Glibc
#else
import Darwin
#endif
@main struct ProtocolCheck {
    static func main() throws {
        let mode = CommandLine.arguments[1], port = UInt16(CommandLine.arguments[2])!
        #if os(Linux)
        let fd = socket(AF_INET,Int32(SOCK_STREAM.rawValue),0)
        #else
        let fd = socket(AF_INET,SOCK_STREAM,0)
        #endif
        precondition(fd >= 0)
        let wire = Wire(fd,controller:true); defer { wire.close() }
        let password = mode == "legacy" ? String(repeating:"5c",count:32) : "Office-LAN#42"
        try wire.connect(address:"127.0.0.1",port:port)
        do {
            try wire.authenticateController(secret:password)
            if mode == "bad-proof" { fatalError("Bad host identity accepted") }
            try wire.json(["op":"handshake","version":1,"quality":0])
            let capabilities = try wire.receive(); _ = try ControllerRules.capabilities(capabilities)
            if mode == "replay" { _ = try wire.receive(); fatalError("Replay accepted") }
            let header = try wire.receive(); _ = try ControllerRules.header(header)
            if mode == "oversized" { _ = try wire.receivePacket(); fatalError("Oversized record accepted") }
            let packet = try wire.receivePacket(); precondition(packet.0 == 2 && packet.1.count == 100_000)
            do { try wire.send(type:1,payload:Data(count:65536)); fatalError("Oversized outgoing control accepted") } catch {}
            for message: [String: Any] in [["op":"key_event","vk":162,"down":true],["op":"key_event","vk":67,"down":true],
                                          ["op":"key_event","vk":67,"down":false],["op":"key_event","vk":162,"down":false],
                                          ["op":"mouse_move","x":0.5,"y":0.25],["op":"release_all"],["op":"disconnect"]] { try wire.json(message) }
            print("PASS production Swift controller wire: \(mode), mutual authentication, 100KiB video and ordered input")
        } catch {
            guard ["bad-proof","replay","oversized"].contains(mode) else { throw error }
            print("PASS production Swift controller rejects \(mode)")
        }
    }
}
