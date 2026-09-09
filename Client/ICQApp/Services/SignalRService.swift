import Foundation
// SignalR client integration placeholder — use SignalR-Client-Swift or similar in real project

@MainActor
class SignalRService: ObservableObject {
    static let shared = SignalRService()
    @Published var isConnected = false

    func connect(token: String) async {
        // TODO: HubConnectionBuilder with access token
        isConnected = true
    }

    func disconnect() async {
        isConnected = false
    }

    func sendMessage(chatId: Int, content: String) async {
        // invoke "SendMessage"
    }

    func onReceiveMessage(_ handler: @escaping (MessageDto) -> Void) {}
    func onUserTyping(_ handler: @escaping (Int, Int, Bool) -> Void) {}
    func onUserStatus(_ handler: @escaping (Int, String) -> Void) {}
}
