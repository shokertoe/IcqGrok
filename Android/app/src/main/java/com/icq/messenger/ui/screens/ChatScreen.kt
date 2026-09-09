package com.icq.messenger.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.icq.messenger.data.AppViewModel

@Composable
fun ChatScreen(vm: AppViewModel, chatId: Int, onBack: () -> Unit) {
    var text by remember { mutableStateOf("") }
    val messages = vm.messages[chatId] ?: emptyList()

    Column(Modifier.fillMaxSize()) {
        TopAppBar(
            title = { Text("Chat #$chatId") },
            navigationIcon = { TextButton(onClick = onBack) { Text("Back") } }
        )
        LazyColumn(Modifier.weight(1f).padding(8.dp), reverseLayout = true) {
            items(messages.reversed()) { msg ->
                val isMe = msg.senderId == vm.currentUser?.id
                Row(Modifier.fillMaxWidth(), horizontalArrangement = if (isMe) Arrangement.End else Arrangement.Start) {
                    Surface(
                        color = if (isMe) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.surfaceVariant,
                        shape = MaterialTheme.shapes.medium,
                        modifier = Modifier.padding(4.dp)
                    ) {
                        Text(msg.content, Modifier.padding(10.dp))
                    }
                }
            }
        }
        Row(Modifier.padding(8.dp), verticalAlignment = Alignment.CenterVertically) {
            OutlinedTextField(text, { text = it }, modifier = Modifier.weight(1f), placeholder = { Text("Message") })
            Spacer(Modifier.width(8.dp))
            Button(onClick = {
                if (text.isNotBlank()) {
                    // vm.sendMessage(chatId, text)
                    text = ""
                }
            }) { Text("Send") }
        }
    }
}
