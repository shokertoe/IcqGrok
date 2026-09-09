import SwiftUI

struct ChatsListView: View {
    @EnvironmentObject var appVM: AppViewModel

    var body: some View {
        NavigationStack {
            List(appVM.chats) { chat in
                NavigationLink(value: chat.id) {
                    VStack(alignment: .leading) {
                        Text(chat.title ?? "Chat #\(chat.id)").font(.headline)
                        if let preview = chat.lastMessagePreview {
                            Text(preview).font(.caption).foregroundStyle(.secondary).lineLimit(1)
                        }
                    }
                }
            }
            .navigationTitle("Chats")
            .navigationDestination(for: Int.self) { chatId in
                ChatView(chatId: chatId)
            }
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Logout") { appVM.logout() }
                }
            }
            .refreshable { await appVM.loadData() }
        }
    }
}
