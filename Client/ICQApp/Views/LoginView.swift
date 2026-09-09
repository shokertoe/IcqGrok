import SwiftUI

struct LoginView: View {
    @EnvironmentObject var appVM: AppViewModel
    @State private var emailOrUin = ""
    @State private var password = ""
    @State private var nickname = ""
    @State private var isRegister = false

    var body: some View {
        VStack(spacing: 20) {
            Text("💬").font(.system(size: 56))
            Text("ICQ").font(.largeTitle.bold()).foregroundStyle(.blue)
            if isRegister {
                TextField("Nickname", text: $nickname)
                    .textFieldStyle(.roundedBorder)
            }
            TextField(isRegister ? "Email" : "Email or UIN", text: $emailOrUin)
                .textFieldStyle(.roundedBorder)
                .textInputAutocapitalization(.never)
            SecureField("Password", text: $password)
                .textFieldStyle(.roundedBorder)
            if let err = appVM.error {
                Text(err).foregroundStyle(.red).font(.caption)
            }
            Button(isRegister ? "Register" : "Login") {
                Task {
                    if isRegister {
                        await appVM.register(email: emailOrUin, password: password, nickname: nickname)
                    } else {
                        await appVM.login(emailOrUin: emailOrUin, password: password)
                    }
                }
            }
            .buttonStyle(.borderedProminent)
            Button(isRegister ? "Already have account" : "Create account") {
                isRegister.toggle()
            }
            .font(.caption)
        }
        .padding(32)
        .frame(maxWidth: 400)
    }
}
