import AppKit

enum MacControllerClipboard {
    static func make() -> TextClipboardSync {
        let board = NSPasteboard.general
        return TextClipboardSync(revision: { board.changeCount },
            read: { (true, board.types?.contains(.fileURL) == true ? nil : board.string(forType: .string)) },
            write: { text in board.clearContents(); return board.setString(text, forType: .string) })
    }
}
