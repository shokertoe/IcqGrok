import SwiftUI

@main
struct ICQApp: App {
    @StateObject private var appVM = AppViewModel()

    var body: some Scene {
        WindowGroup {
            if appVM.isLoggedIn {
                MainView()
                    .environmentObject(appVM)
            } else {
                LoginView()
                    .environmentObject(appVM)
            }
        }
    }
}
