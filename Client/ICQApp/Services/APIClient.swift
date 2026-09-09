import Foundation

actor APIClient {
    static let shared = APIClient()
    private let baseURL = URL(string: "http://localhost:5000")!
    private var accessToken: String?
    private var refreshToken: String?

    func setTokens(access: String?, refresh: String?) {
        accessToken = access
        refreshToken = refresh
        UserDefaults.standard.set(access, forKey: "icq_token")
        UserDefaults.standard.set(refresh, forKey: "icq_refresh")
    }

    func loadTokens() {
        accessToken = UserDefaults.standard.string(forKey: "icq_token")
        refreshToken = UserDefaults.standard.string(forKey: "icq_refresh")
    }

    func register(email: String, password: String, nickname: String) async throws -> AuthResponse {
        try await post("/api/auth/register", body: ["email": email, "password": password, "nickname": nickname])
    }

    func login(emailOrUin: String, password: String) async throws -> AuthResponse {
        try await post("/api/auth/login", body: ["emailOrUin": emailOrUin, "password": password])
    }

    func getChats() async throws -> [ChatDto] {
        try await get("/api/chats")
    }

    func getMessages(chatId: Int) async throws -> [MessageDto] {
        try await get("/api/chats/\(chatId)/messages")
    }

    func sendMessage(chatId: Int, content: String) async throws -> MessageDto {
        try await post("/api/chats/messages", body: ["chatId": chatId, "content": content])
    }

    func getContacts() async throws -> [UserDto] {
        try await get("/api/users/contacts")
    }

    private func get<T: Decodable>(_ path: String) async throws -> T {
        var req = URLRequest(url: baseURL.appendingPathComponent(path))
        req.httpMethod = "GET"
        if let t = accessToken { req.setValue("Bearer \(t)", forHTTPHeaderField: "Authorization") }
        let (data, resp) = try await URLSession.shared.data(for: req)
        guard let http = resp as? HTTPURLResponse, (200..<300).contains(http.statusCode) else {
            throw URLError(.badServerResponse)
        }
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return try decoder.decode(T.self, from: data)
    }

    private func post<T: Decodable>(_ path: String, body: [String: Any]) async throws -> T {
        var req = URLRequest(url: baseURL.appendingPathComponent(path))
        req.httpMethod = "POST"
        req.setValue("application/json", forHTTPHeaderField: "Content-Type")
        if let t = accessToken { req.setValue("Bearer \(t)", forHTTPHeaderField: "Authorization") }
        req.httpBody = try JSONSerialization.data(withJSONObject: body)
        let (data, resp) = try await URLSession.shared.data(for: req)
        guard let http = resp as? HTTPURLResponse, (200..<300).contains(http.statusCode) else {
            throw URLError(.badServerResponse)
        }
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return try decoder.decode(T.self, from: data)
    }
}
