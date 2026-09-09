import Foundation
import Combine

@MainActor
class AppViewModel: ObservableObject {
    @Published var isLoggedIn = false
    @Published var currentUser: UserDto?
    @Published var chats: [ChatDto] = []
    @Published var contacts: [UserDto] = []
    @Published var error: String?

    private let api = APIClient.shared

    init() {
        Task { await restoreSession() }
    }

    func restoreSession() async {
        await api.loadTokens()
        // try me() if token present
    }

    func login(emailOrUin: String, password: String) async {
        do {
            let resp = try await api.login(emailOrUin: emailOrUin, password: password)
            await api.setTokens(access: resp.accessToken, refresh: resp.refreshToken)
            currentUser = resp.user
            isLoggedIn = true
            await loadData()
        } catch {
            self.error = error.localizedDescription
        }
    }

    func register(email: String, password: String, nickname: String) async {
        do {
            let resp = try await api.register(email: email, password: password, nickname: nickname)
            await api.setTokens(access: resp.accessToken, refresh: resp.refreshToken)
            currentUser = resp.user
            isLoggedIn = true
            await loadData()
        } catch {
            self.error = error.localizedDescription
        }
    }

    func loadData() async {
        do {
            async let c = api.getChats()
            async let contacts = api.getContacts()
            self.chats = try await c
            self.contacts = try await contacts
        } catch {
            self.error = error.localizedDescription
        }
    }

    func logout() {
        Task { await api.setTokens(access: nil, refresh: nil) }
        isLoggedIn = false
        currentUser = nil
        chats = []
        contacts = []
    }
}
