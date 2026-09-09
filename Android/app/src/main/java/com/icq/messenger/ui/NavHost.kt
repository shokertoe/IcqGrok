package com.icq.messenger.ui

import androidx.compose.runtime.Composable
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.icq.messenger.data.AppViewModel
import com.icq.messenger.ui.screens.ChatScreen
import com.icq.messenger.ui.screens.HomeScreen
import com.icq.messenger.ui.screens.LoginScreen

@Composable
fun ICQNavHost(viewModel: AppViewModel) {
    val nav = rememberNavController()
    val start = if (viewModel.isAuthenticated) "home" else "login"

    NavHost(navController = nav, startDestination = start) {
        composable("login") {
            LoginScreen(
                viewModel = viewModel,
                onSuccess = {
                    nav.navigate("home") {
                        popUpTo("login") { inclusive = true }
                    }
                }
            )
        }
        composable("home") {
            HomeScreen(
                viewModel = viewModel,
                onOpenChat = { chatId -> nav.navigate("chat/$chatId") },
                onLogout = {
                    viewModel.logout()
                    nav.navigate("login") {
                        popUpTo("home") { inclusive = true }
                    }
                }
            )
        }
        composable("chat/{chatId}") { entry ->
            val chatId = entry.arguments?.getString("chatId") ?: return@composable
            ChatScreen(
                chatId = chatId,
                viewModel = viewModel,
                onBack = { nav.popBackStack() }
            )
        }
    }
}
