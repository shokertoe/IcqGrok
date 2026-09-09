import SwiftUI

struct MainView: View {
    @EnvironmentObject var appVM: AppViewModel
    @State private var selectedTab = 0

    var body: some View {
        TabView(selection: $selectedTab) {
            ChatsListView()
                .tabItem { Label("Chats", systemImage: "message.fill") }
                .tag(0)
            ContactsView()
                .tabItem { Label("Contacts", systemImage: "person.2.fill") }
                .tag(1)
        }
    }
}
