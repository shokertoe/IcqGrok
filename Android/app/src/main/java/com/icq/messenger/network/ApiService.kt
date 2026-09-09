package com.icq.messenger.network

import com.icq.messenger.data.*
import okhttp3.MultipartBody
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.moshi.MoshiConverterFactory
import retrofit2.http.*
import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import java.util.concurrent.TimeUnit

interface ApiService {
    @POST("api/auth/register")
    suspend fun register(@Body body: RegisterRequest): AuthResponse

    @POST("api/auth/login")
    suspend fun login(@Body body: LoginRequest): AuthResponse

    @GET("api/chats")
    suspend fun getChats(): List<ChatDto>

    @POST("api/chats/private")
    suspend fun createPrivateChat(@Body body: CreatePrivateChatRequest): ChatDto

    @GET("api/chats/{chatId}/messages")
    suspend fun getMessages(
        @Path("chatId") chatId: String,
        @Query("limit") limit: Int = 50
    ): List<MessageDto>

    @POST("api/chats/messages")
    suspend fun sendMessage(@Body body: SendMessageRequest): MessageDto

    @GET("api/users/contacts")
    suspend fun getContacts(): List<ContactDto>

    @GET("api/users/search")
    suspend fun searchUsers(@Query("q") query: String): SearchUsersResponse

    @POST("api/users/contacts")
    suspend fun addContact(@Body body: Map<String, Long>): ContactDto

    @Multipart
    @POST("api/files/upload")
    suspend fun uploadFile(@Part file: MultipartBody.Part): Map<String, Any>
}

object ApiClient {
    private const val BASE_URL = "http://10.0.2.2:5000/"

    @Volatile
    var accessToken: String? = null

    private val authInterceptor = okhttp3.Interceptor { chain ->
        val req = chain.request().newBuilder()
        accessToken?.let { req.addHeader("Authorization", "Bearer $it") }
        chain.proceed(req.build())
    }

    private val client = OkHttpClient.Builder()
        .addInterceptor(authInterceptor)
        .addInterceptor(HttpLoggingInterceptor().apply { level = HttpLoggingInterceptor.Level.BASIC })
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .build()

    private val moshi = Moshi.Builder()
        .add(KotlinJsonAdapterFactory())
        .build()

    val api: ApiService = Retrofit.Builder()
        .baseUrl(BASE_URL)
        .client(client)
        .addConverterFactory(MoshiConverterFactory.create(moshi))
        .build()
        .create(ApiService::class.java)

    const val HUB_URL = "http://10.0.2.2:5000/hubs/chat"
}
