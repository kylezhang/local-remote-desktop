import Foundation
@main struct Check {
    static func fill(_ template: String) -> String {
        let regex = try! NSRegularExpression(pattern: #"\{(\d+)\}"#), raw = template as NSString
        var result = "", end = 0
        for match in regex.matches(in: template, range: NSRange(location: 0, length: raw.length)) {
            result += raw.substring(with: NSRange(location: end, length: match.range.location - end))
            result += "opaque中文{6}-" + raw.substring(with: match.range(at: 1))
            end = match.range.location + match.range.length
        }
        return result + raw.substring(from: end)
    }
    static func main() throws {
        let data = try Data(contentsOf: URL(fileURLWithPath: CommandLine.arguments[1]))
        let catalog = try JSONDecoder().decode([String: String].self, from: data)
        try UiText.load(data); UiText.language = "en"
        for (source, english) in catalog {
            precondition(UiText.text(fill(source)) == fill(english), "Translation mismatch: \(source)")
        }
        precondition(UiText.text("untrusted 文件.txt {0}") == "untrusted 文件.txt {0}")
        UiText.language = "zh-Hans"; precondition(UiText.text("目标IP") == "目标IP")
        print("PASS \(catalog.count) shared strings and opaque templates with production Swift Foundation translation code")
    }
}
