package com.icq.messenger.network

import android.util.Log
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.TransportEnum
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class SignalRClient(
    private val hubUrl: String = ApiClient.HUB_URL
) {
    private var connection: HubConnection? = null
    var onMessage: ((Map<String, Any>) -> Unit)? = null
    var onStatusChanged: ((String, Int) -> Unit)? = null

    fun connect(accessToken: String) {
        disconnect()
        connection = HubConnectionBuilder
            .create(hubUrl)
            .withAccessTokenProvider(Single.defer { Single.just(accessToken) })
            .withTransport(TransportEnum.WEBSOCKETS)
            .build()

        connection?.on("ReceiveMessage", { args ->
            @Suppress("UNCHECKED_CAST")
            val map = args as? Map<String, Any>
            if (map != null) onMessage?.invoke(map)
        }, Map::class.java)

        connection?.on("UserStatusChanged", { userId, status ->
            onStatusChanged?.invoke(userId.toString(), (status as? Number)?.toInt() ?: 0)
        }, Any::class.java, Any::class.java)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                connection?.start()?.blockingAwait()
                Log.d("SignalR", "Connected")
            } catch (e: Exception) {
                Log.e("SignalR", "Connect failed", e)
            }
        }
    }

    fun joinChat(chatId: String) {
        connection?.invoke("JoinChat", chatId)
    }

    fun leaveChat(chatId: String) {
        connection?.invoke("LeaveChat", chatId)
    }

    fun sendMessage(request: Map<String, Any?>) {
        connection?.invoke("SendMessage", request)
    }

    fun setTyping(chatId: String, isTyping: Boolean) {
        connection?.invoke("Typing", chatId, isTyping)
    }

    fun disconnect() {
        try {
            connection?.stop()
        } catch (_: Exception) {}
        connection = null
    }
}
