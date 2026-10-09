// Linux-only socket API shape fixture. No real socket operations.
import Foundation
public typealias socklen_t = UInt32
public typealias sa_family_t = UInt8
public let AF_INET: Int32 = 2, SOCK_STREAM: Int32 = 1, SOL_SOCKET: Int32 = 1, SO_REUSEADDR: Int32 = 2, SHUT_RDWR: Int32 = 2
public struct in_addr { public var s_addr: UInt32 = 0; public init() {} }
public struct sockaddr_in {
    public var sin_len: UInt8 = 0, sin_family: sa_family_t = 0
    public var sin_port: UInt16 = 0
    public var sin_addr = in_addr()
    public init() {}
}
public struct sockaddr { public init() {} }
public func socket(_ family: Int32, _ kind: Int32, _ proto: Int32) -> Int32 { 200 }
@discardableResult public func setsockopt(_ fd: Int32, _ level: Int32, _ option: Int32, _ value: UnsafeRawPointer?, _ size: socklen_t) -> Int32 { 0 }
public func bind(_ fd: Int32, _ address: UnsafePointer<sockaddr>?, _ length: socklen_t) -> Int32 { 0 }
public func listen(_ fd: Int32, _ backlog: Int32) -> Int32 { 0 }
public func accept(_ fd: Int32, _ address: UnsafeMutablePointer<sockaddr>?, _ length: UnsafeMutablePointer<socklen_t>?) -> Int32 { -1 }
@discardableResult public func close(_ fd: Int32) -> Int32 { 0 }
@discardableResult public func shutdown(_ fd: Int32, _ how: Int32) -> Int32 { 0 }
