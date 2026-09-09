import Foundation

enum SmileyPack {
    static let map: [String: String] = [
        ":)": "😊", ":-)": "😊",
        ":D": "😃", ":-D": "😃",
        ":(": "😞", ":-(": "😞",
        ";)": "😉", ";-)": "😉",
        ":P": "😛", ":-P": "😛",
        "*BANG*": "🤦‍♂️", "*bang*": "🤦‍♂️",
        "<3": "❤️"
    ]

    static func expand(_ text: String) -> String {
        var result = text
        for (code, emoji) in map.sorted(by: { $0.key.count > $1.key.count }) {
            result = result.replacingOccurrences(of: code, with: emoji, options: .caseInsensitive)
        }
        return result
    }
}
