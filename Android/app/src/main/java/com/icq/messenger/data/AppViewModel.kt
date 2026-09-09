package com.icq.messenger.data

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.icq.messenger.network.ApiClient
import com.icq.messenger.network.SignalRClient
import kotlinx.coroutines.launch

class AppViewModel : ViewModel() {
    var isAuthenticated by mutableStateOf(false)
        private set
    var currentUser by mutableStateOf<UserDto?>(null)
        private set
    var chats by mutableStateOf<List<ChatDto>>(emptyList())
        private set
    var contacts by mutableStateOf<List<ContactDto>>(emptyList())
        private set
    var errorMessage by mutableStateOf<String?>(null)
    var isLoading by mutableStateOf(false)

    private val signalR = SignalRClient()

    fun login(nickname: String, password: String) {
        viewModelScope.launch {
            isLoading = true
            errorMessage = null
            try {
                val res = ApiClient.api.login(LoginRequest(nickname, password))
                handleAuth(res)
            } catch (e: Exception) {
                errorMessage = e.message ?: "Login failed"
            } finally {
                isLoading = false
            }
        }
    }

    fun register(nickname: String, password: String, email: String? = null) {
        viewModelScope.launch {
            isLoading = true
            errorMessage = null
            try {
                val res = ApiClient.api.register(RegisterRequest(nickname, password, email))
                handleAuth(res)
            } catch (e: Exception) {
                errorMessage = e.message ?: "Register failed"
            } finally {
                isLoading = false
            }
        }
    }

    fun logout() {
        signalR.disconnect()
        ApiClient.accessToken = null
        isAuthenticated = false
        currentUser = null
        chats = emptyList()
        contacts = emptyList()
    }

    private fun handleAuth(res: AuthResponse) {
        ApiClient.accessToken = res.accessToken
        currentUser = res.user
        isAuthenticated = true
        signalR.connect(res.accessToken)
        refresh()
    }

    fun refresh() {
        viewModelScope.launch {
            try {
                chats = ApiClient.api.getChats()
                contacts = ApiClient.api.getContacts()
            } catch (e: Exception) {
                errorMessage = e.message
            }
        }
    }

    fun startChat(userId: String, onCreated: (ChatDto) -> Unit) {
        viewModelScope.launch {
            try {
                val chat = ApiClient.api.createPrivateChat(CreatePrivateChatRequest(userId))
                if (chats.none { it.id == chat.id }) {
                    chats = listOf(chat) + chats
                }
                onCreated(chat)
            } catch (e: Exception) {
                errorMessage = e.message
            }
        }
    }

    fun signalRClient(): SignalRClient = signalR
}
