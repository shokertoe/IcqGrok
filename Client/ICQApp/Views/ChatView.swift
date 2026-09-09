import SwiftUI

struct ChatView: View {
    let chatId: Int
    @StateObject private var vm: ChatViewModel

    init(chatId: Int) {
        self.chatId = chatId
        _vm = StateObject(wrappedValue: ChatViewModel(chatId: chatId))
    }

    var body: some View {
        VStack(spacing: 0) {
            ScrollViewReader { proxy in
                ScrollView {
                    LazyVStack(alignment: .leading, spacing: 8) {
                        ForEach(vm.messages) { msg in
                            HStack {
                                if msg.senderId == /* current */ 0 { Spacer() }
                                Text(SmileyPack.expand(msg.content))
                                    .padding(10)
                                    .background(msg.senderId == 0 ? Color.blue : Color.gray.opacity(0.3))
                                    .foregroundStyle(msg.senderId == 0 ? .white : .primary)
                                    .clipShape(RoundedRectangle(cornerRadius: 14))
                                if msg.senderId != 0 { Spacer() }
                            }
                            .id(msg.id)
                        }
                    }
                    .padding()
                }
                .onChange(of: vm.messages.count) { _ in
                    if let last = vm.messages.last { proxy.scrollTo(last.id, anchor: .bottom) }
                }
            }
            HStack {
                TextField("Message", text: $vm.inputText, axis: .vertical)
                    .textFieldStyle(.roundedBorder)
                    .lineLimit(1...4)
                Button {
                    Task { await vm.send() }
                } label: {
                    Image(systemName: "paperplane.fill")
                }
                .disabled(vm.inputText.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)
            }
            .padding()
        }
        .navigationTitle("Chat")
        .task { await vm.load() }
    }
}
