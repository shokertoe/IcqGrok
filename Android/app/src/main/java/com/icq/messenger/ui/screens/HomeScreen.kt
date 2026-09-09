package com.icq.messenger.ui.screens

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.icq.messenger.data.AppViewModel
import com.icq.messenger.data.ChatDto

@Composable
fun HomeScreen(vm: AppViewModel, onOpenChat: (Int) -> Unit) {
    var tab by remember { mutableStateOf(0) }
    Column(Modifier.fillMaxSize()) {
        TabRow(selectedTabIndex = tab) {
            Tab(selected = tab == 0, onClick = { tab = 0 }, text = { Text("Chats") })
            Tab(selected = tab == 1, onClick = { tab = 1 }, text = { Text("Contacts") })
        }
        when (tab) {
            0 -> LazyColumn {
                items(vm.chats) { chat ->
                    ListItem(
                        headlineContent = { Text(chat.title ?: "Chat #${chat.id}") },
                        supportingContent = { Text(chat.lastMessagePreview ?: "") },
                        modifier = Modifier.clickable { onOpenChat(chat.id) }
                    )
                    HorizontalDivider()
                }
            }
            1 -> LazyColumn {
                items(vm.contacts) { u ->
                    ListItem(
                        headlineContent = { Text(u.nickname) },
                        supportingContent = { Text("UIN ${u.uin} · ${u.status}") }
                    )
                    HorizontalDivider()
                }
            }
        }
    }
}
