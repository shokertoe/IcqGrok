import SwiftUI

@main
struct ICQApp: App {
    @StateObject private var appViewModel = AppViewModel()

    var body: some Scene {
        WindowGroup {
            Group {
                if appViewModel.isAuthenticated {
                    MainView()
                } else {
                    LoginView()
                }
            }
            .environmentObject(appViewModel)
            #if os(macOS)
            .frame(minWidth: 400, minHeight: 500)
            #endif
        }
        #if os(macOS)
        .defaultSize(width: 1000, height: 650)
        #endif
    }
}
