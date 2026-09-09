import SwiftUI

struct ContactsView: View {
    @EnvironmentObject var appVM: AppViewModel
    @State private var search = ""

    var body: some View {
        NavigationStack {
            List(appVM.contacts) { user in
                HStack {
                    Circle()
                        .fill(user.isOnline ? Color.green : Color.gray)
                        .frame(width: 10, height: 10)
                    VStack(alignment: .leading) {
                        Text(user.nickname).font(.headline)
                        Text("UIN \(user.uin) · \(user.status)").font(.caption).foregroundStyle(.secondary)
                    }
                }
            }
            .navigationTitle("Contacts")
            .searchable(text: $search)
            .refreshable { await appVM.loadData() }
        }
    }
}
