import Foundation

@MainActor
class ChatViewModel: ObservableObject {
    @Published var messages: [MessageDto] = []
    @Published var inputText = ""
    @Published var isTyping = false

    let chatId: Int
    private let api = APIClient.shared

    init(chatId: Int) {
        self.chatId = chatId
    }

    func load() async {
        do {
            messages = try await api.getMessages(chatId: chatId)
        } catch {}
    }

    func send() async {
        let text = inputText.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !text.isEmpty else { return }
        inputText = ""
        do {
            let msg = try await api.sendMessage(chatId: chatId, content: text)
            messages.append(msg)
        } catch {}
    }
}
