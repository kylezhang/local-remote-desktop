import Foundation

enum UiText {
    static var language = preferredLanguage
    static var preferredLanguage: String {
        if let stored = UserDefaults.standard.string(forKey: "language"), ["en", "zh-Hans"].contains(stored) { return stored }
        return Locale.preferredLanguages.first?.hasPrefix("zh") == true ? "zh-Hans" : "en"
    }
    private struct Template { let pattern: NSRegularExpression, english: String, indexes: [Int], prefix: String }
    private static var catalog: [String: String] = {
        guard let url = Bundle.main.url(forResource: "strings", withExtension: "json"),
              let data = try? Data(contentsOf: url),
              let parsed = try? JSONDecoder().decode([String: String].self, from: data) else { return [:] }
        return parsed
    }()
    private static var templates: [Template] = makeTemplates(catalog)
    private static let token = try! NSRegularExpression(pattern: #"\{(\d+)\}"#)
    static func load(_ data: Data) throws {
        let parsed = try JSONDecoder().decode([String: String].self, from: data)
        catalog = parsed; templates = makeTemplates(parsed)
    }
    private static func makeTemplates(_ dictionary: [String: String]) -> [Template] {
        dictionary.filter { $0.key.contains("{0}") }.sorted { $0.key.count > $1.key.count }.compactMap { key, english in
            let raw = key as NSString, matches = token.matches(in: key, range: NSRange(location: 0, length: raw.length))
            var pattern = "\\A", end = 0, indexes: [Int] = []
            for match in matches {
                pattern += NSRegularExpression.escapedPattern(for: raw.substring(with: NSRange(location: end, length: match.range.location - end))) + "(.*?)"
                indexes.append(Int(raw.substring(with: match.range(at: 1)))!); end = match.range.location + match.range.length
            }
            pattern += NSRegularExpression.escapedPattern(for: raw.substring(from: end)) + "\\z"
            guard let regex = try? NSRegularExpression(pattern: pattern, options: [.dotMatchesLineSeparators]) else { return nil }
            let prefix = raw.substring(to: matches.first?.range.location ?? raw.length)
            return Template(pattern: regex, english: english, indexes: indexes, prefix: prefix)
        }
    }
    static func text(_ raw: String) -> String {
        if language == "zh-Hans" || raw.utf8.count > 16384 { return raw }
        if let exact = catalog[raw] { return exact }
        let value = raw as NSString
        for entry in templates {
            if !raw.hasPrefix(entry.prefix) { continue }
            guard let match = entry.pattern.firstMatch(in: raw, range: NSRange(location: 0, length: value.length)) else { continue }
            var captured: [Int: String] = [:]
            for (group, index) in entry.indexes.enumerated() { captured[index] = value.substring(with: match.range(at: group + 1)) }
            let english = entry.english as NSString
            var result = "", end = 0
            for placeholder in token.matches(in: entry.english, range: NSRange(location: 0, length: english.length)) {
                result += english.substring(with: NSRange(location: end, length: placeholder.range.location - end))
                result += captured[Int(english.substring(with: placeholder.range(at: 1)))!] ?? ""
                end = placeholder.range.location + placeholder.range.length
            }
            return result + english.substring(from: end)
        }
        return raw
    }
}
