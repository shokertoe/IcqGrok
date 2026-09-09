package com.icq.messenger.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import com.icq.messenger.data.AppViewModel

@Composable
fun LoginScreen(vm: AppViewModel, onLoggedIn: () -> Unit) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var nickname by remember { mutableStateOf("") }
    var isRegister by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    var loading by remember { mutableStateOf(false) }

    Column(
        Modifier.fillMaxSize().padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text("💬 ICQ", style = MaterialTheme.typography.headlineLarge)
        Spacer(Modifier.height(24.dp))
        if (isRegister) {
            OutlinedTextField(nickname, { nickname = it }, label = { Text("Nickname") }, modifier = Modifier.fillMaxWidth())
            Spacer(Modifier.height(8.dp))
        }
        OutlinedTextField(email, { email = it }, label = { Text(if (isRegister) "Email" else "Email or UIN") }, modifier = Modifier.fillMaxWidth())
        Spacer(Modifier.height(8.dp))
        OutlinedTextField(password, { password = it }, label = { Text("Password") }, visualTransformation = PasswordVisualTransformation(), modifier = Modifier.fillMaxWidth())
        error?.let { Text(it, color = MaterialTheme.colorScheme.error); Spacer(Modifier.height(8.dp)) }
        Spacer(Modifier.height(16.dp))
        Button(
            onClick = {
                loading = true
                error = null
                // call vm.login or register then onLoggedIn
                loading = false
                onLoggedIn()
            },
            enabled = !loading,
            modifier = Modifier.fillMaxWidth()
        ) { Text(if (isRegister) "Register" else "Login") }
        TextButton(onClick = { isRegister = !isRegister }) {
            Text(if (isRegister) "Already have account? Login" else "Create account")
        }
    }
}
