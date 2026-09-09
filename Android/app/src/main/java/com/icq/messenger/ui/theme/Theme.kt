package com.icq.messenger.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val DarkColors = darkColorScheme(
    primary = Color(0xFF1A5FB4),
    secondary = Color(0xFF62A0EA),
    background = Color(0xFF0F172A),
    surface = Color(0xFF1E293B)
)

@Composable
fun ICQTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = DarkColors,
        content = content
    )
}
