import Foundation

struct UserDto: Codable, Identifiable {
    let id: Int
    let uin: Int
    let email: String
    let nickname: String
    let status: String
    let statusMessage: String?
    let isOnline: Bool
    let lastSeenAt: Date?
}

struct AuthResponse: Codable {
    let accessToken: String
    let refreshToken: String
    let expiresIn: Int
    let user: UserDto
}

struct ChatDto: Codable, Identifiable {
    let id: Int
    let title: String?
    let type: String
    let lastMessageAt: Date?
    let lastMessagePreview: String?
    let participants: [UserDto]
    let unreadCount: Int
}

struct MessageDto: Codable, Identifiable {
    let id: Int
    let chatId: Int
    let senderId: Int
    let senderNickname: String
    let content: String
    let isEncrypted: Bool
    let encryptedPayload: String?
    let createdAt: Date
    let isRead: Bool
    let fileAttachmentId: Int?
    let fileName: String?
    let fileUrl: String?
}

struct KeyBundleDto: Codable {
    let userId: Int
    let identityKeyPublic: String
    let signedPreKeyPublic: String
    let signedPreKeySignature: String
    let signedPreKeyId: Int
    let oneTimePreKey: String?
}
