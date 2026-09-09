package com.icq.messenger.data

import com.squareup.moshi.Json
import com.squareup.moshi.JsonClass

enum class UserStatus(val value: Int) {
    Offline(0), Online(1), Away(2), Busy(3), Invisible(4);

    companion object {
        fun from(v: Int) = entries.firstOrNull { it.value == v } ?: Offline
    }
}

enum class MessageType(val value: Int) {
    Text(0), Image(1), File(2), System(3), Sticker(4);

    companion object {
        fun from(v: Int) = entries.firstOrNull { it.value == v } ?: Text
    }
}

@JsonClass(generateAdapter = true)
data class UserDto(
    val id: String,
    val uin: Long,
    val nickname: String,
    val email: String? = null,
    val firstName: String? = null,
    val lastName: String? = null,
    val statusMessage: String? = null,
    val status: Int = 0,
    val lastSeenAt: String? = null,
    val avatarUrl: String? = null
) {
    val userStatus: UserStatus get() = UserStatus.from(status)
}

@JsonClass(generateAdapter = true)
data class AuthResponse(
    val accessToken: String,
    val refreshToken: String,
    val expiresAt: String,
    val user: UserDto
)

@JsonClass(generateAdapter = true)
data class MessageDto(
    val id: String,
    val chatId: String,
    val senderId: String,
    val senderNickname: String,
    val type: Int = 0,
    val text: String? = null,
    val attachmentUrl: String? = null,
    val attachmentName: String? = null,
    val attachmentSize: Long? = null,
    val sentAt: String,
    val editedAt: String? = null,
    val isDeleted: Boolean = false,
    val clientMessageId: String? = null
)

@JsonClass(generateAdapter = true)
data class ChatDto(
    val id: String,
    val type: Int = 0,
    val title: String? = null,
    val createdAt: String,
    val lastMessageAt: String? = null,
    val participants: List<UserDto> = emptyList(),
    val lastMessage: MessageDto? = null,
    val unreadCount: Int = 0
)

@JsonClass(generateAdapter = true)
data class ContactDto(
    val id: String,
    val user: UserDto,
    val nicknameOverride: String? = null,
    val status: Int = 0,
    val addedAt: String
)

@JsonClass(generateAdapter = true)
data class LoginRequest(
    val nicknameOrEmail: String,
    val password: String
)

@JsonClass(generateAdapter = true)
data class RegisterRequest(
    val nickname: String,
    val password: String,
    val email: String? = null
)

@JsonClass(generateAdapter = true)
data class SendMessageRequest(
    val chatId: String,
    val text: String,
    val type: Int = 0,
    val attachmentUrl: String? = null,
    val attachmentName: String? = null,
    val attachmentSize: Long? = null,
    val clientMessageId: String? = null
)

@JsonClass(generateAdapter = true)
data class CreatePrivateChatRequest(val targetUserId: String)

@JsonClass(generateAdapter = true)
data class SearchUsersResponse(val users: List<UserDto>)
